using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
[Serializable]
public struct Chapter{
    [SerializeField]
    public Action OnEnter;
    public GameObject Position;
    //对话
    [SerializeField]
    public Action OnExit;
}
public class ProgressControl : TInstance<ProgressControl>
{
    public GameObject origin;
    public bool isAutoMoving;
    Progress progress;
    private void Awake() =>progress=GetComponent<Progress>();
    private void Start() {
        Progress();
    }
    async UniTask Progress(){
        if (origin != null){
            foreach (var c in progress.chapters){
                await TranslateTo(c.Position);
                c.OnEnter.Invoke();
                //对话
                c.OnExit.Invoke();
            }
        }
    }

    async UniTask TranslateTo(GameObject Target){
        isAutoMoving=true;
        Move.Instance.targetPos = Target.transform.position;
        await UniTask.WaitUntil(()=>!isAutoMoving);
    }
}
