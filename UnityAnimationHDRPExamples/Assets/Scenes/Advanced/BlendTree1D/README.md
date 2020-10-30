# BlendTree1D Sample

This sample demonstrates a simple 1D blend tree System that implements a speed blending with Walk, Jog, and Sprints.

## What does it show?

This sample demonstrates how to convert a Unity Blend tree asset to a DOTS blend tree asset using the conversion pipeline.
Since the three clips don't loop the pose correctly, while converting the blend tree the sample also bakes the three clips to fix the looping.

## Converting from GameObject to Entity prefab

It uses a **Spawner** MonoBehaviour which needs a Rig Prefab (the animated character) and a Graph Prefab (the animation source that drives the rig).

The **Spawner** converts the Rig Prefab which has a RigComponent to a Rig Entity prefab with an Unity.Animation.RigDefinition.

The **Spawner** also converts the Graph Prefab to convert all embedded Unity assets (UnityEngine.BlendTree and UnityEngine.AnimationClip) into DOTS animation asset (Unity.Animation.BlendTree1D and Unity.Animation.Clip).

## ComponentSystems

The **RigSpawnerSystem** is a ComponentSystem that handles the instantiation of the Rig Entity Prefab.

The **BlendTree1DGraphSystem** is a ComponentSystem that handles 3 operations:
* Creation of the DFG (DataFlowGraph) with:
   * A `TimeCounterNode` that accumulates time in its memory;
   * A `TimeLoopNode` that loops the time of the clip (this node has three outputs: one for the number of cycles, one for a normalized time, and one for the looped time which value is in the range [0, clip duration]);
   * A `BlendTree1DNode` that does the blending between the North-East and North-West directions;
   * A `FloatRcpSimNode` that computes the inverse of a float. The value of the output of the `TimeCounterNode` is connected as an input to this node, so that it outputs the speed.
* Update of the graph parameter's by sending message to the Blend Tree node;
* Destruction of the DFG.

----
## Time Management

Each rig entity has a component **DeltaTime**. This component is updated by the **UpdateAnimationDeltaTime** system. Using a **ComponentNode**, the graph connects the value of the **DeltaTime** component to the converter node **ConvertDeltaTimeToFloatNode**, and this node's output is used as an input for the nodes that require a delta time.


