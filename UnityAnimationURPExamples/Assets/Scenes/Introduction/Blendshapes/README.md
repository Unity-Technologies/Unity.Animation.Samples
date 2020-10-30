# Blendshapes Sample

----
## What does it show?
This sample showscases three objects being deformed using skinning only (left), skinning + blendshapes (middle) and blendshapes only (right) using animation clips.

#### Note
DOTS blendshape depend on compute shaders to perform mesh deformations. Make sure to add the following scripting define to your project player settings : `ENABLE_COMPUTE_DEFORMATIONS`.
Also, for blendshapes to work, you'll need to create a shader graph that contains a `Compute Deformation` node (see _Assets/Shaders/Basic_Compute_Deformation.shadergraph_ as an example) which needs to be applied to a material used by the `SkinnedMeshRenderer` component.

----
## Overview

The entry point to this sample are the root GameObjects defined in the subscene. They hold two important Unity components:

 1. The **RigComponent** component (from the **Unity.Animation** package) which defines the rig for the object. During entity conversion, it is converted to a **Rig** ECS component.
 2. The **Blendshapes_ClipPlayer** component. This allows you to choose an animation clip to play on the rig. It is converted to a **BlendShapes_PlayClipComponent** ECS component.

Once converted, several systems run on the cube entity.

- The **BlendShape_PlayClipSystem**:
 * For every entity that has a **Blendshapes_PlayClipComponent** but no **Blendshapes_PlayClipStateComponent**, it creates an animation graph, and registers it to the **PreAnimationGraphSystem** from the **Unity.Animation** package (by adding a tag component to the entity). It then adds a **BlendShapes_PlayClipStateComponent** to the entity to signal that the graph has been created, and to keep track of this graph (to be able to update and delete it later);
 * For every entity that has both components **BlendShapes_PlayClipComponent** and **BlendShapes_PlayClipStateComponent**, meaning the graph was created, if the clip in the **BlendShapes_PlayClipComponent** has changed the **BlendShapes_PlayClipSystem** will update the clip used in the graph referenced in the **BlendShapes_PlayClipStateComponent**;
 * Finally, if the entity has a **BlendShapes_PlayClipStateComponent** but no **BlendShapes_PlayClipComponent**, it means the entity was deleted and so the graph is destroyed and the nodes created for this instance are destroyed to avoid a leak.
Note that this system is not doing any of the actual computation involved in performing the animation. 

- The **PreAnimationGraphSystem** executes the graph, and the **AnimatedData** buffers are updated on the entity by the **ComponentNode**.

----
## The Animation Graph

The animation graph is using the *Data Flow Graph* (DFG) to compute an animation. The animation graph is the central concept that drives the animation.

When an animated entity is added, the **BlendShapes_PlayClipSystem** creates a **ClipPlayerNode** and a **ComponentNode** (associated with this entity). It sets up parameters (like the clip to be played) on the **ClipPlayerNode** by sending it *messages*. It also connects the output of the **ClipPlayerNode** to the input of the **ComponentNode**. This will set the data of the **AnimatedData** buffer on the entity.

The **AnimationGraphSystem** from the **Unity.Animation** package automatically updates the graphs and applies the output to the entity.

## Time Management

Each rig entity has a component **DeltaTime**. This component is updated by the **UpdateAnimationDeltaTime** system. Using a **ComponentNode**, the graph connects the value of the **DeltaTime** component to the converter node **ConvertDeltaTimeToFloatNode**, and the output of the node is used as an input to evaluate the clip.
