using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class ProgressEvents : MonoBehaviour
{
    public async void OperatePC(Chapter chapter){
        await operatePC(chapter);
    }
    private async UniTask operatePC(Chapter chapter){
        
        await UniTask.Delay(1000);
        Debug.Log("logo加载");
        //logo背景
        await UniTask.Delay(2000);
        Debug.Log("logo全景");
        //logo全景

        await UniTask.Delay(1000);    
        Debug.Log("进入桌面");
        //桌面
    } 
}
