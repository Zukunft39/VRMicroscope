using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class ProgressEvents : MonoBehaviour
{
    public GameObject canvas;
    private GameObject background;
    private void Start() =>background=canvas.GetComponent<PC_CanvasMgr>().GetBackground();
    public async void OperatePC(Chapter chapter){
        await operatePC(chapter);
    }
    private async UniTask operatePC(Chapter chapter){
        
        await UniTask.Delay(1000);
        //logo后面的蓝色背景
        background.transform.GetChild(0).GetChild(0).GetComponent<Image>().DOFade(1f,0.3f);
        await UniTask.Delay(2000);
        //logo本体直接出来
        background.transform.GetChild(0).GetChild(1).GetComponent<RawImage>().color=Color.white;
        await UniTask.Delay(3000);    
        //蓝色背景扩展到全屏
        DOTween.To(()=> background.transform.GetChild(0).GetChild(0).localScale,x => background.transform.GetChild(0).GetChild(0).localScale = x,new Vector3(2.4f,2.4f,1),1);
        await UniTask.Delay(1000);
        //菜单栏动画
        DOTween.To(()=>background.transform.GetChild(1).GetChild(0).position,x=>background.transform.GetChild(1).GetChild(0).position=x,background.transform.GetChild(1).GetChild(0).position+Vector3.up*0.12f,0.3f);
        await UniTask.Delay(300);
        DOTween.To(()=>background.transform.GetChild(1).GetChild(0).position,x=>background.transform.GetChild(1).GetChild(0).position=x,background.transform.GetChild(1).GetChild(0).position-Vector3.up*0.02f,0.2f);
        
    } 
}
