# CS 417 Group 5

Contributors: Hansen Zheng, Wyatt Walsh, Rundong He, Aster Yu, Lucas Zhu

## Collectibles

1. Attach `CollectibleItem` to an object and set **Score Value**. Add and configure physics components yourself: for a falling, colliding item, use a Collider with **Is Trigger** off and a Rigidbody with **Use Gravity** on and **Is Kinematic** off. The script does not add or change physics components. No ID setup is needed, including for duplicated objects.
2. When the player gets close, a prompt appears above the item. Press **E**, the gamepad south button, or a VR controller's primary button to collect it. The item disappears and its points are added to the total. Test the VR button with a headset before using it in the final game.
3. To show the score, create a Canvas with a **UI > Legacy > Text** child. Attach `CollectibleScoreUI` to the Canvas and drag the Text into its **Score Text** field. The script updates the text when a collectible is collected.

Collected items stay hidden when revisiting a scene and cannot award points again during the same play session. Identification uses the scene and the object's initial hierarchy location. Progress resets when a new play session starts.
