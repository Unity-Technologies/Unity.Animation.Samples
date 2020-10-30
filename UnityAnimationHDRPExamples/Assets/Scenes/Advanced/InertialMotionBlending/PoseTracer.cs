using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class PoseTracer : MonoBehaviour
{
    [Tooltip("The amount of frames that are traced")]
    public int HistorySize = 10;

    [Tooltip("How many frames should pass between two points in the trace")]
    public int FramePeriod = 1;

    [Tooltip("Color at the beginning of the trace")]
    public Color Color = Color.cyan;

    [Tooltip("Color at the end of the trace")]
    public Color EndColor = Color.white;
}

[ConverterVersion("PoseTracer", 2)]
[UpdateInGroup(typeof(GameObjectConversionGroup))]
public class PoseTracerConversion : GameObjectConversionSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((PoseTracer poseTracer) =>
        {
            if (poseTracer.HistorySize == 0)
            {
                UnityEngine.Debug.LogWarning($"{this} has parameter HistorySize set to 0, no trace will be generated. Either disable or remove the component to remove the trace.");
                return;
            }
            var entity = GetPrimaryEntity(poseTracer);
            DstEntityManager.AddComponentData(entity, new PoseTracerComponent
            {
                FramesLeftUntilDraw = 0,
                FramePeriod = poseTracer.FramePeriod,
            });
            DstEntityManager.AddBuffer<PoseTracerElement>(entity);
            for (int i = 0; i < poseTracer.HistorySize; i++)
            {
                var color =  Color.Lerp(poseTracer.Color, poseTracer.EndColor, ((float)i) / ((float)poseTracer.HistorySize));
                DstEntityManager.GetBuffer<PoseTracerElement>(entity).Add(new PoseTracerElement
                {
                    SegmentColor = color,
                    EndPosition = poseTracer.transform.position,
                });
            }
        });
    }
}

public struct PoseTracerElement : IBufferElementData
{
    public float3 EndPosition;
    public Color SegmentColor;
}
public struct PoseTracerComponent : IComponentData
{
    public int FramesLeftUntilDraw;
    public int FramePeriod;
}


[UpdateAfter(typeof(Unity.Transforms.TransformSystemGroup))]
public class PoseTracerSystem : SystemBase
{
    protected override void OnUpdate()
    {
        Entities.ForEach((DynamicBuffer<PoseTracerElement> trace, ref PoseTracerComponent poseTracer, in Unity.Transforms.LocalToWorld transform) =>
        {
            DrawTrace(transform.Position, ref trace);

            if (poseTracer.FramesLeftUntilDraw > 0)
            {
                poseTracer.FramesLeftUntilDraw -= 1;
                return;
            }
            poseTracer.FramesLeftUntilDraw = poseTracer.FramePeriod - 1;

            AdvanceTrace(transform.Position, ref trace);
        }).Run();
    }

    static void DrawTrace(float3 beginning, ref DynamicBuffer<PoseTracerElement> trace)
    {
        for (int i = 0; i < trace.Length; i++)
        {
            var line = trace[i];
            DrawLine(beginning, line.EndPosition, line.SegmentColor);
            beginning = line.EndPosition;
        }
    }

    static void AdvanceTrace(float3 newStart, ref DynamicBuffer<PoseTracerElement> trace)
    {
        for (int i = trace.Length - 1; i > 0; i--)
        {
            var traceElement = trace[i];
            traceElement.EndPosition = trace[i - 1].EndPosition;
            trace[i] = traceElement;
        }

        if (trace.Length >= 0)
        {
            var traceElement = trace[0];
            traceElement.EndPosition = newStart;
            trace[0] = traceElement;
        }
    }

    static void DrawLine(float3 start, float3 end, Color color)
    {
        Debug.DrawLine(start, end, color, 0, false);
    }
}
