# FeatherBlend Sample

## What does it show?

This sample shows how to blend two clips with different weights for different channels, instead of a uniform weight for all of them.

## Converting from GameObject to Entity prefab

It uses a **Spawner** MonoBehaviour which needs a Rig Prefab (the animated character) and a Graph Prefab (the animation source that drives the rig).

The **Spawner** converts the Rig Prefab which has a RigComponent to a Rig Entity prefab with an Unity.Animation.RigDefinition.

The Graph Prefab has a **FeatherBlendGraph** component. This component has two clips to be blended together, a list of channel specific weights, and a default weight for when no weight has been specified.

## ComponentSystems

The **RigSpawnerSystem** is a ComponentSystem that handles the instantiation of the Rig Entity Prefab.

The **FeatherBlendGraphSystem** is a ComponentSystem that handles 3 operations:
* Creation of the DFG (DataFlowGraph) with:
   * A `DeltaTimeNode` that outputs Unity's delta time;
   * Two `ClipPlayerNode`s that evaluate the values of the channels of the rig, for two different clips;
   * A `WeightBuilderNode` that creates the buffer with all the channels' weights;
   * A `ChannelWeightMixerNode` that blends the two animation streams from the clips given the channel weight values from the weight builder;
   * Three `ComponentNode` used to write into the `AnimetedData` of three entities : two entities used for debug purpose that show the original two clips, and a third that shows the blended result;
* Update of the graph parameters by sending message to the Blend Tree node;
* Destruction of the DFG.

This system does not run the animation itself, but modifies the DFG NodeSet of the **PreAnimationSystemGroup**.

