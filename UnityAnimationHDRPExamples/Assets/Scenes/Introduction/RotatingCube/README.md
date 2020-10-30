# Rotating Cube Sample

----
## What does it show?

This sample shows how to play an animation clip in a loop on a cube.

----
## Overview

The entry point to this sample is the Cube GameObject in the scene. There are three important Unity components attached to it:

 1. The **RigComponent** component (from the **Unity.Animation** package) which defines the rig for the object. During entity conversion, it is converted to a **Rig** ECS component.
 2. The **RotatingCube_ClipPlayer** component. This allows you to choose an animation clip to play on the rig. It is converted to a **RotatingCube_PlayClipComponent** ECS component.
 3. The **ConvertToEntity** component. This will convert the cube GameObject to and ECS Entity when entering PlayMode.

When the cube is getting converted to an entity (because it has the **ConvertToEntity** gameobject component), several other conversion operations occur.
- The **Unity.Animation** package contains the **RigConversion** system, which is a GameObjectConversionSystem. This conversion system will find the objects that have the **RigComponent** gameobject component, and will setup and add all the necessary rig ECS components on their corresponding entities. That's how the **Rig**, **AnimatedData** and **AnimatedLocalToWorld** ECS components are added to the cube entity.
- Additionally, the **RotatingCube_ClipPlayer** script implements the **IConvertGameObjectToEntity** interface. At conversion, this will add a **RotatingCube_PlayClipComponent** ECS component (specific to this sample) to the cube entity.

Once converted, several systems run on the cube entity.

- The **RotatingCube_PlayClipSystem**:
 * For every entity that has a **RotatingCube_PlayClipComponent** but no **RotatingCube_PlayClipStateComponent**, it creates an animation graph, and registers it to the **PreAnimationGraphSystem** from the **Unity.Animation** package (by adding a tag component to the entity). It then adds a **RotatingCube_PlayClipStateComponent** to the entity to signal that the graph has been created, and to keep track of this graph (to be able to update and delete it later);
 * For every entity that has both components **RotatingCube_PlayClipComponent** and **RotatingCube_PlayClipStateComponent**, meaning the graph was created, if the clip in the **RotatingCube_PlayClipComponent** has changed the **RotatingCube_PlayClipSystem** will update the clip used in the graph referenced in the **RotatingCube_PlayClipStateComponent**;
 * Finally, if the entity has a **RotatingCube_PlayClipStateComponent** but no **RotatingCube_PlayClipComponent**, it means the entity was deleted and so the graph is destroyed and the nodes created for this instance are destroyed to avoid a leak.
Note that this system is not doing any of the actual computation involved in performing the animation. 

- The **PreAnimationGraphSystem** executes the graph, and the **AnimatedData** buffers are updated on the entity by the **ComponentNode**.

----
## The Animation Graph

The animation graph is using the *Data Flow Graph* (DFG) to compute an animation. The animation graph is the central concept that drives the animation.

When an animated entity is added, the **RotatingCube_PlayClipSystem** creates a **ClipPlayerNode** and a **ComponentNode** (associated with this entity). It sets up parameters (like the clip to be played) on the **ClipPlayerNode** by sending it *messages*. It also connects the output of the **ClipPlayerNode** to the input of the **ComponentNode**. This will set the data of the **AnimatedData** buffer on the entity.

The **AnimationGraphSystem** from the **Unity.Animation** package automatically updates the graphs and applies the output to the entity.

## Time Management

Each rig entity has a component **DeltaTime**. This component is updated by the **UpdateAnimationDeltaTime** system. Using a **ComponentNode**, the graph connects the value of the **DeltaTime** component to the converter node **ConvertDeltaTimeToFloatNode**, and the output of the node is used as an input to evaluate the clip.
