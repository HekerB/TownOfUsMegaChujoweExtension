using HarmonyLib;
using MiraAPI.GameOptions;
using Object = UnityEngine.Object;
using Reactor.Utilities;
using System.IO;
using System.Linq;
using System.Reflection;
using System;
using TownOfUs.Options;
using UnityEngine;

namespace TouMegaChujoweExtension.Modules;

[HarmonyPatch]
public static class DuplicateChecker
{
    public static bool HasDuplicate { get; private set; }
    public static string? DuplicatePath { get; private set; }

    public static void Check()
    {
        try
        {
            var currentAssembly = Assembly.GetExecutingAssembly();
            var assemblyName = currentAssembly.GetName().Name ?? "TouMegaChujoweExtension";
            
            // In IL2CPP, Location can be tricky, so let's get the absolute full path
            var currentPath = string.Empty;
            try {
                currentPath = Path.GetFullPath(currentAssembly.Location);
            } catch { /* ignore path access errors during scan */ }

            // Use the global BepInEx plugins directory to be sure we scan everything
            var pluginsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BepInEx", "plugins");
            if (!Directory.Exists(pluginsDir)) {
                // Fallback to assembly dir if plugins dir not found (unlikely)
                var loc = Path.GetDirectoryName(currentAssembly.Location);
                if (!string.IsNullOrEmpty(loc)) pluginsDir = loc;
            }

            if (string.IsNullOrEmpty(pluginsDir)) return;

            Logger<TouMegaChujoweExtensionPlugin>.Info($"[DuplicateChecker] Searching in: {pluginsDir}");
            Logger<TouMegaChujoweExtensionPlugin>.Info($"[DuplicateChecker] Current assembly path: {currentPath}");

            var files = Directory.GetFiles(pluginsDir, "*.dll", SearchOption.AllDirectories);
            
            var duplicates = files
                .Where(f => {
                    var fullF = string.Empty;
                    try {
                        fullF = Path.GetFullPath(f);
                    } catch { return false; }

                    if (!string.IsNullOrEmpty(currentPath) && string.Equals(fullF, currentPath, StringComparison.OrdinalIgnoreCase)) return false;

                    // Log for debugging
                    Logger<TouMegaChujoweExtensionPlugin>.Info($"[DuplicateChecker] Scanning file: {Path.GetFileName(f)}");

                    // 1. Check file name (fast)
                    var fileName = Path.GetFileNameWithoutExtension(f);
                    if (fileName.StartsWith(assemblyName, StringComparison.OrdinalIgnoreCase)) {
                        Logger<TouMegaChujoweExtensionPlugin>.Warning($"[DuplicateChecker] Flagged by NAME: {fileName}");
                        return true;
                    }

                    // 2. Check internal assembly name (robust - handles renamed files)
                    try {
                        var internalName = AssemblyName.GetAssemblyName(f).Name;
                        if (string.Equals(internalName, assemblyName, StringComparison.OrdinalIgnoreCase)) {
                            Logger<TouMegaChujoweExtensionPlugin>.Warning($"[DuplicateChecker] Flagged by INTERNAL NAME: {internalName} (File: {fileName})");
                            return true;
                        }
                    } catch (Exception) { /* skip files that cannot be read as assemblies */ }

                    return false;
                })
                .ToList();

            if (duplicates.Count > 0)
            {
                HasDuplicate = true;
                DuplicatePath = duplicates[0];
                Logger<TouMegaChujoweExtensionPlugin>.Error($"[DuplicateChecker] DUPLICATE FOUND: {DuplicatePath}");
            }
            else {
                Logger<TouMegaChujoweExtensionPlugin>.Info("[DuplicateChecker] No duplicates found.");
            }
        }
        catch (Exception ex)
        {
            Logger<TouMegaChujoweExtensionPlugin>.Error($"[DuplicateChecker] Critical error during check: {ex.Message}");
        }
    }

    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
    [HarmonyPostfix]
    public static void MainMenuStartPostfix(MainMenuManager __instance)
    {
        if (HasDuplicate)
        {
            ShowWarning();
        }
    }

    private static void ShowWarning()
    {
        var message = $"<color=#FF0000><size=200%><b>DUPLICATE MOD DETECTED!</b></size></color>\n\n" +
                      $"Found: <color=#FFFF00>{Path.GetFileName(DuplicatePath)}</color>\n" +
                      $"in your plugins folder.\n\n" +
                      $"<color=#FF7777>You MUST delete this file to avoid crashes!</color>";

        var go = new GameObject("DuplicateWarningUI");
        var text = go.AddComponent<TMPro.TextMeshPro>();
        text.text = message;
        text.alignment = TMPro.TextAlignmentOptions.Center;
        text.fontSize = 3f;
        text.outlineWidth = 0.3f;
        text.outlineColor = Color.black;
        
        // Ensure it's in front of everything
        go.transform.position = new Vector3(0, 0, -15);
        Object.DontDestroyOnLoad(go);

        // Add a second text as shadow for extreme visibility
        var shadowGo = new GameObject("DuplicateWarningShadow");
        var shadow = shadowGo.AddComponent<TMPro.TextMeshPro>();
        shadow.text = message;
        shadow.alignment = TMPro.TextAlignmentOptions.Center;
        shadow.fontSize = 3f;
        shadow.color = new Color(0, 0, 0, 0.5f);
        shadowGo.transform.position = new Vector3(0.05f, -0.05f, -14.9f);
        Object.DontDestroyOnLoad(shadowGo);
    }

    private static bool kickTriggered = false;

    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.Update))]
    [HarmonyPostfix]
    public static void AmongUsClientUpdatePostfix()
    {
        // Reset the trigger flag when we are not connected (in menu, disconnected)
        if (!AmongUsClient.Instance.AmConnected)
        {
            kickTriggered = false;
            return;
        }

        if (HasDuplicate && !kickTriggered && PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.Data != null)
        {
            kickTriggered = true;
            Coroutines.Start(CoSelfKick());
        }
    }

    private static System.Collections.IEnumerator CoSelfKick()
    {
        // Wait a bit so the player fully spawns and connection is established
        yield return new WaitForSeconds(1.2f);
        
        if (!HasDuplicate) yield break;

        // Notify the host via Custom RPC
        try {
            if (AmongUsClient.Instance.AmConnected && PlayerControl.LocalPlayer != null) {
                var fileName = Path.GetFileName(DuplicatePath);
                string playerName = PlayerControl.LocalPlayer.Data.PlayerName;

                if (AmongUsClient.Instance.AmHost)
                {
                    ShowDuplicateModSystemMessage(playerName, fileName);
                }
                else
                {
                    // Find a player that won't be destroyed immediately (like the host or the first available player)
                    var targetNetId = PlayerControl.LocalPlayer.NetId;
                    foreach (var pc in PlayerControl.AllPlayerControls)
                    {
                        if (pc.PlayerId != PlayerControl.LocalPlayer.PlayerId)
                        {
                            targetNetId = pc.NetId;
                            break;
                        }
                    }

                    var writer = AmongUsClient.Instance.StartRpcImmediately(targetNetId, (byte)TouMegaChujoweExtension.Networking.ExtensionRpc.DuplicateModKick, Hazel.SendOption.Reliable, AmongUsClient.Instance.HostId);
                    writer.Write(playerName);
                    writer.Write(fileName);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                }
            }
        } catch { /* ignore RPC delivery failures */ }

        // Give a bit more time for RPC to send over network before objects are destroyed
        yield return new WaitForSeconds(1.0f);

        // Kick the player
        AmongUsClient.Instance.ExitGame(DisconnectReasons.ExitGame);
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleRpc))]
    public static class DuplicateCheckerRpcPatch
    {
        [HarmonyPostfix]
        public static void Postfix(PlayerControl __instance, byte callId, Hazel.MessageReader reader)
        {
            if (callId == (byte)TouMegaChujoweExtension.Networking.ExtensionRpc.DuplicateModKick)
            {
                string playerName = reader.ReadString();
                string fileName = reader.ReadString();

                if (AmongUsClient.Instance.AmHost)
                {
                    ShowDuplicateModSystemMessage(playerName, fileName);
                }
            }
        }
    }

    public static void ShowDuplicateModSystemMessage(string playerName, string fileName)
    {
        if (HudManager.Instance == null || HudManager.Instance.Chat == null) return;
        var player = PlayerControl.LocalPlayer;
        if (player == null || player.Data == null) return;

        string systemName = "<color=#FF4444>System</color>";
        string msg = $"{playerName} was KICKED for duplicated mod files:\n<color=#FF4444>{fileName}</color>";

        TownOfUs.Utilities.MiscUtils.AddFakeChat(player.Data, systemName, msg, true, true);
    }
}













