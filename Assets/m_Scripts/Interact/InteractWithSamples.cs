
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;


public class InteractWithSamples:MonoBehaviour
{
    InputAction PickOrPutSampleAction;
    private InteractableSamples interactableObject;
    private bool isSampleOnHand = false;
    public RawImage Inventory;

    private void Update()
    {
        if (!ReferenceEquals(interactableObject,null))
        {
            // 如果当前处于教程状态，不允许拾取物品
            if (Interactor.Instance != null && Interactor.Instance.CurrentState == Interactor.GameState.Tutorial)
                return;

            if (Input.GetKey(KeyCode.R))
            {
                PickSample();
            }
        }
    }

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
        
        Inventory.texture = interactableObject.SampleImage;
        temp.transform.GetChild(0).GetComponent<Renderer>().material.SetTexture("_MainTexture",interactableObject.SampleImage);
        isSampleOnHand = true;
    }
    public void EnablePickSample(InteractableSamples interactable)
    {
        interactableObject=interactable;
        Debug.Log("enable pick sample");
    }

    public void DisablePickSample(InteractableSamples interactable)
    {
        if (interactableObject == interactable)
        {
            interactableObject = null;
        }
    }
}