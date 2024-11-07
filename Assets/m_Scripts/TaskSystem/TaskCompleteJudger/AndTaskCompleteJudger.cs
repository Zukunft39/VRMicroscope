using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This judger will check if all conditions are satisfied.
/// </summary>
public class AndTaskCompleteJudger : ITaskCompleteJudger
{
    private List<ITaskCondition> taskConditions;
    public event Action OnTaskJudgedCompleted;

    public void Initialize(List<ITaskCondition> conditions)
    {
        taskConditions = conditions;
        foreach (var condition in taskConditions)
        {
            condition.OnConditionMet += CheckCompletion;
        }
    }

    public bool IsTaskCompleted()
    {
        foreach (var condition in taskConditions)
        {
            if (!condition.IsSatisfied)
            {
                return false;
            }
        }

        return true;
    }

    // This method is called when any of the conditions are met.
    private void CheckCompletion()
    {
        if (IsTaskCompleted())
        {
            OnTaskJudgedCompleted?.Invoke();
        }
    }
}