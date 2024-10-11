using UnityEngine;
using System;

[CreateAssetMenu(fileName = "New Task Condition", menuName = "Task System/Task Condition")]
public abstract class BaseTaskCondition : ScriptableObject, ITaskCondition
{
    public bool IsSatisfied { get; protected set; }
    public event Action OnConditionMet;

    protected void ConditionMet()
    {
        if (!IsSatisfied)
        {
            IsSatisfied = true;
            OnConditionMet?.Invoke();
        }
    }

    public abstract void Initialize();
}