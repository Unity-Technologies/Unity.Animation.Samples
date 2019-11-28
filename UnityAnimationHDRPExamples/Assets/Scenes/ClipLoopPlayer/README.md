# Clip Loop Player Sample

This sample demonstrates a simple clip played with looped time on a character.

## What does it show?

This sample demonstrates how to convert a Unity AnimationClip asset to a DOTS clip asset using the conversion pipeline and how to play a single clip on an animated character.

The time progression is handled by the Unity.Animation.DeltaTimeNode which uses the Time.DeltaTime to increase the current clip time.

The Unity.Animation.ClipPlayerNode in this case is parameterized with an Unity.Animation.ClipConfigurationMask to automatically make the time loop by itself.

## Converting from GameObject to Entity prefab

It uses a **Spawner** MonoBehaviour which needs a Rig Prefab (the animated character) and a Graph Prefab (the animation source that drives the rig).

The **Spawner** converts the Rig Prefab which has a RigComponent to a Rig Entity prefab with an Unity.Animation.RigDefinition.

The **Spawner** also converts the Graph Prefab to convert all embedded Unity assets (UnityEngine.AnimationClip) into DOTS animation asset (Unity.Animation.Clip).

## ComponentSystems

The **RigSpawnerSystem** is a ComponentSystem that handles the instantiation of the Rig Entity Prefab.

The **ClipLoopPlayerGraphSystem** is a ComponentSystem that handles 2 operation:
* Creation of the DFG (DataFlowGraph) with an Unity.Animation.ClipPlayerNode and an Unity.Animation.DeltaTimeNode.
* Destruction of the DFG.

