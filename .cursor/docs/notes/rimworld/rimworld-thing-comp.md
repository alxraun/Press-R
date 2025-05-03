# Rimworld Thing Comp Research Notes

## Core Concepts

*   **`ThingComp`**: Abstract base class for all components (`Verse/ThingComp.cs`).
    *   Contains virtual methods for lifecycle events (e.g., `Initialize`, `PostSpawnSetup`, `CompTick`, `PostExposeData`) and behavior modification (e.g., `CompGetGizmosExtra`, `TransformLabel`, `CompInspectStringExtra`).
    *   Holds references to `parent` (`ThingWithComps`) and `props` (`CompProperties`).
*   **`ThingWithComps`**: Base class for things that can host components (`Verse/ThingWithComps.cs`).
    *   Inherits from `Thing`.
    *   Manages a `List<ThingComp> comps`.
    *   Overrides `Thing` methods (like `Tick`, `Draw`, `ExposeData`, `GetGizmos`) and calls corresponding methods on its components.
*   **`CompProperties`**: Base class for component settings (`Verse/CompProperties.cs`).
    *   Defined within a `ThingDef`'s `<comps>` list in XML.
    *   Holds configuration data for a component.
    *   Contains a `System.Type compClass` field, pointing to the actual `ThingComp` implementation class.
    *   **Key Virtual Methods**:
        *   `ResolveReferences(ThingDef parentDef)`: Called after all Defs are loaded. Used to resolve string-based Def names (e.g., `<someDef>MyDef</someDef>`) into direct object references (e.g., `this.someDef = DefDatabase<MyDefType>.GetNamed("MyDef")`). Essential for linking Defs efficiently.
        *   `SpecialDisplayStats(StatRequest req)`: Allows the component to add custom entries to the Thing's stats window (opened with 'i'). Returns an `IEnumerable<StatDrawEntry>`. Used by components like `CompPowerTrader` to show power consumption/generation.
        *   `ConfigErrors(ThingDef parentDef)`: Used to validate the properties defined in XML during loading, yielding error strings if issues are found.
*   **`ThingCompUtility`**: Static helper class with extension methods for easily accessing components from a `Thing` (`TryGetComp<T>`, `HasComp<T>`). (`Verse/ThingCompUtility.cs`).

## Lifecycle & Management

1.  **Initialization (`InitializeComps` in `ThingWithComps`)**:
    *   Called automatically in `ThingWithComps.PostMake()` (when creating a new Thing) and `ThingWithComps.ExposeData()` (when loading from save).
    *   Iterates through the `ThingDef.comps` list.
    *   For each `CompProperties`:
        *   Creates an instance of the specified `compClass` using `Activator.CreateInstance`.
        *   Sets the `parent` reference on the new `ThingComp`.
        *   Adds the component to the `ThingWithComps.comps` list.
        *   Calls the component's `Initialize(props)` method, passing the `CompProperties` instance.
2.  **Updates (`Tick`, `TickRare`, `TickLong`)**:
    *   `ThingWithComps` overrides these methods.
    *   It iterates through its `comps` list and calls the corresponding `CompTick`, `CompTickRare`, or `CompTickLong` method on each component.
    *   Components are ticked based on the parent `Thing`'s `TickerType`.
3.  **Saving/Loading (`PostExposeData`)**:
    *   `ThingWithComps` overrides `ExposeData`.
    *   After handling its own data, it calls `PostExposeData()` on each component in its `comps` list.
    *   Components are responsible for saving/loading their own state using the `Scribe` system within their `PostExposeData` method. (e.g., `Scribe_Values.Look(ref this.internalState, "internalState", defaultValue);`).
4.  **Spawning/Despawning**:
    *   `PostSpawnSetup(bool respawningAfterLoad)`: Called after the parent Thing is placed on the map. Good place to find other components (`parent.GetComp<T>()`), register with map systems (like `map.glowGrid`), or initialize state dependent on the map.
    *   `PostDeSpawn(Map map)`: Called when the parent Thing is removed from the map. Used for cleanup, like deregistering from map systems.
5.  **Other Events**: Components can hook into numerous other events by overriding virtual methods in `ThingComp` (See "Key ThingComp Methods" below).

## XML Definition (`ThingDef.comps`)

Components are added to a `ThingDef` within the `<comps>` list:

```xml
<ThingDef ParentName="BuildingBase">
  <defName>ExampleBuilding</defName>
  <!-- ... other properties ... -->
  <comps>
    <!-- Component with specific properties -->
    <li Class="CompProperties_Power">
      <compClass>CompPowerTrader</compClass> <!-- Usually defined in CompProperties_Power, can be overridden -->
      <basePowerConsumption>100</basePowerConsumption>
      <resolvedPowerConsumptionDef>Chemfuel</resolvedPowerConsumptionDef> <!-- Example for ResolveReferences -->
      <!-- ... other CompPowerTrader settings ... -->
    </li>
    <!-- Component using default CompProperties -->
    <li Class="CompProperties_Flickable"/>
    <!-- Custom component -->
    <li Class="MyModNamespace.CompProperties_MyCustomComponent">
        <myCustomSetting>true</myCustomSetting>
        <anotherSetting>10</anotherSetting>
    </li>
  </comps>
</ThingDef>
```

*   Each `<li Class="...">` defines one component.
*   The `Class` attribute specifies the `CompProperties` subclass holding the configuration.
*   XML tags inside `<li>` correspond to public fields in the specified `CompProperties` class. These are loaded via reflection.
*   **Order Matters**: Components are initialized and ticked in the order they appear in the XML list.

## Component Interaction

Components within the *same* `ThingWithComps` can interact in several ways:

1.  **Signals (`BroadcastCompSignal` / `ReceiveCompSignal`)**:
    *   **Mechanism**: A component (or the parent `ThingWithComps`) calls `parent.BroadcastCompSignal("MyCustomSignal")`. This calls `ReceiveCompSignal("MyCustomSignal")` on *all* other components of that *same* Thing, and on the Thing itself if it overrides the method.
    *   **Use Case**: Loose coupling. Good for notifying other components about an event without needing direct references (e.g., `CompFlickable` sends "FlickedOff", `CompPowerTrader` and `CompGlower` react).
2.  **Direct Access (`parent.GetComp<T>()`)**:
    *   **Mechanism**: A component gets a direct reference to another specific component using `T otherComp = parent.GetComp<T>()`. This is typically done once in `PostSpawnSetup`. Then, methods/properties of `otherComp` can be accessed directly.
    *   **Use Case**: Tighter coupling. Needed when one component needs to actively control or query the state of another specific component (e.g., `CompPowerTrader` checking the state of `CompFlickable`). Use `parent.GetComps<T>()` to get all components of a type.
3.  **Via Parent `ThingWithComps`**:
    *   Components always have a reference to their `parent`. They can read public properties or call public methods on the parent if needed.
4.  **Via Global Systems**:
    *   Components can register with and interact via map-wide systems (e.g., `map.glowGrid`, `map.powerNetManager`, `map.componentManager`).
5.  **Via Interfaces**:
    *   Components often implement specific interfaces (e.g., `IVerbOwner`, `IThingHolder`, `IThingGlower`, `IActivity`, `ITargetingSource`) to integrate with various game systems that look for these interfaces on Things or their components.

## Key `ThingComp` Methods (Categorized by Purpose)

Besides the core lifecycle methods, `ThingComp` offers many virtual methods for hooking into game events:

*   **Initialization & Setup**: `Initialize`, `PostPostMake`, `PostSpawnSetup`
*   **Updates**: `CompTick`, `CompTickRare`, `CompTickLong`
*   **Saving/Loading**: `PostExposeData`
*   **Destruction/Despawning**: `PostDeSpawn`, `PostDestroy`, `Notify_Killed`
*   **Drawing & UI**: `PostDraw`, `PostDrawExtraSelectionOverlays`, `PostPrintOnto`, `CompPrintForPowerGrid`, `DrawGUIOverlay`, `CompDrawWornExtras`, `CompRenderNodes`
*   **Gizmos & Menus**: `CompGetGizmosExtra`, `CompGetWornGizmosExtra`, `CompFloatMenuOptions`, `CompMultiSelectFloatMenuOptions`
*   **Information & Descriptions**: `CompInspectStringExtra`, `CompTipStringExtra`, `GetDescriptionPart`
*   **Stats**: `SpecialDisplayStats`, `GetStatFactor`, `GetStatOffset`, `GetStatsExplanation`
*   **Damage Interaction**: `PostPreApplyDamage`, `PostPostApplyDamage`
*   **Item Stacking/Splitting**: `PreAbsorbStack`, `PostSplitOff`, `AllowStackWith`
*   **Pawn Interactions (Equipping/Using)**: `Notify_Equipped`, `Notify_Unequipped`, `Notify_UsedVerb`, `Notify_UsedWeapon`
*   **Pawn Interactions (Social/Health)**: `Notify_WearerDied`, `Notify_Downed`, `Notify_AddBedThoughts`, `Notify_Arrested`, `Notify_PrisonBreakout`, `Notify_KilledPawn`
*   **Trading & Ingestion**: `PrePreTraded`, `PrePostIngested`, `PostIngested`, `PostPostGeneratedForTrader`
*   **Signals & Map Events**: `ReceiveCompSignal`, `Notify_SignalReceived`, `Notify_LordDestroyed`, `Notify_MapRemoved`, `Notify_AbandonedAtTile`
*   **Production & Resources**: `GetAdditionalLeavings`, `GetAdditionalHarvestYield`, `Notify_RecipeProduced`
*   **Visibility**: `Notify_BecameVisible`, `Notify_BecameInvisible`, `Notify_ForcedVisible`
*   **Other Gameplay Hooks**: `CompAllowVerbCast`, `CompPreventClaimingBy`, `CompForceDeconstructable`, `CompGetSpecialApparelScoreOffset`, `Notify_DuplicatedFrom`, `Notify_DefsHotReloaded`, `Notify_Released`

## Component Interaction Diagram (Updated)

```mermaid
graph TD
    ThingDef -- Defines --> CompsListXML[comps List&lt;CompProperties&gt; in XML]
    CompsListXML -- Contains --> CompProps1XML[CompProperties_Power XML]
    CompsListXML -- Contains --> CompProps2XML[CompProperties_Flickable XML]
    CompsListXML -- Contains --> CompPropsNXML[...]

    subgraph ThingWithComps Instance (Runtime)
        TwC(ThingWithComps) -- Has --> CompsInstanceList[comps List&lt;ThingComp&gt;]
        TwC -- Manages Lifecycle --> CompsInstanceList

        CompsInstanceList -- Contains --> CompInstance1[CompPowerTrader]
        CompsInstanceList -- Contains --> CompInstance2[CompFlickable]
        CompsInstanceList -- Contains --> CompInstanceN[...]

        CompInstance1 -- Accesses --> ParentRef1[parent (TwC)]
        CompInstance2 -- Accesses --> ParentRef2[parent (TwC)]
        CompInstanceN -- Accesses --> ParentRefN[parent (TwC)]

        CompInstance1 -- Accesses --> PropsRef1[props (CompProperties_Power)]
        CompInstance2 -- Accesses --> PropsRef2[props (CompProperties_Flickable)]
        CompInstanceN -- Accesses --> PropsRefN[props (...)]
    end

    subgraph Definition Loading
        ThingDef -- Creates --> CompProps1Obj[CompProperties_Power Object]
        ThingDef -- Creates --> CompProps2Obj[CompProperties_Flickable Object]
        ThingDef -- Creates --> CompPropsNObj[...]
        CompProps1XML -- Populates --> CompProps1Obj
        CompProps2XML -- Populates --> CompProps2Obj
        CompPropsNXML -- Populates --> CompPropsNObj
        CompProps1Obj -- Calls --> ResolveRefs1[ResolveReferences()]
        CompProps2Obj -- Calls --> ResolveRefs2[ResolveReferences()]
    end


    subgraph Initialization (PostMake / ExposeData Loading)
       TwC -- Iterates --> ThingDefComps[ThingDef.comps]
       ThingDefComps -- For Each --> CreateInst[Activator.CreateInstance(CompProperties.compClass)]
       CreateInst -- Returns --> CompInstance1
       CreateInst -- Returns --> CompInstance2
       TwC -- Sets parent --> CompInstance1
       TwC -- Sets parent --> CompInstance2
       TwC -- Adds to List --> CompsInstanceList
       TwC -- Calls Initialize --> CompInstance1
       TwC -- Calls Initialize --> CompInstance2
       CompInstance1 -- Receives --> CompProps1Obj
       CompInstance2 -- Receives --> CompProps2Obj
    end


    subgraph Component Interaction Examples
        CompInstance2 -- Sends Signal --> TwC_Broadcast[parent.BroadcastCompSignal("FlickedOff")]
        TwC_Broadcast -- Calls --> CompInstance1_Receive[CompPowerTrader.ReceiveSignal("FlickedOff")]
        TwC_Broadcast -- Calls --> OtherComp_Receive[OtherComp.ReceiveSignal("FlickedOff")]

        CompInstance1 -- Gets Reference --> GetCompCall[parent.GetComp&lt;CompFlickable&gt;()]
        GetCompCall -- Returns --> CompInstance2_Ref[Ref to CompFlickable]
        CompInstance1 -- Uses Reference --> CompInstance2_Ref
    end


    style TwC fill:#f9f,stroke:#333,stroke-width:2px
    style CompsInstanceList fill:#eee,stroke:#aaa,stroke-width:1px,stroke-dasharray: 5 5
    style CompInstance1 fill:#ccf,stroke:#333,stroke-width:1px
    style CompInstance2 fill:#ccf,stroke:#333,stroke-width:1px
    style CompInstanceN fill:#ccf,stroke:#333,stroke-width:1px
    style CompProps1Obj fill:#cfc,stroke:#333,stroke-width:1px
    style CompProps2Obj fill:#cfc,stroke:#333,stroke-width:1px
    style CompPropsNObj fill:#cfc,stroke:#333,stroke-width:1px
```

## Error Handling

*   **Initialization**: `ThingWithComps.InitializeComps` includes a `try-catch` block. If a component fails to instantiate or initialize, an error is logged, and that specific component is skipped, allowing others to load.
*   **Runtime Methods (Tick, etc.)**: There is **no** built-in `try-catch` around calls like `CompTick`, `PostExposeData`, `ReceiveCompSignal` within `ThingWithComps`. An unhandled exception in one component's method can potentially disrupt the processing for subsequent components in the same tick/event and may cause errors at the `Thing` level. Component code should be robust and handle its own potential errors where necessary.

## Related Systems (`HediffComp`, `WorldObjectComp`)

Rimworld uses the same component pattern for other systems:

*   **`HediffWithComps` / `HediffComp`**: Used for adding modular behaviors to health conditions (hediffs). Defined in `HediffDef`. Examples: `HediffComp_SeverityPerDay`, `HediffComp_Immunizable`.
*   **`WorldObject` / `WorldObjectComp`**: Used for adding behaviors to objects on the world map. Defined in `WorldObjectDef`. Examples: `CompTimedMakeVisible`, `CompImportantWorksite`.

The core principles (parent reference, props reference, XML definition, lifecycle methods like `CompTick` and `PostExposeData`) are largely identical.
