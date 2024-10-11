using System;

public interface ITaskCondition 
{
    bool IsSatisfied { get; } 
    event Action OnConditionMet;
}
