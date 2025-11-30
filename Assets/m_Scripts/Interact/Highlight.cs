
using System;
using UnityEngine;
[RequireComponent(typeof(BoxCollider))]
public class Highlight:MonoBehaviour
{
    private Renderer HighlightRenderer;
    private void Start()
    {
        HighlightRenderer=transform.GetChild(0).GetComponent<Renderer>();
    }

    private void OnCollisionEnter(Collision other)
    {
        if(other.transform.CompareTag("MainCamera"))
            EnableHighlightAndOutline();
    }

    private void OnCollisionExit(Collision other)
    {
        if(other.transform.CompareTag("MainCamera"))
            DisableHighlightAndOutline();
    }

    void EnableHighlightAndOutline()
    {
        HighlightRenderer.enabled = true;
    }

    void DisableHighlightAndOutline()
    {
        HighlightRenderer.enabled = false;
    }
}
