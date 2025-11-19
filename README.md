**⚔️ Fusion Card Battle Prototype**

This is a **1v1 turn-based multiplayer card game prototype** developed in **Unity** and utilizing **Photon Fusion 2** for high-performance networking. The project serves as a technical evaluation for network synchronization, robust reconnection handling, JSON-driven content, and a modular, event-driven architecture.

**✨ Key Features**

- **Photon Fusion 2 Networking:** Implements host-client topology for reliable, synchronized gameplay.
- **JSON-Driven Card System:** All card data (cost, power, abilities) is externalized in JSON files for easy content iteration and modification.
- **Event-Driven Gameplay:** Core game actions are managed through a flexible event system (OnCardPlayed, OnTurnResolved, OnAbilityTriggered).
- **Full Synchronization:** All game actions and state changes are sent/received via JSON payloads to ensure perfect state synchronization between clients.
- **Modular Architecture:** Clear separation of concerns between Networking, Game Logic, and UI components.

**🃏 Gameplay Overview**

The goal is to have the **highest score after 6 turns**.

| **Feature** | **Detail** |
| --- | --- |
| **Match Duration** | 6 Turns per match |
| **Deck Size** | 12 Cards per player |
| **Starting Hand** | Draw 3 cards |
| **Draw per Turn** | +1 card per turn |
| **Turn Timer** | 30 seconds (Automatic resolution if expired) |
| **Energy System** | Starts at 1, +1 per turn, capped at 6 |
| **Playing Cards** | Multiple cards can be played per turn as long as \$\\sum \\text{Card Cost} \\le \\text{Current Energy}\$ |

**Implemented Card Abilities**

Cards can trigger special effects defined in their JSON data:

- **GainPoints:** Add points directly to the player's score.
- **StealPoints:** Take points from the opponent's score.
- **BlockNextAttack:** Nullify the opponent's card power for the current turn.
- **DoublePower:** Double the base power of the card for score calculation.
- **DrawExtraCard:** Draw an additional card immediately.

**📁 Card Example (JSON)**

Cards are defined in Assets/Resources/Cards/:

JSON

{

"id": 1,

"name": "Shield Bearer",

"cost": 2,

"power": 3,

"abilities": \["BlockNextAttack"\]

}

**🛠️ Technical Implementation**

**Core Dependencies**

- **Game Engine:** Unity 2023.6.0.61f
- **Networking:** Photon Fusion 2

**Project Structure**

| **Folder** | **Description** |
| --- | --- |
| Assets/Scripts/Networking | Handles Photon Fusion setup, session management, and network state synchronization. |
| Assets/Scripts/GameLogic | Manages the turn system, card resolution order, energy tracking, and ability execution. |
| Assets/Scripts/UI | Contains scripts for displaying the hand, energy/cost, score tracker, and card selection. |
| Assets/Resources/Cards | Stores all the JSON files defining the card roster. |

**⚙️ How to Run**

- **Clone the Repository:** Get the project files.
- **Open in Unity:** Launch the project using **Unity 2023.6.0.61f**.
- **Open Scene:** Navigate to the Assets/Scenes folder and open **MainScene**.
- **Run Multiple Clients:**
  - **Option 1 (Recommended):** Build a standalone executable and run the game in the Unity Editor simultaneously.
  - **Option 2:** Run two instances of the standalone build.
- **Start Match:**
  - In the first client, **Join/Create a Room**. This client will act as the Host.
  - In the second client, **Join the same Room** name.
- **Play:** Complete a 6-turn match to observe synchronization and resolution logic.

**🐛 Known Issues**

- **Timer Desync/Score Bug:** A known issue where timer desynchronization during the card reveal phase can sometimes prevent the final score display from updating correctly.
- **Minor Bugs:** Other minor, documented issues are present within the project's issue tracker.
