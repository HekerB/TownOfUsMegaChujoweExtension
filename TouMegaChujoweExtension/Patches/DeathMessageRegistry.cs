using System.Collections;
using MiraAPI.Modifiers;
using Reactor.Utilities;
using TownOfUs.Modifiers;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Modules;

public static class DeathMessageRegistry
{
    private static readonly Dictionary<Type, Func<PlayerControl, PlayerControl, (string localeKey, string fallback)>>
        Registry = new();

    static DeathMessageRegistry()
    {
	// === REGISTER DEATH MESSAGES ===
	//
	// Poisoner: Supported by RpcSpecialMurder with causeOfDeath parameter
	// (no entry needed here)
	//
	// For roles that use RpcCustomMurder (MiraAPI) and need a custom death message:
	// Register<RoleName>("DiedToLocaleKey", "Fallback"); //
	// For roles that have ONE kill type:
	// DiedTo{LocaleKey} in en_US.xml is sufficient (TownOfUs auto-detection)
	//	
	// === REGISTER DEATH MESSAGES ===
	// Adding a new role = 1 line
	// Only roles that need to OVERWRITE the default DiedTo{LocaleKey}
	// A role with a simple single death message → DiedTo{LocaleKey} in en_US.xml is sufficient
	
	// Serial Killer — always "Murdered"


	// Poisner - depends on the kill type (poison/vine/regular)
	// Poisoner - poison/vine have custom, regular kill → do not override (TownOfUs will use DiedToPoisoner)
	// Poisoner: NOT HERE - handled directly in RpcPoisonKill
	// (because RpcCustomMurder with showKillAnim:false bypasses MurderPlayer)
	//
	

	
	
	
	// == Info ==
	// Though dunno if works
	// === END OF REGISTRATION ===
    }

    public static void Register<TRole>(string localeKey, string fallback) where TRole : class
    {
        Registry[typeof(TRole)] = (_, _) => (localeKey, fallback);
    }

    public static void RegisterDynamic<TRole>(
        Func<PlayerControl, PlayerControl, (string localeKey, string fallback)> resolver) where TRole : class
    {
        Registry[typeof(TRole)] = resolver;
    }

    public static void HandleMurder(PlayerControl killer, PlayerControl victim)
    {
        if (killer == null || victim == null) return;

        var roleType = killer.Data?.Role?.GetType();
        if (roleType == null) return;

        if (!Registry.TryGetValue(roleType, out var resolver)) return;

        var (localeKey, fallback) = resolver(killer, victim);
        if (string.IsNullOrEmpty(localeKey) && string.IsNullOrEmpty(fallback)) return;

        var causeOfDeath = !string.IsNullOrEmpty(localeKey)
            ? TouLocale.Get(localeKey)
            : fallback;

        if (causeOfDeath.Contains("STRMISS"))
            causeOfDeath = fallback;

        Coroutines.Start(CoOverrideDeathReason(victim, causeOfDeath));
    }

    private static IEnumerator CoOverrideDeathReason(PlayerControl target, string causeOfDeath)
    {
        yield return new WaitForSeconds(0.3f);

        var timeout = 2f;
        while (!target.HasModifier<DeathHandlerModifier>() && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        if (target.TryGetModifier<DeathHandlerModifier>(out var deathHandler))
        {
            deathHandler.CauseOfDeath = causeOfDeath;
            deathHandler.LockInfo = true;
        }
    }
}
