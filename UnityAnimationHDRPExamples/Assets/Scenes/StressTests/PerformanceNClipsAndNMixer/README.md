# N Clips and a N-Mixer Performance Sample



## What does it show?

This sample demonstrates how to blend N clips using a N-Mixer before playing the result on an animated character.

## Converting from GameObject to Entity prefab

It uses a **Spawner** MonoBehaviour which needs a Rig Prefab (the animated character) and a Graph Prefab (the animation source that drives the rig).

The **Spawner** converts the Rig Prefab which has a RigComponent to a Rig Entity prefab with an Unity.Animation.RigDefinition.

The **Spawner** also converts the Graph Prefab to convert all embedded Unity assets (UnityEngine.AnimationClip) into DOTS animation asset (Unity.Animation.Clip).

## ComponentSystems

The **RigSpawnerSystem** is a ComponentSystem that handles the instantiation of the Rig Entity Prefab.

The **PerformanceGraphSystem** is a ComponentSystem that handles 2 operations:
* Creation of the DFG (DataFlowGraph) with an Unity.Animation.MixerNode.
* Destruction of DFG data.