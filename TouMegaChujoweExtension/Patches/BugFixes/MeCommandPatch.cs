using AmongUs.GameOptions;
using HarmonyLib;
using MiraAPI.Modifiers.Types;
using MiraAPI.Modifiers;
using MiraAPI.Roles;
using MiraAPI.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;
using TMPro;
using TownOfUs.Modules.Components;
using TownOfUs.Modules.Wiki;
using TownOfUs.Patches.Misc;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.BugFixes;

[HarmonyPatch(typeof(ChatController), nameof(ChatController.SendChat))]
public static class MeCommandPatch
{
    private struct ModLinkInfo
    {
        public string Placeholder;
        public string Name;
        public string TypeFullName;
        public Color Color;
    }

    [HarmonyPrefix]
    [HarmonyPriority(Priority.First + 50)]
    public static bool Prefix(ChatController __instance)
    {
        var text = __instance.freeChatField.Text;
        if (string.IsNullOrEmpty(text)) return true;

        var spaceLess = text.Replace(" ", "").ToLower();
        if (spaceLess.StartsWith("/draft"))
        {
            HandleDraftCommand(__instance, text);
            return false;
        }

        if (spaceLess.StartsWith("/setname") && DraftLobbyPatch._draftInProgress)
        {
            DraftLobbyPatch.ShowSystemMessage("<color=#FF0000>Draft Mode</color>: You cannot change your name during the draft.");
            ClearChat(__instance);
            return false;
        }

        if (!spaceLess.StartsWith("/me")) return true;

        var player = PlayerControl.LocalPlayer;
        if (player == null || player.Data == null)
        {
            ClearChat(__instance);
            return false;
        }

        var modLinks = new List<ModLinkInfo>();
        var msg = BuildPlayerInfo(player, modLinks);

        ShowInfoBubble(player.Data, "<color=#FFD700>Player Info</color>", msg, modLinks);

        ClearChat(__instance);
        return false;
    }

    private static void HandleDraftCommand(ChatController chat, string text)
    {
        string[] args = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (args.Length < 2)
        {
            DraftLobbyPatch.ShowSystemMessage("<color=#FF0000>Draft Mode</color>: Usage: /draft [start|cancel]");
            ClearChat(chat);
            return;
        }

        string sub = args[1].ToLower();
        if (sub == "start")
        {
            if (AmongUsClient.Instance.AmHost)
            {
                DraftLobbyPatch.StartDraft();
            }
            else
            {
                DraftLobbyPatch.ShowSystemMessage("<color=#FF0000>Draft Mode</color>: Only the Host can start the draft.");
            }
        }
        else if (sub == "cancel" || sub == "end")
        {
            if (AmongUsClient.Instance.AmHost)
            {
                DraftNetworking.SendDraftCancel();
            }
            else
            {
                DraftLobbyPatch.ShowSystemMessage("<color=#FF0000>Draft Mode</color>: Only the Host can cancel the draft.");
            }
        }
        else
        {
            DraftLobbyPatch.ShowSystemMessage($"<color=#FF0000>Draft Mode</color>: Unknown command '{sub}'");
        }

        ClearChat(chat);
    }

    private static string BuildPlayerInfo(PlayerControl player, List<ModLinkInfo> modLinks)
    {
        var sb = new StringBuilder();
        var role = player.Data.Role;

        // === ROLE ===
        if (role != null)
        {
            string roleTag = MiscUtils.GetHyperlinkText(role);
            sb.AppendLine($"Role: {roleTag}");

            try
            {
                var alignment = role.GetRoleAlignment();
                sb.AppendLine($"Alignment: {FormatAlignment(alignment.ToString())}");
            }
            catch { /* Ignore reflection errors */ }
        }
        else
        {
            sb.AppendLine("Role: None");
        }

        // === MODIFIERS (placeholders for manual hyperlinks) ===
        try
        {
            var modifiers = player.GetModifiers<BaseModifier>();
            bool hasVisible = false;

            if (modifiers != null)
            {
                int modIdx = 0;
                foreach (var mod in modifiers)
                {
                    if (mod == null) continue;

                    var registered = MiscUtils.AllModifiers
                        .FirstOrDefault(m => m.ModifierName != null &&
                            m.ModifierName.Equals(mod.ModifierName, StringComparison.OrdinalIgnoreCase));

                    if (registered is not IWikiDiscoverable) continue;

                    string placeholder = $"[MOD{modIdx}]";
                    modLinks.Add(new ModLinkInfo
                    {
                        Placeholder = placeholder,
                        Name = mod.ModifierName,
                        TypeFullName = registered.GetType().FullName ?? mod.ModifierName,
                        Color = registered.FreeplayFileColor
                    });

                    if (hasVisible) sb.Append(", ");
                    else sb.Append("Modifiers: ");

                    sb.Append(placeholder);
                    hasVisible = true;
                    modIdx++;
                }
            }

            if (hasVisible)
                sb.AppendLine();
            else
                sb.AppendLine("Modifiers: No modifiers");
        }
        catch
        {
            sb.AppendLine("Modifiers: No modifiers");
        }

        // === TASKS ===
        if (!LobbyBehaviour.Instance && role != null && role.TeamType != RoleTeamTypes.Impostor)
        {
            try
            {
                int total = 0, completed = 0;
                foreach (var task in player.myTasks)
                {
                    if (task == null) continue;
                    if (task.TaskType is TaskTypes.FixComms or TaskTypes.FixLights
                        or TaskTypes.ResetReactor or TaskTypes.ResetSeismic
                        or TaskTypes.RestoreOxy) continue;
                    total++;
                    if (task.IsComplete) completed++;
                }
                if (total > 0)
                    sb.AppendLine($"Tasks: {completed}/{total}");
            }
            catch { /* ignore task tracking errors */ }
        }

        sb.Append(" ");
        return sb.ToString();
    }

    private static void ShowInfoBubble(NetworkedPlayerInfo basePlayer, string nameText, string message,
        List<ModLinkInfo> modLinks)
    {
        var chat = HudManager.Instance.Chat;
        var pooledBubble = chat.GetPooledBubble();

        pooledBubble.transform.SetParent(chat.scroller.Inner);
        pooledBubble.transform.localScale = Vector3.one;
        pooledBubble.SetLeft();
        pooledBubble.SetCosmetics(basePlayer);
        pooledBubble.NameText.text = nameText;
        pooledBubble.NameText.color = Color.white;
        pooledBubble.NameText.ForceMeshUpdate(true, true);
        pooledBubble.votedMark.enabled = false;
        pooledBubble.Xmark.enabled = false;

        // Process role #tags through TOU system
        var processed = WikiHyperLinkPatches.CheckForTags(message, pooledBubble.TextArea);

        // Count how many links CheckForTags already created
        int nextLinkIndex = 0;
        try
        {
            foreach (var _ in pooledBubble.TextArea.GetComponents<WikiHyperlink>())
                nextLinkIndex++;
        }
        catch { /* ignore UI link counting errors */ }

        // Replace modifier placeholders with proper clickable hyperlinks
        var fontTag = "<font=\"LiberationSans SDF\" material=\"LiberationSans SDF - BlackOutlineMasked\">";
        foreach (var modInfo in modLinks)
        {
            var colorHex = ColorUtility.ToHtmlStringRGB(modInfo.Color);
            var replacement =
                $"{fontTag}<b><color=#{colorHex}><link={modInfo.TypeFullName}:{nextLinkIndex}>{modInfo.Name}</link></color></b></font>";

            processed = processed.Replace(modInfo.Placeholder, replacement);

            var hyperlink = pooledBubble.TextArea.gameObject.AddComponent<WikiHyperlink>();
            hyperlink.HyperlinkIndex = nextLinkIndex;
            hyperlink.HyperlinkString = replacement;
            hyperlink.HoverHyperlinkString = $"<i>{replacement}</i>";
            nextLinkIndex++;
        }

        pooledBubble.TextArea.text = processed;
        pooledBubble.TextArea.ForceMeshUpdate(true, true);
        pooledBubble.Background.size = new Vector2(5.52f,
            0.2f + pooledBubble.NameText.GetNotDumbRenderedHeight() +
            pooledBubble.TextArea.GetNotDumbRenderedHeight());
        pooledBubble.MaskArea.size = pooledBubble.Background.size - new Vector2(0, 0.03f);
        pooledBubble.AlignChildren();
        chat.AlignAllBubbles();

        if (chat is { IsOpenOrOpening: false, notificationRoutine: null })
            chat.notificationRoutine = chat.StartCoroutine(chat.BounceDot());
    }

    private static string FormatAlignment(string alignment)
    {
        var result = new StringBuilder();
        foreach (char c in alignment)
        {
            if (char.IsUpper(c) && result.Length > 0)
                result.Append(' ');
            result.Append(c);
        }
        return result.ToString();
    }

    private static void ClearChat(ChatController chat)
    {
        chat.freeChatField.Clear();
        chat.quickChatMenu.Clear();
        chat.quickChatField.Clear();
        chat.UpdateChatMode();
    }
}














