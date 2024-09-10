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
    public XRRayInteractor[] interactors;//0左1右
    public float speed;
    public GameObject cinema;
    private Vector3 direction=Vector3.zero;
    public Vector3 targetPos=Vector2.zero;

    private void Start() {
        inputActions.actionMaps[2].actions[0].started+=(Input)=>{
            interactors[0].endPointDistance=1000f;
            targetPos=interactors[0].rayEndPoint;
            
        };
        inputActions.actionMaps[4].actions[2].started+=(Input)=>{
            interactors[1].endPointDistance=1000f;
            targetPos=interactors[1].rayEndPoint;
        };
    }
    private void FixedUpdate() {
        if(progressControl.isAutoMoving){
            Debug.Log(cinema.GetComponent<CinemachineBrain>().OutputCamera.transform.position);
            transform.position=cinema.GetComponent<CinemachineBrain>().OutputCamera.transform.position;
            GetComponent<CharacterController>().Move(Vector3.zero);
        }
        else if(moveTest.read()==Vector2.zero){
            direction=new Vector3(targetPos.x-transform.position.x,0,targetPos.z-transform.position.z);
            if(direction.magnitude<0.3f){
                progressControl.isAutoMoving=false;
                direction=Vector3.zero;
            }
            else GetComponent<CharacterController>().Move(direction.normalized*speed);
        }
        else if(moveTest.read()!=Vector2.zero){
            targetPos=transform.position;
            direction=Vector3.zero;
        }
        
    }
    private void OnControllerColliderHit(ControllerColliderHit hit) {
        direction=Vector3.zero;
    }
}