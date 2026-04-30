using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Modifiers;
using TouMegaChujoweExtension.Roles.Crewmate;
using TouMegaChujoweExtension.Options.Roles.Crewmate;
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
using MiraAPI.Hud;
using TownOfUs.Buttons;
using Reactor.Utilities;
using HarmonyLib;
using TouMegaChujoweExtension.Buttons.Neutral;

namespace TouMegaChujoweExtension.Events.Universal;

public static class ShieldEvents
{
    [RegisterEvent(Priority.Last)]
    public static void BeforeMurderEventHandler(BeforeMurderEvent @event)
    {
        var source = @event.Source;
        var target = @event.Target;

        if (source == null || target == null || MeetingHud.Instance != null || ExileController.Instance != null) return;

        var shieldType = target.GetShieldType();
        if (shieldType == ShieldType.None) return;

        // Check for Bodyguard specific option "Can Kill Crew Killing"
        if (shieldType == ShieldType.Bodyguard && source.Data.Role.GetRoleAlignment() == RoleAlignment.CrewmateKilling)
        {
            var options = OptionGroupSingleton<BodyguardOptions>.Instance;
            if (!options.CanKillCrewKilling)
            {
                // If option is OFF, the shield DOES NOT protect against Crewmate Killing roles.
                return;
            }
        }

        Logger<TouMegaChujoweExtensionPlugin>.Info($"[ShieldEvents] Murder from {source.Data.PlayerName} blocked by {shieldType} on {target.Data.PlayerName}");

        // Cancel the murder event
        @event.Cancel();

        // Handle specific shield behaviors (RPCs to notify owners)
        HandleShieldRpc(source, target, shieldType);

        // Show flash for the killer
        if (source.AmOwner)
        {
            ResetKillButton(source, shieldType);
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

    private static void ResetKillButton(PlayerControl source, ShieldType shieldType)
    {
        var saveCd = OptionGroupSingleton<GeneralOptions>.Instance.TempSaveCdReset;
        
        // Custom durations based on user request
        float duration = saveCd;
        switch (shieldType)
        {
            case ShieldType.Mirrorcaster:
                duration = source.GetKillCooldown(); // Full CD
                break;
            case ShieldType.Bodyguard:
                duration = 10f;
                break;
            case ShieldType.FirstDead:
                duration = 5.0f;
                break;
            case ShieldType.Cleric:
                duration = 5f;
                break;
            case ShieldType.Warden:
                duration = 1f;
                break;
            case ShieldType.Medic:
            case ShieldType.Fairy:
                duration = saveCd; // Usually 5s
                break;
            // Others use default saveCd
        }
        
        // Reset vanilla kill button
        if (HudManager.Instance != null && HudManager.Instance.KillButton != null)
        {
            source.SetKillTimer(duration);
        }

        // Reset custom KILL buttons only
        try
        {
            foreach (var button in CustomButtonManager.Buttons)
            {
                if (button != null && button.Button != null && button.Button.gameObject.activeSelf)
                {
                    if (button is IKillButton || button is PelicanSwallowButton)
                    {
                        button.Timer = duration;
                        try { button.SetUses(button.UsesLeft); } catch { }
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Logger<TouMegaChujoweExtensionPlugin>.Error($"[ShieldEvents] Error resetting custom buttons: {ex.Message}");
        }
    }
}
