# Animation Rig Remap Sample

This sample demonstrates different ways to remap a source animation from a character rig to another destination rig.

## What does it show?

This sample demonstrates 3 different ways to perform animation remapping:
1) `EllenRig_MocapRetarget` demonstrates how to retarget source animation to another character with different proportions using a **Unity.Animation.RigRemapQuery** and **Unity.Animation.RigRemapTable** with offsets. Offsets are needed to retarget the source animation on the destination character because rotation axes may differ.
2) `TerraformerLOD0_LOD1BindingRemap` demonstrate usage of **Unity.Animation.RigRemapUtils.CreateRemapTable** to automatically build a remapping table based on existing **Unity.Animation.RigDefinition** bindings. This is useful when skeleton hierarchies are similar (ie. LOD setups).
    This example showcases remapping animation from a lower to higher resolution skeleton definition.
3) `HandRigLOD0_SubRigPartRemap` demonstrate usage of **Unity.Animation.Hybrid.RigRemapUtils.CreateRemapTable** to automatically build a remapping table based on existing **Unity.Animation.Hybrid.RigComponent** properties. In this example we show how to remap a sub part of a hierarchy to a hand rig.
    Given that the hierarchies are very different in this case, we use a name based matching strategy. Finally as we are remapping to a specific sub part we override the hand transform offsets to perform operations in LocalToRoot space instead of LocalToParent space to displace to hand rig at the wanted reference frame.

----
## Converting from GameObject to Entity prefab

This samples contains a subscene with the 3 different setups each of which have different conversion components:
1) **RetargetComponent** which builds a very simple text based remapping table to create it's **RigRemapQuery**. For example, `Hips Ellen_Hips TR` means that the bone named `Hips` on the source rig is mapped to the `Ellen_Hips` bone on the destination rig in `Translation` and `Rotation`.
2) **MatchBindingsComponent** takes in a source rig prefab to generate it's remapping table from the **RigDefintion** bindings.
3) **RemapSubRigPartComponent** takes in a source rig prefab and a transform to remap in LocalToRoot space.

All conversion components create **RigRemapSetup** component data in order for the **RigRemapGraphSystem** to initialize an animation graph that plays source animation on the source rig.
This is then used as input in a RigRemapperNode to convert the animation to the destination rig using the **RigRemapTable**.


----
## RigRemapGraphSystem

The **RigRemapGraphSystem** creates an animation graph which uses a **Unity.Animation.RigRemapperNode** and destroys the graph when the entity is destroyed.

----
## Time Management

Each rig entity has a component **DeltaTime**. This component is updated by the **UpdateAnimationDeltaTime** system. Using a **ComponentNode**, the graph connects the value of the **DeltaTime** component to the converter node **ConvertDeltaTimeToFloatNode**, and this node's output is used as an input for the nodes that require a delta time.
