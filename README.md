# CS 417 Group 5

Contributors: Hansen Zheng, Wyatt Walsh, Rundong He, Aster Yu, Lucas Zhu

## Collectibles and score

`CollectibleItem.cs` makes an object collectible: approach it, press the interaction button, and it disappears while adding points. Attach the script and set **Score Value**; configure Collider/Rigidbody components yourself. IDs are automatic, and collected items stay hidden when returning to the room during the same play session.

`CollectibleScoreUI.cs` displays the total score in the UI Text assigned to **Score Text**. The shared `PlayerRig` prefab already includes this score UI.

## Door travel

`SceneLoader.cs` lets players approach a door and hold an interaction button to enter another scene. It displays a floating prompt and can block travel until the room's puzzle is complete. Assign the destination scene and prompt UI in the Inspector. Default controls are holding **E** or the **right controller's primary button (Quest A)** for 1.5 seconds.

## Shared game progress

`GameProgress.cs` remembers completed rooms and collected items across scene changes, so players can revisit rooms without repeating finished puzzles or earning the same points again. Records last for the current play session; they are not saved after restarting the game.

Each room's puzzle script should call `GameProgress.CompleteRoom(roomId)` when solved, then check `GameProgress.IsRoomCompleted(roomId)` on entry to restore its solved state and open doors. Use the same room ID as `SceneLoader`; its default is the scene path. Collectible records are handled automatically. `GameProgress` is a static class and does not need to be attached to an object.

## Office escape room (`Assets/Scenes/office.unity`)

Rundong He's scene.

Three Key Props (mug, file folder, phone handset) each go back into one Lock (vending-machine
drip tray, filing-cabinet drawer, phone cradle). All three unlock the exit door. Design notes,
the signifier write-up for the rubric, and a walkthrough-video checklist are in
[docs/office-scene.md](docs/office-scene.md).
