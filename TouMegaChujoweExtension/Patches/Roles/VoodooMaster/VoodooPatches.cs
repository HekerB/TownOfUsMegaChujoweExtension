using HarmonyLib;
using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Modifiers.Impostor;
using TownOfUs.Patches.Roles;
using TownOfUs.Modules;
using TMPro;

namespace TouMegaChujoweExtension.Patches.Roles.VoodooMaster;

[HarmonyPatch]
public static class VoodooPatches
{
    [HarmonyPatch(typeof(ChatController), nameof(ChatController.SendChat))]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    public static void SendChatDeafenedPrefix(ChatController __instance)
    {
        if (PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.HasModifier<VoodooDeafenedModifier>())
        {
            if (__instance.freeChatField?.textArea != null)
            {
                __instance.freeChatField.textArea.SetText("...");
            }
        }
    }

    [HarmonyPatch(typeof(ChatController), nameof(ChatController.UpdateChatMode))]
    [HarmonyPostfix]
    public static void UpdateChatModeVoodooPostfix(ChatController __instance)
    {
        if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data.IsDead)
        {
            return;
        }

        var field = typeof(ChatControllerPatches).GetField("_noticeText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var noticeText = (TextMeshPro?)field?.GetValue(null);

        if (PlayerControl.LocalPlayer.HasModifier<VoodooMutedModifier>())
        {
            if (noticeText != null)
            {
                noticeText.text = "You have been muted by a Voodoo curse.";
            }
            __instance.freeChatField.SetVisible(false);
            __instance.quickChatField.SetVisible(false);
        }
        else if (PlayerControl.LocalPlayer.HasModifier<VoodooDeafenedModifier>())
        {
            if (noticeText != null)
            {
                noticeText.text = "You are deafened. Your messages will be replaced with '...'.";
            }
        }
    }

    [HarmonyPatch(typeof(PlayerVoteArea), nameof(PlayerVoteArea.Select))]
    [HarmonyPrefix]
    public static bool PlayerVoteAreaSelectPrefix(PlayerVoteArea __instance)
    {
        if (PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.HasModifier<VoodooDeafenedModifier>())
        {
            if (MeetingHud.Instance != null && !__instance.DidVote && MeetingHud.Instance.state == MeetingHud.VoteStates.NotVoted)
            {
                __instance.SetVote(__instance.TargetPlayerId);
                MeetingHud.Instance.Confirm(__instance.TargetPlayerId);
                return false;
            }
        }
        return true;
    }

    [HarmonyPatch(typeof(MeetingMenu), nameof(MeetingMenu.GenButtons))]
    [HarmonyPrefix]
    public static void MeetingMenuGenButtonsPrefix(MeetingMenu __instance, ref bool usable)
    {
        if (__instance.Owner != null && __instance.Owner.Player != null && __instance.Owner.Player.HasModifier<VoodooDeafenedModifier>())
        {
            usable = false;
        }
    }
}
