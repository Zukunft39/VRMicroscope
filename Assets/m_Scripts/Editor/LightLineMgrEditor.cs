using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
[CustomEditor(typeof(SquareLightSource))]
public class SquareLightSourceEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        SquareLightSource myScript = (SquareLightSource)target;
        if (GUILayout.Button("发射/更新光线"))
        {
            myScript.RemoveLightLine();
            myScript.DrawLightLine();
        }
        if (GUILayout.Button("清除当前光线"))
        {
            myScript.RemoveLightLine();
        }
    }
}
