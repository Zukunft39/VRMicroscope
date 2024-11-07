using System;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "NewDebugButtonTaskCondition", menuName = "Task System/Task Condition/Debug Button Condition")]
public class DebugButtonCondition : BaseTaskCondition
{
    public string buttonName;
    
    //Runtime reference
    private Button button;

    public override void Initialize()
    {
        base.Initialize();
        button = GameObject.Find(buttonName)?.GetComponent<Button>();
        if(button == null)
        {
            Debug.LogError($"Button {buttonName} not found");
            return;
        }
        button.onClick.AddListener(ConditionMet);
        
        OnConditionMet += () => Debug.Log($"Button {button.name} clicked condition meet");
    }
}
