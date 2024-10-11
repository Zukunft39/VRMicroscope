using System;

/// <summary>
/// The broadcaster of main game events
/// Open to extension
/// </summary>
public static class GameEvents
{
    public static event Action<string> OnItemCollected;

    public static void ItemCollected(string itemId)
    {
        OnItemCollected?.Invoke(itemId);
    }
}