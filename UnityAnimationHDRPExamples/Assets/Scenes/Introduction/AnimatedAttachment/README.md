# Animated Attachment Sample

----
## What does it show?

This sample shows how to use pre-animation write transform handles to animate an object that has no skinned mesh, and to attach it to a socket on a rig.

----
## Overview

There's three parts:
1. A clip is played on the rig.
    1. A `RigComponent` component (from the **Unity.Animation** package), which defines the rig, is added to the root of the Default Male gameobject. During entity conversion, it is converted to a **Rig** ECS component.
    1. The `AnimatedAttachment_ClipPlayer` component allows you to choose an animation clip to play on the rig. It is converted to an `AnimatedAttachment_PlayClipComponent` ECS component.

2. A clip is played on the propeller hat.
    1. A `RigComponent` component is put on the root of the Propeller Hat gameobject.
    1. The `AnimatedAttachment_ClipPlayer` component is also added to play the clip on the hat.
    1. A `PreAnimationGraphWriteTransformHandle` component is added to the Capsule transform in the Propeller Hat gameobject. Because the hat is not a skinned mesh, this component is necessary to write back the animated transform values to the components of the Capsule entity.

See [**My FirstAnimation Clip**](../MyFirstAnimationClip/README.md) for more details about how playing a clip works, and the [**Scorpion Sample**](../Scorpion/README.md) for animating non-skinned meshes.

3. The cap is placed on the rig.
    1. An empty gameobject Socket is created outside the Default Male prefab. The component `AnimatedAttachment_AttachToComponent` is added to it. It references the Head transform of the character, so that at conversion time the Socket entity gets parented to the Head entity. **MaintainOffset** is set to true, so that once you've placed the socket where you want, its relative position to the head is preserved.
    1. An `AnimatedAttachment_AttachToComponent` component is also added to the Propeller Hat gameobject. This time it references the Socket gameobject, to be parented to this intermediate entity at conversion. Here, **MaintainOffset** is set to false, so that the hat is moved to the head _socket_ position.

See the [**Socket Sample**](../Socket/README.md) for more ways of attaching an object to a rig.
