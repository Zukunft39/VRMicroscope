using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PC_CanvasMgr : TInstance<PC_CanvasMgr>
{
    public GameObject GetBackground(){
        return transform.GetChild(0).GetChild(0).gameObject;
    }
}
