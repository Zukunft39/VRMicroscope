using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Task", menuName = "Task System/Task")]
public class TaskSO : ScriptableObject
{
    public List<BaseTaskCondition> taskConditions;
    public ITaskCompleteJudger taskCompleteJudger;
    
    public Action<Task> OnTaskCompleted;
    
    public string taskName;
    public string taskDescription;
    
    public virtual Task CreateTask()
    {
        Task task = new Task(taskName, taskDescription, 
            taskConditions, taskCompleteJudger);
        task.OnTaskCompleted += (completedTask) =>
        {
            Debug.Log("Task Completed: " + completedTask.taskName);
            OnTaskCompleted?.Invoke(completedTask);
        };
        return task;
    }
}
