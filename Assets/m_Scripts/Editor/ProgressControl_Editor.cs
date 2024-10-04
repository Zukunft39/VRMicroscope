using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ProgressControl))]

public class ProgressControl_Editor : Editor {
    public override void OnInspectorGUI() {
        base.OnInspectorGUI();
        ProgressControl myScript= (ProgressControl)target;
        if(GUILayout.Button("切换自由视角")){
            if(!myScript.isAutoMoving)
                myScript.ChangeViewToFree();
        }
        if(GUILayout.Button("切换预设视角")){
            myScript.ChangeViewToPreset();
        }
        if(GUILayout.Button("当前对话结束")){
            myScript.isCurrentChapterOver=true;
        }
    }
}