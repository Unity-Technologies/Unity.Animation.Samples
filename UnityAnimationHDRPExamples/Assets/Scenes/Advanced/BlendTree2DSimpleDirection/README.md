# BlendTree2DSimpleDirection Sample

This sample demonstrates a 2D Simple Directional blend tree System that implements running in any direction.

## What does it show?

This sample demonstrates how to convert a Unity 2D Simple Directional Blend tree asset to a DOTS blend tree asset using the conversion pipeline and how to play it on an animated character.

## Converting from GameObject to Entity prefab

It uses a **Spawner** MonoBehaviour which needs a Rig Prefab (the animated character) and a Graph Prefab (the animation source that drives the rig).

The **Spawner** converts the Rig Prefab which has a RigComponent to a Rig Entity prefab with an Unity.Animation.RigDefinition.

The **Spawner** also converts the Graph Prefab to convert all embedded Unity assets (UnityEngine.BlendTree and UnityEngine.AnimationClip) into DOTS animation asset (Unity.Animation.BlendTree2DNode and Unity.Animation.Clip).

## ComponentSystems

The **RigSpawnerSystem** is a ComponentSystem that handles the instantiation of the Rig Entity Prefab.

The **BlendTree2DGraphSystem** is a ComponentSystem that handles 3 operations:
* Creation of the DFG (DataFlowGraph) with an Unity.Animation.BlendTree2DNode.
* Update of the graph parameter's by sending message to the Blend Tree node.
* Destruction of the DFG.

----
## Time Management

Each rig entity has a component **DeltaTime**. This component is updated by the **UpdateAnimationDeltaTime** system. Using a **ComponentNode**, the graph connects the value of the **DeltaTime** component to the converter node **ConvertDeltaTimeToFloatNode**, and this node's output is used as an input for the nodes that require a delta time.
