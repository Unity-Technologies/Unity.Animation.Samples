using Unity.Entities;
using Unity.Animation;
using Unity.DataFlowGraph;
using Unity.Mathematics;
using Unity.Transforms;

#if UNITY_EDITOR
using UnityEngine;
using Unity.Animation.Hybrid;
using System.Collections.Generic;

public class RetargetGraph : AnimationGraphBase
{
    public GameObject SourceRigPrefab;
    public AnimationClip SourceClip;

    public List<string> RetargetMap;

    private RigRemapQuery m_RemapQuery;

    public override void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs) =>
        referencedPrefabs.Add(SourceRigPrefab);

    public override void PreProcessData<T>(T data)
    {
        if (data is RigComponent)
        {
            var dstRig = data as RigComponent;
            var srcRig = SourceRigPrefab.GetComponent<RigComponent>();
            m_RemapQuery = CreateRemapQuery(srcRig, dstRig, RetargetMap);
        }
    }

    public override void AddGraphSetupComponent(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        if (dstManager.HasComponent<Rig>(entity))
        {
            var srcRigPrefab = conversionSystem.TryGetPrimaryEntity(SourceRigPrefab);
            var srcRigDefinition = dstManager.GetComponentData<Rig>(srcRigPrefab);
            var dstRigDefinition = dstManager.GetComponentData<Rig>(entity);

            var graphSetup = new RetargetSetup
            {
                SrcClip = ClipBuilder.AnimationClipToDenseClip(SourceClip),
                SrcRig = srcRigDefinition.Value,
                RemapTable = m_RemapQuery.ToRigRemapTable(srcRigDefinition.Value, dstRigDefinition.Value)
            };

            dstManager.AddComponentData(entity, graphSetup);
        }
    }

    public RigRemapQuery CreateRemapQuery(RigComponent srcRig, RigComponent dstRig, List<string> retargetMap)
    {
        string status = "";

        List<ChannelMap> translationChannels = new List<ChannelMap>();
        List<ChannelMap> rotationChannels = new List<ChannelMap>();
        List<RigTranslationOffset> translationOffsets = new List<RigTranslationOffset>();
        List<RigRotationOffset> rotationOffsets = new List<RigRotationOffset>();

        for (var mapIter = 0; mapIter < retargetMap.Count; mapIter++)
        {
            bool success = false;

            string[] splitMap = retargetMap[mapIter].Split(new char[] { ' ' }, 3);

            for (var srcBoneIter = 0; srcBoneIter < srcRig.Bones.Length; srcBoneIter++)
            {
                if (splitMap.Length > 0 && splitMap[0] == srcRig.Bones[srcBoneIter].name)
                {
                    for (var dstBoneIter = 0; dstBoneIter < dstRig.Bones.Length; dstBoneIter++)
                    {
                        if (splitMap.Length > 1 && splitMap[1] == dstRig.Bones[dstBoneIter].name)
                        {
                            if (splitMap.Length > 2)
                            {
                                var srcPath = RigGenerator.ComputeRelativePath(srcRig.Bones[srcBoneIter], srcRig.transform);
                                var dstPath = RigGenerator.ComputeRelativePath(dstRig.Bones[dstBoneIter], dstRig.transform);

                                if (splitMap[2] == "TR" || splitMap[2] == "T")
                                {
                                    var translationOffset = new RigTranslationOffset();

                                    // heuristic that computes retarget scale based on translation node (ex: hips) height (assumed to be y)
                                    translationOffset.Scale = dstRig.Bones[dstBoneIter].position.y / srcRig.Bones[srcBoneIter].position.y;
                                    
                                    translationOffset.Rotation = mathex.mul(math.conjugate(dstRig.Bones[dstBoneIter].parent.rotation), srcRig.Bones[srcBoneIter].parent.rotation);

                                    translationOffsets.Add(translationOffset);
                                    translationChannels.Add(new ChannelMap() { SourceId = srcPath, DestinationId = dstPath, OffsetIndex = translationOffsets.Count });
                                }

                                if (splitMap[2] == "TR" || splitMap[2] == "R")
                                {
                                    var rotationOffset = new RigRotationOffset();

                                    rotationOffset.PreRotation = mathex.mul(math.conjugate(dstRig.Bones[dstBoneIter].parent.rotation), srcRig.Bones[srcBoneIter].parent.rotation);
                                    rotationOffset.PostRotation = mathex.mul(math.conjugate(srcRig.Bones[srcBoneIter].rotation), dstRig.Bones[dstBoneIter].rotation);

                                    rotationOffsets.Add(rotationOffset);
                                    rotationChannels.Add(new ChannelMap() { SourceId = srcPath, DestinationId = dstPath, OffsetIndex = rotationOffsets.Count });
                                }

                                success = true;
                            }
                        }
                    }
                }
            }

            if (!success)
            {
                status = status + mapIter + " ";
            }
        }

        RigRemapQuery rigRemapQuery = new RigRemapQuery();

        rigRemapQuery.TranslationChannels = new ChannelMap[translationChannels.Count];

        for (var iter = 0; iter < translationChannels.Count; iter++)
        {
            rigRemapQuery.TranslationChannels[iter] = translationChannels[iter];
        }

        rigRemapQuery.TranslationOffsets = new RigTranslationOffset[translationOffsets.Count + 1];
        rigRemapQuery.TranslationOffsets[0] = new RigTranslationOffset();

        for (var iter = 0; iter < translationOffsets.Count; iter++)
        {
            rigRemapQuery.TranslationOffsets[iter + 1] = translationOffsets[iter];
        }

        rigRemapQuery.RotationChannels = new ChannelMap[rotationChannels.Count];

        for (var iter = 0; iter < rotationChannels.Count; iter++)
        {
            rigRemapQuery.RotationChannels[iter] = rotationChannels[iter];
        }

        rigRemapQuery.RotationOffsets = new RigRotationOffset[rotationOffsets.Count + 1];
        rigRemapQuery.RotationOffsets[0] = new RigRotationOffset();

        for (var iter = 0; iter < rotationOffsets.Count; iter++)
        {
            rigRemapQuery.RotationOffsets[iter + 1] = rotationOffsets[iter];
        }

        if (!string.IsNullOrEmpty(status))
        {
            UnityEngine.Debug.LogError("Faulty Entries : " + status);
        }

        return rigRemapQuery;
    }
}
#endif

public struct RetargetSetup : ISampleSetup
{
    public BlobAssetReference<Clip> SrcClip;
    public BlobAssetReference<RigDefinition> SrcRig;
    public BlobAssetReference<RigRemapTable> RemapTable;
};

public struct RetargetData : ISampleData
{
    public NodeHandle<DeltaTimeNode>   DeltaTimeNode;
    public NodeHandle<ClipPlayerNode>  ClipPlayerNode;
    public NodeHandle<RigRemapperNode> RemapperNode;

    public NodeHandle<ComponentNode>   EntityNode;
    public NodeHandle<ComponentNode>   DebugEntityNode;
}

[UpdateBefore(typeof(PreAnimationSystemGroup))]
public class RetargetGraphSystem : SampleSystemBase<
    RetargetSetup,
    RetargetData,
    PreAnimationGraphTag,
    PreAnimationGraphSystem
    >
{
    protected override RetargetData CreateGraph(Entity entity, ref Rig rig, PreAnimationGraphSystem graphSystem, ref RetargetSetup setup)
    {
        var set = graphSystem.Set;
        var debugEntity = RigUtils.InstantiateDebugRigEntity(
            setup.SrcRig,
            EntityManager,
            new BoneRendererProperties { BoneShape = BoneRendererUtils.BoneShape.Line, Color = new float4(0f, 1f, 0f, 0.5f), Size = 1f }
            );

        if (EntityManager.HasComponent<Translation>(entity))
        {
            PostUpdateCommands.AddComponent(
                debugEntity,
                EntityManager.GetComponentData<Translation>(entity)
                );
        }

        var data = new RetargetData();

        data.DeltaTimeNode   = set.Create<DeltaTimeNode>();
        data.ClipPlayerNode  = set.Create<ClipPlayerNode>();
        data.RemapperNode    = set.Create<RigRemapperNode>();
        data.EntityNode      = set.CreateComponentNode(entity);
        data.DebugEntityNode = set.CreateComponentNode(debugEntity);

        set.SetData(data.ClipPlayerNode, ClipPlayerNode.KernelPorts.Speed, 1.0f);

        // Connect kernel ports
        set.Connect(data.DeltaTimeNode, DeltaTimeNode.KernelPorts.DeltaTime, data.ClipPlayerNode, ClipPlayerNode.KernelPorts.DeltaTime);
        set.Connect(data.ClipPlayerNode, ClipPlayerNode.KernelPorts.Output, data.RemapperNode, RigRemapperNode.KernelPorts.Input);

        // Connect EntityNode
        set.Connect(data.RemapperNode, RigRemapperNode.KernelPorts.Output, data.EntityNode);

        // Connect DebugEntityNode
        set.Connect(data.ClipPlayerNode, ClipPlayerNode.KernelPorts.Output, data.DebugEntityNode);

        // Send messages
        set.SendMessage(data.ClipPlayerNode, ClipPlayerNode.SimulationPorts.Configuration, new ClipConfiguration { Mask = ClipConfigurationMask.LoopTime });
        set.SendMessage(data.ClipPlayerNode, ClipPlayerNode.SimulationPorts.Rig, new Rig { Value = setup.SrcRig });
        set.SendMessage(data.ClipPlayerNode, ClipPlayerNode.SimulationPorts.Clip, setup.SrcClip);
        set.SendMessage(data.RemapperNode, RigRemapperNode.SimulationPorts.SourceRig, new Rig { Value = setup.SrcRig });
        set.SendMessage(data.RemapperNode, RigRemapperNode.SimulationPorts.DestinationRig, rig);
        set.SendMessage(data.RemapperNode, RigRemapperNode.SimulationPorts.RemapTable, setup.RemapTable);

        PostUpdateCommands.AddComponent(entity, graphSystem.Tag);
        PostUpdateCommands.AddComponent(debugEntity, graphSystem.Tag);

        return data;
    }

    protected override void DestroyGraph(Entity entity, PreAnimationGraphSystem graphSystem, ref RetargetData data)
    {
        var set = graphSystem.Set;
        set.Destroy(data.DeltaTimeNode);
        set.Destroy(data.ClipPlayerNode);
        set.Destroy(data.RemapperNode);
        set.Destroy(data.EntityNode);
        set.Destroy(data.DebugEntityNode);
    }
}
