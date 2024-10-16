using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
[CustomEditor(typeof(LightLineMgr))]
public class LightLineMgrEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        LightLineMgr myScript = (LightLineMgr)target;
        if (GUILayout.Button("发射光线（直接显示）"))
        {
            LightLineMgr.DrawLineWithAnim(myScript.start,myScript.direction,Color.white,myScript.num,10,0,3);
        }

        if (GUILayout.Button("发射光线（动画显示）"))
        {
            LightLineMgr.DrawLineWithAnim(myScript.start,myScript.direction,Color.white,myScript.num,10,myScript.duration,3);
        }
        if (GUILayout.Button("清除当前光线"))
        {
            myScript.RemoveLightLine();
        }
    }
}
