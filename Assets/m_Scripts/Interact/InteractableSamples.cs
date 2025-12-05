using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InteractableSamples : MonoBehaviour
{
    public GameObject Sample;
    public Texture SampleImage;

    private void OnTriggerEnter(Collider other)
    {
        if(!other.CompareTag("MainCamera"))return;
        other.transform.parent.GetComponent<InteractWithSamples>().EnablePickSample(this);
    }

    private void OnTriggerExit(Collider other)
    { 
        if(!other.gameObject.CompareTag("MainCamera"))return;
        other.transform.parent.GetComponent<InteractWithSamples>().DisablePickSample(this);
    }
}
