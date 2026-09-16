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
    [Tooltip("Target ID for AI navigation, e.g. sample_red, sample_green, sample_blue, sample_yellow")]
    public string sampleTargetId;

    public string SampleTargetId
    {
        get
        {
            if (!string.IsNullOrEmpty(sampleTargetId)) return sampleTargetId;
            if (SampleImage != null)
            {
                string texName = SampleImage.name;
                if (texName.IndexOf("e0825e3d", StringComparison.OrdinalIgnoreCase) >= 0 || texName.IndexOf("red", StringComparison.OrdinalIgnoreCase) >= 0)
                    return "sample_red";
                if (texName.IndexOf("f68b722c", StringComparison.OrdinalIgnoreCase) >= 0 || texName.IndexOf("green", StringComparison.OrdinalIgnoreCase) >= 0)
                    return "sample_green";
                if (texName.IndexOf("872da4f4", StringComparison.OrdinalIgnoreCase) >= 0 || texName.IndexOf("blue", StringComparison.OrdinalIgnoreCase) >= 0)
                    return "sample_blue";
                if (texName.IndexOf("e0a4ff12", StringComparison.OrdinalIgnoreCase) >= 0 || texName.IndexOf("yellow", StringComparison.OrdinalIgnoreCase) >= 0)
                    return "sample_yellow";
            }
            if (name.IndexOf("(3)", StringComparison.OrdinalIgnoreCase) >= 0) return "sample_red";
            if (name.IndexOf("(1)", StringComparison.OrdinalIgnoreCase) >= 0) return "sample_blue";
            if (name.IndexOf("(2)", StringComparison.OrdinalIgnoreCase) >= 0) return "sample_yellow";
            if (name.IndexOf("Slide 1", StringComparison.OrdinalIgnoreCase) >= 0) return "sample_green";
            return null;
        }
    }

    private void OnCollisionEnter(Collision other)
    {
        TryEnablePick(other.gameObject);
    }

    private void OnCollisionStay(Collision other)
    {
        TryEnablePick(other.gameObject);
    }

    private void OnCollisionExit(Collision other)
    {
        TryDisablePick(other.gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryEnablePick(other.gameObject);
    }

    private void OnTriggerStay(Collider other)
    {
        TryEnablePick(other.gameObject);
    }

    private void OnTriggerExit(Collider other)
    {
        TryDisablePick(other.gameObject);
    }

    private void TryEnablePick(GameObject obj)
    {
        if (obj == null) return;
        InteractWithSamples handler = obj.GetComponentInParent<InteractWithSamples>();
        if (handler == null && Camera.main != null)
        {
            handler = Camera.main.GetComponentInParent<InteractWithSamples>();
        }
        if (handler != null)
        {
            handler.EnablePickSample(this);
        }
    }

    private void TryDisablePick(GameObject obj)
    {
        if (obj == null) return;
        InteractWithSamples handler = obj.GetComponentInParent<InteractWithSamples>();
        if (handler == null && Camera.main != null)
        {
            handler = Camera.main.GetComponentInParent<InteractWithSamples>();
        }
        if (handler != null)
        {
            handler.DisablePickSample(this);
        }
    }
}
