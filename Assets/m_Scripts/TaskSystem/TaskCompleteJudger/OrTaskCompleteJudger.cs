using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This judger will check if any conditions are satisfied.
/// </summary>
public class OrTaskCompleteJudger : ITaskCompleteJudger
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
            if (condition.IsSatisfied)
            {
                return true;
            }
        }
        return false;
    }

    private void CheckCompletion()
    {
        if (IsTaskCompleted())
        {
            OnTaskJudgedCompleted?.Invoke();
        }
        else
        {
            Debug.Log("You have not completed any task yet.");
        }
    }
}