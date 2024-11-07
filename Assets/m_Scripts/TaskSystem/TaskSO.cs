using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "New Task", menuName = "Task System/Task")]
public class TaskSO : ScriptableObject
{
    public List<BaseTaskCondition> taskConditions;
    public JudgerFactory.JudgerType judgerType;
    
    public UnityAction<Task> OnTaskCompleted;
    
    public string taskName;
    public string taskDescription;
    public bool isAutoTriggerNextTask;
    
    public virtual Task CreateTask()
    {
        Task task = new Task(taskName, taskDescription, 
            taskConditions, JudgerFactory.CreateJudger(judgerType),isAutoTriggerNextTask);
        task.OnTaskCompleted += (completedTask) =>
        {
            Debug.Log("Task Completed: " + completedTask.taskName);
            OnTaskCompleted?.Invoke(completedTask);
        };
        return task;
    }
}
