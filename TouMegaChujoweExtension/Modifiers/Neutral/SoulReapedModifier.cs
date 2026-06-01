using MiraAPI.Modifiers;
using MiraAPI.Roles;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using TownOfUs.Events;
using TownOfUs.Extensions;
using TownOfUs.Modifiers;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Modifiers.Neutral;

public sealed class SoulReapedModifier(byte soulCollectorId, int markedRound, int durationRounds) : BaseRevealModifier
{
    public override string ModifierName => TouLocale.Get("ExtensionModifierSoulCollectorReaped", "Reaped");
    public override bool HideOnUi => true;
    public override string ExtraNameText { get; set; } = $" {TouExtensionColors.SoulCollector.ToTextColor()}{{}}</color>";
    public override Color? NameColor { get; set; } = TouExtensionColors.SoulCollector;

    public byte SoulCollectorId { get; } = soulCollectorId;
    public int MarkedRound { get; } = markedRound;
    public int DurationRounds { get; } = durationRounds;

    public bool IsExpired()
    {
        if (DurationRounds <= 0)
        {
            return false;
        }

        var roundsElapsed = DeathEventHandlers.CurrentRound - MarkedRound + 1;
        return roundsElapsed > DurationRounds;
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        var localPlayer = PlayerControl.LocalPlayer;
        Visible = Player != null &&
                  !Player.HasDied() &&
                  !IsExpired() &&
                  localPlayer != null &&
                  !localPlayer.HasDied() &&
                  localPlayer.PlayerId == SoulCollectorId &&
                  localPlayer.IsRole<SoulCollectorRole>();
    }
}
