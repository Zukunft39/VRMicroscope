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

    private void OnCollisionEnter(Collision other)
    {
        if(!other.gameObject.CompareTag("Player"))return;
        Debug.Log(other.gameObject.name);
        Debug.Log(other.transform.parent.GetComponent<InteractWithSamples>());
        other.transform.parent.GetComponent<InteractWithSamples>().EnablePickSample(this);
    }

    private void OnCollisionExit(Collision other)
    {
        if(!other.gameObject.CompareTag("Player"))return;
        other.transform.parent.GetComponent<InteractWithSamples>().DisablePickSample(this);
    }
}
