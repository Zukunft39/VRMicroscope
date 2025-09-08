using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InteractWithMicroscope : MonoBehaviour
{
    public InputActionAsset inputActions;
    public Microscope microscope;
    InputAction MoveAction=>inputActions.actionMaps[3].actions[5];
    InputAction ContinuousTurnAction=>inputActions.actionMaps[6].actions[4];
    InputAction SnapTurnAction=>inputActions.actionMaps[6].actions[7];
    InputAction AutoMoveAction=>inputActions.actionMaps[5].actions[0];
    private void LightSwitch(InputAction.CallbackContext context)
    {
        microscope.LightSwitch();
    }

    private void ObserveOrQuit(InputAction.CallbackContext context)
    {
        if (microscope.lookCamera.activeSelf)
        {
            microscope.QuitObserve();
            MoveAction.Enable();
            ContinuousTurnAction.Enable();
            SnapTurnAction.Enable();
            AutoMoveAction.Enable();
            return;
        }
        microscope.PutAndObserve();
        MoveAction.Disable();
        ContinuousTurnAction.Disable();
        SnapTurnAction.Disable();
        AutoMoveAction.Disable();
    }

    private void TakeOutObject(InputAction.CallbackContext context)
    {
        microscope.TakeOutobj();
    }

    private void RotateGlass(InputAction.CallbackContext context)
    {
        microscope.RotateGlass();
        microscope.RotateGlassOnObserving();
    }

    private void SwitchModeOfChange(InputAction.CallbackContext context)
    {
        microscope.SwitchModeOfChange();
    }

    private void DisableTurn(InputAction.CallbackContext context)
    {
        ContinuousTurnAction.Disable();
        SnapTurnAction.Disable();
    }

    private void EnableTurn(InputAction.CallbackContext context)
    {
        ContinuousTurnAction.Enable();
        SnapTurnAction.Enable();
    }
    public void EnableInteract(Microscope microscope)
    {
        this.microscope = microscope;
        inputActions.actionMaps[9].actions[0].started += LightSwitch;
        inputActions.actionMaps[9].actions[1].started += ObserveOrQuit;
        inputActions.actionMaps[9].actions[2].started += TakeOutObject;
        inputActions.actionMaps[9].actions[4].started += RotateGlass;
        inputActions.actionMaps[9].actions[5].started += SwitchModeOfChange;
        inputActions.actionMaps[9].actions[6].started += DisableTurn;
        inputActions.actionMaps[9].actions[6].canceled += EnableTurn;
    }

    private void Update()
    {
        if(microscope == null)return;
        Vector2 temp = inputActions.actionMaps[9].actions[3].ReadValue<Vector2>();
        microscope.ChangeFocal(temp.x);
        microscope.AdjustLight(temp.y);
    }

    public void DisableInteract(Microscope microscope)
    {
        this.microscope = null;
        inputActions.actionMaps[9].actions[0].started -= LightSwitch;
        inputActions.actionMaps[9].actions[1].started -= ObserveOrQuit;
        inputActions.actionMaps[9].actions[2].started -= TakeOutObject;
        inputActions.actionMaps[9].actions[4].started -= RotateGlass;
        inputActions.actionMaps[9].actions[5].started -= SwitchModeOfChange;
        inputActions.actionMaps[9].actions[6].started -= DisableTurn;
        inputActions.actionMaps[9].actions[6].canceled -= EnableTurn;
    }
}
