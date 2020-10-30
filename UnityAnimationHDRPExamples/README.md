# Welcome

## Disclaimer

These samples are intended to serve as a reference for the `com.unity.animation` package. The package is highly experimental, and should NOT be considered suitable for production. Because it is currently undergoing a great deal of regular changes, these samples are provided as-is, without any expectation of support for the time being.

## General Structure

The samples are split into three folders depending on their level of complexity. The Introduction folder has examples that show some basic setups and graphs. The Advanced examples have more elaborated graphs and animation concepts. The StressTests folder has scenes that test the limits of some of the animation core functions.

## Input

Some samples are interactive and use the horizontal and/or vertical inputs to modify the Entities data.

To do this, a GameObject with a script inheriting from the `AnimationInputBase` is put in the Scene (not the SubScene). This object keeps a list of Entities and sets their `ISampleData` Component value on the main thread in the `Update` function. The overriden function `UpdateComponentData` computes the new data used for the Entities Components, and you could use other inputs than Horizontal and Vertical axis if you want.

## Bone Rendering

To use the bone rendering, the RigPrefab must have the script `BoneRendererComponent` attached to it. The `BoneRendererConversion` System will convert it. In addition to the rig Entity, the `BoneRendererEntityBuilder` will create two Entities: one with the data required to compute the world matrices of the bones, that will have a reference to the rig Entity, a buffer for the bones' world matrices and a size for the bones; the other entity will have components for the instanced rendering, that is the color and shape of the bones, and a reference to the bone renderer data entity.

In the `BoneRendererSystemGroup`, two systems will compute the matrices then and render the bones, each working on the Entities previsouly mentioned:
1) The `BoneRendererMatrixSystem` is a `JobComponentSystem` and as such is executed on multiple threads. This System computes the bones's matrices using the position of the rig's joints and the scale of the bones. It then sets the value of the `BoneWorldMatrix` Component;
2) The `BoneRendererRenderingSystem` is updated after the `BoneRendererMatrixSystem` System and is a `ComponentSystem`. It's necessary to work on the main thread here because the bones are drawn using `UnityEngine.Graphics.DrawMeshInstanced`. The buffer of `BoneWorldMatrix`, computed in the previous system, is memcopied into a `Matrix4x4` array that is then used for GPU instanciation.

Note that currently the bone renderer uses a custom shader to render the bones, Runtime/BoneRenderer/Shaders/BoneRenderer.shader.

# Samples

The samples are independent one from another and are here to illustrate one possible way of implementing some animation features.

## Introduction Samples

* [**Animated Attachment**](Assets/Scenes/Introduction/AnimatedAttachment/README.md) Shows how to attach an animated object to an animated rig.
* [**Animation Curve**](Assets/Scenes/Introduction/AnimationCurve/README.md) Shows how to convert an animation curve and use it to move a cube. 
* [**Blendshapes**](Assets/Scenes/Introduction/Blendshapes/README.md) Shows how to play an animation clip to animate blendshapes.
* [**My FirstAnimation Clip**](Assets/Scenes/Introduction/MyFirstAnimationClip/README.md) Shows how to play an animation clip in a loop on an existing model in a scene. 
* [**Rotating Cube**](Assets/Scenes/Introduction/RotatingCube/README.md) Shows how to play a simple clip on a cube.
* [**Scorpion**](Assets/Scenes/Introduction/Scorpion/README.md) Animates a rig by writing to the Transform components of the entities from a post-animation system, using exposed transforms. No skinned mesh is used.
* [**Socket**](Assets/Scenes/Introduction/Socket/README.md) Shows three ways to "attach" an object entity to a rig entity.

## Advanced Samples

* [**Animation Controller**](Assets/Scenes/Advanced/AnimationController/README.md) Uses directional input to control the speed and direction of a walking character. Also shows a hybrid implementation of a follow behaviour for the camera.
* [**Animation Rig Remap**](Assets/Scenes/Advanced/AnimationRigRemap/README.md) Showcases 3 different ways to remap animation from a source to a destination rig.
* [**BlendTree 1D**](Assets/Scenes/Advanced/BlendTree1D/README.md) Converts a 1D blend tree to control the direction of a character running.
* [**BlendTree 2D Simple Direction**](Assets/Scenes/Advanced/BlendTree2DSimpleDirection/README.md) Converts a 2D simple directional blend tree to control if a character moves forward/backward and to the left/right.
* [**Configurable Clip**](Assets/Scenes/Advanced/ConfigurableClip/README.md) Evaluates a clip at a certain time. You can configure the clip to use normalized time, to loop the time, to loop the transform and to use root motion. You can go forward and back in time for the evaluation using the right and left keys respectively.
* [**Constraints**](Assets/Scenes/Advanced/Constraints/README.md) Shows a character that plays a walking clip, while having a LookAt constraint for his head and a Two Bone IK Constraint for each of his hand. The IK targets are dynamic.
* [**Feather Blend**](Assets/Scenes/Advanced/FeatherBlend/README.md) Shows how to blend two clips with different weights for different channels.
* [**Inertial Motion Blending**](Assets/Scenes/Advanced/InertialMotionBlending/README.md) Shows how to blend two clips using inertial motion blending.
* Phase Matching:
  * [**Synchronize Motion**](Assets/Scenes/Advanced/Phase%20Matching/01%20-%20Synchronize%20Motion/README.md) Shows how to blend between clips that are aligned in their motion.
  * [**Synchronize Tags**](Assets/Scenes/Advanced/Phase%20Matching/02%20-%20Synchronize%20Tags/README.md) Shows how to use tags to blend between clips that don't have synchronized motion.

## Stress Tests

* [**Performance Single Clip**](Assets/Scenes/StressTests/PerformanceSingleClip/README.md) Uses single clip to animate a character (using randomized clip speed).
* [**Performance Two Clips And Mixer**](Assets/Scenes/StressTests/PerformanceTwoClipsAndMixer/README.md) Mixes two clips using a binary mixer to animate a character (uses randomized clip speeds and mixer weight).
* [**Performance N Clips And NMixer**](Assets/Scenes/StressTests/PerformanceNClipsAndNMixer/README.md) Mixes N clips using an N-Mixer to animate a character (uses randomized clip speeds and mixer weights).


# Requirements

This version of Animation is compatible with Unity 2020.1.0b15+.


