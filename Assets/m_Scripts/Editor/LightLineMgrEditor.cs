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
            LightLineMgr.DrawLine(myScript.start,myScript.direction,Color.cyan,10,myScript.num,3);
        }

        if (GUILayout.Button("发射光线（动画显示）"))
        {
            LightLineMgr.DrawLineWithAnim(myScript.start,myScript.direction,Color.cyan,myScript.num,10,10f,3);
        }
        if (GUILayout.Button("清除当前光线"))
        {
            myScript.RemoveLightLine();
        }
    }
}
