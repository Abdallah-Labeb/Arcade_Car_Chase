# Arcade Car Chase

A simple arcade-style car escape game built with Unity. Drive your car through obstacles and try to reach the finish line before the police catch you.

## Gameplay

- You control a car driving forward on a straight track filled with obstacles (speed bumps, barriers).
- A police car chases you from behind with AI-driven pursuit, catch-up boost, and auto-recovery.
- If the police car reaches you, you lose.
- If your car flips on the ground for too long, you crash.
- If you reach the finish line, you win.
- You can perform aerial tricks -- flipping in the air is allowed and won't cause a crash.

## Controls

| Key         | Action                        |
|-------------|-------------------------------|
| W / Up      | Accelerate                    |
| S / Down    | Reverse / Brake               |
| A / Left    | Steer left (ground) / Roll left (air) |
| D / Right   | Steer right (ground) / Roll right (air) |
| Space       | Handbrake                     |
| R           | Restart                       |

## Project Structure

```
Assets/
  Script/
    CarController.cs      - Player and AI car physics, input, air control
    CarChaseAI.cs          - Police AI: chase logic, recovery, track clamping
    GameManager.cs         - Game flow, UI (start screen, speed HUD, game over)
    FlipDetector.cs        - Detects car flip/fall and triggers game over
    FinishLine.cs          - Win trigger at end of track
    SpeedBumpTrigger.cs    - Obstacle that slows and bumps the car
    SmoothFollow.cs        - Third-person camera follow with flip-safe yaw
  Fonts/
    Bangers-Regular.ttf    - Arcade-style display font
    Bangers SDF.asset      - TextMeshPro font asset
  Scenes/
    Level.unity            - Main game scene
  kenney_car-kit/          - Car models (CC0 license, by Kenney.nl)
```

## Requirements

- Unity 6 (6000.x) or later
- TextMeshPro (included with Unity)

## Setup

1. Clone this repository.
2. Open the project folder in Unity Hub.
3. Open `Assets/Scenes/Level.unity`.
4. Press Play.

## Credits

- Car models: [Kenney Car Kit](https://kenney.nl/assets/car-kit) (CC0 1.0 Universal)
- Font: [Bangers](https://fonts.google.com/specimen/Bangers) (OFL)
