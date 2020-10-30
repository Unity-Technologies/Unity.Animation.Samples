using UnityEngine;

using Unity.Animation;
using Unity.Animation.Hybrid;

public enum HumanoidGait
{
    LeftFootContact = 1,
    RightFootPassover = 2,
    RightFootContact = 3,
    LeftFootPassover = 4
}

public class HumanoidGaitAuthoringComponent : MonoBehaviour, ISynchronizationTag
{
    public HumanoidGait m_State;

    public StringHash Type => nameof(HumanoidGait);
    public int         State
    {
        get { return (int)m_State; }
        set { m_State = (HumanoidGait)value; }
    }
}
