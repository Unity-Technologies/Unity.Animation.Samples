# Inertial Motion Blending Sample

This sample demonstrates how to use inertial motion blending.

----
## What does it show?

This sample demonstrates inertial motion blending by comparing it with a regular linear blending.

----
## Converting from GameObject to Entity prefab

It uses two rigged characters (game objects with a **RigComponent**).
The setup of the characters is similar to the setup in the [**My FirstAnimation Clip**](../../Introduction/MyFirstAnimationClip/README.md) scene.

One character has an **InertialBlendingGraphComponent**, which contains two clips, and will be converted into an **InertialBlendingGraphSetup**.
A system called **InertialBlendingGraphSystem** takes care of building and updating the animation graph that performs inertial motion blending,
by using an **TransitionByBoolNode**.

The other character is setup in an almost identical way, but the **InertialBlendingGraphComponent** is configured so that the 
**TransitionByBoolNode** uses linear blending (crossfade) instead.

In addition, a **DurationComponent** and a **DurationSystem** take care of setting the duration of the blends with a ui slider.

----
## ComponentSystems

The **InertialBlendingGraphSystem** is a ComponentSystem that handles 3 operations:

* Creation of the DFG (DataFlowGraph) with:
   * Time nodes (`DeltaTimeNode` and `TimeCounterNode`);
   * Two `ClipPlayerNode`s to sample the clips that will be blended
   * A `TransitionByBool` that blends the result of the two `ClipPlayerNode`s.
   * An `EntityNode` to write the results to the ECS system.
* Destruction of the DFG.

The **BlendTriggerSystem** takes care of triggering a blend when the "space" key is pressed.

The **DurationSystem** reads the duration from the **DurationComponent** each frame, and updates both graphs with it.

----
## Time Management

Each rig entity has a component **DeltaTime**. This component is updated by the **UpdateAnimationDeltaTime** system. Using a **ComponentNode**, the graph connects the value of the **DeltaTime** component to the converter node **ConvertDeltaTimeToFloatNode**, and this node's output is used as an input for the nodes that require a delta time.

