# Two Clips and a Mixer Sample



## What does it show?

This sample demonstrates how to mix two clips before playing the result on an animated character.

## Converting from GameObject to Entity prefab

It uses a **Spawner** MonoBehaviour which needs a Rig Prefab (the animated character) and a Graph Prefab (the animation source that drives the rig).

The **Spawner** converts the Rig Prefab which has a RigComponent to a Rig Entity prefab with an Unity.Animation.RigDefinition.

The **Spawner** also converts the Graph Prefab to convert all embedded Unity assets (UnityEngine.AnimationClip) into DOTS animation asset (Unity.Animation.Clip).

## ComponentSystems

The **RigSpawnerSystem** is a ComponentSystem that handles the instantiation of the Rig Entity Prefab.

The **TwoClipsAndMixerGraphSystem** is a ComponentSystem that handles 3 operations:
* Creation of the DFG (DataFlowGraph) with an Unity.Animation.MixerNode.
* Update of the graph parameter's by sending message to the Mixer node.
* Destruction of the DFG.