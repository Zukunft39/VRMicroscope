
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;


public class InteractWithSamples:MonoBehaviour
{
    InputAction PickOrPutSampleAction;
    private InteractableSamples interactableObject;
    [SerializeField, HideInInspector] private bool isSampleOnHand = false;
    [SerializeField, HideInInspector] private Texture currentSampleTexture;
    [SerializeField, HideInInspector] private string currentSampleName;
    public RawImage Inventory;
    private Texture defaultInventoryTexture;

    private void Awake()
    {
        if (Inventory != null)
        {
            defaultInventoryTexture = Inventory.texture;
        }
    }

    public Texture CurrentSampleTexture => currentSampleTexture;
    public bool AssistantCanPick => (interactableObject != null && interactableObject.Sample != null) || (ResolveTargetSample() != null && ResolveTargetSample().Sample != null);
    public bool AssistantHasHandSample
    {
        get
        {
            Transform anchor = GetSampleParentAnchor();
            if (anchor == null || anchor.childCount == 0) return false;
            foreach (Transform child in anchor) if (child.CompareTag("ObserveObjects")) return true;
            return false;
        }
    }
    public event Action SampleChanged;
    public string CurrentSampleName => !string.IsNullOrWhiteSpace(currentSampleName)
        ? currentSampleName
        : CurrentSampleTexture != null ? CurrentSampleTexture.name : "Teaching grating";

    public Transform GetSampleParentAnchor()
    {
        if (transform.childCount > 0)
        {
            Transform first = transform.GetChild(0);
            if (first.CompareTag("MainCamera") || first.GetComponent<Camera>() != null)
            {
                return first;
            }
            for (int i = 0; i < transform.childCount; i++)
            {
                if (transform.GetChild(i).CompareTag("MainCamera"))
                {
                    return transform.GetChild(i);
                }
            }
            return first;
        }

        if (Camera.main != null)
        {
            return Camera.main.transform;
        }

        return transform;
    }

    public InteractableSamples ResolveTargetSample()
    {
        if (interactableObject != null)
        {
            return interactableObject;
        }

        // 1. 尝试通过桌面鼠标/准星射线探测样本
        Ray ray;
        float maxDistance = 5.0f;
        bool hasRay = false;

        if (CameraTryMove.TryGetDesktopPointerRay(out ray, out float desktopMaxDist))
        {
            hasRay = true;
            maxDistance = Mathf.Min(maxDistance, desktopMaxDist);
        }
        else if (Camera.main != null)
        {
            Vector3 pointerPos = Cursor.lockState == CursorLockMode.Locked
                ? new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f)
                : Input.mousePosition;
            ray = Camera.main.ScreenPointToRay(pointerPos);
            hasRay = true;
        }

        if (hasRay)
        {
            RaycastHit[] hits = Physics.RaycastAll(ray, maxDistance, ~0, QueryTriggerInteraction.Collide);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
            {
                var sample = hit.collider.GetComponentInParent<InteractableSamples>();
                if (sample != null && sample.gameObject.activeInHierarchy)
                {
                    return sample;
                }
            }
        }

        // 2. 备选方案：如果未精准对中碰撞体，检测玩家视线前方和近距离（<= 3.5m）范围内的最近样本
        Camera cam = Camera.main;
        Vector3 searchOrigin = cam != null ? cam.transform.position : transform.position;
        Vector3 searchForward = cam != null ? cam.transform.forward : transform.forward;

        InteractableSamples[] allSamples = FindObjectsOfType<InteractableSamples>();
        InteractableSamples bestSample = null;
        float bestScore = float.MaxValue;

        foreach (var sample in allSamples)
        {
            if (sample == null || !sample.gameObject.activeInHierarchy) continue;
            Vector3 diff = sample.transform.position - searchOrigin;
            float dist = diff.magnitude;
            if (dist > 3.5f) continue;

            float dot = Vector3.Dot(searchForward, diff.normalized);
            if (dot < 0.2f) continue; // 必须在视野前方

            float score = dist * (2.0f - dot);
            if (score < bestScore)
            {
                bestScore = score;
                bestSample = sample;
            }
        }

        return bestSample;
    }

    private void Update()
    {
        bool actualOnHand = AssistantHasHandSample;
        if (isSampleOnHand != actualOnHand)
        {
            isSampleOnHand = actualOnHand;
            if (!actualOnHand)
            {
                currentSampleTexture = null;
                currentSampleName = null;
                if (Inventory != null)
                {
                    Inventory.texture = defaultInventoryTexture;
                }
            }
            else if (currentSampleTexture == null)
            {
                Transform anchor = GetSampleParentAnchor();
                if (anchor != null)
                {
                    foreach (Transform child in anchor)
                    {
                        if (child.CompareTag("ObserveObjects"))
                        {
                            var r = child.GetComponentInChildren<Renderer>();
                            if (r != null && r.material != null)
                            {
                                currentSampleTexture = r.material.GetTexture("_MainTexture");
                                if (Inventory != null) Inventory.texture = currentSampleTexture;
                            }
                            break;
                        }
                    }
                }
            }
            SyncSampleStateToForceTutorialPrerequisites();
            SampleChanged?.Invoke();
        }
    }

    public void PickSample()
    {
        Debug.Log("[InteractWithSamples] PickSample requested");

        InteractableSamples target = ResolveTargetSample();
        if (ReferenceEquals(target, null))
        {
            Debug.Log("[InteractWithSamples] PickSample aborted: no InteractableSamples in range or targeted.");
            return;
        }

        interactableObject = target;

        Transform anchor = GetSampleParentAnchor();
        if (anchor == null)
        {
            Debug.LogError("[InteractWithSamples] PickSample failed: sample parent anchor is null.");
            return;
        }

        if (isSampleOnHand)
        {
            for (int i = 0; i < anchor.childCount; i++)
            {
                if (anchor.GetChild(i).CompareTag("ObserveObjects"))
                {
                    Destroy(anchor.GetChild(i).gameObject);
                    break;
                }
            }
        }

        GameObject temp = Instantiate(interactableObject.Sample, Vector3.zero, Quaternion.identity, anchor);
        temp.transform.localScale = Vector3.one;
        temp.transform.localPosition = new Vector3(23, -5, -4);
        temp.transform.localRotation = Quaternion.Euler(22, -180, 0);

        // 拿在手上时，将刚体设置为运动学(Kinematic)，防止物理引擎报错，并避免它受到重力掉落
        Rigidbody[] rbs = temp.GetComponentsInChildren<Rigidbody>();
        foreach (var rb in rbs)
        {
            rb.isKinematic = true;
        }
        
        currentSampleTexture = interactableObject.SampleImage;
        currentSampleName = interactableObject.name;
        isSampleOnHand = true;
        if (Inventory != null)
        {
            Inventory.texture = currentSampleTexture;
        }

        Renderer sampleRenderer = temp.transform.childCount > 0
            ? temp.transform.GetChild(0).GetComponent<Renderer>()
            : temp.GetComponentInChildren<Renderer>();

        if (sampleRenderer != null && sampleRenderer.material != null && interactableObject.SampleImage != null)
        {
            sampleRenderer.material.SetTexture("_MainTexture", interactableObject.SampleImage);
        }

        Debug.Log($"[InteractWithSamples] Successfully picked sample: {currentSampleName}");
        SyncSampleStateToForceTutorialPrerequisites();
        SampleChanged?.Invoke();
    }

    public bool HasSampleOnHand()
    {
        return AssistantHasHandSample;
    }

    public void ApplySampleStateToTutorialPrerequisite(MandatoryTutorialTrigger tutorialTrigger)
    {
        if (tutorialTrigger == null)
        {
            return;
        }

        tutorialTrigger.SetPrerequisiteSatisfied(isSampleOnHand);
    }

    public void SyncSampleStateToForceTutorialPrerequisites()
    {
        MandatoryTutorialTrigger[] tutorialTriggers = FindObjectsOfType<MandatoryTutorialTrigger>(true);
        for (int i = 0; i < tutorialTriggers.Length; i++)
        {
            MandatoryTutorialTrigger tutorialTrigger = tutorialTriggers[i];
            if (tutorialTrigger == null || !tutorialTrigger.requirePrerequisite)
            {
                continue;
            }

            tutorialTrigger.SetPrerequisiteSatisfied(isSampleOnHand);
        }
    }

    public void EnablePickSample(InteractableSamples interactable)
    {
        interactableObject=interactable;
        // Debug.Log("enable pick sample");
    }

    public void DisablePickSample(InteractableSamples interactable)
    {
        if (interactableObject == interactable)
        {
            interactableObject = null;
        }
    }
}
