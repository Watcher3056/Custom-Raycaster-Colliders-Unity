# Custom Raycast System for Unity

A cross-platform raycast system for Unity with custom primitive support and spatial acceleration structures. Built with a pure C# core that can run outside Unity environments.

![Demo](https://i.imgur.com/E1I2Rm7.gif)

## Performance

Measured with the included headless harness (.NET 8, Release, 20,000 rays per batch, seeded
random scenes, QuadTree enabled). Absolute numbers vary with hardware:

| Scene | Throughput | Narrow-phase tests / ray | GC allocation / raycast |
|---|---:|---:|---:|
| 100 boxes | ~1,190,000 rays/s | < 0.01 | 0 B |
| 2,000 boxes | ~209,000 rays/s | 0.09 | 0 B |
| 2,000 mixed spheres + boxes | ~144,000 rays/s | 0.46 | 0 B |
| 10,000 boxes | ~58,000 rays/s | 0.36 | 0 B |

Engine design behind the numbers:

- Quadtree with cached per-subtree bounds and near-to-far, early-out traversal — closest-hit
  queries skip whole subtrees that cannot beat the current best hit.
- Zero-allocation query path: `Raycast` produces no garbage at steady state.
- Deterministic `RaycastAll` ordering (distance, then primitive id).

## Testing

`Tests.Core/` is a headless xUnit suite — run `dotnet test Tests.Core`. It covers a
QuadTree==SimpleList differential (including move/remove mutation scripts), geometric
invariants, capsule geometry, the id contract, and zero-allocation assertions.

## Features

- 🌐 **Cross-Platform** - Pure C# core works in Unity and standalone environments
- 🎯 **Custom Primitives** - Box, Sphere and Capsule collision detection
- 📊 **Dual Acceleration** - QuadTree and SimpleList spatial structures  
- ♻️ **Zero-GC raycasts** - closest-hit queries allocate nothing at steady state
- 🔧 **Modular Design** - Separated Core logic and Unity integration layer
- 🧪 **Performance Testing** - Built-in comparison tools with Unity Physics
- ⚡ **Configurable** - Optimizable for different scene sizes

## Why not Unity Physics?

Unlike Unity Physics, this module runs in pure C# environments:
- Dedicated game servers
- Headless simulations
- Non-Unity applications
- Cross-platform game logic

For very small scenes (< 20-30 colliders), disable QuadTree acceleration — a plain list scan
is cheaper than tree traversal.

## Architecture

The system is built with two distinct layers:

### Core Layer (Pure C#)
- `CustomRaycastSystemCore` - Main raycast engine
- `BoxPrimitive`, `SpherePrimitive` & `CapsulePrimitive` - Collision primitives
- `QuadTree` & `SimpleListAccelerationStructure` - Spatial optimization
- Platform-agnostic math utilities

### Unity Layer
- `CustomRaycastSystem` - Unity singleton wrapper
- `CustomBoxCollider` & `CustomSphereCollider` - Unity components
- `CustomHitInfo` - Unity-compatible hit information

## Supported Primitives

### Box Primitive
- **Shape**: Oriented bounding box (OBB)
- **Properties**: Position, Rotation, Size (3D scale)
- **Features**: Full transform support, non-uniform scaling
- **Usage**: Perfect for rectangular objects, platforms, walls

### Sphere Primitive  
- **Shape**: Perfect sphere
- **Properties**: Position, Radius
- **Features**: Uniform scaling only, rotation ignored
- **Usage**: Ideal for projectiles, characters, circular areas

### Capsule Primitive
- **Shape**: Oriented capsule (cylinder with hemispherical caps, local +Y axis)
- **Properties**: Position, Rotation, Size = (radius, totalHeight, radius)
- **Features**: Full transform support; totalHeight is clamped to ≥ 2×radius
- **Usage**: Characters, pills, rounded obstacles

## Quick Setup

1. **Import the System**
   ```
   Assets/Custom Raycast System/
   ├── Core/                    # Platform-agnostic raycast engine
   ├── Unity Layer/             # Unity-specific components
   └── Tests/                   # Demo and testing scripts
   Tests.Core/                  # Headless xUnit test suite (dotnet test Tests.Core)
   ```

2. **Add the Singleton**
   - Create an empty GameObject in your scene
   - Add the `CustomRaycastSystem` component
   - For small scenes: **Disable "Use Quad Tree"** for better performance

3. **Replace Colliders**
   ```csharp
   // Instead of SphereCollider
   gameObject.AddComponent<CustomSphereCollider>();
   
   // Instead of BoxCollider  
   gameObject.AddComponent<CustomBoxCollider>();
   ```
   Capsules are Core-level primitives (no Unity component yet) — register manually:
   ```csharp
   CustomRaycastSystem.Instance.RegisterPrimitive(
       new CapsulePrimitive(0, position, rotation, new Vector3(radius, height, radius)), gameObject);
   ```

## Basic Usage

### Single Raycast
```csharp
Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

if (CustomRaycastSystem.Instance.Raycast(ray, out CustomHitInfo hit, 100f))
{
    Debug.Log($"Hit: {hit.HitGameObject.name} at {hit.HitPoint}");
    Debug.Log($"Distance: {hit.Distance}, Normal: {hit.Normal}");
}
```

### Multiple Raycasts
```csharp
Ray ray = new Ray(transform.position, Vector3.forward);
var hits = CustomRaycastSystem.Instance.RaycastAll(ray, 50f, sortByDistance: true);

foreach (var hit in hits)
{
    Debug.Log($"Hit object: {hit.HitGameObject.name}");
}
```

### Custom Collider Setup
```csharp
// Sphere Collider
var sphereCollider = gameObject.AddComponent<CustomSphereCollider>();
sphereCollider.Radius = 2.5f;

// Box Collider
var boxCollider = gameObject.AddComponent<CustomBoxCollider>();
boxCollider.Size = new Vector3(2f, 1f, 3f);
```

## Demo Scenes

### Interactive Demo
Run the **InteractiveRaycastTest** to see mouse-based raycasting in action:
- Mouse over objects to highlight them
- Objects move in real-time with smooth animations
- Visual feedback with color changes

### Performance Comparison
Use **AutoTest** for performance and accuracy comparison:
- Generates random spheres and boxes
- Compares custom system vs Unity Physics
- Reports timing differences and validates accuracy
- Configurable test parameters

## Configuration

### Acceleration Structures

**QuadTree (For larger scenes with 30+ objects)**
```csharp
[SerializeField] private bool _useQuadTree = true;
[SerializeField] private Vector3 _quadTreeCenter = Vector3.zero;
[SerializeField] private Vector3 _quadTreeSize = new Vector3(1000, 1000, 1000);
[SerializeField] private int _quadTreeCapacity = 4;
```

**SimpleList (Recommended for small scenes)**
```csharp
[SerializeField] private bool _useQuadTree = false;  // Better for <30 objects
```

## API Reference

### CustomRaycastSystem (Unity Layer)
- `Raycast(Ray ray, out CustomHitInfo hitInfo, float maxDistance)` - Single raycast
- `RaycastAll(Ray ray, float maxDistance, bool sortByDistance)` - Multiple raycasts
- `RegisterPrimitive(IPrimitive primitive, GameObject gameObject)` - Manual registration (returns the primitive with its assigned ID)
- `UnregisterPrimitive(int primitiveId)` - Manual removal
- `UpdatePrimitive(int primitiveId, Vector3 position, Quaternion rotation, Vector3 size)` - Move / re-orient / resize a registered primitive

IDs are system-owned: constructor ids are placeholders, `AddPrimitive`/`RegisterPrimitive` assign unique sequential ids.

### CustomHitInfo
- `GameObject HitGameObject` - The hit GameObject (Unity only)
- `Vector3 HitPoint` - World space hit position
- `Vector3 Normal` - Surface normal at hit point
- `float Distance` - Distance from ray origin
- `int PrimitiveID` - Unique primitive identifier

## Cross-Platform Usage (Pure C#)

The core system works independently of Unity for server-side logic:

```csharp
// Create the core system (useQuadTree, worldCenter, worldSize, nodeCapacity)
var coreSystem = new CustomRaycastSystemCore(false, UVector3.zero,
    new UVector3(1000, 1000, 1000), 4);

// Constructor ids are placeholders — AddPrimitive assigns and returns unique ids
var box     = coreSystem.AddPrimitive(new BoxPrimitive(0, position, rotation, size));
var sphere  = coreSystem.AddPrimitive(new SpherePrimitive(0, position, radius));
var capsule = coreSystem.AddPrimitive(
    new CapsulePrimitive(0, position, rotation, new UVector3(radius, height, radius)));

// Raycast
var ray = new CRay(origin, direction, maxDistance);
if (coreSystem.Raycast(ray, out CHitInfo hit, maxDistance))
{
    Console.WriteLine($"Hit primitive {hit.PrimitiveID} at distance {hit.Distance}");
}

// Move / re-orient / resize later, by id
coreSystem.UpdatePrimitive(box.ID, newPosition, newRotation, newSize);
```

## Performance Guidelines

### When to Use QuadTree
- ✅ Large scenes (world size > 100 units)
- ✅ Many objects (30+ colliders)
- ✅ Sparse object distribution
- ✅ Long-range raycasts

### When to Use SimpleList  
- ✅ Small scenes (world size < 100 units)
- ✅ Few objects (< 30 colliders)
- ✅ Dense object clusters
- ✅ Short-range raycasts

### Memory
- Raycasts allocate **0 B** at steady state; `RaycastAll` allocates only the returned list
- Scene storage: ~0.3–0.4 KB per primitive including QuadTree overhead (≈650 KB for 2,000 primitives)

## Requirements

- Unity 2019.4 or later (for Unity layer)
- .NET Standard 2.0 compatible core; no external dependencies
- .NET 8 SDK (optional) — only for running the headless test suite (`Tests.Core/`)

## Use Cases

### Unity Projects
- Prototyping physics systems
- Custom collision detection
- Educational purposes
- Cross-platform game development

### Server Applications
- Dedicated game servers
- Physics simulations
- Pathfinding systems
- Non-Unity game engines

## License

MIT — see [LICENSE](LICENSE). Feel free to use and modify for your projects.

---

**Need help?** Check the test scenes for complete working examples, or examine the InteractiveRaycastTest script for usage patterns.
