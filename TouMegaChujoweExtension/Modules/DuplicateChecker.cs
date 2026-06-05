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
            try
            {
                currentPath = Path.GetFullPath(currentAssembly.Location);
            }
            catch { /* ignore path access errors during scan */ }
            var pluginsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BepInEx", "plugins");
            if (!Directory.Exists(pluginsDir))
            {
                var loc = Path.GetDirectoryName(currentAssembly.Location);
                if (!string.IsNullOrEmpty(loc)) pluginsDir = loc;
            }

            if (string.IsNullOrEmpty(pluginsDir)) return;

            Logger<TouMegaChujoweExtensionPlugin>.Info($"[DuplicateChecker] Searching in: {pluginsDir}");
            Logger<TouMegaChujoweExtensionPlugin>.Info($"[DuplicateChecker] Current assembly path: {currentPath}");

            var files = Directory.GetFiles(pluginsDir, "*.dll", SearchOption.AllDirectories);

            var matchingFiles = new System.Collections.Generic.List<string>();
            foreach (var f in files)
            {
                try
                {
                    var fullF = Path.GetFullPath(f);
                    if (!string.IsNullOrEmpty(currentPath) && string.Equals(fullF, currentPath, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var fileName = Path.GetFileNameWithoutExtension(f);
                    if (fileName.Contains(assemblyName, StringComparison.OrdinalIgnoreCase))
                    {
                        Logger<TouMegaChujoweExtensionPlugin>.Info($"[DuplicateChecker] Match found: {fileName}");
                        matchingFiles.Add(fullF);
                    }
                }
                catch { }
            }

            bool duplicateDetected = false;
            string? detectedPath = null;

            if (!string.IsNullOrEmpty(currentPath))
            {
                if (matchingFiles.Count > 0)
                {
                    duplicateDetected = true;
                    detectedPath = matchingFiles[0];
                }
            }
            else
            {
                if (matchingFiles.Count > 1)
                {
                    duplicateDetected = true;
                    detectedPath = matchingFiles.FirstOrDefault(p => !p.Contains(assemblyName + ".dll")) ?? matchingFiles[0];
                }
            }

            if (duplicateDetected && detectedPath != null)
            {
                HasDuplicate = true;
                DuplicatePath = detectedPath;
                Logger<TouMegaChujoweExtensionPlugin>.Error($"[DuplicateChecker] DUPLICATE FOUND: {DuplicatePath}");
            }
            else
            {
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
    public static void MainMenuStartPostfix()
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
        go.transform.position = new Vector3(0, 0, -15);
        Object.DontDestroyOnLoad(go);
        var shadowGo = new GameObject("DuplicateWarningShadow");
        var shadow = shadowGo.AddComponent<TMPro.TextMeshPro>();
        shadow.text = message;
        shadow.alignment = TMPro.TextAlignmentOptions.Center;
        shadow.fontSize = 3f;
        shadow.color = new Color(0, 0, 0, 0.5f);
        shadowGo.transform.position = new Vector3(0.05f, -0.05f, -14.9f);
        Object.DontDestroyOnLoad(shadowGo);
    }

    private static bool kickTriggered;

    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.Update))]
    [HarmonyPostfix]
    public static void AmongUsClientUpdatePostfix()
    {
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
        yield return new WaitForSeconds(1.2f);

        if (!HasDuplicate) yield break;

        try
        {
            if (AmongUsClient.Instance.AmConnected && PlayerControl.LocalPlayer is not null)
            {
                var fileName = Path.GetFileName(DuplicatePath) ?? "Unknown.dll";
                string playerName = PlayerControl.LocalPlayer.Data.PlayerName;

                if (AmongUsClient.Instance.AmHost)
                {
                    ShowDuplicateModSystemMessage(playerName, fileName);
                }
                else
                {
                    // Always use LocalPlayer.NetId to call RPCs since we own this player object.
                    // This avoids network authority exceptions.
                    var writer = AmongUsClient.Instance.StartRpcImmediately(
                        PlayerControl.LocalPlayer.NetId, 
                        (byte)TouMegaChujoweExtension.Networking.ExtensionRpc.DuplicateModKick, 
                        Hazel.SendOption.Reliable, 
                        AmongUsClient.Instance.HostId);
                    writer.Write(playerName);
                    writer.Write(fileName);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                }
            }
        }
        catch { /* ignore RPC delivery failures */ }

        // Give a bit more time for RPC to send over network before objects are destroyed
        yield return new WaitForSeconds(1.0f);

        // Kick the player
        AmongUsClient.Instance.ExitGame(DisconnectReasons.ExitGame);
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleRpc))]
    public static class DuplicateCheckerRpcPatch
    {
        [HarmonyPostfix]
        public static void Postfix(byte callId, Hazel.MessageReader reader)
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