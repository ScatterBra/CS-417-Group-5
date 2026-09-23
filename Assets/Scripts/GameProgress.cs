using System.Collections.Generic;
using UnityEngine;

/// <summary>Puzzle completion for this play session, shared by all scenes.</summary>
public static class GameProgress
{
    private static readonly HashSet<string> completedRooms = new HashSet<string>();
    private static readonly HashSet<string> collectedItems = new HashSet<string>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void ResetProgress()
    {
        completedRooms.Clear();
        collectedItems.Clear();
    }

    public static bool IsItemCollected(string itemId) => collectedItems.Contains(itemId);

    public static bool TryCollectItem(string itemId) => collectedItems.Add(itemId);

    public static bool IsRoomCompleted(string roomId) =>
        !string.IsNullOrWhiteSpace(roomId) && completedRooms.Contains(roomId.Trim());

    public static void CompleteRoom(string roomId)
    {
        if (!string.IsNullOrWhiteSpace(roomId)) completedRooms.Add(roomId.Trim());
    }
}
