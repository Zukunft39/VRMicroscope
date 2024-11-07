using UnityEngine;
using System;
using UnityEngine.Events;

public abstract class BaseTaskCondition : ScriptableObject , ITaskCondition
{
    public bool IsSatisfied { get; protected set; }
    public event UnityAction OnConditionMet;

    protected void ConditionMet()
    {
        if (!IsSatisfied)
        {
            IsSatisfied = true;
            OnConditionMet?.Invoke();
        }
    }

    public virtual void Initialize()
    {
        OnConditionMet = null;
        IsSatisfied = false;
    }
}