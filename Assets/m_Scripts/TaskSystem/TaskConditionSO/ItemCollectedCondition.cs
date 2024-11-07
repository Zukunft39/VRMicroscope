using UnityEngine;

[CreateAssetMenu(fileName = "NewItemCollectedTaskCondition", menuName = "Task System/Task Condition/Item Collected Condition")]
public class ItemCollectedCondition : BaseTaskCondition
{
    public string itemId;
    public int requiredAmount;

    private int currentAmount;

    public override void Initialize()
    {
        base.Initialize();
        GameEvents.OnItemCollected += HandleItemCollected;
    }

    private void HandleItemCollected(string collectedItemId)
    {
        if (collectedItemId == itemId)
        {
            currentAmount++;
            if (currentAmount >= requiredAmount)
            {
                ConditionMet();
                GameEvents.OnItemCollected -= HandleItemCollected;
            }
        }
    }
}