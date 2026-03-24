using System;
using Cinemachine;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem.XR;
using UnityEngine.Serialization;

[Serializable]
public class Chapter{
    [FormerlySerializedAs("OnEnter")] [SerializeField]
    public UnityEvent<Chapter> onEnter;
    [FormerlySerializedAs("Position")] public CinemachineVirtualCamera position;
    public bool isCutToNext;
    public bool isFreeView;
    //对话
    [FormerlySerializedAs("OnExit")] [SerializeField]
    public UnityEvent onExit;
}
public class ProgressControl : TInstance<ProgressControl>
{
    public static bool isFreeView=false;
    public GameObject origin;
    public bool isAutoMoving;
    public bool isCurrentChapterOver;
    public static Chapter currentChapter;
    Progress progress;
    public Vector3 velocity;
    public CinemachineVirtualCamera CurrentCinema;
    public CinemachineBrain cinemachineBrain;
    protected new void Awake() 
    {
        base.Awake();
        progress = GetComponent<Progress>();
    }
    private void Start() {
        origin.transform.position=new Vector3(progress.chapters[0].position.transform.position.x,origin.transform.position.y,progress.chapters[0].position.transform.position.z);
        origin.transform.GetChild(3).GetComponent<CharacterController>().Move(Vector2.zero);
        //isAutoMoving=true;
        _ = Progress();
    }
    async UniTask Progress(){
        if (origin != null){
            // CurrentCinema=progress.chapters[0].Position;
            foreach (var c in progress.chapters){
                isCurrentChapterOver=false;
                currentChapter=c;
                Debug.Log("1");
                await TranslateTo(CurrentCinema,c.position,c.isCutToNext);
                Debug.Log("2");
                if(c.isFreeView)ChangeViewToFree();
                else ChangeViewToPreset();

                c.onEnter?.Invoke(c);
                //对话
                c.onExit?.Invoke();
                Debug.Log("3");
                await UniTask.WaitUntil(()=>c.isFreeView);
                Debug.Log("4");
                await UniTask.WaitUntil(()=>!isFreeView);
                Debug.Log("5");
            }
        }
    }
    private void Update() {
        velocity=Move.Instance.targetPos;
    }
    async UniTask TranslateTo(CinemachineVirtualCamera current,CinemachineVirtualCamera next,bool isCut){
        if(!isCut){
            //isAutoMoving=true;
            current?.gameObject.SetActive(false);
            next?.gameObject.SetActive(true);
            
            await UniTask.WaitForSeconds(cinemachineBrain.m_DefaultBlend.BlendTime);
            //isAutoMoving=false;
            CurrentCinema=next;
            return ;
        }
        else{

        }
    }
    public void ChangeViewToFree(){
        Debug.Log("切换至自由视角");
        cinemachineBrain.enabled=false;
        cinemachineBrain.GetComponent<TrackedPoseDriver>().enabled=true;
        isFreeView=true;
    }
    public void ChangeViewToPreset(){
        Debug.Log("切换至预设视角");
        cinemachineBrain.enabled=true;
        cinemachineBrain.GetComponent<TrackedPoseDriver>().enabled=false;
        isFreeView=false;
    }
}
