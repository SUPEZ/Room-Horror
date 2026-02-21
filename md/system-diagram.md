# System Diagram - TopDown Strategy

## Visual Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                         CAMERA MANAGER                           │
│                         (Singleton)                              │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌──────────────────────┐         ┌─────────────────────────┐  │
│  │   FREE CAMERA MODE   │   F4    │  CHARACTER FOLLOW MODE  │  │
│  │                      │ ◄─────► │                         │  │
│  │  - WASD movement     │         │  - TopDown Engine       │  │
│  │  - Mouse drag        │         │  - Character control    │  │
│  │  - State Machine     │         │  - Standard camera      │  │
│  └──────────┬───────────┘         └─────────────────────────┘  │
│             │                                                    │
│             ▼                                                    │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │          FREE CAMERA STATE MACHINE                        │  │
│  │                                                            │  │
│  │  [1] None  [2] Manipulator  [3] Builder  [4] Bulldozer   │  │
│  └──┬────────────┬────────────────┬──────────────┬──────────┘  │
│     │            │                │              │              │
└─────┼────────────┼────────────────┼──────────────┼──────────────┘
      │            │                │              │
      ▼            ▼                ▼              ▼
   ┌─────┐  ┌────────────┐  ┌────────────┐  ┌──────────┐
   │NONE │  │MANIPULATOR │  │  BUILDER   │  │BULLDOZER │
   └─────┘  └──────┬─────┘  └─────┬──────┘  └────┬─────┘
                   │                │              │
                   │                │              │
         ┌─────────▼────────┐      │       ┌──────▼──────┐
         │ ManipulatorMode  │      │       │BulldozerMode│
         │                  │      │       │             │
         │ • Select units   │      │       │ • Destroy   │
         │ • Box select     │      │       │ • Confirm   │
         │ • Assign tasks   │      │       │ • Highlight │
         └──────────────────┘      │       └─────────────┘
                                   │
                          ┌────────▼────────┐
                          │  BuilderMode    │
                          │                 │
                          │ • Place builds  │
                          │ • Grid snap     │
                          │ • Preview       │
                          │ • Validate      │
                          └─────────────────┘
```

---

## NPC Decision System

```
┌──────────────────────────────────────────────────────────┐
│                    NPC CONTROLLER                         │
│                  (on NPC GameObject)                      │
├──────────────────────────────────────────────────────────┤
│                                                           │
│  ┌────────────────────────────────────────────────────┐  │
│  │          NPC DECISION SYSTEM                       │  │
│  │                                                     │  │
│  │  Current Decision: [WorkDecision]                  │  │
│  │                                                     │  │
│  │  Decision Queue:                                   │  │
│  │  1. [GuardDecision]                                │  │
│  │  2. [MoveDecision]                                 │  │
│  │  3. [GatherDecision]                               │  │
│  │     ...                                            │  │
│  │                                                     │  │
│  │  Max Queue Size: 10                                │  │
│  └────────────────────────────────────────────────────┘  │
│                                                           │
└───────────────────────────────────────────────────────────┘
```

---

## Decision Types (ScriptableObjects)

```
         NPCDecision (abstract base)
                  │
      ┌───────────┼───────────┬───────────┬────────────┐
      │           │           │           │            │
      ▼           ▼           ▼           ▼            ▼
 ┌────────┐ ┌─────────┐ ┌────────┐ ┌─────────┐ ┌─────────┐
 │  Work  │ │  Guard  │ │ Build  │ │ Gather  │ │  Move   │
 │        │ │         │ │        │ │         │ │         │
 │Duration│ │Position │ │Prefab  │ │Resource │ │Target   │
 │Location│ │ Radius  │ │Time    │ │Amount   │ │Position │
 └────────┘ └─────────┘ └────────┘ └─────────┘ └─────────┘
```

---

## State Flow Diagram

```
                    ┌─────────────┐
                    │  Game Start │
                    └──────┬──────┘
                           │
                           ▼
                  ┌────────────────┐
                  │ CameraManager  │
                  │   Singleton    │
                  │   Awake()      │
                  └────────┬───────┘
                           │
              ┌────────────┼────────────┐
              │            │            │
              ▼            ▼            ▼
    ┌─────────────┐ ┌──────────┐ ┌──────────┐
    │Init Camera  │ │ Init SM  │ │Init Modes│
    └─────────────┘ └──────────┘ └──────────┘
                           │
                           ▼
                  ┌────────────────┐
                  │ Wait for Player│
                  │    Spawn       │
                  └────────┬───────┘
                           │
                ┌──────────▼──────────┐
                │   startMode ==      │
                │   FreeCamera?       │
                └──┬───────────────┬──┘
                   │               │
              YES  │               │ NO
                   ▼               ▼
          ┌────────────┐   ┌──────────────┐
          │Enable Free │   │Enable Char   │
          │Camera Mode │   │Follow Mode   │
          └────────────┘   └──────────────┘
                   │
                   ▼
          ┌────────────────┐
          │ State Machine  │
          │   Enabled      │
          └────────┬───────┘
                   │
        ┌──────────┼──────────┬──────────┐
        │          │          │          │
        ▼          ▼          ▼          ▼
    [None]   [Manipulator] [Builder] [Bulldozer]
```

---

## Input Flow

```
┌─────────────────────────────────────────────────────────┐
│                    KEYBOARD INPUT                        │
└───────────┬─────────────────────────────────────────────┘
            │
     ┌──────┴──────┐
     │             │
     ▼             ▼
  ┌────┐     ┌─────────┐
  │ F4 │     │ 1,2,3,4 │
  └─┬──┘     └────┬────┘
    │             │
    │             ▼
    │    ┌───────────────────┐
    │    │ State Machine     │
    │    │ HandleInput()     │
    │    └─────────┬─────────┘
    │              │
    │     ┌────────┼────────┬────────┐
    │     ▼        ▼        ▼        ▼
    │  [None] [Manip] [Build] [Bulldz]
    │
    ▼
┌──────────────────┐
│ CameraManager    │
│ ToggleMode()     │
└────────┬─────────┘
         │
    ┌────┴────┐
    ▼         ▼
 [Free]   [Character]
```

---

## Component Dependencies

```
CameraManager
├── Requires: Camera (Main Camera)
├── Requires: LevelManager (optional, for bounds)
├── Creates: FreeCameraStateMachine
├── Creates: ManipulatorMode
├── Creates: BuilderMode
└── Creates: BulldozerMode

FreeCameraStateMachine
├── Depends: Keyboard (New Input System)
└── Events: OnStateChanged, OnStateEnter, OnStateExit

ManipulatorMode
├── Requires: Camera (for raycasting)
└── Optional: LineRenderer (selection box)

BuilderMode
├── Requires: Camera (for raycasting)
├── Requires: Building Prefabs (assigned)
└── Optional: Preview Materials

BulldozerMode
├── Requires: Camera (for raycasting)
└── Optional: Destruction Effect Prefab

NPCController
├── Auto-creates: NPCDecisionSystem
└── Tag: "NPC"

NPCDecisionSystem
├── Requires: NPCController
└── Manages: Queue<NPCDecision>
```

---

## Execution Order

```
Frame 0 (Awake):
1. CameraManager.Awake()
   ├── InitializeCamera()
   ├── CreateInvisibleTarget()
   ├── UpdateBounds()
   ├── InitializeStateMachine()
   └── InitializeGameModes()

Frame 1 (Start):
1. CameraManager.Start()
   └── StartCoroutine(InitializeAfterPlayerSpawn())
       └── Wait for LevelManager & Player
           └── SetMode(startMode)

Frame N (Update):
1. CameraManager.Update()
   ├── HandleModeSwitch() [Check F4]
   └── HandleFreeCameraInput() [if Free mode]

2. FreeCameraStateMachine.Update()
   └── HandleInput() [Check 1,2,3,4]
       └── SetState()
           └── OnStateChanged event
               └── CameraManager enables appropriate mode

3. Active Mode.Update()
   ├── ManipulatorMode.Update() [if active]
   │   └── HandleSelectionInput()
   ├── BuilderMode.Update() [if active]
   │   ├── HandleBuildingSelection()
   │   ├── UpdatePreviewPosition()
   │   └── HandlePlacement()
   └── BulldozerMode.Update() [if active]
       ├── HandleTargeting()
       └── HandleDestruction()

4. NPCDecisionSystem.Update()
   └── ProcessDecisions()
       ├── Update current decision
       └── Start next queued decision
```

---

## Data Flow: Assigning NPC Task

```
1. User Input
   │
   ▼
2. ManipulatorMode.AssignDecisionToSelected("Work")
   │
   ▼
3. Iterate selected units
   │
   ▼
4. NPCController.AssignDecision(decision)
   │
   ▼
5. NPCDecisionSystem.EnqueueDecision(decision)
   │
   ├── Add to queue
   └── If no current decision → StartNextDecision()
       │
       ▼
6. NPCDecision.Execute(npc)
   │
   ▼
7. NPCDecision.Update(npc) [per frame]
   │
   ▼
8. NPCDecision.IsComplete(npc) [check]
   │
   └── If true → CompleteCurrentDecision()
       └── StartNextDecision() [if queue not empty]
```

---

## Memory Layout

```
Scene Hierarchy:
├── CameraManager (GameObject)
│   ├── CameraManager (Component)
│   ├── FreeCameraStateMachine (Component)
│   ├── ManipulatorMode (Component)
│   ├── BuilderMode (Component)
│   └── BulldozerMode (Component)
│
├── Main Camera
│   └── Camera (Component)
│
├── CameraTarget_Invisible (Runtime Created)
│
├── NPCs (Folder)
│   ├── NPC_1
│   │   ├── NPCController
│   │   └── NPCDecisionSystem
│   └── NPC_2
│       ├── NPCController
│       └── NPCDecisionSystem
│
└── Buildings (Runtime Created)
    ├── Building_1
    └── Building_2
```

---

## Performance Profile

```
Component              | Update Cost | Memory    | Allocation
-----------------------|-------------|-----------|------------
CameraManager          | Low         | ~2 KB     | None
FreeCameraStateMachine | Very Low    | <1 KB     | None
ManipulatorMode        | Medium*     | ~1 KB     | Box select
BuilderMode            | Low         | ~2 KB     | Preview mat
BulldozerMode          | Low         | <1 KB     | None
NPCDecisionSystem      | Very Low    | ~500 B    | None per NPC

* Selection with 100+ NPCs may need optimization (spatial partitioning)
```

---

**Legend:**
- `[ ]` = State/Mode
- `┌─┐` = Component/System
- `│` = Dependency/Flow
- `▼` = Direction of flow
- `*` = Conditional

