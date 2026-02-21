# Quick Start Guide - TopDown Strategy

## 🚀 5-Minute Setup

### Step 1: Add Unity Tags (1 min)
1. `Edit → Project Settings → Tags and Layers`
2. Add these tags:
   - `NPC`
   - `Building`
   - `Destructible`

### Step 2: Setup CameraManager (2 min)
1. Find or create GameObject with `CameraManager` component in scene
2. In Inspector, verify:
   - `Main Camera` is assigned
   - `Start Mode` = `FreeCamera`
3. Components auto-added: `FreeCameraStateMachine`, `ManipulatorMode`, `BuilderMode`, `BulldozerMode`

### Step 3: Create Test NPC (1 min)
1. Create empty GameObject, name it "TestNPC"
2. Set Tag to `NPC`
3. Add component: `NPCController`
4. Position in scene

### Step 4: Create Test Building Prefab (1 min)
1. Create Cube (or any 3D object)
2. Set Tag to `Building`
3. Drag to Project → create prefab
4. Assign to `CameraManager → BuilderMode → Building Prefabs`

### Step 5: Test! (30 sec)
1. Press **Play**
2. Press **F4** → Free Camera mode
3. Press **2** → Manipulator mode → Click NPC
4. Press **3** → Builder mode → Place building
5. Press **4** → Bulldozer mode → Destroy objects

✅ **Done!** You're ready to build your strategy game.

---

## 📋 Controls Reference

### Global
- **F4** - Toggle Camera Mode

### Free Camera
- **WASD** - Move camera
- **Middle Mouse Drag** - Pan
- **1** - None mode
- **2** - Manipulator mode
- **3** - Builder mode
- **4** - Bulldozer mode

### Manipulator (State 2)
- **Click** - Select unit
- **Drag** - Box select
- **Shift** - Add to selection

### Builder (State 3)
- **Wheel** - Cycle buildings
- **Click** - Place
- **Right Click** - Rotate

### Bulldozer (State 4)
- **Click** - Destroy
- **Right Click** - Cancel

---

## 🔍 Troubleshooting

### "NPC tag not found" warning
→ Add tag in Project Settings (Step 1)

### Can't select units
→ Configure `ManipulatorMode → Selectable Layer Mask`

### Buildings won't place
→ Assign prefabs to `BuilderMode → Building Prefabs`

### State machine not switching
→ Ensure you're in Free Camera mode (press F4)

---

## 📚 Full Documentation

- **Architecture:** `md/architecture.md`
- **Implementation:** `md/README-implementation.md`
- **Summary:** `md/implementation-summary.md`

---

## 💡 Next Steps

1. **Visuals:** Add outline shader for selection
2. **Movement:** Integrate NavMesh pathfinding
3. **Resources:** Create resource system
4. **UI:** Add state indicators and minimap

---

**Ready to code? Check** `md/architecture.md` **for detailed API documentation!**

