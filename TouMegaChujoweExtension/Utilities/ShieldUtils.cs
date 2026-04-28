using MiraAPI.Modifiers;
using MiraAPI.GameOptions;
using Reactor.Utilities;
using TouMegaChujoweExtension.Modifiers;
using TouMegaChujoweExtension.Modifiers.Universal;
using TownOfUs.Modifiers;
using TownOfUs.Modifiers.Neutral;
using TownOfUs.Utilities;
using TownOfUs.Modifiers.Crewmate;
using TownOfUs.Modifiers.Game.Crewmate;
using TownOfUs.Modifiers.Game.Impostor;
using TownOfUs.Options.Modifiers.Impostor;
using TownOfUs.Options.Roles.Neutral;
using UnityEngine;

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
    Cleric,
    SchrodingersCat,
    Doctor
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
        if (player.TryGetModifier<ChildModifier>(out var child) && !child.IsAdult) return ShieldType.Child;
        if (player.HasModifier<GuardianAngelProtectModifier>()) return ShieldType.Fairy;
        if (player.HasModifier<MercenaryGuardModifier>() && 
            OptionGroupSingleton<MercenaryOptions>.Instance.GuardProtection.Value) return ShieldType.Mercenary;
        if (player.HasModifier<TownOfUs.Modifiers.Crewmate.OracleBlessedModifier>()) return ShieldType.Oracle;
        if (player.TryGetModifier<DeadlyQuotaModifier>(out var deadlyQuota))
        {
            var hasShieldOpt = OptionGroupSingleton<DeadlyQuotaOptions>.Instance.QuotaShield;
            var underQuota = deadlyQuota.KillCount < deadlyQuota.KillQuota;
            if (hasShieldOpt && underQuota) return ShieldType.DeadlyQuota;
        }
        if (player.HasModifier<ClericBarrierModifier>()) return ShieldType.Cleric;
        if (player.IsRole<SchrodingersCatRole>() && !player.GetRole<SchrodingersCatRole>().IsAdopted) return ShieldType.SchrodingersCat;
        if (player.HasModifier<DoctorShieldModifier>()) return ShieldType.Doctor;
        
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
            ShieldType.SchrodingersCat => TouExtensionColors.ShieldFlashes.SchrodingersCat,
            ShieldType.Doctor => TouExtensionColors.ShieldFlashes.Doctor,
            _ => Color.clear
        };
    }

    public static void TriggerShieldFlash(PlayerControl killer, ShieldType shieldType)
    {
        if (killer == null || !killer.AmOwner || shieldType == ShieldType.None || 
            shieldType == ShieldType.DeadlyQuota || shieldType == ShieldType.Child) return;

        var color = GetFlashColor(shieldType);
        if (color != Color.clear)
        {
            Coroutines.Start(MiscUtils.CoFlash(color));
        }
    }
}
