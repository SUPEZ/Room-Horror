# Room-Horror Project - Copilot Instructions

## Project Overview

This is a Unity3D **horror/adventure game project** built for **PC (Windows)** platform using the **TopDown Engine** (MoreMountains) as the core gameplay framework with **procedural level generation**.

### Core Technologies
- **Unity3D** (Universal Render Pipeline - URP)
- **TopDown Engine** (MoreMountains) - main game mechanics framework
- **Zenject** - dependency injection
- **New Input System** - input handling
- **Unity 2D Lighting** - standard Unity lighting system
- **Procedural Level Generation** - Warehouse-based level builder

### Target Platform
- **PC (Windows)** - full .NET API available
- Can use: async/await, Threading, File I/O, native plugins

---

## Project Structure

```
Assets/
├── Scripts/               # Main game scripts
│   ├── Camera/            # Camera management system
│   │   ├── CameraManager.cs         # Main camera manager (singleton, mode switching)
│   │   ├── CameraModeSwitcher.cs    # Legacy mode switcher
│   │   └── FreeCameraController.cs  # Free camera movement controller
│   ├── Environment/       # Environment interaction scripts
│   │   └── CameraAngleObserver.cs   # Camera angle based visibility
│   ├── General/           # General purpose utilities
│   │   └── RotateTowardsCamera.cs   # Billboard/rotate towards camera
│   ├── LevelGeneration/   # Procedural level generation
│   │   └── OrientationSwitcher.cs   # Object visibility based on camera angle
│   └── NavigationContext/ # Navigation system
├── Prefabs/               # Game prefabs
│   ├── MainCharacter.prefab         # Player character
│   ├── CrateCharacter.prefab        # Crate interactable
│   └── LevelGeneration/Warehouse/   # Warehouse level pieces (Floor, Wall, Doorway, SideWall)
├── Art/                   # Art assets (textures, sprites, materials)
├── Scenes/                # Game scenes
│   └── MainHideoutScene3D.unity     # Main game scene
└── LevelGeneration/       # Level generation configs
    ├── Warehouse.asset              # Warehouse level configuration
    └── WareHouseBuildPlanner.asset  # Build planner for procedural generation
```

---

## Coding Standards

### Language & Comments
- **Comments in English only**
- Use XML documentation for public APIs (`/// <summary>`)
- Keep comments concise and meaningful

### Code Style
```csharp
// Preferred naming conventions
public class MyClassName : MonoBehaviour      // PascalCase for classes
private float _privateField;                  // Underscore prefix for private fields
public float PublicProperty { get; set; }     // PascalCase for properties
private void HandleSomeAction()               // PascalCase for methods
const float MAX_SPEED = 10f;                  // UPPER_SNAKE_CASE for constants
```

### Unity Best Practices
- Use `[SerializeField]` instead of public fields where possible
- Cache component references in `Awake()` or `Start()`
- Avoid `Find()` methods in `Update()` - cache references
- Use object pooling for frequently spawned objects
- Prefer `TryGetComponent<T>()` over `GetComponent<T>()`

### Performance (PC Optimization)
- Minimize garbage collection (avoid allocations in Update)
- Use `StringBuilder` for string concatenation
- Use object pooling for frequently spawned objects
- Prefer `TryGetComponent<T>()` over `GetComponent<T>()`
- Cache component references in `Awake()` or `Start()`
- Avoid `Find()` methods in `Update()` - cache references
- Consider async/await for I/O operations
- Use Threading for heavy computations (outside main thread)

---

## Key Systems

### Camera System
The project uses a dual-mode camera system:
1. **Free Camera Mode** - Navigate level freely (WASD/Mouse drag)
2. **Character Follow Mode** - Standard TopDown Engine camera following player

Switch modes with **F4** key.

```csharp
// Access camera manager
CameraManager.Instance.SetMode(CameraManager.CameraMode.FreeCamera);
CameraManager.Instance.ToggleMode();
```

### TopDown Engine Integration
- Uses `LevelManager` for player spawning and level control
- Uses `MMCameraEvent` for camera control events
- Character abilities extend `CharacterAbility`

```csharp
// Example: Access player
if (LevelManager.HasInstance && LevelManager.Instance.Players.Count > 0)
{
    var player = LevelManager.Instance.Players[0];
}
```

### Orientation/Angle Based Visibility
`OrientationSwitcher` - Shows/hides objects based on camera viewing angle:
- Same direction (0° ± tolerance) → Show object A
- Opposite direction (180° ± tolerance) → Show object B  
- Perpendicular (90° ± tolerance) → Show object C

---

## Level Generation System

The project uses procedural level generation based on **Warehouse** prefabs:

### Components:
- **Warehouse.asset** - Level configuration
- **WareHouseBuildPlanner.asset** - Build planner for procedural generation
- **Prefabs/LevelGeneration/Warehouse/** - Modular pieces:
  - Floor pieces
  - Wall segments
  - Doorway connectors
  - SideWall elements

### Working with Level Generation:
```csharp
// Access level generation systems through appropriate managers
// Follow existing patterns in LevelGeneration/ folder
```

---

## Common Tasks

### Adding New Scripts
1. Place in appropriate `Assets/Scripts/` subfolder
2. Use namespace if domain-specific (e.g., `namespace LevelGeneration`)
3. Add XML documentation for public members
4. Follow Unity best practices for PC platform

### Working with Camera
- Use `CameraManager` singleton for camera control
- Don't directly manipulate `Camera.main.transform` in Update
- Use events (`MMCameraEvent`) for camera state changes

### Character Abilities
- Extend `CharacterAbility` from TopDown Engine
- Override `ProcessAbility()` for per-frame logic
- Use `AbilityPermitted` checks

### Level Generation
- Warehouse prefabs are located in `Prefabs/LevelGeneration/Warehouse/`
- Configuration files in `Assets/LevelGeneration/`
- Follow modular design for new level pieces

---

## Documentation

Create `.md` files for analysis and prompts in the `md/` folder at project root:
```
Room-Horror/
└── md/
    ├── architecture.md
    ├── camera-system.md
    ├── level-generation.md
    └── prompt-analysis.md
```

---

## Testing Checklist

Before submitting code:
- [ ] No compilation errors
- [ ] Works in Editor Play mode
- [ ] Comments in English
- [ ] Proper null checks
- [ ] No heavy allocations in Update loops
- [ ] Performance optimized for PC platform

