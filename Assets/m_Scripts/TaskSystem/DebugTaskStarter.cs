using UnityEngine;
using System.Collections;

public class DebugTaskStarter : MonoBehaviour
{
    public TaskSO[] taskQueue;
    
    void Start()
    {
        TaskManager.Instance.StartTaskQueue(taskQueue);
    }
}
