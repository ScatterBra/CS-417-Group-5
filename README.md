# CS 417 Group5
---
Contributors: Hansen Zheng, Wyatt Walsh, Rundong He, Aster Yu, Lucas Zhu

## CollectibleItem.cs

1. Add a Collider (for example, a Box Collider) to the object, then attach `CollectibleItem`. A Rigidbody is required and is added automatically if missing. The script makes the Collider a trigger and the Rigidbody kinematic with gravity disabled.
2. Set **Score Value** to the points awarded. Set **Interaction Distance** to the camera proximity range (default: 2.5 Unity units).
3. In Play mode, each collectible creates its own child World Space Canvas named **Collect Prompt**. The prompt stays with the item, faces the player camera, and appears when the player is nearby or inside its trigger.
4. Press **E**, the gamepad south button, or a supported VR controller's primary button to collect. The item disappears and adds its points to `CollectibleItem.TotalScore`. An XR interaction event can also call `Collect()`.

Use a camera tagged **MainCamera**. The lab scene uses the headset camera in **XR Origin (XR Rig)** and has a 10-point test collectible near the starting position. The keyboard **E** fallback can be tested in Play mode. Test the VR primary button with a connected headset and controllers before relying on it for the final game.

## CollectibleScoreUI.cs

This script only updates an existing UI Text; it does not create a Canvas or choose its layout.

1. Create a Canvas and a child **UI > Legacy > Text** in the scene.
2. Attach `CollectibleScoreUI` to the Canvas (or another UI object).
3. Drag the child Text into the script's **Score Text** Inspector field.
4. Set font, size, color and layout directly in the editor. The script displays the current total when enabled and listens to `ScoreChanged` for updates.

The lab is configured as **XR Origin (XR Rig) > Camera Offset > Main Camera > Score Canvas > Score Text**. The Canvas uses **Screen Space - Camera**, with the headset camera as its Render Camera. The Text is anchored to the upper-right corner, so it stays there when the Game view size changes. Edit its layout in the Unity editor. Each scene needing a score display should contain its own configured Canvas.
