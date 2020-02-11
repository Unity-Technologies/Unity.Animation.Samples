# Welcome



## General Structure

All the samples are built upon a similar structure and differ by the rig and animation graph they use.

Each scene contains a Subscene that has a GameObject with the `Spawner` MonoBehaviour script attached to it. This MonoBehaviour implements the `IDeclareReferencedPrefabs` and the `IConvertGameObjectToEntity` interfaces. 

The `IDeclareReferencedPrefabs` is used to get an Entity from the RigPrefab GameObject that the `Spawner` references. The `Spawner` also references a GraphPrefab that inherits from the class `AnimationGraphBase`. During the conversion of the Spawner GameObject, the GraphPrefab functions are called to add an `ISampleSetup` Component to the RigPrefab Entity. Once this entity prefab is built, it is referenced in a RigSpawner Component, that also contains the dimensions of the spawning grid. 

The `RigSpawnerSystem` is a ComponentSystem that takes all the Entities having a RigSpawner Component. First, the Components required for Animation are added to the RigPrefab Entity (AnimatedTranslations, AnimatedRotations, etc) and initialized, using the `RigEntityBuilder`. To work, this needs the RigPrefab to have the `RigComponent` script attached to it. Then the system instantiates the RigPrefab Entity as many times as needed. Finally, the spawner Entity is destroyed.

As said earlier, the GraphPrefab is an `AnimationGraphBase`. This abstract class is used to add a Component inheriting from the `ISampleSetup` interface to the RigPrefab Entity, before it is instantiated by the Spawner. The implementation of the `ISampleSetup` interface should have all the necessary information for setting up a custom DataFlowGraph (DFG) NodeSet. 

The base class `SampleSystemBase` takes care of the creation, update, and destruction of the graph System. Only the `CreateGraph`, `UpdateGraph` and `DestroyGraph` functions specific to your custom DFG graph need to be implemented by your child class. This System will take the Entities having the `ISampleSetup` Component and will setup the DFG Graph with it. Once the DFG graph is initialized, a SystemStateComponent inheriting from `ISampleData` is added to the rig Entity. This `ISampleData` component should keep a reference to the nodes of your DFG graph so that you can update the graph in the `UpdateGraph` function (by sending messages to the ndoes for example), and so that you can destroy the nodes in `DestroyGraph` when you graph Entity is destroyed.

## Input

Some samples are interactive and use the horizontal and/or vertical inputs to modify the Entities data.

To do this, a GameObject with a script inheriting from the `AnimationInputBase` is put in the Scene (not the SubScene). This object keeps a list of Entities and sets their `ISampleData` Component value on the main thread in the `Update` function. The overriden function `UpdateComponentData` computes the new data used for the Entities Components, and you could use other inputs than Horizontal and Vertical axis if you want.

## Bone Rendering

To use the bone rendering, the RigPrefab in the `Spawner` must have the script `BoneRendererComponent` attached to it. The `BoneRendererConversion` System will convert it. In addition to the rig Entity, the `BoneRendererEntityBuilder` will create two Entities: one with the data required to compute the world matrices of the bones, that will have a reference to the rig Entity, a buffer for the bones' world matrices and a size for the bones; the other entity will have components for the instanced rendering, that is the color and shape of the bones, and a reference to the bone renderer data entity.

In the `BoneRendererSystemGroup`, two systems will compute the matrices then and render the bones, each working on the Entities previsouly mentioned:
1) The `BoneRendererMatrixSystem` is a `JobComponentSystem` and as such is executed on multiple threads. This System computes the bones's matrices using the position of the rig's joints and the scale of the bones. It then sets the value of the `BoneWorldMatrix` Component;
2) The `BoneRendererRenderingSystem` is updated after the `BoneRendererMatrixSystem` System and is a `ComponentSystem`. It's necessary to work on the main thread here because the bones are drawn using `UnityEngine.Graphics.DrawMeshInstanced`. The buffer of `BoneWorldMatrix`, computed in the previous system, is memcopied into a `Matrix4x4` array that is then used for GPU instanciation.

Note that currently the bone renderer uses a custom shader to render the bones, Runtime/BoneRenderer/Shaders/BoneRenderer.shader.

## Make your own

To make your own sample:
1) Create a new Scene;
1) Create a GameObject and add the `Spawner` script to it;
1) Create a prefab with the `RigComponent` script. You can also add the `BoneRendererComponent` if you want to render the bones. Reference this component in the RigPrefab field of your Spawner;
1) Create a prefab for your graph. You'll have to implement your own custom graph inheriting from `AnimationGraphBase`, add it to your prefab, and then reference your prefab in the GraphPrefab field of your Spawner;
1) Implement your own graph system. You'll need two structs implementing the `ISampleSetup` and `ISampleData` interfaces respectively, and a class inheriting from the `SampleSystemBase` class. You'll have to implement the construction, destruction and update of your graph;
1) Right click on your Spawner GameObject and click _New SubScene From Selection_;
1) If you want to interact with your entities, you'll need to implement a class that inherits from `AnimationInputBase` and put this script on a GameObject in your scene;
1) Done!

# Samples

The samples are independent one from another and are here to illustrate one possible way of implementing some animation features.

* [**Animation Controller**](Assets/Scenes/Examples/AnimationController/README.md) Uses directional input to control the speed and direction of a walking character. Also shows a hybrid implementation of a follow behaviour for the camera.
* [**BlendTree1D**](Assets/Scenes/Examples/BlendTree1D/README.md) Converts a 1D blend tree to control the direction of a character running.
* [**BlendTree2DSimpleDirection**](Assets/Scenes/Examples/BlendTree2DSimpleDirection/README.md) Converts a 2D simple directional blend tree to control if a character moves forward/backward and to the left/right.
* [**ClipLoopPlayer**](Assets/Scenes/Examples/ClipLoopPlayer/README.md) Plays a clip in loop.
* [**ConfigurableClip**](Assets/Scenes/Examples/ConfigurableClip/README.md) Evaluates a clip at a certain time. You can configure the clip to use normalized time, to loop the time, to loop the transform and to use root motion. You can go forward and back in time for the evaluation using the right and left keys respectively.
* [**Retarget**](Assets/Scenes/Examples/Retarget/README.md) Maps the transforms of a rig on another rig and plays a clip.
* [**PerformanceSingleClip**](Assets/Scenes/StressTests/PerformanceSingleClip/README.md) Uses single clip to animate a character (using randomized clip speed).
* [**PerformanceTwoClipsAndMixer**](Assets/Scenes/StressTests/PerformanceTwoClipsAndMixer/README.md) Mixes two clips using a binary mixer to animate a character (uses randomized clip speeds and mixer weight).
* [**PerformanceNClipsAndNMixer**](Assets/Scenes/StressTests/PerformanceNClipsAndNMixer/README.md) Mixes N clips using an N-Mixer to animate a character (uses randomized clip speeds and mixer weights).


# Requirements

This version of Animation is compatible with the following versions of the Unity Editor:

* 2019.3 and later (recommended)


