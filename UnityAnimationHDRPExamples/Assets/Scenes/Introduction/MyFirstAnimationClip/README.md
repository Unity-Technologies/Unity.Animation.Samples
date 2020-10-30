# My First Animation Clip

----
## What does it show?

This sample shows how to play an animation clip in a loop on an existing model in a scene. 

----
## Overview

The entry point to this sample is the root GameObject in the subscene. There are two important Unity components attached to this GameObject:

 1. The **RigComponent** component (from the **Unity.Animation** package) which defines the rig for the object. During entity conversion, it is converted to a **Rig** ECS component.
 2. The **ClipPlayer** component. This allows you to choose an animation clip to play on the rig. It is converted to a **PlayClipComponent** ECS component.

The only part left to understand is the **PlayClipSystem**. The system does the following:
 
 * for every **PlayClipComponent**, it creates an animation graph, and registers it to the **AnimationGraphSystem** from the **Unity.Animation** package.
 * it keeps track of the generated graphs by attaching a **PlayClipStateComponent** to the entity;
 * when a **PlayClipComponent** is deleted or changed, the corresponding graph is deleted or updated.

Note that this system is not doing any of the actual computation involved in performing the animation. The **AnimationGraphSystem** executes the graphs, and the **AnimatedData** buffers are updated on the entity by the **ComponentNode**.

----
## The Animation Graph

The animation graph is using the *Data Flow Graph* (DFG) to compute an animation. The animation graph is the central concept that drives the animation.

When an animated entity is added, the **PlayClipSystem** creates a **ClipPlayerNode** and a **ComponentNode** (associated with this entity). It sets up parameters (like the clip to be played) on the **ClipPlayerNode** by sending it *messages*. It also connects the output of the **ClipPlayerNode** to the input of the **ComponentNode**. This will set the data of the **AnimatedData** buffer on the entity.
When a **PlayClipComponent** changes, the **PlayClipSystem** sends another message to the graph to update the clip in use.

The **AnimationGraphSystem** from the **Unity.Animation** package automatically updates the graphs and applies the output to the entity.

When the **PlayClipComponent** is removed from an entity, the **PlayClipSystem** needs to release the nodes created for this instance to avoid a leak. This is why **PlayClipStateComponent** exists.

## Time Management

Each rig entity has a component **DeltaTime**. This component is updated by the **UpdateAnimationDeltaTime** system. Using a **ComponentNode**, the graph connects the value of the **DeltaTime** component to the converter node **ConvertDeltaTimeToFloatNode**, and the output of the node is used as an input to evaluate the clip.
