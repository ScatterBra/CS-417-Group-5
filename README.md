# CS 417 Group 5

Contributors: Hansen Zheng, Wyatt Walsh, Rundong He, Aster Yu, Lucas Zhu

## Collectibles

1. Add a Collider and `CollectibleItem` to an object. Set its **Score Value** in the Inspector. The script adds a Rigidbody if needed and makes the Collider a trigger.
2. When the player gets close, a prompt appears above the item. Press **E**, the gamepad south button, or a VR controller's primary button to collect it. The item disappears and its points are added to the total. Test the VR button with a headset before using it in the final game.
3. To show the score, create a Canvas with a **UI > Legacy > Text** child. Attach `CollectibleScoreUI` to the Canvas and drag the Text into its **Score Text** field. The script updates the text when a collectible is collected.
