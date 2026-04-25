using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Modifiers;
using TouMegaChujoweExtension.Roles.Crewmate;
using TouMegaChujoweExtension.Utilities;
using TownOfUs.Modifiers;
using TownOfUs.Roles;
using TownOfUs.Roles.Crewmate;
using TownOfUs.Roles.Neutral;
using TownOfUs.Modifiers.Crewmate;
using TownOfUs.Modifiers.Game.Crewmate;
using TownOfUs.Utilities;
using TownOfUs.Extensions;
using TownOfUs.Options;
using MiraAPI.GameOptions;
using Reactor.Utilities;
using HarmonyLib;

namespace TouMegaChujoweExtension.Events.Universal;

public static class ShieldEvents
{
    [RegisterEvent(Priority.First)]
    public static void BeforeMurderEventHandler(BeforeMurderEvent @event)
    {
        var source = @event.Source;
        var target = @event.Target;

        if (source == null || target == null || MeetingHud.Instance != null || ExileController.Instance != null) return;

        var shieldType = target.GetShieldType();
        if (shieldType == ShieldType.None) return;

        // Cancel the murder event
        @event.Cancel();

        // Handle specific shield behaviors (RPCs to notify owners)
        HandleShieldRpc(source, target, shieldType);

        // Show flash for the killer
        if (source.AmOwner)
        {
            ResetKillButton(source);
            ShieldUtils.TriggerShieldFlash(source, shieldType);
        }
    }

    private static void HandleShieldRpc(PlayerControl source, PlayerControl target, ShieldType shieldType)
    {
        switch (shieldType)
        {
            case ShieldType.Bodyguard:
                if (target.TryGetModifier<BodyguardShieldModifier>(out var bgMod) && bgMod.Bodyguard != null)
                {
                    BodyguardRole.RpcBodyguardShieldAttacked(bgMod.Bodyguard, source, target);
                }
                break;

            case ShieldType.Medic:
                if (target.TryGetModifier<MedicShieldModifier>(out var medicMod))
                {
                    MedicRole.RpcMedicShieldAttacked(medicMod.Medic, source, target);
                }
                break;

            case ShieldType.Warden:
                if (target.TryGetModifier<WardenFortifiedModifier>(out var wardenMod))
                {
                    WardenRole.RpcWardenNotify(wardenMod.Warden, source, target);
                }
                break;

            case ShieldType.Mirrorcaster:
                if (target.TryGetModifier<MagicMirrorModifier>(out var mirrorMod))
                {
                    MirrorcasterRole.RpcMagicMirrorAttacked(mirrorMod.Mirrorcaster, source, target);
                }
                break;

            case ShieldType.Oracle:
                if (target.TryGetModifier<TownOfUs.Modifiers.Crewmate.OracleBlessedModifier>(out var oracleMod))
                {
                    OracleRole.RpcOracleBlessNotify(source, oracleMod.Oracle, target);
                }
                break;

            case ShieldType.Cleric:
                if (target.TryGetModifier<ClericBarrierModifier>(out var clericMod))
                {
                    ClericRole.RpcClericBarrierAttacked(clericMod.Cleric, source, target);
                }
                break;
        }
    }

    private static void ResetKillButton(PlayerControl source)
    {
        if (HudManager.Instance == null || HudManager.Instance.KillButton == null) return;
        var reset = OptionGroupSingleton<GeneralOptions>.Instance.TempSaveCdReset;
        source.SetKillTimer(reset);
    }
}
