using UnityEngine;
using UnityEngine.UI;
using Unity.Animation;

public class ConfigurableClipInput : AnimationInputBase<ConfigurableClipData>
{
    public float ClipTime;
    public bool NormalizedTime;
    public bool LoopTime;
    public bool LoopValues;
    public bool CycleRootMotion;
    public bool InPlace;
    public bool BankPivot;
    public bool Play;
    public float Speed = 1.0f;

    private float ParamXStep = 0.1f;
    private float ParamYStep = 0.1f;

    [HideInInspector]
    public Text MenuText;

    protected override void UpdateComponentData(ref ConfigurableClipData data)
    {
        UpdateParameters();
        UpdateText();

        data.ClipTime = ClipTime;

        data.UpdateConfiguration = false;
        data.UpdateConfiguration |= HasFlag(data.ClipOptions, ClipConfigurationMask.NormalizedTime) != NormalizedTime;
        data.UpdateConfiguration |= HasFlag(data.ClipOptions, ClipConfigurationMask.LoopTime) != LoopTime;
        data.UpdateConfiguration |= HasFlag(data.ClipOptions, ClipConfigurationMask.LoopValues) != LoopValues;
        data.UpdateConfiguration |= HasFlag(data.ClipOptions, ClipConfigurationMask.CycleRootMotion) != CycleRootMotion;
        data.UpdateConfiguration |= HasFlag(data.ClipOptions, ClipConfigurationMask.BankPivot) != BankPivot;
        data.UpdateConfiguration |= data.InPlace != InPlace;

        data.ClipOptions =
            (NormalizedTime ? ClipConfigurationMask.NormalizedTime : 0) |
            (LoopTime ? ClipConfigurationMask.LoopTime : 0) |
            (LoopValues ? ClipConfigurationMask.LoopValues : 0) |
            (CycleRootMotion ? ClipConfigurationMask.CycleRootMotion : 0) |
            (BankPivot ? ClipConfigurationMask.BankPivot : 0);

        data.InPlace = InPlace;
    }

    private bool HasFlag(ClipConfigurationMask mask, ClipConfigurationMask flag) =>
        (mask & flag) != 0;

    private void UpdateParameters()
    {
        if (Play)
        {
            ClipTime += Time.deltaTime * Speed;
        }
        else
        {
            var deltaX = Input.GetAxisRaw("Horizontal");
            ClipTime += deltaX * ParamXStep * Speed;
        }

        var deltaY = Input.GetAxisRaw("Vertical");
        Speed += deltaY * ParamYStep;

        if (Input.GetKeyDown(KeyCode.P))
        {
            Play = !Play;
        }
        if (Input.GetKeyDown(KeyCode.N))
        {
            NormalizedTime = !NormalizedTime;
        }
        if (Input.GetKeyDown(KeyCode.T))
        {
            LoopTime = !LoopTime;
        }
        if (Input.GetKeyDown(KeyCode.V))
        {
            LoopValues = !LoopValues;
        }
        if (Input.GetKeyDown(KeyCode.C))
        {
            CycleRootMotion = !CycleRootMotion;
        }
        if (Input.GetKeyDown(KeyCode.I))
        {
            InPlace = !InPlace;
        }
        if (Input.GetKeyDown(KeyCode.B))
        {
            BankPivot = !BankPivot;
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            ClipTime = 0.0f;
            NormalizedTime = false;
            LoopTime = false;
            LoopValues = false;
            CycleRootMotion = false;
            InPlace = false;
            BankPivot = false;
            Speed = 1.0f;
        }
    }

    private void UpdateText()
    {
        MenuText.text =
            $"Down-Up : Speed (current = {Speed})\n" +
            $"Left-Right : ClipTime ({ClipTime})\n" +
            $"P : Toggle Play ({Play})\n" +
            $"N : Toggle NormalizedTime ({NormalizedTime})\n" +
            $"T : Toggle LoopTime ({LoopTime})\n" +
            $"V : Toggle LoopValues ({LoopValues})\n" +
            $"C : Toggle CycleRootMotion ({CycleRootMotion})\n" +
            $"I : Toggle InPlace ({InPlace})\n" +
            $"B : Toggle BankPivot ({BankPivot})\n" +
            $"R : Reset";
    }
}
