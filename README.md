# ex4

itch.io link: https://ori755.itch.io/ex4-ori-and-yarin

README – Game Summary
**How to Run the Game
Open the project in Unity (version 2021+ recommended).

Load the main scene:

SampleScene


Press Play at the top of the Unity Editor to start the game.

Use the controls:

Arrow keys to move left/right

Space to jump

The goal of the game is to reach the trophy object.
When the player touches it, a YOU WIN message appears and the game stops.

Short Description of Class Relationships

Below is a brief summary of the relationships between the scripts in the project:

PlayerController2D

The main gameplay script.

Handles movement, jumping, fall-damage logic, wind influence, spring-shoes effect, and pole sliding.

WindZone2D

Detects when the player enters a wind area.

Calls SetWind() on the player when entered, and ClearWind() when exited.

SpringShoesPickup

When the player touches this object, the script activates the spring-shoes effect by calling ActivateSpringShoes() in PlayerController2D.

GoalTrigger

Detects when the player reaches the trophy.

Calls WinMessageUI.ShowWin() to display the victory screen.

WinMessageUI

Contains a reference to the UI panel that shows “YOU WIN!”.

The ShowWin() function activates this panel when the player wins.

Summary of relationships:

WindZone2D → PlayerController2D (applies wind effect)

SpringShoesPickup → PlayerController2D (activates power-up)

GoalTrigger → WinMessageUI (shows winning message)

Notable Design Decisions

Modularity: Each mechanic (wind, spring shoes, goal trigger) has its own script, making the project easy to expand or maintain.

Trigger-Based Interactions: All gameplay interactions use Unity’s 2D trigger system (OnTriggerEnter2D), which keeps the design simple and efficient.

Single Responsibility: Each class has one clear role:

Player handles physics + states

Pickups only apply effects

UI only shows visual feedback

Safe Landing System: Fall damage is calculated based on the height the player drops from, unless the player slides on a pole (safe landing).

Wind Zones Are Configurable: The wind force and drag are editable per wind zone, allowing easy tuning inside the Unity Inspector.
