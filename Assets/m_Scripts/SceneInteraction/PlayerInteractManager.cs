using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInteractManager:MonoBehaviour
{
    public bool isInteractable;
    public InteractableObjViewer interactableObjViewer;
    
    private List<InteractableObject> _interactableObjects = new();
    private bool _isHasInteractableObject;
    
    private void SetHasInteractableObjectState(bool state)
    {
        _isHasInteractableObject=state;
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.GetComponent<InteractableObject>()==null)
            return;
        InteractableObject currentObj = other.gameObject.GetComponent<InteractableObject>();
        // if(Vector3.Distance(transform.position,other.transform.position)>currentObj.interactDistance)
        //     return;
        _interactableObjects.Add(currentObj);
        if(!_isHasInteractableObject)
        {
            SetHasInteractableObjectState(true);
            interactableObjViewer.Enable(_interactableObjects);
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if(other.gameObject.GetComponent<InteractableObject>()==null)
            return;
        _interactableObjects.Remove(other.gameObject.GetComponent<InteractableObject>());
        if(_interactableObjects.Count==0)
        {
            SetHasInteractableObjectState(false);
            interactableObjViewer.Disable();
        }
    }
    
    public void Interact()
    {
        if(!isInteractable || !_isHasInteractableObject)
            return;
        InteractableObject currentInteractableObj = interactableObjViewer.GetCurrentInteractableObj();
        currentInteractableObj.TryInteract();
    }
}
