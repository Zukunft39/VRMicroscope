
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

    public Texture CurrentSampleTexture => currentSampleTexture;
    public bool AssistantCanPick => interactableObject != null && interactableObject.Sample != null;
    public bool AssistantHasHandSample
    {
        get
        {
            if (transform.childCount == 0) return false;
            foreach (Transform child in transform.GetChild(0)) if (child.CompareTag("ObserveObjects")) return true;
            return false;
        }
    }
    public event Action SampleChanged;
    public string CurrentSampleName => !string.IsNullOrWhiteSpace(currentSampleName)
        ? currentSampleName
        : CurrentSampleTexture != null ? CurrentSampleTexture.name : "Teaching grating";

    public void PickSample()
    {
        Debug.Log("Picking Sample");
        if(ReferenceEquals(interactableObject,null))return;
        if (isSampleOnHand)
        {
            for (int i = 0; i < transform.GetChild(0).transform.childCount; i++)
            {
                if (transform.GetChild(0).GetChild(i).CompareTag("ObserveObjects"))
                {
                    Destroy(transform.GetChild(0).GetChild(i).gameObject);
                    break;
                }
            }
        }

        GameObject temp = Instantiate(interactableObject.Sample, Vector3.zero, Quaternion.identity,
            transform.GetChild(0));
        temp.transform.localScale = Vector3.one;
        temp.transform.localPosition=new Vector3(23,-5,-4);
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
        temp.transform.GetChild(0).GetComponent<Renderer>().material.SetTexture("_MainTexture",interactableObject.SampleImage);
        SyncSampleStateToForceTutorialPrerequisites();
        SampleChanged?.Invoke();
    }

    public bool HasSampleOnHand()
    {
        return isSampleOnHand;
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
