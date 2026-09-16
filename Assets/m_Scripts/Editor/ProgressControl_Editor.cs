using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ProgressControl))]

public class ProgressControl_Editor : Editor {
    public override void OnInspectorGUI() {
        base.OnInspectorGUI();
        ProgressControl myScript= (ProgressControl)target;
        if(GUILayout.Button("Switch to Free View")){
            if(!myScript.isAutoMoving)
                myScript.ChangeViewToFree();
        }
        if(GUILayout.Button("Switch to Preset View")){
            myScript.ChangeViewToPreset();
        }
        if(GUILayout.Button("End Current Dialog")){
            myScript.isCurrentChapterOver=true;
        }
    }
}