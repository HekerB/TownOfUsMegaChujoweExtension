using Il2CppInterop.Runtime.Attributes;
using MiraAPI.Modifiers;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Modifiers;

/// <summary>
/// Modifier that tracks and displays the kill cooldown reduction for Serial Killer.
/// </summary>
public sealed class SerialKillerReductionModifier(float reductionAmount) : BaseModifier
{
    public override string ModifierName => TouLocale.Get("ExtensionModifierSerialKillerReduction", "Cooldown Reduction");
    public override bool HideOnUi => true;
    [HideFromIl2Cpp] public bool IsHiddenFromList => true;
    public float ReductionAmount { get; set; } = reductionAmount;

    public override string GetDescription()
    {
        return $"-{ReductionAmount:F1}s";
    }

    public override void OnActivate()
    {
        base.OnActivate();
    }
}
