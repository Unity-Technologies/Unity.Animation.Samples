# Animation Controller Sample

This sample demonstrates how to use a directional input to control the speed and direction of a character.

----
## What does it show?

This sample demonstrates how to convert several Clip assets using the conversion pipeline and how to mix them depending on some input. It also shows how you can _bake_ a `UberClipNode` to reduce the number of nodes and improve the performance of your NodeSet. Moreover, a GameObject camera keeps track of the character's Entity to implement a "follow" behaviour.

----
## Converting and Baking from GameObject to Entity prefab

It uses a **Spawner** MonoBehaviour which needs a Rig Prefab (the animated character) and a Graph Prefab (the animation source that drives the rig).

The **Spawner** converts the Rig Prefab which has a **RigComponent** to a Rig Entity prefab with an **Unity.Animation.RigDefinition**.

The **Spawner** also converts the **AnimationControllerGraph** Prefab to convert all embedded Unity assets (UnityEngine.AnimationClip) into DOTS animation asset (Unity.Animation.Clip).

The **AnimationControllerGraph** does have an option call `Bake` to eithey play the complex graph with root motion node extraction and Looping node or bake down all thoses options and nodes into a single clip to improve runtime performance.

To bake all those options at convertion time we do use the **Unity.Animation.UberClipNode.Bake()** methods which create a new world to carry thoses operation and produce a new **Unity.Animation.Clip**

----
## ComponentSystems

The **RigSpawnerSystem** is a ComponentSystem that handles the instantiation of the Rig Entity Prefab.

The **AnimationControllerGraphSystem** is a ComponentSystem that handles 3 operations:

* Creation of the DFG (DataFlowGraph) with:
   * Time node (`TimeCounterNode`);
   * Two mixer nodes for the directions of walking and jogging (`DirectionMixerNode`s);
   * One mixer node to mix between walking and jogging depending on the speed (`MixerNode`);
   * A `RootMotionNode`.

   The graph outputs a buffer of float and also a RigidTransform used to make the Camera follow the character.

* Update of the graph parameter's by sending message to the nodes.
* Destruction of the DFG.

----
## Time Management

Each rig entity has a component **DeltaTime**. This component is updated by the **UpdateAnimationDeltaTime** system. Using a **ComponentNode**, the graph connects the value of the **DeltaTime** component to the converter node **ConvertDeltaTimeToFloatNode**, and this node's output is used as an input for the nodes that require a delta time.
