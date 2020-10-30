using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

#if UNITY_EDITOR
using UnityEngine;
#endif

namespace ConstraintsSample
{
#if UNITY_EDITOR
    public class TargetMovementAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        public float Speed = 1.0f;
        public float Amplitude = 1.0f;
        public float YOffset = 1.4f;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            var moveTargetData = new MoveTargetData
            {
                Speed = Speed,
                Amplitude = Amplitude,
                YOffset = YOffset
            };

            dstManager.AddComponentData(entity, moveTargetData);
        }
    }
#endif

    public struct MoveTargetData : IComponentData
    {
        public float Speed;
        public float Amplitude;
        public float YOffset;
    };

    public class TargetMovementSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var time = (float)Time.ElapsedTime;

            // Schedule job to rotate around up vector
            Entities
                .WithName("RotationSpeedSystem_ForEach")
                .ForEach((ref Translation translation, in MoveTargetData movementData) =>
                {
                    translation.Value.y = movementData.YOffset +
                        movementData.Amplitude * math.sin(time * movementData.Speed);

                    translation = new Translation
                    {
                        Value = translation.Value
                    };
                })
                .ScheduleParallel();
        }
    }
}
