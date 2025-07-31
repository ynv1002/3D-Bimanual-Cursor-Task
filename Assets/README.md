## Unity Version
This project requires Unity version `6000.0.25f1`.

## Main Camera Management
The **Main Camera** object is located within a structured hierarchy:
```
BananaMan
└── Armature
    ...
    └── Head
        └── Main Camera
```
To adjust the visual appearance of your task environment, configure these camera settings in the Inspector:
- **Field of View (FOV)**: Controls the camera's zoom and perspective.
- **Transform Rotation**: Adjusts the camera orientation for optimal viewing angle.

# Input Management

## Manual Input (Closed-loop)
Currently, the game utilizes Unity’s built-in **Input Manager** (`Edit > Project Settings > Input Manager`) to handle manual keyboard controls. Input axes (`LeftArmX`, `LeftArmY`, `LeftArmZ`, etc.) are clearly defined here. 

The **ArmController.cs** script contains the actual use and control of these inputs, the script is attached to the banana man twice (one for each arm) along with an animator component on banana man that allows for unitys built-in IK to work on the banana man avatar via IK targets (`LeftIK_Target` / `RightIK_Target`).

## Automated Input (Open-loop)
Automated control is managed by the **AutoPlayer.cs** script for running trials without manual intervention. To activate automated (open-loop) mode, ensure:

- The `Enable Automation` checkbox on the `AutoPlayer.cs` script within the **AutoPlayer** GameObject is checked.
- The `Use External Input` checkbox on both `ArmController.cs` scripts attached to **BananaMan** is checked.

> Notes:
> - AutoPlayer will only take control when both toggles (`Enable Automation` and `Use External Input`) are active and synced.

## Managing Animator and IK
- **IK Pass** enabled on the Animator tab -> base layer settings.
- Hand movements are directly controlled by IK targets (`LeftIK_Target` / `RightIK_Target` --> Children of `Cursor_L` / `Cursor_R`).
- To switch to joint-based control:
  - Disable IK pass in the Animator tab.
  - Implement joint rotation animations or programmatic joint control.

# Frame Rate Management

## ⚙️ Where It’s Configured

- In Unity, go to:  
  **Edit → Project Settings → Time**  
  Set the value: Fixed Timestep = 0.02
  This ensures that all `FixedUpdate()` methods run exactly 50 times per second.

## What Uses FixedUpdate

- **`ArmController.cs`**: Core movement logic for the IK arms is executed in `FixedUpdate()` to maintain precise and consistent updates.
This is critical to ensure uniform physics and movement behavior, regardless of the display's visual frame rate.

## Note on Frame Rate vs. Physics Rate
The visual frame rate (FPS) is **not capped** and may run faster than 50Hz. However, physics-based movement and input handling stay locked to 50Hz via `FixedUpdate()` and Unity’s Time settings.

If changes are needed:
- Adjust `Fixed Timestep` in the **Time settings**.
- Ensure any logic tied to physical motion remains in `FixedUpdate()`.

## Logging Timelines

- **Trial logs** (`DistanceLogger`) are recorded at the end of each trial — they are **event-based**, not time-based.
- **Posture logs** (`PostureLogger`) are sampled every **0.2 seconds** using a repeating timer. This logging is not synced with `FixedUpdate()` but is suitable for posture tracking.

# Game Parameters & Customization

Parameters adjustable via the **Inspector** of an object:

| Parameter                        | Script File                  | Object/Location        |
|----------------------------------|------------------------------|------------------------|
| **Box Size/Thickness**           | `CubeWireframe.cs`           | CubeForTargets object  |
| **Line Guidance**                | `CursorToTargetLine.cs`      | LineManager object     |
| **Dwell Time + Target colors**   | `TargetManager.cs`           | TargetManager object   |
| **Detection Threshold**          | `TargetBehavior.cs`          | Target objects         |
| **Cursor/Target Size**           | N/A, transfrom of object     | Target objects         |

## Output Data & Logging

### 🎯 Trial Data – `DistanceLogger.cs`
Logs movement metrics for each arm per trial:
- Trial index  
- Input mode (`AutoPlayer` or `Manual`)  
- Arm side (`Left` / `Right`)  
- Time-to-target (trial duration)  
- Direct distance (start to target)  
- Actual path length  
- Path efficiency (actual / direct)  
- Trajectory as a serialized string  

**Usage:**
- Call `DistanceLogger.Initialize()` once at the beginning.  
- Call `DistanceLogger.LogFullTrial(...)` after each trial.  
- Use `DistanceLogger.Close()` to flush and finalize the log file.  
> The logger writes immediately to avoid data loss.

### Posture Data – `PostureLogger.cs`
Logs upper-body joint angles over time:
- Shoulder, elbow, and wrist angles per arm  
- Samples every **0.2 seconds**  
- Uses `Vector3.Angle` between limb segments  
- Written to `posture_log.csv`  

**Notes:**
- No inspector setup is required.  
- Logging stops when the logger object is destroyed.

## Script Interactions

- `TargetManager` reads `isHovered` from both `TargetBehavior` scripts to detect dual-hand overlap.
- When overlap is sustained for the required `holdDuration`, `TargetManager` triggers:
  - Logging via `DistanceLogger`
  - Path recording restart via `ArmController`
  - Repositioning of the targets
- `TargetBehavior` remains lightweight and purely handles distance-based hover detection.
- `CursorToTargetLine` provides visual feedback only — it has no gameplay impact.
- `PostureLogger` uses Unity's `Animator` to read bone transforms; logging continues until the GameObject is destroyed.

# Known Issues & Future Improvements

## Path Efficiency Calculation:
- Currently, path efficiency sometimes exceeds 100%, indicating inaccuracies.
- Needs further debugging; specifically verify path length calculations.