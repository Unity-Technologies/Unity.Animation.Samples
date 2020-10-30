# Configurable Clip Sample

This sample demonstrates the use of a configurable clip.

## What does it show?

This sample plays a clip on an animated character. The clip has several parameters: 
- NormalizedTime;
- LoopTime;
- LoopValues;
- CycleRootMotion;
- BankPivot;

## Converting from GameObject to Entity prefab

It uses a **Spawner** MonoBehaviour which needs a Rig Prefab (the animated character) and a Graph Prefab (the animation source that drives the rig).

The **Spawner** converts the Rig Prefab which has a RigComponent to a Rig Entity prefab with an Unity.Animation.RigDefinition.

The **Spawner** also converts the Graph Prefab to convert all embedded Unity assets (UnityEngine.AnimationClip) into DOTS animation asset (Unity.Animation.Clip).

## ComponentSystems

The **RigSpawnerSystem** is a ComponentSystem that handles the instantiation of the Rig Entity Prefab.

The **ConfigurableClipGraphSystem** is a ComponentSystem that handles 3 operations:
* Creation of the DFG (DataFlowGraph) with an Unity.Animation.ConfigurableClipNode and a Unity.Animation.RootMotionNode.
* Update of the graph parameter's by sending message to thoses nodes.
* Destruction of the DFG.

----
## Time Management

Each rig entity has a component **DeltaTime**. This component is updated by the **UpdateAnimationDeltaTime** system. Using a **ComponentNode**, the graph connects the value of the **DeltaTime** component to the converter node **ConvertDeltaTimeToFloatNode**, and this node's output is used as an input for the nodes that require a delta time.
