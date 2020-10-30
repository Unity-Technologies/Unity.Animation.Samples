# Synchronize Motion

----
## What does it show?

This sample showcases how to synchronize clips using `Synchronized Motion` to blend them in a continous manner. 

----
## Synchronize clips

Synchronize clips are animation clips that express the same kind of motions. Examples of similar motions could be various walk, jog and run animations. 

In order for the blend to work well, the movements in the clips must take place at the same points in normalized time. 
For example, walking and running animations can be aligned so that the moments of contact of feet on the floor take place at the same points in normalized time (e.g. the left foot hits at 0.0 and the right foot at 0.5). 
Since normalized time is used, it doesn’t matter if the clips are of different lengths.

----
## Overview

The entry point to this sample is the root GameObject in the subscene. There are three important Unity components attached to this GameObject:

 1. The **RigComponent** component (from the **Unity.Animation** package) which defines the rig for the object. During entity conversion, it is converted to a **Rig** ECS component.
 2. The **PhaseMatchingAuthoringComponent** component. This allows you to choose animation clips to play on the rig. Clips are converted and injected into the entity has **SampleClip** and **SampleClipDuration** ECS components.
 3. The **SynchronizeMotionAuthoringComponent** component which add a timer **NormalizedTimeComponent** that drives all synchronized clips.
 
The **SynchronizeMotionSystem** system does the following:
 
 * if any **SynchronizeMotionSample** are found, it creates an animation graph, and registers it to the **AnimationGraphSystem** from the **Unity.Animation** package.
 * it keeps track of the generated graphs by attaching a **SynchronizeMotionGraphComponent** to the entity;
 * when all **SampleClip** are deleted or changed, the corresponding graph is deleted or updated.
 * every frame it sends the World.DeltaTime to the **ComputeSynchronizeMotionsNormalizedTimeAndWeightsNode** node

The **AnimationGraphSystem** executes the graphs, and the **AnimatedData** buffers are updated on the entity by the **ComponentNode**.

The **ComputeSynchronizeMotionsNormalizedTimeAndWeightsNode** is a DFG node that computes the normalize time which defines the time the clips are going to be sampled and also the weights of each clip in the blend based on the **SampleClipBlendThreshold**.
