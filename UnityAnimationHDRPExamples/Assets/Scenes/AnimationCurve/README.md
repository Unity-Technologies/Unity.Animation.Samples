# Animation Curve Sample

This example shows two cubes moving up and down. Their y-value depends on the evaluation of a curve. Both cubes use a different workflow to evaluate a DOTS Animation Curve.

### Cube Authoring

The GameObject CubeAuthoring has the script `AnimationCurveAuthoring`. This script simply attaches a tag `CurveTag` to the entity of the cube during the conversion. Then, the system `AnimationCurveTranslationSystem` takes the entities with this tag and changes the value of their `Translation` component depending on the evaluation of a hardcoded `KeyframeCurve` (that is declared in at the creation of the system).

### Cube Blob Authoring

The GameObject CubeBlobAuthoring has the script `AnimationCurveBlobAuthoring`. During the conversion, this script converts an UnityEngine AnimationCurve into a blob asset and adds a `CurveBlobComponent` containing this blob asset reference to the entity of the cube. Then, the `AnimationCurveBlobTranslationSystem` modifies the value of the `Translation` component by evaluating the curve from the blob.

---

The main differences between the two workflows are that:
- The first workflow hardcodes a curve inside a system. As such all the entities use the same curve. It shows how you could build a DOTS animation curve manually into your game;
- The second workflow allows the user to edit a curve in the editor and then converts it to a blob. Here all the entities can have different curves. It shows how to use a BlobAssetReference representing the curve.