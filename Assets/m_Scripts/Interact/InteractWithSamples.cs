
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
    public InputActionAsset inputActions;
    private void Start()
    {
        PickOrPutSampleAction = inputActions.actionMaps[9].actions[2];
    }

    private void Update()
    {
        if (!ReferenceEquals(interactableObject,null))
        {
            if (Input.GetKey(KeyCode.R))
            {
                PickSample(new InputAction.CallbackContext());
            }
        }
    }

    void PickSample(InputAction.CallbackContext  context)
    {
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
        Inventory.texture = interactableObject.SampleImage;
        isSampleOnHand = true;
    }
    public void EnablePickSample(InteractableSamples interactable)
    {
        interactableObject=interactable;
        PickOrPutSampleAction.started += PickSample;
    }

    public void DisablePickSample(InteractableSamples interactable)
    {
        interactableObject = null;
        PickOrPutSampleAction.started -= PickSample;
    }
}