# Tiny Golf - Let's Build a F2P Multiplayer VR Game

## Project Overview
Tiny Golf is a **multiplayer VR mini-golf game** built in Unity as part of a three-part live tutorial series by Quentin Valembois (Valem) for the Meta Horizon Start program. Over about 5 hours of development, the series demonstrates how to implement key free-to-play multiplayer features using the Meta Quest Platform SDK and Unity’s Netcode for GameObjects.

This project features a fully networked mini-golf experience with support for multiplayer interactions and Meta platform integrations. Players can join public or private rooms to play mini-golf together, using **join codes** for matchmaking and **proximity voice chat** to communicate. The game integrates **Meta Avatars** for player representation and includes sample features like **leaderboards** and **in-app purchases (IAP)** to demonstrate Meta’s platform capabilities. All project files from the tutorial series are provided here, along with recorded sessions and slides for learning purposes.

https://github.com/user-attachments/assets/dc150813-059b-42ca-83ed-287c8160d160

## Download Files (Tutorial Resources)
You can follow the development journey through the recorded sessions and materials:

- **Part 1 – Multiplayer Foundation:** Player connection flow and how to test a VR multiplayer game.  
  [Watch Part 1](https://metahorizondevelopers.zendesk.com/hc/en-us/articles/43887393497107--Build-Along-Let-s-Build-a-F2P-Multiplayer-VR-Game-Part-1-Multiplayer-Foundation)

- **Part 2 – Gameplay Networking:** Networked gameplay logic (ownership transfers, network variables, RPCs) and how to turn a single-player VR game into multiplayer.  
  [Watch Part 2](https://metahorizondevelopers.zendesk.com/hc/en-us/articles/44087414245011-Let-s-Build-a-F2P-Multiplayer-VR-Game-Build-Along)

- **Part 3 – Meta Platform Features:** Adding Meta platform integrations such as Meta Avatars, leaderboards, in-app purchasing, and user info.  
  [Watch Part 3](https://metahorizondevelopers.zendesk.com/hc/en-us/articles/44282045559955--Quentin-8-26-25-Let-s-Build-a-F2P-Multiplayer-VR-Game)

- **Slides (PDF):** Download the slides covering Parts 1–3 of the series:  
  [F2P Multiplayer Part 1-2-3.pdf](https://github.com/Meta-Horizon-Start-Program/Tiny-Golf/blob/main/F2P%20Multiplayer%20Part%201-2-3.pdf)

## Getting Started
**Prerequisites:** You will need **Unity** (this project was built with the Unity VR Multiplayer Template, which uses Unity Gaming Services) and a **Meta Quest** VR headset for testing. Make sure you have a Meta developer account to obtain an App ID for platform features.

Follow these steps to set up the project:

1. **Download or Clone the Repository:** Get the project files onto your local machine and open the project in Unity (use the Unity Hub to open the project folder). This project includes all required Unity packages (XR Interaction Toolkit, Netcode for GameObjects, etc.) from the VR Multiplayer Template.

2. **Unity Services Setup:** Sign in to Unity within the Editor and link the project to your Unity organization (if prompted). This ensures Unity Gaming Services (such as Lobby, Relay, and Vivox for voice) are enabled. The project leverages Unity’s Lobby and Relay for matchmaking via join codes, and Vivox for voice chat, so being logged in with your Unity account is required for those services to work.

3. **Meta App ID Configuration:** To use Meta platform features (avatars, leaderboard, IAP), you must provide your own Meta **App ID**. If you haven’t already, create an app in the [Meta Developer Dashboard](https://developer.oculus.com/manage) to get an App ID. In Unity, go to **Meta > Platform > Edit Settings** and enter your App ID in the settings panel.

4. **Open the Appropriate Scene:** The project contains separate scenes corresponding to the end of each part of the tutorial. For example, you can open the Part 1 scene to see the basic connection flow, Part 2 scene for the networked mini-golf gameplay, or Part 3 scene for the full game with Meta features. The Part 3 scene represents the complete Tiny Golf game. *(All scenes are located under the **Scenes** folder in the Unity project.)*

5. **Test the Game:** You can test in **Play Mode** with multiple instances. For multiplayer testing on one PC, consider using the [ParrelSync](https://github.com/VeriorPies/ParrelSync) Unity extension (allows opening duplicate Unity editor instances for multiplayer). Alternatively, build and run the project on multiple Quest headsets. Use the on-screen UI to create or join rooms (by code or quick join) and play mini-golf together.

After completing these steps, you should be able to run Tiny Golf and experiment with all its multiplayer features and Meta platform integrations. For any additional setup questions (e.g., enabling platform permissions or performing entitlement checks), please refer to the official Meta documentation and Unity manuals (see External Resources below).

## Features
Tiny Golf demonstrates a range of multiplayer and platform features:

### Player Connection Flow 
- **Public & Private Rooms:** Players can create public rooms or invite-only private rooms (with a join code system for friends to join). A **Quick Join** option is available to instantly join any open room.
- **Room Info & UI:** The game displays room information (room name/code, player count) in a UI panel, and provides an interface for entering join codes or starting new sessions.
- **Voice Chat:** Integrated **proximity voice chat** using Vivox (players can talk to each other when near in-game) for a more immersive multiplayer experience.

<img width="300" height="300" alt="image" src="https://github.com/user-attachments/assets/2427c721-518f-4c13-be65-ca2812292ba4" />

### Networked Mini-Golf Gameplay 
- **Networked Players & Equipment:** Each player is represented by a networked avatar (with a golf club). The project uses Unity Netcode for GameObjects to synchronize player transforms and actions.
- **Golf Ball Ownership:** Golf balls are network-spawned objects. When a player takes a turn, the ball’s **ownership** transfers to that player, allowing them to putt while others watch. This ensures only the active player’s input moves the ball.
- **Networking Logic:** The game logic (scores, ball resets, hole completion, etc.) runs in a networked context. It uses **NetworkVariables** and **Remote Procedure Calls (RPCs)** to keep all clients in sync, turning what began as a single-player mini-golf into a fully multiplayer game.

<img width="300" height="300" alt="image" src="https://github.com/user-attachments/assets/d30e85ae-9700-45e3-994c-16dd33475e91" />

### Meta Platform Integration 
- **Meta Avatars:** Players appear as their **Meta Avatar** in-game, using the Oculus/Meta Avatars SDK for a personalized VR presence.
- **Player Name Tags:** Each avatar is tagged with the user’s account name above their avatar, demonstrating how to pull basic **user info** from the platform.
- **Leaderboard:** The project includes a sample **leaderboard** feature, showing how to record and display high scores or standings using Meta’s platform services.
- **In-App Purchasing (IAP):** A mock **in-app purchase** flow is integrated, illustrating how to set up and call the Meta Quest **IAP** API (for example, to buy cosmetic items or extra content). This is for demonstration and requires configuration in your Meta app backend if you want it fully functional.

<img width="300" height="300" alt="image" src="https://github.com/user-attachments/assets/45368a90-02a9-47f6-ad15-470ab6441276" />

## External Resources
This project and tutorial series reference several external resources and packages:

- [Kenney Mini Golf Asset Kit](https://kenney.nl/assets/minigolf-kit) – Free 3D asset pack used for the golf course models (tracks, holes, obstacles, etc.).
- [Unity VR Multiplayer Sample](https://docs.unity3d.com/Packages/com.unity.template.vr-multiplayer@2.0/manual/index.html) – Unity’s official VR Multiplayer Template, which comes pre-configured with XR Interaction Toolkit and Unity Gaming Services.
- [ParrelSync](https://github.com/VeriorPies/ParrelSync) – A Unity editor extension for running multiple instances of your project to test multiplayer locally.

## License
This project is licensed under the **MIT License**. You are free to use, modify, and distribute the code and assets in accordance with the MIT license terms. See the [LICENSE](https://github.com/Meta-Horizon-Start-Program/Tiny-Golf/blob/main/LICENSE) file for details.
