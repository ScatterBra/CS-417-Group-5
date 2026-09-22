# CS 417 Group5
---
Contributors: Hansen Zheng, Wyatt Walsh, Rundong He, Aster Yu, Lucas Zhu

## Collectibles

Attach `CollectibleItem` to any scene object to make it a collectible, then set its `Score Value` in the Inspector. The component configures its Collider as a trigger and its Rigidbody as kinematic. While the player is inside the trigger, press **E** on a keyboard or the south button on a gamepad to collect it. The object adds its value to `CollectibleItem.TotalScore` and disappears.

The VR interaction button can be selected later by connecting an XR interaction event to `CollectibleItem.Collect()`. A future UI script can read `CollectibleItem.TotalScore` and subscribe to `CollectibleItem.ScoreChanged` to update the displayed score.
