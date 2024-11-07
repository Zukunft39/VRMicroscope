using System;
using System.Collections.Generic;

/// <summary>
/// A task complete judger will check if certain conditions are satisfied.
/// If these conditions are satisfied, the task is completed.
/// </summary>
public interface ITaskCompleteJudger
{
    bool IsTaskCompleted();
    void Initialize(List<ITaskCondition> conditions);
}