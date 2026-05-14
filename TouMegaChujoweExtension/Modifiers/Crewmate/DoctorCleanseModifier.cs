using MiraAPI.Modifiers;
using MiraAPI.Modifiers.Types;
using TouMegaChujoweExtension.Events.Crewmate;
using TouMegaChujoweExtension.Modifiers.Impostor;
using TownOfUs.Modules.Localization;
using UnityEngine;
using System.Linq;

namespace TouMegaChujoweExtension.Modifiers.Crewmate;

public sealed class DoctorCleanseModifier : TimedModifier
{
    public override string ModifierName => "Doctor Cleansed";
    public override bool HideOnUi => true;

    public override float Duration => 1f; // Instant effect, but TimedModifier needs a duration

    public override void OnActivate()
    {
        if (Player == null) return;

        // Remove negative modifiers
        var modifiersToRemove = Player.GetModifiers<BaseModifier>().Where(m =>
            m is InjectedInvertedControlsModifier ||
            m is InjectedLowVisionModifier ||
            m is InjectedSlownessModifier ||
            m is InjectedVeryLowVisionModifier ||
            m is InjectedConfusedModifier ||
            m is InjectedNoVentModifier ||
            m is InjectedNoUseModifier ||
            m is InjectedNoReportModifier ||
            m is InjectedNauseaModifier ||
            m is InjectedWeaknessModifier
        ).ToList();

        foreach (var mod in modifiersToRemove)
        {
            Player.RemoveModifier(mod);
        }

        Player.RemoveModifier(this);
    }
}
