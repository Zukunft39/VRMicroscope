using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
public class Move : TInstance<Move>
{
    public InputActionAsset inputActions;
    public ProgressControl progressControl;
    InputAction inputAction;
    public MoveTest moveTest;
    public XRRayInteractor interactor;//右
    public float speed;
    public GameObject cinema;
    private Vector3 direction=Vector3.zero;
    public Vector3 targetPos=Vector2.zero;

    private void autoMove(InputAction.CallbackContext  context)
    {
        interactor.endPointDistance=1000f;
        moveTest.SetAutoMoveTarget(interactor.rayEndPoint);
    }
    private void Start() {
        inputActions.FindActionMap("Roaming").FindAction("AutoMove").started+=autoMove;
        inputActions.FindActionMap("XRI LeftHand Locomotion").FindAction("Move").performed += (context) =>
        {
            if (context.ReadValue<Vector2>() != Vector2.zero)
            {
                moveTest.isAutoNavigating = false;
            }
        };
    }
    private void FixedUpdate() {
        if(progressControl.isAutoMoving){
            transform.position=cinema.GetComponent<CinemachineBrain>().OutputCamera.transform.position;
        }
    }
}