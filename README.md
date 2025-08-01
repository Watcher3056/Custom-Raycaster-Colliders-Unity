# Custom Raycast System for Unity

A cross-platform raycast system for Unity with custom primitive support and spatial acceleration structures. Built with a pure C# core that can run outside Unity environments.

![Demo](https://i.imgur.com/E1I2Rm7.gif)

## Features

- 🌐 **Cross-Platform** - Pure C# core works in Unity and standalone environments
- 🎯 **Custom Primitives** - Box and Sphere collision detection
- 📊 **Dual Acceleration** - QuadTree and SimpleList spatial structures  
- 🔧 **Modular Design** - Separated Core logic and Unity integration layer
- 🧪 **Performance Testing** - Built-in comparison tools with Unity Physics
- ⚡ **Configurable** - Optimizable for different scene sizes

## Performance Characteristics

**⚠️ Important Performance Note:**
This system is **10-20x slower** than Unity's built-in Physics system. However, unlike Unity Physics, this module can be used in pure C# environments such as:
- Dedicated game servers
- Headless simulations
- Non-Unity applications
- Cross-platform game logic

For small scenes or when you have few colliders (< 20-30), **disable QuadTree acceleration** for better performance.

## Architecture

The system is built with two distinct layers:

### Core Layer (Pure C#)
- `CustomRaycastSystemCore` - Main raycast engine
- `BoxPrimitive` & `SpherePrimitive` - Collision primitives
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

## Quick Setup

1. **Import the System**
   ```
   Assets/Custom Raycast System/
   ├── Core/                    # Platform-agnostic raycast engine
   ├── Unity Layer/             # Unity-specific components
   └── Tests/                   # Demo and testing scripts
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
- `RegisterPrimitive(IPrimitive primitive, GameObject gameObject)` - Manual registration
- `UnregisterPrimitive(int primitiveId)` - Manual removal

### CustomHitInfo
- `GameObject HitGameObject` - The hit GameObject (Unity only)
- `Vector3 HitPoint` - World space hit position
- `Vector3 Normal` - Surface normal at hit point
- `float Distance` - Distance from ray origin
- `int PrimitiveID` - Unique primitive identifier

## Cross-Platform Usage (Pure C#)

The core system works independently of Unity for server-side logic:

```csharp
// Create core system
var coreSystem = new CustomRaycastSystemCore(
    useQuadTree: false,  // Disable for small scenes
    center: UVector3.zero, 
    size: new UVector3(1000, 1000, 1000), 
    capacity: 4
);

// Add primitives
var box = new BoxPrimitive(0, position, rotation, size);
var sphere = new SpherePrimitive(1, position, radius);
coreSystem.AddPrimitive(box);
coreSystem.AddPrimitive(sphere);

// Perform raycast
var ray = new CRay(origin, direction, maxDistance);
if (coreSystem.Raycast(ray, out CHitInfo hit))
{
    Console.WriteLine($"Hit primitive {hit.PrimitiveID} at distance {hit.Distance}");
}
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

### Memory Usage
- **SimpleList**: ~100 bytes per primitive
- **QuadTree**: ~100 bytes per primitive + tree overhead (~500 bytes per node)
- **Recommendation**: Profile your specific use case

## Requirements

- Unity 2019.4 or later (for Unity layer)
- .NET Standard 2.0 compatible
- No external dependencies

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

This project is open source. Feel free to use and modify for your projects.

---

**Need help?** Check the test scenes for complete working examples, or examine the InteractiveRaycastTest script for usage patterns.
