# Scorpion

----
## What does it show?

This sample shows how to use write handles to copy the data computed from an animation clip to the LocalToWorld component of entities.
The model is a hierarchy of transforms and uses mesh renderers, not a skinned mesh.

----
## Overview

The entry point to this sample is the root GameObject in the subscene. There are two important Unity components attached to this GameObject:

 1. The **RigComponent** component (from the **Unity.Animation** package) which defines the rig for the object. During entity conversion, it is converted to a **Rig** ECS component.
 2. The **ClipPlayer** component. This allows you to choose an animation clip to play on the rig. It is converted to a **PlayClipComponent** ECS component.

 Moreover, every Transform in the Scorpio GameObject hierarchy has a **PostAnimationGraphWriteTransformHandle** component. During the **RigComponent** conversion, each transform that has this handle will get converted to an Entity, and a **PostAnimationGraphSystem.WriteTransformHandle** ECS BufferElement referencing the transform Entity will be added to the rig Entity.
 The **PostAnimationGraphSystem.WriteTransformHandle** contains an Entity and an index that are used in the **PostAnimationGraphSystem** to copy the rig data at the specified index into the **LocalToWorld** component of the target Entity.

 The **PlayClipSystem** adds and connects the nodes necessary to compute an animation clip on the rig in the **PreAnimationGraphSystem** NodeSet. A ComponentNode is used to get the **AnimatedData** result in the rig Entity.


----
## The Animation Graph

The animation graph is using the *Data Flow Graph* (DFG) to compute an animation. The animation graph is the central concept that drives the animation.

When an animated entity is added, the **Scorpion_PlayClipSystem** creates a **ClipPlayerNode** and a **ComponentNode** (associated with this entity). It sets up parameters (like the clip to be played) on the **ClipPlayerNode** by sending it *messages*. It also connects the output of the **ClipPlayerNode** to the input of the **ComponentNode**. This will set the data of the **AnimatedData** buffer on the entity.
When a **Scorpion_PlayClipComponent** changes, the **Scorpion_PlayClipSystem ** sends another message to the graph to update the clip in use.

The **AnimationGraphSystem** from the **Unity.Animation** package automatically updates the graphs and applies the output to the entity.

When the **Scorpion_PlayClipComponent** is removed from an entity, the **Scorpion_PlayClipSystem** needs to release the nodes created for this instance to avoid a leak. This is why **Scorpion_PlayClipStateComponent** exists.

## Time Management

Each rig entity has a component **DeltaTime**. This component is updated by the **UpdateAnimationDeltaTime** system. Using a **ComponentNode**, the graph connects the value of the **DeltaTime** component to the converter node **ConvertDeltaTimeToFloatNode**, and the output of the node is used as an input to evaluate the clip.
