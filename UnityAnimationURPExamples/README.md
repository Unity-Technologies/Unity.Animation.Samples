# URP Samples

The samples are independent one from another and are here to illustrate one possible way of implementing some animation features.

## Disclaimer

These samples are intended to serve as a reference for the `com.unity.animation` package. The package is highly experimental, and should NOT be considered suitable for production. Because it is currently undergoing a great deal of regular changes, these samples are provided as-is, without any expectation of support for the time being.

## Introduction Samples

* [**Blendshapes**](Assets/Scenes/Introduction/Blendshapes/README.md) Shows how to play an animation clip to animate blendshapes.
* [**My FirstAnimation Clip**](Assets/Scenes/Introduction/MyFirstAnimationClip/README.md) Shows how to play an animation clip in a loop on an existing model in a scene. 
* [**Rotating Cube**](Assets/Scenes/Introduction/RotatingCube/README.md) Shows how to play a simple clip on a cube.
* [**Scorpion**](Assets/Scenes/Introduction/Scorpion/README.md) Animates a rig by writing to the Transform components of the entities from a post-animation system, using exposed transforms. No skinned mesh is used.

## Stress Tests

* [**Performance N Clips And NMixer**](Assets/Scenes/StressTests/PerformanceNClipsAndNMixer/README.md) Mixes N clips using an N-Mixer to animate a character (uses randomized clip speeds and mixer weights).

## What about the other samples present in HDRP?

For now, we have no plans on porting those samples to URP. 

However, if you wish to, you can convert them by taking the assets and scripts from HDRP; afterwards you'll need to change the material of your skinned mesh to `SimpleVertexSkinning` (if you have blendshapes, you'll have to use `SimpleComputeDeformation`) and update the lighting settings, and you should be good to go.

# Requirements

This version of Animation is compatible with Unity 2020.1.0b15+.