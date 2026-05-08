using MiraAPI.Modifiers;
using MiraAPI.GameOptions;
using Reactor.Utilities;
using TouMegaChujoweExtension.Modifiers;
using TouMegaChujoweExtension.Modifiers.Universal;
using TouMegaChujoweExtension.Roles.Crewmate;
using TownOfUs.Modifiers;
using TownOfUs.Modifiers.Neutral;
using TownOfUs.Utilities;
using TownOfUs.Modifiers.Crewmate;
using TownOfUs.Modifiers.Game.Crewmate;
using TownOfUs.Modifiers.Game.Impostor;
using TownOfUs.Options.Modifiers.Impostor;
using TownOfUs.Options.Roles.Neutral;
using TownOfUs.Options;
using TownOfUs.Roles;
using TownOfUs.Roles.Crewmate;
using UnityEngine;
using System.Reflection;
using System.Collections;
using MiraAPI.Hud;
using TouMegaChujoweExtension.Roles.Neutral;
using TownOfUs.Buttons;

namespace TouMegaChujoweExtension.Utilities;

public enum ShieldType
{
    None,
    Medic,
    Warden,
    Mirrorcaster,
    Bodyguard,
    FirstDead,
    Child,
    Fairy,
    Mercenary,
    Oracle,
    DeadlyQuota,
    Cleric
}

public static class ShieldUtils
{
    public static ShieldType GetShieldType(this PlayerControl player)
    {
        if (player == null) return ShieldType.None;

        if (player.HasModifier<FirstDeadShield>()) return ShieldType.FirstDead;
        if (player.HasModifier<MedicShieldModifier>()) return ShieldType.Medic;
        if (player.HasModifier<WardenFortifiedModifier>()) return ShieldType.Warden;
        if (player.HasModifier<MagicMirrorModifier>()) return ShieldType.Mirrorcaster;
        if (player.HasModifier<BodyguardShieldModifier>()) return ShieldType.Bodyguard;
        var child = player.GetModifiers<ChildModifier>().FirstOrDefault();
        if (child != null && !child.IsAdult) return ShieldType.Child;
        if (player.HasModifier<GuardianAngelProtectModifier>()) return ShieldType.Fairy;
        if (player.HasModifier<MercenaryGuardModifier>() && 
            OptionGroupSingleton<MercenaryOptions>.Instance.GuardProtection.Value) return ShieldType.Mercenary;
        var deadlyQuota = player.GetModifiers<DeadlyQuotaModifier>().FirstOrDefault();
        if (deadlyQuota != null)
        {
            var hasShieldOpt = OptionGroupSingleton<DeadlyQuotaOptions>.Instance.QuotaShield;
            var underQuota = deadlyQuota.KillCount < deadlyQuota.KillQuota;
            if (hasShieldOpt && underQuota) return ShieldType.DeadlyQuota;
        }
        if (player.HasModifier<ClericBarrierModifier>()) return ShieldType.Cleric;

        return ShieldType.None;
    }

    public static Color GetFlashColor(ShieldType shieldType)
    {
        return shieldType switch
        {
            ShieldType.Medic => TouExtensionColors.ShieldFlashes.Medic,
            ShieldType.Warden => TouExtensionColors.ShieldFlashes.Warden,
            ShieldType.Mirrorcaster => TouExtensionColors.ShieldFlashes.Mirrorcaster,
            ShieldType.Bodyguard => TouExtensionColors.ShieldFlashes.Bodyguard,
            ShieldType.FirstDead => Color.clear,
            ShieldType.Fairy => TouExtensionColors.ShieldFlashes.Fairy,
            ShieldType.Mercenary => TouExtensionColors.ShieldFlashes.Mercenary,
            ShieldType.Oracle => TouExtensionColors.ShieldFlashes.Oracle,
            ShieldType.Cleric => TouExtensionColors.ShieldFlashes.Cleric,
            _ => Color.clear
        };
    }

    private static SpriteRenderer _localFlashRenderer;

    public static float LastShieldTriggerTime = 0f;
    public static byte LastShieldTargetId = 255;

    public static void TriggerShieldFlash(PlayerControl killer, ShieldType shieldType)
    {
        if (killer == null || !killer.AmOwner || shieldType == ShieldType.None || 
            shieldType == ShieldType.DeadlyQuota || shieldType == ShieldType.Child) return;

        // Anti-spam guard for visual flash
        // We use a small threshold to prevent double flashes from overlapping systems
        if (Time.time - LastShieldTriggerTime < 0.05f) return;

        var color = GetFlashColor(shieldType);
        if (color != Color.clear)
        {
            Logger<TouMegaChujoweExtensionPlugin>.Info($"[ShieldUtils] Triggering {shieldType} flash for {killer.Data.PlayerName}");
            Coroutines.Start(CoFlashLocal(color));
        }
    }

    private static System.Collections.IEnumerator CoFlashLocal(Color color)
    {
        if (HudManager.Instance == null || HudManager.Instance.FullScreen == null) yield break;
        
        if (_localFlashRenderer == null)
        {
            _localFlashRenderer = UnityEngine.Object.Instantiate(HudManager.Instance.FullScreen, HudManager.Instance.FullScreen.transform.parent);
            _localFlashRenderer.transform.localScale *= 25f;
            _localFlashRenderer.name = "TouExtensionFlashRenderer";
        }
        
        var flashColor = color;
        flashColor.a = 0.5f;
        _localFlashRenderer.color = flashColor;
        _localFlashRenderer.gameObject.SetActive(true);

        yield return new WaitForSeconds(0.5f);

        if (_localFlashRenderer != null)
        {
            _localFlashRenderer.gameObject.SetActive(false);
        }
    }
    public static bool HandleButtonShieldClick(object button, PlayerControl target)
    {
        if (!InternalHandleShieldHit(target, out float duration)) return false;
        
        if (button != null)
        {
            try
            {
                var prop = button.GetType().GetProperty("Timer", BindingFlags.Instance | BindingFlags.Public);
                if (prop != null && prop.CanWrite)
                {
                    prop.SetValue(button, duration);
                }
            }
            catch (Exception ex)
            {
                Logger<TouMegaChujoweExtensionPlugin>.Error($"[ShieldUtils] Failed to set button timer: {ex.Message}");
            }
        }
        return true;
    }

    private static bool InternalHandleShieldHit(PlayerControl target, out float duration)
    {
        duration = 0f;
        if (target == null) return false;
        var attacker = PlayerControl.LocalPlayer;
        if (attacker == null) return false;

        var shieldType = target.GetShieldType();
        if (shieldType == ShieldType.None) return false;

        // Anti-multi-trigger guard
        if (Time.time - LastShieldTriggerTime < 0.1f && LastShieldTargetId == target.PlayerId)
        {
            duration = 10f; // Default safety
            return true; 
        }
        LastShieldTriggerTime = Time.time;
        LastShieldTargetId = target.PlayerId;

        Logger<TouMegaChujoweExtension.TouMegaChujoweExtensionPlugin>.Info($"[ShieldUtils] Button shield hit detected on {target.Data.PlayerName} (Shield: {shieldType})");

        // Trigger flash for the attacker
        TriggerShieldFlash(attacker, shieldType);

        // Notify Bodyguard/Medic etc.
        HandleShieldRpc(attacker, target, shieldType);

        // Calculate cooldown
        var saveCd = OptionGroupSingleton<GeneralOptions>.Instance.TempSaveCdReset;
        duration = saveCd;
        switch (shieldType)
        {
            case ShieldType.Medic:
            case ShieldType.Bodyguard:
            case ShieldType.Fairy:
            case ShieldType.Mercenary:
                duration = 10f;
                break;
            case ShieldType.Warden:
                duration = 1f;
                break;
            case ShieldType.Cleric:
                duration = 5f;
                break;
            case ShieldType.Mirrorcaster:
                duration = attacker.GetKillCooldown();
                break;
            default:
                duration = saveCd;
                break;
        }

        return true;
    }

    private static void HandleShieldRpc(PlayerControl source, PlayerControl target, ShieldType shieldType)
    {
        switch (shieldType)
        {
            case ShieldType.Bodyguard:
                var bgMod = target.GetModifiers<BodyguardShieldModifier>().FirstOrDefault();
                if (bgMod != null && bgMod.Bodyguard != null)
                {
                    BodyguardRole.RpcBodyguardShieldAttacked(bgMod.Bodyguard, source, target);
                }
                break;

            case ShieldType.Medic:
                var medicMod = target.GetModifiers<MedicShieldModifier>().FirstOrDefault();
                if (medicMod != null)
                {
                    MedicRole.RpcMedicShieldAttacked(medicMod.Medic, source, target);
                }
                break;

            case ShieldType.Warden:
                var wardenMod = target.GetModifiers<WardenFortifiedModifier>().FirstOrDefault();
                if (wardenMod != null)
                {
                    WardenRole.RpcWardenNotify(wardenMod.Warden, source, target);
                }
                break;
        }
    }

    public static bool HandleToURoleButtonPrefix(TownOfUsButton __instance)
    {
        if (__instance == null) return true;
        
        var bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var target = (__instance.GetType().GetProperty("Target", bindingFlags)?.GetValue(__instance) ?? 
                    __instance.GetType().GetField("Target", bindingFlags)?.GetValue(__instance) ??
                    __instance.GetType().GetField("_target", bindingFlags)?.GetValue(__instance)) as PlayerControl;

        if (target == null) return true;
        if (!IsHarmfulInteraction(__instance, target)) return true;

        return !HandleButtonShieldClick(__instance, target);
    }

    public static bool IsHarmfulInteraction(object button, PlayerControl? target)
    {
        if (button == null || target == null) return false;
        
        // 1. Explicitly harmful button types
        if (button is IKillButton) return true;
        
        var buttonType = button.GetType();
        while (buttonType != null)
        {
            if (buttonType.Name.StartsWith("TownOfUsKillRoleButton")) return true;
            buttonType = buttonType.BaseType;
        }

        // 2. Local player role alignment
        var localPlayer = PlayerControl.LocalPlayer;
        if (localPlayer == null || localPlayer.Data == null) return false;
        
        var role = localPlayer.Data.Role;
        if (role == null) return false;

        if (localPlayer.IsImpostor() || localPlayer.Is(RoleAlignment.NeutralKilling)) return true;
        
        // Special extension roles that are harmful but might not be NK alignment (depending on config)
        if (role is PelicanRole or ShifterRole) return true;
        
        // Crewmate roles that can kill
        if (localPlayer.IsRole<SheriffRole>() || 
            localPlayer.IsRole<OfficerRole>() || 
            localPlayer.IsRole<HunterRole>()) return true;

        return false;
    }
}
