using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NATweaker : MonoBehaviour
{
    public MeshRenderer screenMeshRenderer;
    [Range(0, 1)]
    public float naInspectorValue = 1.0f;
    
    public float NAValue
    {
        get => curNAValue;
        set
        {
            if (value < 0)
                value = 0;
            if (value > 1)
                value = 1;
            curNAValue = value;
            screenMeshRenderer.material.SetFloat("_Light", curNAValue);
            screenMeshRenderer.material.SetFloat("_BlurStrengh", 1-curNAValue);
        }
    }
    
    private float curNAValue;

    // Update is called once per frame
    void OnValidate()
    {
         if(Math.Abs(naInspectorValue - curNAValue) > 0.001f)
         {
             NAValue = naInspectorValue;
         }
    }
}
