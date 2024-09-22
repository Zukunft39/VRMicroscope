using System;
using UnityEngine;
using UnityEngine.Events;

public class InteractableObject:MonoBehaviour
{
    public string interactName = "";
    public float interactDistance = 3;
    public UnityEvent interactAction;
    public bool isInteractable = true;
    
    public void TryInteract()
    {
        if (isInteractable)
        {
            interactAction.Invoke();
        }
    }
}