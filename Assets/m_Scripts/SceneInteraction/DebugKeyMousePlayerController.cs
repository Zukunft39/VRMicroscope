using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugKeyMousePlayerController : MonoBehaviour
{
    public PlayerInteractManager playerInteractManager;
    public InteractableObjViewer interactableObjViewer;

    private void Start()
    {
        interactableObjViewer.Disable();
    }

    private void Update()
    {
        Interact();
    }
    
    private void Interact()
    {
        
    }
}
