# Retarget Sample

This sample demonstrates how to retarget a source animation from a character rig to another character rig with different proportions.

----
## What does it show?

This sample demonstrates how to retarget a source animation to another character with different proportions by using the **RigRemapQuery** and **RigRemapTable**

----
## Converting from GameObject to Entity prefab

It uses a **Spawner** MonoBehaviour which needs a Rig Prefab (the animated character) and a Graph Prefab (the animation source that drives the rig).

The **Spawner** converts the Rig Prefab which has a **RigComponent** to a Rig Entity prefab with an **Unity.Animation.RigDefinition**.

The **Spawner** also converts the **RetargetGraph** Prefab to convert all embedded Unity assets (Source character rig, source animation clip and a remap table) into DOTS animation asset.

The **RetargetGraph** Prefab have a list of string that represent the mapping table between the source and destination character.

By example `Hips Ellen_Hips TR` mean that the bone named `Hips` is mapped on `Ellen_Hips` in `translation` and `rotation`.

It does also apply some heuristic to compute the translation and rotation offset between each bone of both character rig.

Thoses offsets are needed to retarget the source animation on the destination character because they may not have the same rotation axis.

----
## ComponentSystems

The **RigSpawnerSystem** is a ComponentSystem that handle the instantiation of the Rig Entity Prefab.

The **RetargetGraphSystem** is a ComponentSystem that handle 2 operations:

* Creation of the DFG(DataFlowGraph) with an **Unity.Animation.RigRemapperNode**.
* Destruction of the DFG.


