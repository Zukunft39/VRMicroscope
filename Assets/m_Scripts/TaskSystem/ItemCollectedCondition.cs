using UnityEngine;

[CreateAssetMenu(fileName = "ItemCollectedCondition", menuName = "Tasks/Conditions/ItemCollected")]
public class ItemCollectedCondition : BaseTaskCondition
{
    public string itemId;
    public int requiredAmount;

    private int currentAmount;

    public override void Initialize()
    {
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