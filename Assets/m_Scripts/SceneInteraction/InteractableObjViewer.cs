using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class InteractableObjViewer : MonoBehaviour
{
    public TMP_Text interactableObjLabel;
    
    private List<InteractableObject> _interactableObjects;
    private int _currentInteractableObjIndex;
    private InteractableObject _currentInteractableObj;
    
    private void UpdateUI()
    {
        interactableObjLabel.text = _currentInteractableObj.interactName == "" ? 
            _currentInteractableObj.gameObject.name : _currentInteractableObj.interactName;
    }
    
    public void Enable(List<InteractableObject> interactableObjects)
    {
        _interactableObjects = interactableObjects;
        interactableObjLabel.gameObject.SetActive(true);
        _currentInteractableObjIndex = 0;
        _currentInteractableObj = _interactableObjects[_currentInteractableObjIndex];
        UpdateUI();
    }
    
    public void Disable()
    {
        interactableObjLabel.gameObject.SetActive(false);
    }
    
    public void NextInteractableObj()
    {
        if(_interactableObjects == null||_interactableObjects.Count==0)
            return;
        _currentInteractableObjIndex = (_currentInteractableObjIndex + 1)%_interactableObjects.Count;
        _currentInteractableObj=_interactableObjects[_currentInteractableObjIndex];
        UpdateUI();
    }
    
    public void PreviousInteractableObj()
    {
        if(_interactableObjects == null||_interactableObjects.Count==0)
            return;
        _currentInteractableObjIndex = (_currentInteractableObjIndex - 1 + _interactableObjects.Count)%_interactableObjects.Count;
        _currentInteractableObj=_interactableObjects[_currentInteractableObjIndex];
        UpdateUI();
    }
    
    public InteractableObject GetCurrentInteractableObj()
    {
        return _currentInteractableObj;
    }
}
