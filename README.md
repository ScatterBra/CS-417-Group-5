# CS 417 Group5
---
Contributors: Hansen Zheng, Wyatt Walsh, Rundong He, Aster Yu, Lucas Zhu

## CollectibleItem.cs

1. Add a Collider (for example, a Box Collider) to the object, then attach `CollectibleItem`. A Rigidbody is required and is added automatically if missing. The script makes the Collider a trigger and the Rigidbody kinematic with gravity disabled.
2. Set **Score Value** to the points awarded. Set **Interaction Distance** to the camera proximity range (default: 2.5 Unity units).
3. In Play mode, each collectible creates its own child World Space Canvas named **Collect Prompt**. The prompt stays with the item, faces the player camera, and appears when the player is nearby or inside its trigger.
4. Press **E**, the gamepad south button, or a supported VR controller's primary button to collect. The item disappears and adds its points to `CollectibleItem.TotalScore`. An XR interaction event can also call `Collect()`.

Use a camera tagged **MainCamera**. The current lab has a 10-point test collectible in front of the starting camera. Enter Play mode, focus the Game view and press **E** to test; the item should disappear and the score should become 10. Headset/controller integration still needs testing with the team's XR rig.

## CollectibleScoreUI.cs

This script only updates an existing UI Text; it does not create a Canvas or choose its layout.

1. Create a Canvas and a child **UI > Legacy > Text** in the scene.
2. Attach `CollectibleScoreUI` to the Canvas (or another UI object).
3. Drag the child Text into the script's **Score Text** Inspector field.
4. Set font, size, color and layout directly in the editor. The script displays the current total when enabled and listens to `ScoreChanged` for updates.

The lab is already configured as **Main Camera > Score Canvas > Score Text**. Its Canvas uses **World Space** and follows the camera because it is a camera child. You can edit it before pressing Play. For a future XR rig, move Score Canvas under the headset camera and assign that camera to the Canvas. Each scene needing a score display should contain its own configured Canvas.
