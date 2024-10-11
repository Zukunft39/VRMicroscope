using System;
using System.Collections.Generic;
using UnityEngine;

public class Task
{
    public List<BaseTaskCondition> taskConditions;
    private ITaskCompleteJudger taskCompleteJudger;
    
    public event Action<Task> OnTaskCompleted;
    
    public readonly string taskName;
    public readonly string taskDescription;
    public bool isAutoTriggerNextTask;

    public void SetCompleteJudger(ITaskCompleteJudger judger)
    {
        taskCompleteJudger = judger;
    }

    public Task(string name, string description, 
        List<BaseTaskCondition> conditions, ITaskCompleteJudger judger,
        bool autoTriggerNextTask = false)
    {
        taskName = name;
        taskDescription = description;
        taskConditions = conditions;
        taskCompleteJudger = judger;
        isAutoTriggerNextTask = autoTriggerNextTask;
        
        taskCompleteJudger.Initialize(
            taskConditions.ConvertAll(input => input as ITaskCondition)
            ); 
        
        taskCompleteJudger.OnTaskJudgedCompleted += CompleteTask;

        foreach (var condition in taskConditions)
        {
            condition.Initialize();
        }
    }

    private void CompleteTask()
    {
        Debug.Log("Task Completed: " + taskName);
        OnTaskCompleted?.Invoke(this);
    }
    
    // Currently the task hash just based on name and description
    public override int GetHashCode()
    {
        return (taskName+taskDescription).GetHashCode();
    }
}