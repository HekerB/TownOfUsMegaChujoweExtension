using MiraAPI.Modifiers;
using TownOfUs.Modifiers;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;

namespace TouMegaChujoweExtension.Modifiers.Neutral;

/// <summary>
/// Bread a Baker handed out. It remembers which Baker gave it, so two Bakers (and the Famine each becomes)
/// count only their own bread.
/// </summary>
public sealed class BakerBreadModifier(byte bakerId) : BaseModifier
{
    public override string ModifierName => TouLocale.Get("ExtensionModifierBakerBread", "Has Bread");
    public override bool HideOnUi => true;

    public byte BakerId { get; } = bakerId;

    /// <summary>
    /// Set once removal starts: MiraAPI deactivates a modifier before it leaves the player's modifier list, so
    /// for that moment HasModifier still sees it.
    /// </summary>
    public bool Removing { get; private set; }

    public static bool HasBreadFrom(PlayerControl? player, byte bakerId)
    {
        return player != null &&
               player.GetModifiers<BakerBreadModifier>().Any(x => !x.Removing && x.BakerId == bakerId);
    }

    /// <summary>
    /// A holder the Baker's Famine could actually starve: alive, not the Baker, not its Apocalypse ally and not
    /// invulnerable (a holder who became Famine, Death or War themself can never be eaten).
    /// </summary>
    public static bool IsEligibleHolder(byte bakerId, PlayerControl? holder)
    {
        if (holder == null || holder.PlayerId == bakerId || holder.HasDied() ||
            holder.HasModifier<InvulnerabilityModifier>())
        {
            return false;
        }

        var baker = MiscUtils.PlayerById(bakerId);
        return baker == null || !ApocalypseUtils.AreAllied(baker, holder);
    }

    /// <summary>
    /// Eligible players holding bread from this Baker - what the Baker's progress, the Famine's targets and the
    /// Famine's unlock all count.
    /// </summary>
    public static int CountFor(byte bakerId)
    {
        var count = 0;
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (IsEligibleHolder(bakerId, player) && HasBreadFrom(player, bakerId))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Every way bread goes away - eaten by Famine, cleansed by a Cleric or Doctor - ends here, on every client:
    /// the Baker's mark goes with it, and the host rechecks the Famine it belonged to.
    /// </summary>
    public override void OnDeactivate()
    {
        Removing = true;
        if (Player != null && Player.HasModifier<BakerBreadRevealModifier>())
        {
            Player.RemoveModifier<BakerBreadRevealModifier>();
        }

        Events.Neutral.BakerEvents.QueueBreadReevaluation(BakerId);
    }
}
