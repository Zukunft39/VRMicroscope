using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cinemachine;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem.XR;
[Serializable]
public struct Chapter{
    [SerializeField]
    public UnityEvent<Chapter> OnEnter;
    public CinemachineVirtualCamera Position;
    public bool isCutToNext;
    public bool isFreeView;
    //对话
    [SerializeField]
    public UnityEvent OnExit;
    public bool isOver;
}
public class ProgressControl : TInstance<ProgressControl>
{
    public GameObject origin;
    public bool isAutoMoving;
    Progress progress;
    public Vector3 velocity;
    public CinemachineVirtualCamera CurrentCinema;
    public CinemachineBrain cinemachineBrain;
    private void Awake() =>progress=GetComponent<Progress>();
    private void Start() {
        origin.transform.position=new Vector3(progress.chapters[0].Position.transform.position.x,origin.transform.position.y,progress.chapters[0].Position.transform.position.z);
        origin.transform.GetChild(3).GetComponent<CharacterController>().Move(Vector2.zero);
        isAutoMoving=true;
        _ = Progress();
    }
    async UniTask Progress(){
        if (origin != null){
            CurrentCinema=progress.chapters[0].Position;
            foreach (var c in progress.chapters){
                cinemachineBrain.transform.GetComponent<TrackedPoseDriver>().enabled=c.isFreeView;
                await TranslateTo(CurrentCinema,c.Position,c.isCutToNext);
                c.OnEnter?.Invoke(c);
                //对话
                c.OnExit?.Invoke();
                await UniTask.WaitUntil(()=>c.isOver);
            }
        }
    }
    private void Update() {
        velocity=Move.Instance.targetPos;
    }
    async UniTask TranslateTo(CinemachineVirtualCamera current,CinemachineVirtualCamera next,bool isCut){
        if(!isCut){
            isAutoMoving=true;
            current?.gameObject.SetActive(false);
            next?.gameObject.SetActive(true);
            
            await UniTask.WaitForSeconds(cinemachineBrain.m_DefaultBlend.BlendTime);
            isAutoMoving=false;
            CurrentCinema=next;
            return ;
        }
        else{

        }
    }
}
