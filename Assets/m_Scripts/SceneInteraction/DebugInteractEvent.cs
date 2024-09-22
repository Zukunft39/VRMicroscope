using System;
using UnityEngine;

public class DebugInteractEvent:MonoBehaviour
{
    public void Interact(String message)
    {
        Debug.Log($"Interacted with {message}");
    }
}
