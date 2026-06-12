using AmongUs.GameOptions;
using HarmonyLib;
using MiraAPI.Roles;
using Reactor.Utilities.Extensions;
using TownOfUs;
using TownOfUs.Extensions;
using TownOfUs.Modifiers.Crewmate;
using TownOfUs.Modifiers.Game.Alliance;
using TownOfUs.Modules.Localization;
using TownOfUs.Patches;
using TownOfUs.Patches.Options;
using TownOfUs.Events.Impostor;
using TownOfUs.Roles;
using TownOfUs.Roles.Impostor;
using TownOfUs.Utilities;
using UnityEngine;
using TouMegaChujoweExtension.Roles.Classic.Crewmate;
using TouMegaChujoweExtension.Utilities;

namespace TouMegaChujoweExtension.Patches.Roles.Agent
{
    [HarmonyPatch(typeof(PlayerRoleTextExtensions))]
    public static class AgentPatches
    {
        [HarmonyPatch(nameof(PlayerRoleTextExtensions.UpdateTargetColor), typeof(Color), typeof(PlayerControl), typeof(bool))]
        [HarmonyPostfix]
        public static void UpdateTargetColorPostfix(ref Color __result, PlayerControl player, bool hidden)
        {
            var local = PlayerControl.LocalPlayer;
            if (player == null || local == null || local.Data == null)
                return;

            if (local.IsRole<AgentRole>() && player.PlayerId != local.PlayerId)
            {
                __result = Color.white;
                return;
            }

            if (player.IsRole<AgentRole>() && local.IsImpostorAligned() && player.PlayerId != local.PlayerId)
            {
                __result = Palette.ImpostorRed;
            }
        }

        [HarmonyPatch(nameof(PlayerRoleTextExtensions.UpdateTargetSymbols), typeof(string), typeof(PlayerControl), typeof(bool))]
        [HarmonyPostfix]
        public static void UpdateTargetSymbolsPostfix(ref string __result, PlayerControl player, bool hidden)
        {
            var local = PlayerControl.LocalPlayer;
            if (player == null || local == null || local.Data == null)
                return;

            if (player.IsRole<AgentRole>() && local.IsImpostorAligned() && player.PlayerId != local.PlayerId)
            {
                var role = player.Data.Role;
                if (role != null)
                {
                    string roleName = role is ITownOfUsRole touRole ? touRole.RoleName : (role as ICustomRole)?.RoleName ?? "Agent";
                    if (!string.IsNullOrEmpty(__result) && __result.Contains(roleName))
                    {
                        __result = __result.Replace(roleName, "");
                    }
                }

                var crewpostorName = TouLocale.Get("TouModifierCrewpostorShortName", "Imp");
                if (AgentUtils.AppearsAsCrewpostor && !__result.Contains(crewpostorName))
                {
                    __result += $"<color=#FFFFFF> (<color=#D64042>{crewpostorName}</color>)</color>";
                }
            }
        }
    }

    [HarmonyPatch(typeof(HudManagerPatches), nameof(HudManagerPatches.UpdateRoleNameText))]
    public static class AgentFakeRoleNamePatch
    {
        [HarmonyPostfix]
        public static void UpdateRoleNameTextPostfix()
        {
            var local = PlayerControl.LocalPlayer;
            if (local == null || local.IsRole<AgentRole>() || !local.IsImpostorAligned())
            {
                return;
            }

            if (MeetingHud.Instance)
            {
                foreach (var playerVA in MeetingHud.Instance.playerStates)
                {
                    var agent = MiscUtils.PlayerById(playerVA.TargetPlayerId);
                    if (agent?.Data?.Role == null ||
                        !agent.IsRole<AgentRole>() ||
                        agent.PlayerId == local.PlayerId)
                    {
                        continue;
                    }

                    var fakeRoleText = AgentUtils.GetFakeCrewpostorRoleText(agent);
                    if (!playerVA.NameText.text.Contains(fakeRoleText))
                    {
                        playerVA.NameText.text = $"{fakeRoleText}\n{playerVA.NameText.text}";
                    }
                }

                return;
            }

            foreach (var agent in PlayerControl.AllPlayerControls.ToArray())
            {
                if (agent?.Data?.Role == null ||
                    agent.cosmetics?.nameText == null ||
                    !agent.IsRole<AgentRole>() ||
                    agent.PlayerId == local.PlayerId)
                {
                    continue;
                }

                var fakeRoleText = AgentUtils.GetFakeCrewpostorRoleText(agent);
                if (!agent.cosmetics.nameText.text.Contains(fakeRoleText))
                {
                    agent.cosmetics.nameText.text = $"{fakeRoleText}\n{agent.cosmetics.nameText.text}";
                }
            }
        }
    }

    [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.CoBegin))]
    public static class AgentFakeRoleResetPatch
    {
        [HarmonyPrefix]
        public static void CoBeginPrefix()
        {
            AgentUtils.ResetFakeRoles();
        }
    }

    [HarmonyPatch(typeof(ToBecomeTraitorModifier), nameof(ToBecomeTraitorModifier.AssignTargets))]
    public static class AgentBlocksTraitorPatch
    {
        public static bool Prefix()
        {
            return !AgentUtils.AgentCanSpawn();
        }
    }

    [HarmonyPatch(typeof(CrewpostorModifier), nameof(CrewpostorModifier.AssignTargets))]
    public static class AgentBlocksCrewpostorPatch
    {
        public static bool Prefix()
        {
            return !AgentUtils.AgentCanSpawn();
        }
    }

    [HarmonyPatch(typeof(TeamChatPatches), nameof(TeamChatPatches.RpcSendImpTeamChat))]
    public static class AgentImpostorChatPatch
    {
        [HarmonyPostfix]
        public static void RpcSendImpTeamChatPostfix(PlayerControl player, string text)
        {
            var local = PlayerControl.LocalPlayer;
            if (local == null ||
                player?.Data == null ||
                !local.IsRole<AgentRole>() ||
                local.IsImpostorAligned() ||
                !AgentUtils.CanUseImpostorChat)
            {
                return;
            }

            MiscUtils.AddTeamChat(
                local.Data,
                $"<color=#{TownOfUsColors.ImpSoft.ToHtmlStringRGBA()}>{AgentUtils.GetAnonymousImpostorChatTitle(player)}</color>",
                text,
                bubbleType: BubbleType.Impostor,
                onLeft: !player.AmOwner);

            if (!MeetingHud.Instance)
            {
                return;
            }

            var chats = TeamChatPatches.TeamChatManager.GetAllAvailableChats();
            var hasForcedChat = chats.Any(c => c.IsForced);
            var currentChat = TeamChatPatches.CurrentChatIndex >= 0 && TeamChatPatches.CurrentChatIndex < chats.Count
                ? chats[TeamChatPatches.CurrentChatIndex]
                : null;

            if ((!TeamChatPatches.TeamChatActive || currentChat == null || currentChat.Priority != 30) && !hasForcedChat)
            {
                TeamChatPatches.TeamChatManager.MarkChatAsUnread(30);
            }
        }
    }

    [HarmonyPatch(typeof(ImpostorRole), nameof(ImpostorRole.IsValidTarget))]
    public static class AgentImpostorTargetPatch
    {
        [HarmonyPostfix]
        public static void IsValidTargetPostfix(NetworkedPlayerInfo target, ref bool __result)
        {
            var local = PlayerControl.LocalPlayer;
            if (local != null &&
                local.IsImpostorAligned() &&
                target?.Object != null &&
                target.Object.PlayerId != local.PlayerId &&
                target.Object.IsRole<AgentRole>())
            {
                __result = false;
            }
        }
    }

    [HarmonyPatch(typeof(AmbassadorRole), "IsExempt")]
    public static class AgentAmbassadorExemptPatch
    {
        [HarmonyPostfix]
        public static void IsExemptPostfix(PlayerVoteArea voteArea, ref bool __result)
        {
            var target = voteArea != null ? MiscUtils.PlayerById(voteArea.TargetPlayerId) : null;
            if (target != null && target.IsRole<AgentRole>())
            {
                __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(AmbassadorRole), nameof(AmbassadorRole.Click))]
    public static class AgentAmbassadorClickPatch
    {
        [HarmonyPrefix]
        public static bool ClickPrefix(PlayerVoteArea voteArea)
        {
            var target = voteArea != null ? MiscUtils.PlayerById(voteArea.TargetPlayerId) : null;
            return target == null || !target.IsRole<AgentRole>();
        }
    }

    [HarmonyPatch(typeof(AmbassadorRole), "RpcRetrain")]
    public static class AgentAmbassadorRetrainPatch
    {
        [HarmonyPrefix]
        public static bool RpcRetrainPrefix(byte playerId)
        {
            if (playerId == byte.MaxValue)
            {
                return true;
            }

            var target = MiscUtils.PlayerById(playerId);
            return target == null || !target.IsRole<AgentRole>();
        }
    }

    [HarmonyPatch(typeof(AmbassadorRole), nameof(AmbassadorRole.RpcRetrainConfirm))]
    public static class AgentAmbassadorRetrainConfirmPatch
    {
        [HarmonyPrefix]
        public static bool RpcRetrainConfirmPrefix(PlayerControl player)
        {
            return player == null || !player.IsRole<AgentRole>();
        }
    }

    [HarmonyPatch(typeof(AmbassadorEvents), nameof(AmbassadorEvents.RoundStartEventHandler))]
    public static class AgentAmbassadorRoundStartPatch
    {
        [HarmonyPrefix]
        public static void RoundStartEventHandlerPrefix()
        {
            foreach (var ambassador in CustomRoleUtils.GetActiveRolesOfType<AmbassadorRole>())
            {
                if (ambassador?.SelectedPlr?._object != null &&
                    ambassador.SelectedPlr._object.IsRole<AgentRole>())
                {
                    ambassador.Clear();
                }
            }
        }
    }
}
