using HarmonyLib;
using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.GameOptions;
using TouMegaChujoweExtension.Options;
using TownOfUs.Events;
using TownOfUs.Modules.Localization;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches;

[HarmonyPatch(typeof(EmergencyMinigame), nameof(EmergencyMinigame.Update))]
public static class BlockFirstRoundEmergencyPatch
{
    private const int ReadyState = 0;
    private const int CooldownState = 1;

    private static float firstRoundStartedAt = -1f;
    private static bool appliedCooldown;

    [RegisterEvent]
    public static void RoundStartEventHandler(RoundStartEvent @event)
    {
        firstRoundStartedAt = DeathEventHandlers.CurrentRound <= 1 ? Time.time : -1f;
        appliedCooldown = false;
    }

    public static void Postfix(EmergencyMinigame __instance)
    {
        if (!OptionGroupSingleton<ExtensionGameMechanicOptions>.Instance.BlockFirstRoundEmergency)
        {
            Reset();
            return;
        }

        if (DeathEventHandlers.CurrentRound != 1)
        {
            Reset();
            return;
        }

        var gameOptions = GameOptionsManager.Instance?.currentNormalGameOptions;
        if (gameOptions == null || gameOptions.EmergencyCooldown <= 0f)
        {
            return;
        }

        if (firstRoundStartedAt < 0f)
        {
            firstRoundStartedAt = Time.time;
        }

        var remaining = gameOptions.EmergencyCooldown - (Time.time - firstRoundStartedAt);
        if (remaining > 0f)
        {
            SetCooldown(__instance, remaining);
            return;
        }

        if (appliedCooldown && __instance.state == CooldownState && CanReactivateButton())
        {
            __instance.ButtonActive = true;
            __instance.state = ReadyState;
            appliedCooldown = false;
        }
    }

    private static void SetCooldown(EmergencyMinigame minigame, float remaining)
    {
        var seconds = Mathf.CeilToInt(remaining).ToString();
        minigame.StatusText.text = TouLocale.GetParsed(
            "ExtensionFirstRoundEmergencyCooldownStatus",
            "Emergency meeting cooldown: <time>s").Replace("<time>", seconds);
        minigame.NumberText.text = string.Empty;
        minigame.ButtonActive = false;
        minigame.state = CooldownState;
        appliedCooldown = true;
    }

    private static bool CanReactivateButton()
    {
        var player = PlayerControl.LocalPlayer;
        return player != null &&
               player.RemainingEmergencies > 0 &&
               !PlayerTask.PlayerHasTaskOfType<IHudOverrideTask>(player);
    }

    private static void Reset()
    {
        firstRoundStartedAt = -1f;
        appliedCooldown = false;
    }
}
