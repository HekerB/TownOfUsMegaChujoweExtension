using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Events.Crewmate;
using TownOfUs.Modules.Localization;
using UnityEngine;

namespace TouMegaChujoweExtension.Modifiers;

public sealed class CleanseModifier : TimedModifier
{
    public override string ModifierName => "Cleansed";
    public override bool HideOnUi => true;

    public override float Duration => 1f; // Instant effect, but TimedModifier needs a duration

    public override void OnActivate()
    {
        if (Player == null) return;

        // Remove negative modifiers
        var modifiersToRemove = Player.GetModifiers().Where(m => 
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

        if (Player.AmOwner)
        {
            DoctorEvents.ShowNotification(Player, "ExtensionDoctorNotificationCleanse", "All negative effects removed");
        }
        
        Player.RemoveModifier(this);
    }
}
