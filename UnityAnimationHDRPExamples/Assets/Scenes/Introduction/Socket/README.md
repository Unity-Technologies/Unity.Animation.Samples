# Socket Sample

----
## What does it show?

We call _exposed transform_ an entity with transform components (Translation, Rotation, Scale/NonUniformScale) that are written to or read from the animation system. It allows the communication between different systems (for example, Animation and Physics) through those common transform components.

This sample shows how to use pre-animation write transform handles to copy the animation data to the transform components of an entity. It also shows how to attach another entity to this exposed transform entity.

----
## Overview

The SocketSubscene showcases 3 workflows using write exposed transforms.

1. Add a transform into your prefab and attach an object to it (The Cube Orb in Socket)
- The component `PreAnimationGraphWriteTransformHandle` was added to the LeftHand child transform of the rig. This component is used to specify that the animation values of the rig computed in the pre-animation system need to be copied to the transform components of the LeftHand entity.
- An additional transform, named Socket, has been added to the default male instantiated prefab, as a child of the LeftHand transform. Because the write transform handle is going to write into the transform components of the LeftHand entity, the transform components of all its children are also going to be updated by the transform systems, including the ones of the Socket entity. Any child entity of the Socket will also get updated.
- The script `AttachToComponent` sets the parent of the orb entity to the Socket entity added to the default male gameobject. Because **MaintainOffset** is set to false, the editor Transform values of the orb will be used to define the LocalToParent between the orb and the socket.
- This workflow is useful when you have the same rig used for different characters, but they have some variations as to where you can attach things to them. Add empty "socket" transforms as children to exposed transforms (such as the LeftHand), and attach other entities to them.

2. Directly place the object you want to attach (The Shield Attached Directly)
- This gameobject showcases a second workflow, where you attach an object directly to an exposed transform. First, the component `PreAnimationGraphWriteTransformHandle` was added to the RightHand child transform of the rig.
- Then, the script `AttachToComponent` sets the parent of the shield entity to the RighHand entity of the rig. The bool **MaintainOffset** is set to true, meaning that you can set the transform in the editor and the LocalToParent will be computed so that the world transform is preserved after parenting.

3. Place a socket outside the prefab and attach an object to it (The Head Socket + Hat)
- You can chain as many attachments as you want. It can be useful if you don't want to add sockets into your prefab directly for example.
- The component `PreAnimationGraphWriteTransformHandle` was added to the Head child transform of the rig.
- The gameobject Head Socket has the `AttachToComponent` referencing this Head transform, but is still *outside* the rig hierarchy. **MaintainOffset** is set to true, so that once you've placed the socket where you want, its relative position to the head is preserved.
- Then the `AttachToComponent` of the gameobject Hat references the Head Socket gameobject transform. Here, **MaintainOffset** is set to false, so that the hat is moved to the head socket position.
