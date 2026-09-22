# CS 417 Group5
---
Contributors: Hansen Zheng, Wyatt Walsh, Rundong He, Aster Yu, Lucas Zhu

## Collectibles

Attach `CollectibleItem` to any scene object to make it a collectible, then set its `Score Value` in the Inspector. The component configures its Collider as a trigger and its Rigidbody as kinematic. When the player is within 2.5 meters, a floating instruction appears. Press **E** on a keyboard, the south button on a gamepad, or the primary button on a VR controller to collect it. The object adds its value to `CollectibleItem.TotalScore` and disappears.

`CollectibleScoreUI` automatically creates a score display in front of the active camera when a scene starts, so no additional setup is required. An XR interaction event can also call `CollectibleItem.Collect()` directly if the project later uses a different VR interaction button.
