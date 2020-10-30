# Synchronize Tags

----
## What does it show?

This sample showcases how to synchronize two clips using `Synchronization Tags` to blend them in a continuous manner. 

----
## Synchronized Tags

Synchronized Tags are time based annotations that you can add on your animation clips.
Each tags has a Type (ex: HumanoidGait, HorseTrotGait, HorseCanterGait, etc...) and a sub state (ex: LeftFootContact, RightFootPassover, RightFootContact, LeftFootPassover).

In order for the blend to work well, both clips must have the same Tag types. 

----
## Overview

The entry point to this sample is the root GameObject in the subscene. There are three important Unity components attached to this GameObject:

 1. The **RigComponent** component (from the **Unity.Animation** package) which defines the rig for the object. During entity conversion, it is converted to a **Rig** ECS component.
 2. The **PhaseMatchingAuthoringComponent** component. This allows you to choose two animation clips to play on the rig. Clips are converted and injected into the entity has **SampleClip** and **SampleClipDuration** ECS components.
 3. The **SynchronizeTagsSampleAuthoringComponent** component which adds a timer **SampleClipTime** for each clip and also defines which tag types you want to blend togheter with **SynchronizeTagsSample**.
 
The **SynchronizeTagsSystem** system does the following:
 
 * if **SynchronizeTagsSample** is found, it creates an animation graph, and registers it to the **AnimationGraphSystem** from the **Unity.Animation** package.
 * it keeps track of the generated graphs by attaching a **SynchronizeMotionGraphComponent** to the entity;
 * when all **SampleClip** are deleted or changed, the corresponding graph is deleted or updated.
 * every frame it sends the World.DeltaTime to the **SynchronizeMotionTimerOnSyncTagsNode** node

The **AnimationGraphSystem** executes the graphs, and the **AnimatedData** buffers are updated on the entity by the **ComponentNode**.

The **SynchronizeMotionTimerOnSyncTagsNode** is a DFG node that does the following:

 * Update the master **SampleClipTimer** based on the current blend weight define by **WeightComponent**.
 * Computes the sync ratio for the master clip based on it's **SampleClipTimer**.
 * Then for each slave clip compute their **SampleClipTimer** based on the sync ratio.
