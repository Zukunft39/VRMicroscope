using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PC_CanvasMgr : TInstance<PC_CanvasMgr>
{
    public enum Apps{
        FilterAndDichroicMirror,//滤光片与二色镜
    }
    public List<GameObject> apps;
    public GameObject GetBackground(){
        return transform.GetChild(0).GetChild(0).gameObject;
    }
    public void OpenApp(Apps app){
        switch (app){
            case Apps.FilterAndDichroicMirror:
                
                break;
        }
    }
}
