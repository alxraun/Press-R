## Main Rotation Mechanisms

## Rotation

### 1. Rot4 (Base Rotation)
- **Classes**: `Verse.Rot4`, `Verse.RotationDirection`
- **Directions**: North, East, South, West
- **Conversions**: Rot4 ↔ degrees (`AsAngle`)
- **Orientation**: `IsHorizontal`

### 2. Graphic_RandomRotated (Random Rotation)
- **Class**: `Verse.Graphic_RandomRotated`
- **Parameter**: `maxAngle`
- **Formula**: `-(maxAngle) + (thingIDNumber * 542) % (maxAngle * 2.0)`

### 3. Graphic_Random (Random Graphic)
- **Class**: `Verse.Graphic_Random`
- **Feature**: Selects a random sub-graphic based on ID

### 4. ID-based Rotation
- **Application**: Chunks, rocks, filth, corpses
- **Formula**: `Rand.ValueSeeded(thingIDNumber * multiplier) * 360f`

## Rotation Attributes (GraphicData)

### 1. flipExtraRotation
- **Application**: Chunks, maxAngle = 80

### 2. onGroundRandomRotateAngle
- **Application**: Weapons, apparel, maxAngle = 35

### 3. drawRotated
- **Application**: Various items, rotation flag

### 4. randomizeRotationOnSpawn
- **Application**: Various items, flag on spawn (ThingDef)

## Rotation Overriding

### 1. OverrideGraphicIndex
- **Application**: Special cases, index override (Thing)

### 2. ThingComp.PostDraw
- **Application**: Additional elements (ThingComp)

## Scaling

### 1. drawSize
- **Class**: `Verse.Graphic`
- **Type**: Vector2 (x, y)

### 2. Graphic_Multi
- **Feature**: XY swap when `thing.Rotation.IsHorizontal`

### 3. Specifics
- Medicine: 1.1f
- Corpse: Special handling
- Apparel: Depends on type

## Rendering

### 1. Graphics.DrawMesh
- **Parameters**: mesh, matrix, material

### 2. Matrix4x4.TRS
- **Parameters**: position, rotation (Quaternion), scale (Vector3)

### 3. Material Property Block
- **Classes**: `UnityEngine.MaterialPropertyBlock`, `Verse.Graphic_WithPropertyBlock`

## Special Cases

### 1. Books/Schematics
- **Classes**: `RimWorld.Book`, `RimWorld.CompProperties_Book`

### 2. Weapons
- **Logic**: `ShouldDrawRotated`, `randomizeRotationOnSpawn`

### 3. Apparel
- **Class**: `Verse.PawnRenderNode_Apparel`

### 4. Chunks
- **Attribute**: `flipExtraRotation`, XML rotation

## Rendering & ThingDef

### 1. ThingDef: Hierarchy
- Rendering: Hierarchical, individual.
- `<graphicData>`: XML, rendering properties (`<graphicClass>`, `<drawSize>`).
- `ParentName`: Inheritance.

### 2. GraphicClass & DrawWorker: Differences
- `GraphicClass` (`Graphic_Single`, `Graphic_Random`, `Graphic_Multi`) & `DrawWorker`: Different rendering.
- `DrawWorker`: Specific to `GraphicClass`.
- Attributes (`flipExtraRotation`, `onGroundRandomRotateAngle`): Affect overlays, `GraphicClass`-specific.

### 3. Overlays: Replication
- Goal: Correct overlays = replicate original rendering.
- Consider: `ThingDef`, `GraphicClass`.
- Analyze: `DrawWorker` logic.
- Use: Rotation, scaling attributes.

### 4. Simplicity vs Complexity
- Simple objects: Render → OK.
- Complex objects: Logic, algorithms → complex.
- Overlays for complex objects: Study & reproduce rendering.

## Rendering in RimWorld

### 1. Rendering Pipeline

```ASCII
[DynamicDrawManager] --> [Thing.DynamicDrawPhaseAt] --> [Thing.DrawAt] --> [Graphic.Draw] --> [Graphic.DrawWorker] --> [Graphics.DrawMesh]
```

**Key Methods:**

- **`DynamicDrawManager.DrawDynamicThings()`**: Draws dynamic objects on the map.
- **`Thing.DynamicDrawPhaseAt(DrawPhase phase, Vector3 drawLoc, bool flip)`**: Entry point for `Thing` rendering.
- **`Thing.DrawAt(Vector3 drawLoc, bool flip)`**: Base method for drawing a `Thing` at a position.
- **`Graphic.Draw(Vector3 loc, Rot4 rot, Thing thing, float extraRotation)`**: Prepares parameters and calls `DrawWorker`.
- **`Graphic.DrawWorker(Vector3 loc, Rot4 rot, ThingDef thingDef, Thing thing, float extraRotation)`**: **Main method**: calculates `Matrix4x4` and calls `Graphics.DrawMesh`.
- **`Graphics.DrawMesh(Mesh mesh, Matrix4x4 matrix, Material material, int layer)`**: **Unity method**: Draws a mesh with a matrix and material.

### 2. Rotation Mechanisms

#### 2.1. Rot4 (Base Rotation)

- **Classes**: `Verse.Rot4`, `Verse.RotationDirection`
- **Directions**: North, East, South, West
- **Conversions**: Rot4 ↔ degrees (`AsAngle`), `IsHorizontal` (orientation)

#### 2.2. `Graphic_RandomRotated` (Random Rotation)

- **Class**: `Verse.Graphic_RandomRotated`
- **Parameter**: `maxAngle`
- **Formula**: `angle = -(maxAngle) + (thingIDNumber * 542) % (maxAngle * 2.0) + extraRotation`

#### 2.3. ID-based Rotation

- **Application**: Chunks, rocks, filth, corpses
- **Formula**: `angle = Rand.ValueSeeded(thingIDNumber * multiplier) * 360f`

#### 2.4. Rotation Attributes (GraphicData)

- **`flipExtraRotation`**: Chunks, `maxAngle = 80`
- **`onGroundRandomRotateAngle`**: Weapons, apparel, `maxAngle = 35`
- **`drawRotated`**: Rotation flag for various items
- **`randomizeRotationOnSpawn`**: Random rotation flag on spawn (ThingDef)

### 3. Scaling

#### 3.1. `drawSize`

- **Class**: `Verse.Graphic`
- **Type**: `Vector2(x, y)` - draw size

#### 3.2. `Graphic_Multi`

- **Feature**: XY swap when `thing.Rotation.IsHorizontal`

### 4. Rendering Methods

#### 4.1. `Graphics.DrawMesh`

- **Parameters**: `mesh`, `matrix`, `material`, `layer`, `propertyBlock` (optional)

#### 4.2. `Matrix4x4.TRS`

- **Parameters**: `position`, `rotation (Quaternion)`, `scale (Vector3)`
- **Purpose**: Creates a transformation matrix (position, rotation, scale)

#### 4.3. `MaterialPropertyBlock`

- **Classes**: `UnityEngine.MaterialPropertyBlock`, `Verse.Graphic_WithPropertyBlock`
- **Purpose**: Optimized transfer of parameters to the shader, reduces draw calls.

### 5. Graphic Classes (`GraphicClass`)

```ASCII
[Graphic] (abstract class)
  |
  ├── [Graphic_Single] - Simple graphic (one texture)
  |     ├── [Graphic_Gas] - Gas
  |     ├── [Graphic_Single_AgeSecs] - Time-based animation
  |     └── [Graphic_WithPropertyBlock] - Shader properties
  |
  ├── [Graphic_Multi] - Multi-directional graphic (4 directions)
  |     ├── [Graphic_Multi_AgeSecs] - Time-based animation
  |     └── [Graphic_Multi_BuildingWorking] - Working state
  |
  ├── [Graphic_RandomRotated] - Random rotation
  ├── [Graphic_Random] - Random texture
  ├── [Graphic_Cluster] - Cluster (filth)
  └── [Graphic_Indexed] - Indexed graphic
        └── [Graphic_Indexed_SquashNStretch] - Shape animation
```

### 6. `ThingDef` Rendering Parameters (`graphicData`)

- **`<graphicData.drawSize>`**: Base size (`Vector2`).
- **`<graphicData.drawRotated>`**: Rotation flag (bool).
- **`<graphicData.graphicClass>`**: Graphic type (`GraphicClass`).
- **`<flipExtraRotation>`**: Additional rotation on flip (chunks, bool).
- **`<randomizeRotationOnSpawn>`**: Random rotation on spawn (bool).
- **`<onGroundRandomRotateAngle>`**: Random rotation on ground (float, maxAngle).

### 7. Rendering Optimization

#### 7.1. `MaterialPropertyBlock`

- **Advantages**: Reduces draw calls, material switches.
- **Usage**: `Graphic_WithPropertyBlock`, `Graphic_Multi_AgeSecs`, `Graphic_Mote`, `PawnOverlayDrawer`.

#### 7.2. `DrawBatch`

- **Purpose**: Groups draw calls for batching.
- **Mechanism**: Groups by `mesh + material + layer + renderInstanced`, uses `DrawMeshInstanced`.

#### 7.3. `DynamicDrawManager`

- **Purpose**: Efficiently manages rendering of many objects.
- **Optimizations**:
    - **Culling**: Omitting invisible objects.
    - **Multithreading**: Parallel processing.
    - **`ThingCullDetails`**: Caching visibility data.
    - **`DrawPhases`**: Dividing the rendering process into phases.

### 8. Special Rendering Cases

#### 8.1. Items in Stacks (`Graphic_StackCount`)

```ASCII
[ThingWithComps] --- [stacks] --> [Graphic_StackCount]
    |                                  |
    |                                  v
    +---- [stackCount] ------------> [SubGraphicFor]
```

- **Logic**: Texture changes based on `thing.stackCount`.

#### 8.2. Carried Items

- **Rendering**: `PawnRenderNodeWorker_Carried.PostDraw` -> `PawnRenderUtility.DrawCarriedThing`.
- **Feature**: Special transformation of `position`, `rotation`.

#### 8.3. Items on Shelves/Racks

- **Rotation**: Fixed, determined by `thingDef.rotateInShelves` (-90f or 0f).
- **Detection**: Checks for placement on a shelf/rack.

#### 8.4. Items with `ThingComp`

- **Rendering**: `ThingComp.PostDraw()` - additional visual effects after main rendering.
- **Problem**: Effects are not always reflected in the main matrix.

#### 8.5. Multiple Items in One Cell

- **Scaling**: Size reduction (`size *= 0.8f`) when `thing.MultipleItemsPerCellDrawn()`.

### 9. Static Rendering (`Thing.Print`)

#### 9.1. `Draw` vs `Print`

```ASCII
[Thing.Draw] - Dynamic objects (each frame)
   |
   v
[Thing.Print] - Static objects (during SectionLayer build, cached)
   |
   v
[Graphic.Print] - Rendering to a static mesh
```

#### 9.2. `Graphic.Print` Logic

- **Method**: `Printer_Plane.PrintPlane(layer, center, size, material, rot, flipUv, uvs, colors)`.
- **Class**: `SectionLayer_Things` - creates static meshes for objects on the map.

### 10. Harmony Patch Points

1. **`Thing.DynamicDrawPhaseAt`**: Intercept the entire `Thing` rendering process.
2. **`Graphic.DrawWorker`**: Intercept before `Matrix4x4` creation.
3. **`Graphics.DrawMesh`**: Intercept after `Matrix4x4` creation, before drawing.
4. **`ThingComp.PostDraw`**: Intercept for additional `ThingComp` effects.

### 11. Render Data Capture Strategies

1. **Harmony Prefix/Postfix for `Matrix4x4`**: Capture draw parameters.
2. **Caching by `Thing.thingIDNumber`**: Save rendering data.
3. **Reconstruct `DrawWorker` Logic**: Repeat rotation/scale calculations.
4. **Access `Thing.Graphic`**: Get `drawSize`, `rotation`, etc.
5. **`[ThreadStatic]`**: Track the current `Thing` in multithreaded rendering.
6. **Intercept `Graphics.DrawMesh`**: Capture `matrix`, `mesh`, `material`, `propertyBlock`.
7. **Extended Cache Structure**: Account for shader properties (`MaterialPropertyBlock`).

## Reliable Intercept Points for Rendering ThingCategory.Item

### Context Loss Problem

During the rendering process of RimWorld objects, there's a "context loss" problem - by the time `Graphics.DrawMesh` is called, we lose the direct reference to the `Thing` object being rendered. This complicates creating systems for applying effects to specific objects.

### Call Chains and Context Loss Points

```ascii
[Dynamic Objects]
Thing.DynamicDrawPhaseAt → Thing.DrawAt → Graphic.Draw → Graphic.DrawWorker → [Thing Available] → Graphic.DrawMeshInt → [Thing Lost] → Graphics.DrawMesh

[Static Objects]
Thing.Print → Graphic.Print → [Thing Available] → Printer_Plane.PrintPlane/Printer_Mesh.PrintMesh → LayerSubMesh → [Thing Lost] → SectionLayer.DrawLayer → Graphics.DrawMesh
```

### Solution: System.Runtime.CompilerServices.CallContext

There's a more reliable solution than `[ThreadStatic]`, especially in asynchronous contexts - using `CallContext` from System.Runtime.CompilerServices.

### Rendering Intercept

**Goal**: Capture `Thing` context in `Graphics.DrawMesh` for dynamic & static objects.

**Patches**:

*   **Dynamic Objects**: `Graphic.DrawWorker`
    *   `Prefix`: `TrackThing(thing)`
    *   `Postfix`: `ClearThing()`

*   **Static Objects**: `Graphic.Print`
    *   `Prefix`: `TrackThing(thing)`
    *   `Postfix`: `ClearThing()`

*   **`Graphics.DrawMesh` Intercept**:
    *   `Prefix`:
        *   `Thing thing = CurrentThingDrawingTracker.GetCurrentThing()`
        *   If `IsValidItem(thing)`:
            *   `CurrentDrawnThings ??= new Dictionary<Thing, RenderData>()`
            *   `CurrentDrawnThings[thing] = new RenderData(mesh, matrix, material)`

**Helpers**:

*   `CurrentThingDrawingTracker`: Tracks current `Thing` being drawn.
*   `TrackThing(Thing thing)`: Sets the current thing if `IsValidItem(thing)`.
*   `ClearThing()`: Clears the currently tracked thing.
*   `IsValidItem(Thing thing)`: Checks if thing is not null and `thing.def.category == Item`.
*   `RenderData`: Holds mesh, matrix, and material.

### ThingCategory.Item: Special Cases

*   **Stacks**: `Graphic_StackCount`, special rendering.
*   **Carried**: `PawnRenderNodeWorker_Carried`.
*   **Racks/Shelves**: Different rendering, rotation.
*   **Components**: `ThingComp.PostDraw`.

**Patch for 100% Coverage:**

*   `Graphic.DrawWorker`
*   `Graphic.Print`
*   `PawnRenderNodeWorker_Carried.PostDraw`
