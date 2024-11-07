using System;
using System.Collections.Generic;
using UnityEngine;

public class TaskManager : TInstance<TaskManager>
{
    private List<Task> taskQueue = new ();
    private int currentTaskIndex = -1;
    public bool allowManualTrigger = true;

    private void Reset()
    {
        taskQueue.Clear();
        currentTaskIndex = -1;
    }
    
    private void LoadTasks(TaskSO[] taskSoList)
    {
        foreach (var taskSO in taskSoList)
        {
            Task task =taskSO.CreateTask();
            taskQueue.Add(task);
        }
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="taskSoList"></param>
    /// <param name="isForcedStart">Set this to true when you want to flush current task queue</param>
    public void StartTaskQueue(TaskSO[] taskSoList,bool isForcedStart = false)
    {
        if( taskQueue.Count > 0)
        {
            if(!isForcedStart)
            {
                Debug.LogWarning("Task queue is already started!");
                return;
            }else
            {
                Reset();
            }
        }
        
        LoadTasks(taskSoList);
        
        if(taskQueue.Count > 0)
        {
            TriggerNextTask();
        }
        else
        {
            Debug.LogWarning("No tasks to start!");
        }
    }
    
    private Task TriggerNextTask()
    {
        if (currentTaskIndex < taskQueue.Count - 1)
        {
            currentTaskIndex++;
            Task currentTask = taskQueue[currentTaskIndex];
            currentTask.OnTaskCompleted += HandleTaskCompletion;
            Debug.Log("Task Started: " + currentTask.taskName + "\nDescription: " + currentTask.taskDescription);
            return currentTask;
        }
        else
        {
            Debug.Log("All tasks are completed!");
            return null;
        }
    }
    
    private void HandleTaskCompletion(Task completedTask)
    {
        Debug.Log("Task Completed: " + completedTask.taskName);
        
        if (completedTask.isAutoTriggerNextTask)
        {
            TriggerNextTask();
        }
        else if (allowManualTrigger)
        {
            Debug.Log("Waiting for player to trigger the next task manually...");
        }
        
        completedTask.OnTaskCompleted -= HandleTaskCompletion;
    }
    
    public void TriggerNextTaskManually()
    {
        if (!taskQueue[currentTaskIndex].isAutoTriggerNextTask && allowManualTrigger)
        {
            TriggerNextTask();
        }
        else
        {
            Debug.LogWarning("Cannot manually trigger the next task. Either auto-trigger is enabled or manual triggering is disabled.");
        }
    }
}