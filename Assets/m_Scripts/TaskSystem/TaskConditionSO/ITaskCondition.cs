using System;
using UnityEngine.Events;

public interface ITaskCondition 
{
    bool IsSatisfied { get; } 
    event UnityAction OnConditionMet;

    public void Initialize();
}
