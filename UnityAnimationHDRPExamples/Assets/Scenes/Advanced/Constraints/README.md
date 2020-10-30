# Constraints Sample

----
## What does it show?

This sample shows a character that plays a walking clip, while having an Aim constraint for his head and two TwoBoneIK constraints for each arm. IK targets are dynamic.

----
## Overview

The entry point to this sample is the GameObject `Constraint_DefaultMale` in the subscene. There are two important Unity components attached to this GameObject:

 1. The **RigComponent** component (from the **Unity.Animation** package) which defines the rig for the object. During entity conversion, it is converted to a **Rig** ECS component.
 2. The **ConstraintGraphComponent** component. During the conversion, this component will set up the ECS components that are necessary to build the animation graphs.
 In particular, in this sample we build two graphs, one for the forward kinematics pass, and one for the inverse kinematics pass. The component references an AnimationClip `Clip` that is played in the FK pass. The `Left Arm IK` fields, `Right Arm IK` fields and `Head Look At` fields define the transforms that will be used for the two TwoBoneIK constraints and the Aim constraint in the IK pass.

During the conversion of **ConstraintGraphComponent**, several things are happening:
 * A **FKGraphSetup** component is attached to the rig entity. This component contains the BlobAssetReference to the animation clip. It will get queried by the **FKGraphSystem** that will build the FK animation graph from it. Once the **FKGraphSystem** has built the graph, it will attach the **FKGraphSetup** state component to the rig entity to signal that the graph has been created, and to keep a graph handle so the nodes can be properly disposed when the entity is destroyed.
 * An **IKGraphSetup** component is attached to the rig entity. This component contains the indices of the bones used for the three constraints, as well as the Aim constraint's axis and the references to the TwoBoneIK's target entities. Similarly to the **FKGraphSystem**, the **IKGraphSystem** will build the IK animation graph from it and attach the **IKGraphSetup** state component to the rig entity to signal that the graph has been created.

Note that the **FKGraphSystem** and the **IKGraphSystem** are not doing any of the actual computation involved in performing the animation. They're just managing the creation, update and deletion of the graphs. The **AnimationGraphSystem** executes the graphs, and the **AnimatedData** buffers are updated on the entity using **ComponentNodes**.

The **TargetMovementAuthoring** put on the Targets GameObject in the subscene attaches a **MoveTargetData** component to the entity at conversion. This ECS component is picked up by the **TargetMovementSystem** that updates the translation to make IK targets go up and down.

----
## The Animation Graphs

Animation graphs use *Data Flow Graph* (DFG) to compute new animation data. The animation graphs are the central concept that drives the animation.
The **AnimationGraphSystem** from the **Unity.Animation** package automatically updates the graphs and applies the output to the entity.

### FK Graph

In this sample the FK Graph only plays a clip. A **DeltaTimeNode**, a **ClipPlayerNode** and a **ComponentNode** (associated with the rig entity) are created. The **DeltaTimeNode** is an input to the **ClipPlayerNode** to know at which time to sample the clip. The **ComponentNode** is set as the output of the **ClipPlayerNode** so that the `AnimatedData` buffer on the rig entity is updated with the clip values.

### IK Graph

This graph can be seen as three steps. After the FK pass, the rig data goes through 1) the left arm's TwoBonesIK constraint, then 2) the right arm's TwoBonesIK constraint, and finally 3) the head's Aim constraint.
First, to get the rig data, we need to create a  **ComponentNode** associated with the rig entity. This will allow us to read and write to its `AnimatedData` buffer.

1. The left arm's TwoBonesIK constraint is executed with a **TwoBoneIKNode**. It takes the rig entity's **ComponentNode** as an input for the `AnimatedData` buffer. For the target matrix, it takes the output of a **WorldToRootNode**. This node will remap the transform of the target from world space to root space, whichever root mode is used. The **WorldToRootNode** takes the rig entity's **ComponentNode** as an input for the `AnimatedData` buffer and also as an input to the root entity port. This port connects the `RigRootEntity` component of the rig entity. With the `AnimatedData` and the `RigRootEntity`, the **WorldToRootNode** is able to remap the target space before the TwoBoneIK constraint node is executed.
2. The right arm setup is the same as the left arm. The output stream of the left arm's **TwoBoneIKNode** is set as an input for the input of the right arm's **TwoBoneIKNode** and the right arm's **WorldToRootNode**.
3. For the aim constraint, an **AimConstraintNode** is created. The output stream of the right arm's **TwoBoneIKNode** is set as an input for it. Messages are sent to set internally the data needed to execute the constraint.

----
## Time Management

Each rig entity has a component **DeltaTime**. This component is updated by the **UpdateAnimationDeltaTime** system. Using a **ComponentNode**, the graph connects the value of the **DeltaTime** component to the converter node **ConvertDeltaTimeToFloatNode**, and this node's output is used as an input for the nodes that require a delta time.

