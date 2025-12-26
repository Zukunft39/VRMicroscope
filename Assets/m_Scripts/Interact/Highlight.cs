
using System;
using UnityEngine;
[RequireComponent(typeof(BoxCollider))]
public class Highlight:MonoBehaviour
{
    private Renderer HighlightRenderer;
    private MaterialPropertyBlock _propBlock;
    private Color hightlightColor=new(1f, 1f, 0.8f);
    private Color normalColor=new(1f, 1f, 1f);
    private void Start()
    {
        _propBlock = new MaterialPropertyBlock();
        HighlightRenderer=transform.GetChild(0).GetComponent<Renderer>();
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.transform.CompareTag("MainCamera"))
        {
            EnableHighlightAndOutline();
        }
            
    }

    private void OnCollisionExit(Collision other)
    {
        if (other.transform.CompareTag("MainCamera"))
        {
            DisableHighlightAndOutline();
        }
            
    }

    void EnableHighlightAndOutline()
    {
        HighlightRenderer.GetPropertyBlock(_propBlock);
        _propBlock.SetColor("_HighlightColor", hightlightColor);
        _propBlock.SetInteger("_boolean", 1);
        HighlightRenderer.SetPropertyBlock(_propBlock);
    }

    void DisableHighlightAndOutline()
    {
        HighlightRenderer.GetPropertyBlock(_propBlock);
        _propBlock.SetColor("_HighlightColor", normalColor);
        _propBlock.SetInteger("_boolean", 0);
        HighlightRenderer.SetPropertyBlock(_propBlock);
    }
}
