# Roll-a-Ball Coin Collector

A small 3D Unity game about steering a ball around a beginner-friendly course, collecting golden coins, and avoiding red hazards. The project includes a generated playable scene and uses Unity's built-in shapes, so it does not depend on external art or audio downloads.

## Gameplay

Roll around the arena and collect all **12 coins** to win. Static barriers and moving sweepers make the route more interesting. Colliding with hazards or leaving the safe area costs a life; the ball returns to its start position if it leaves the platform. The run ends after three lost lives.

## Features

- Physics-driven blue sphere with camera-relative, accelerated movement and a speed limit.
- WASD and arrow-key controls.
- Twelve gold coins that rotate and disappear when collected.
- Static barriers, moving red hazards, and a three-life system.
- Out-of-bounds detection, life loss, and player respawn.
- Smooth third-person camera follow.
- HUD with collected and total coins, remaining lives, and a level label.
- Title screen, win and game-over screens, restart, and main-menu buttons.
- Optional audio clip references for coin pickup, obstacle impact, and background music. No audio files are bundled.
- TextMesh Pro UI and URP lighting with soft shadows, ambient occlusion, tone mapping, bloom, and a subtle vignette.
- Phase 1 visual polish: textured grass, richer metallic finishes, a blue ball trail, coin glow rings, hazard highlights, atmospheric sparkles, decorative orbs, and a procedural skybox.

## Technologies

- Unity **6000.6.7f1** (Unity 6)
- C# and Unity Physics (`Rigidbody`, colliders, and trigger callbacks)
- Universal Render Pipeline (URP)
- Unity Input System
- uGUI and TextMesh Pro

## Controls

| Action | Input |
| --- | --- |
| Roll | **WASD** or **Arrow Keys** |
| Start a run | Click **PLAY** on the title screen |

## Install the project

1. Install Unity Hub and Unity Editor **6000.6.7f1**.
2. In Unity Hub, choose **Add project from disk**.
3. Select the `RollABallCoinCollector` folder in this repository.
4. Open the project and let Unity resolve the packages listed in `Packages/manifest.json`.

## Run the game

1. Open `Assets/Scenes/RollABallMain.unity`.
2. Press **Play** in the Unity Editor.
3. Click **PLAY** on the title screen, then roll through the course.

The playable scene is already enabled in Build Settings. **RESTART** starts a new run; **MAIN MENU** returns to the title screen.

## Project structure

```text
Roll-a-Ball-Coin-Collector/
├── README.md
├── CONTRIBUTING.md
├── .gitignore
└── RollABallCoinCollector/
    ├── Assets/
    │   ├── Editor/ProjectBootstrap/  # Scene generator and in-place visual upgrade tool
    │   ├── Materials/                # Game and glow materials
    │   ├── Scenes/                   # Main game scene and Unity sample scene
    │   ├── Scripts/                  # Camera, coins, game, hazards, and player
    │   ├── Settings/                 # URP and volume settings
    │   └── TextMesh Pro/             # Font and TMP essentials
    ├── Packages/                     # Package manifest and lock file
    └── ProjectSettings/              # Unity project and build settings
```

## Screenshots

Gameplay screenshots are not included yet. Add captures from the Unity Game view here when available.

## Optional audio setup

Select **Game Manager** in the Hierarchy and assign `AudioClip` assets to **Coin Collect Clip**, **Obstacle Hit Clip**, and **Background Music Clip** on its `GameAudio` component. Leaving these slots empty is supported.

## Scene generator

The Editor setup script is at `Assets/Editor/ProjectBootstrap/RollABallSceneSetup.cs`. Use **Tools > Roll-a-Ball Coin Collector > Generate Playable Scene** to create the course, player, coins, hazards, camera, lighting, and UI. It preserves an existing `RollABallMain.unity`; move or rename that scene before generating a replacement.

To apply or reapply the Phase 1 art pass to the current playable scene, open `Assets/Scenes/RollABallMain.unity` and choose **Tools > Roll-a-Ball Coin Collector > Apply Phase 1 Visual Upgrade**. The pass creates its grass texture and presentation materials inside `Assets` and reuses existing gameplay objects.

See [CONTRIBUTING.md](CONTRIBUTING.md) for Unity asset and repository workflow notes.

## Future improvements

- Add sound effects and a music track.
- Add more courses with different layouts and hazards.
- Add gamepad or touch controls.
- Save a best score between runs.
- Add accessibility and sound settings.
