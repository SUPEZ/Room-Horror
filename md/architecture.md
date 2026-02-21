# TopDown Strategy Architecture

## Overview

This document describes the architecture of the TopDown Strategy game built on the TopDown Engine framework.

## Core Game Modes

The game has **two primary camera modes**:

### 1. Character Control Mode
- Direct control of a single NPC character
- Uses TopDown Engine's standard character control system
- Camera follows the controlled character
- Switch to this mode: Press **F4**

### 2. Free Camera Mode (Strategy Mode)
- Navigate freely across the level (WASD + Mouse Drag)
- Access to **State Machine** with 4 sub-states:
  - **None** (1) - Free navigation only
  - **Manipulator** (2) - Select and command NPCs
  - **Builder** (3) - Place buildings and structures
  - **Bulldozer** (4) - Destroy objects and NPCs
- Switch to this mode: Press **F4**

---

## System Architecture

```
CameraManager (Singleton)
├── FreeCameraStateMachine
│   ├── State: None
│   ├── State: Manipulator → ManipulatorMode
│   ├── State: Builder → BuilderMode
│   └── State: Bulldozer → BulldozerMode
└── Character Follow Mode
    └── TopDown Engine Character Control
```

---

## Component Structure

### Camera System

| Component | Purpose | Location |
|-----------|---------|----------|
| `CameraManager.cs` | Main singleton managing camera modes | `Scripts/Camera/` |
| `FreeCameraStateMachine.cs` | State machine for free camera modes | `Scripts/Camera/` |
| `FreeCameraController.cs` | Legacy controller (can be removed) | `Scripts/Camera/` |
| `CameraModeSwitcher.cs` | Legacy switcher (can be removed) | `Scripts/Camera/` |

### Game Modes

| Component | Purpose | Location |
|-----------|---------|----------|
| `ManipulatorMode.cs` | Unit selection & command | `Scripts/GameModes/` |
| `BuilderMode.cs` | Building placement system | `Scripts/GameModes/` |
| `BulldozerMode.cs` | Destruction system | `Scripts/GameModes/` |

### NPC System

| Component | Purpose | Location |
|-----------|---------|----------|
| `NPCController.cs` | NPC state and behavior controller | `Scripts/NPC/` |
| `NPCDecisionSystem.cs` | Decision queue management | `Scripts/NPC/` |
| `NPCDecision.cs` | Base class for decisions | `Scripts/NPC/` |
| `NPCDecisions.cs` | Concrete decision implementations | `Scripts/NPC/` |

---

## Key Systems

### 1. Free Camera State Machine

**File:** `FreeCameraStateMachine.cs`

**States:**
- `None` - No special interaction mode
- `Manipulator` - Unit selection and command mode
- `Builder` - Building placement mode
- `Bulldozer` - Destruction mode

**Input:**
- `1` - Switch to None
- `2` - Switch to Manipulator
- `3` - Switch to Builder
- `4` - Switch to Bulldozer

**Events:**
- `OnStateChanged(previousState, newState)` - Fired when state changes
- `OnStateEnter(state)` - Fired when entering a state
- `OnStateExit(state)` - Fired when exiting a state

---

### 2. Manipulator Mode

**File:** `ManipulatorMode.cs`

**Features:**
- Click to select single unit
- Drag to box-select multiple units
- Hold **Shift** for additive selection
- Visual feedback for selected units

**Future Integration:**
- Assign decisions to selected units
- Formation control
- Group commands

---

### 3. Builder Mode

**File:** `BuilderMode.cs`

**Features:**
- Mouse wheel to cycle through building types
- Number keys (1-9) for quick selection
- Real-time preview with grid snapping
- Visual feedback for valid/invalid placement
- Right-click to rotate building
- Left-click to place

**Validation:**
- Collision checking
- Minimum distance from other buildings
- Custom placement rules

---

### 4. Bulldozer Mode

**File:** `BulldozerMode.cs`

**Features:**
- Hover to target objects
- Click to destroy
- Confirmation required for NPCs (double-click)
- Right-click to cancel
- Red highlight for targets

**Safety:**
- Confirmation for NPC destruction
- Cannot destroy non-destructible objects

---

### 5. NPC Decision System

**Files:** `NPCDecisionSystem.cs`, `NPCDecisions.cs`

**Architecture:**
```
NPCController (on NPC GameObject)
└── NPCDecisionSystem (decision queue manager)
    └── Queue<NPCDecision>
        ├── WorkDecision
        ├── GuardDecision
        ├── BuildDecision
        ├── GatherDecision
        └── MoveDecision
```

**Decision Types (ScriptableObjects):**

| Decision | Purpose |
|----------|---------|
| `WorkDecision` | Perform work at location |
| `GuardDecision` | Patrol/guard area |
| `BuildDecision` | Construct building |
| `GatherDecision` | Collect resources |
| `MoveDecision` | Navigate to position |

**Creating New Decisions:**
1. Create ScriptableObject: `Assets → Create → NPC → Decisions`
2. Configure parameters
3. Assign to NPCs via `ManipulatorMode`

---

## Input Map

### Global
- **F4** - Toggle Camera Mode (Free ↔ Character)

### Free Camera Mode
- **WASD** - Move camera target
- **Middle Mouse Drag** - Pan camera
- **1** - None state
- **2** - Manipulator state
- **3** - Builder state
- **4** - Bulldozer state

### Manipulator State
- **Left Click** - Select unit
- **Left Drag** - Box select units
- **Shift + Click** - Add to selection

### Builder State
- **Mouse Wheel** - Cycle buildings
- **1-9** - Select building by index
- **Left Click** - Place building
- **Right Click** - Rotate building

### Bulldozer State
- **Left Click** - Destroy target
- **Right Click** - Cancel

---

## Integration Points

### TopDown Engine
- `LevelManager` - Player spawn, bounds
- `MMCameraEvent` - Camera control events
- `CharacterAbility` - Character abilities

### Zenject (DI)
- Not currently used extensively
- Can be integrated for:
  - Global event bus
  - Service locator pattern
  - NPC decision factory

### Procedural Generation
- Warehouse system (`LevelGeneration/`)
- Can be extended for:
  - Dynamic building placement
  - Level streaming
  - Resource node generation

---

## Future Enhancements

### Short-term
1. Visual selection feedback (outline shader)
2. Unit health bars and info panels
3. Resource system integration
4. Building construction animations
5. Minimap implementation

### Medium-term
1. Pathfinding integration (A*, NavMesh)
2. Combat system
3. Resource gathering mechanics
4. Tech tree / upgrades
5. Save/Load system

### Long-term
1. Multiplayer support
2. Fog of war
3. AI opponents
4. Campaign mode
5. Level editor

---

## Performance Considerations

### Optimization Targets
- **Selection**: Use spatial partitioning for large unit counts
- **Building Preview**: Object pooling for preview instances
- **Decisions**: Limit queue size per NPC (currently 10)
- **State Machine**: Minimal overhead (event-based)

### PC Platform Benefits
- Can use threading for pathfinding
- Async file I/O for saves
- Full .NET API available

---

## Testing Checklist

### Camera System
- [ ] F4 switches modes correctly
- [ ] Free camera moves with WASD
- [ ] Mouse drag pans camera
- [ ] Bounds checking works

### State Machine
- [ ] Number keys switch states
- [ ] Only one mode active at a time
- [ ] State change events fire correctly

### Manipulator Mode
- [ ] Click selection works
- [ ] Box selection works
- [ ] Shift-selection adds to selection
- [ ] Selection clears properly

### Builder Mode
- [ ] Building preview follows mouse
- [ ] Grid snapping works
- [ ] Collision detection prevents invalid placement
- [ ] Rotation works
- [ ] Placement creates actual building

### Bulldozer Mode
- [ ] Hover highlights targets
- [ ] Click destroys buildings
- [ ] Double-click required for NPCs
- [ ] Right-click cancels

### NPC System
- [ ] Decisions queue properly
- [ ] Decisions execute in order
- [ ] Decision completion works
- [ ] Cancellation works

---

## Known Issues

1. **FreeCameraController.cs** - Duplicates functionality from CameraManager
   - **Solution:** Remove or refactor

2. **CameraModeSwitcher.cs** - Legacy component
   - **Solution:** Remove, functionality moved to CameraManager

3. **Visual Feedback** - No outline/highlight shaders yet
   - **Solution:** Implement outline shader or use particle effects

4. **Pathfinding** - No movement implementation yet
   - **Solution:** Integrate NavMesh or A* pathfinding

---

## Code Examples

### Accessing CameraManager
```csharp
// Switch to free camera mode
CameraManager.Instance.SetMode(CameraManager.CameraMode.FreeCamera);

// Switch to character mode
CameraManager.Instance.SetMode(CameraManager.CameraMode.CharacterFollow);

// Toggle mode
CameraManager.Instance.ToggleMode();
```

### Creating NPC Decision
```csharp
// Create decision asset
[CreateAssetMenu(fileName = "MyDecision", menuName = "NPC/Decisions/MyDecision")]
public class MyDecision : NPCDecision
{
    public override void Execute(NPCController npc)
    {
        // Implementation
    }
    
    public override bool IsComplete(NPCController npc)
    {
        return true; // When done
    }
}
```

### Assigning Decision to NPC
```csharp
NPCController npc = GetComponent<NPCController>();
MyDecision decision = ScriptableObject.CreateInstance<MyDecision>();
npc.AssignDecision(decision, immediate: true);
```

### Listening to State Changes
```csharp
CameraManager.Instance.stateMachine.OnStateChanged += (prev, next) =>
{
    Debug.Log($"State changed from {prev} to {next}");
};
```

