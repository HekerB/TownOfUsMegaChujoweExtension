using HarmonyLib;
using MiraAPI;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Roles;
using MiraAPI.Utilities;
using TownOfUs;
using TownOfUs.Utilities.Appearances;
using TownOfUs.Modifiers.Game.Alliance;
using TownOfUs.Patches;
using TownOfUs.Utilities;
using TownOfUs.Roles;
using TownOfUs.Roles.Neutral;
using TownOfUs.Roles.Other;
using TownOfUs.Options;
using TownOfUs.Modifiers;
using TownOfUs.Modifiers.Neutral;
using TownOfUs.Options.Roles.Crewmate;
using UnityEngine;
using TouMegaChujoweExtension.Options;
using System.Linq;
using Reactor.Utilities.Extensions;
using TMPro;

namespace TouMegaChujoweExtension.Patches.Roles.Lovers;

[HarmonyPatch(typeof(TownOfUs.Modules.Components.HudManagerHelper), nameof(TownOfUs.Modules.Components.HudManagerHelper.UpdateRoleNameText))]
public static class LoverKnowsRolePatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        var extGenOpt = OptionGroupSingleton<ExtensionGeneralOptions>.Instance;
        if (extGenOpt == null || !extGenOpt.LoversKnowEachOthersRoles) return;

        var localPlayer = PlayerControl.LocalPlayer;
        if (localPlayer == null || localPlayer.Data == null) return;

        if (!localPlayer.TryGetModifier<LoverModifier>(out var localLoverMod) || localLoverMod == null) return;

        var otherLover = localLoverMod.OtherLover;
        if (otherLover == null || otherLover.Data == null || otherLover.Data.Role == null) return;

        // The local player is a lover and their other lover exists.
        // We want to make sure the other lover's role and color are displayed to the local player.
        if (MeetingHud.Instance)
        {
            foreach (var playerVA in MeetingHud.Instance.playerStates)
            {
                if (playerVA != null && playerVA.TargetPlayerId == otherLover.PlayerId)
                {
                    UpdateLoverNameTextMeeting(playerVA, otherLover);
                }
            }
        }
        else
        {
            UpdateLoverNameTextInGame(otherLover);
        }
    }

    private static void UpdateLoverNameTextMeeting(PlayerVoteArea playerVA, PlayerControl otherLover)
    {
        var colorPlayerNames = LocalSettingsTabSingleton<TownOfUsLocalSettings>.Instance.ColorPlayerNameToggle.Value;
        var playerName = otherLover.GetDefaultAppearance().PlayerName ?? "Unknown";
        var playerColor = Color.white;

        playerColor = playerColor.UpdateTargetColor(otherLover);
        playerName = playerName.UpdateTargetSymbols(otherLover);
        playerName = playerName.UpdateProtectionSymbols(otherLover);
        playerName = playerName.UpdateAllianceSymbols(otherLover);
        playerName = playerName.UpdateStatusSymbols(otherLover);

        var role = otherLover.Data.Role;
        if (role == null) return;
        var color = role.TeamColor;
        var roleName = $"<size=80%>{color.ToTextColor()}{otherLover.Data.Role.GetRoleName()}</color></size>";

        if (!otherLover.HasModifier<VampireBittenModifier>() && role is VampireRole)
        {
            roleName += "<size=80%><color=#FFFFFF> (<color=#A22929>OG</color>)</color></size>";
        }

        if (otherLover.IsCrewmate() || otherLover.Data.Role is SpectreRole)
        {
            roleName += $" <size=80%>{otherLover.TaskInfo()}</size>";
        }

        if (otherLover.Data.IsDead && otherLover.TryGetModifier<DeathHandlerModifier>(out var deathMod))
        {
            var deathReason = $"<size=60%>『{Color.yellow.ToTextColor()}{deathMod.CauseOfDeath}</color>』</size>\n";
            roleName = $"{deathReason}{roleName}";
        }

        if (!string.IsNullOrEmpty(roleName))
        {
            if (colorPlayerNames)
            {
                playerName = $"{roleName}\n{color.ToTextColor()}<size=92%>{playerName}</size></color>";
            }
            else
            {
                playerName = $"{roleName}\n<size=92%>{playerName}</size>";
            }
        }

        playerVA.NameText.text = playerName;
        playerVA.NameText.color = playerColor;
    }

    private static void UpdateLoverNameTextInGame(PlayerControl otherLover)
    {
        var colorPlayerNames = LocalSettingsTabSingleton<TownOfUsLocalSettings>.Instance.ColorPlayerNameToggle.Value;
        var playerName = otherLover.GetAppearance().PlayerName ?? "Unknown";
        var playerColor = Color.white;

        var isVisible = (PlayerControl.LocalPlayer.TryGetModifier<DeathHandlerModifier>(out var deathHandler) &&
                         !deathHandler.DiedThisRound) || TutorialManager.InstanceExists;

        playerColor = playerColor.UpdateTargetColor(otherLover, !isVisible);
        playerName = playerName.UpdateTargetSymbols(otherLover, !isVisible);
        playerName = playerName.UpdateProtectionSymbols(otherLover, !isVisible);
        playerName = playerName.UpdateAllianceSymbols(otherLover, !isVisible);
        playerName = playerName.UpdateStatusSymbols(otherLover, !isVisible);

        var role = otherLover.Data.Role;
        if (role == null) return;
        var color = role.TeamColor;
        var roleName = $"<size=80%>{color.ToTextColor()}{otherLover.Data.Role.GetRoleName()}</color></size>";

        if (!otherLover.HasModifier<VampireBittenModifier>() && role is VampireRole)
        {
            roleName += "<size=80%><color=#FFFFFF> (<color=#A22929>OG</color>)</color></size>";
        }

        if (otherLover.IsCrewmate() || otherLover.Data.Role is SpectreRole)
        {
            roleName += $" <size=80%>{otherLover.TaskInfo()}</size>";
        }

        var canSeeDeathReason = false;
        if (otherLover.Data.IsDead && isVisible && otherLover.TryGetModifier<DeathHandlerModifier>(out var deathMod))
        {
            var deathReason = $"<size=75%>『{Color.yellow.ToTextColor()}{deathMod.CauseOfDeath}</color>』</size>\n";
            roleName = $"{deathReason}{roleName}";
            canSeeDeathReason = true;
        }

        if (canSeeDeathReason)
        {
            playerName += $"\n<size=75%> </size>";
        }

        if (!string.IsNullOrEmpty(roleName))
        {
            playerName = colorPlayerNames
                ? $"{roleName}\n{color.ToTextColor()}{playerName}</color>"
                : $"{roleName}\n{playerName}";
        }

        otherLover.cosmetics.nameText.text = playerName;
        otherLover.cosmetics.nameText.color = playerColor;
    }
}
