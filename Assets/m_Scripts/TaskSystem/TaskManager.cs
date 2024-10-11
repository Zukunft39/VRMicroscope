using System.Collections.Generic;
using UnityEngine;

public class TaskManager : MonoBehaviour
{
    public TaskSO[] taskSOs;
    
    private List<Task> taskQueue = new ();
    private int currentTaskIndex = -1;
    public bool allowManualTrigger = true;
    
    private void LoadTasks()
    {
        foreach (var taskSO in taskSOs)
        {
            Task task =taskSO.CreateTask();
            taskQueue.Add(task);
        }
    }
    
    private void Awake()
    {
        LoadTasks();
    }
    
    public void StartTaskQueue()
    {
        if(taskQueue.Count > 0)
        {
            Task currentTask = TriggerNextTask();
            Debug.Log("Task Started: " + currentTask.taskName);
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
            Debug.Log("Task Started: " + currentTask.taskName);
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

    private void Start()
    {
        
    }
}