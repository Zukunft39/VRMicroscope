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
        if (GUILayout.Button("Emit / Update Rays"))
        {
            myScript.RemoveLightLine();
            myScript.DrawLightLine();
        }
        if (GUILayout.Button("Clear Current Rays"))
        {
            myScript.RemoveLightLine();
        }
    }
}
