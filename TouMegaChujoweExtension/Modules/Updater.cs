using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using HarmonyLib;
using MiraAPI.Utilities.Assets;
using Reactor.Utilities;
using Twitch;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TouMegaChujoweExtension.Modules
{
    public static class ModUpdater
    {
        public static LoadableAsset<Sprite> UpdateButtonSprite { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.Buttons.Update_Button.png", 100f);
        public static LoadableAsset<Sprite> UpdateButtonHoverSprite { get; } = new LoadableResourceAsset("TouMegaChujoweExtension.Resources.Buttons.Update_Button_Hover.png", 100f);

        private static GenericPopup? _popupComponent;
        private static GameObject? _btnObj;

        /// <summary>
        /// Cleans up any leftover .old files from previous updates on startup.
        /// </summary>
        public static void CleanOldVersions()
        {
            try
            {
                var pluginsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BepInEx", "plugins");
                if (!Directory.Exists(pluginsDir)) return;

                var oldFiles = Directory.GetFiles(pluginsDir, "*.old", SearchOption.AllDirectories);
                foreach (var oldFile in oldFiles)
                {
                    try
                    {
                        File.Delete(oldFile);
                    }
                    catch (Exception)
                    {
                        // Ignore locked or read-only files
                    }
                }
            }
            catch (Exception)
            {
                // Ignore overall scan errors
            }
        }

        /// <summary>
        /// Spawns and positions the update button in the main menu.
        /// </summary>
        public static void BuildButton()
        {
            try
            {
                const string buttonPath = "MainMenuManager/MainUI/AspectScaler/LeftPanel/Main Buttons/BottomButtonBounds/ExitGameButton";
                var exitBtn = GameObject.Find(buttonPath);
                if (exitBtn == null) return;

                // Instantiate as sibling under BottomButtonBounds
                _btnObj = Object.Instantiate(exitBtn, exitBtn.transform.parent);
                _btnObj.name = "button_TouMegaChujoweExtensionUpdater";

                // Remove AspectPosition so we can custom offset it to the bottom-right corner
                var aspect = _btnObj.GetComponent<AspectPosition>();
                if (aspect != null) Object.Destroy(aspect);

                // Set local scale and position it on the bottom-right!
                _btnObj.transform.localScale = new Vector3(1.2f, 1.2f, 1.0f);
                _btnObj.transform.localPosition = new Vector3(8.0f, exitBtn.transform.localPosition.y, exitBtn.transform.localPosition.z);

                // Remove original Exit game text
                var textTransform = _btnObj.transform.Find("FontPlacer");
                if (textTransform != null) Object.Destroy(textTransform.gameObject);

                // Get custom textures
                var btnSprite = UpdateButtonSprite.LoadAsset();
                var btnHoverSprite = UpdateButtonHoverSprite.LoadAsset();

                // Setup Active / Inactive states for SpriteRenderers
                var active = _btnObj.transform.Find("Highlight")?.gameObject;
                var inactive = _btnObj.transform.Find("Inactive")?.gameObject;

                if (active != null)
                {
                    var btnRendererActive = active.GetComponent<SpriteRenderer>();
                    if (btnRendererActive != null)
                    {
                        btnRendererActive.sprite = btnHoverSprite;
                        btnRendererActive.size = new Vector2(1.3f, 1.3f);
                    }
                }

                if (inactive != null)
                {
                    var btnRendererInactive = inactive.GetComponent<SpriteRenderer>();
                    if (btnRendererInactive != null)
                    {
                        btnRendererInactive.sprite = btnSprite;
                        btnRendererInactive.size = new Vector2(1.3f, 1.3f);
                    }
                }

                // Setup Click Listener
                var btnComponent = _btnObj.GetComponent<PassiveButton>();
                if (btnComponent != null)
                {
                    btnComponent.OnClick = new Button.ButtonClickedEvent();
                    btnComponent.OnClick.AddListener((System.Action)UpdateMod);
                }

                // Adjust box collider size to match the sprite renderer
                var btnCollider = _btnObj.GetComponent<BoxCollider2D>();
                if (btnCollider != null)
                {
                    btnCollider.size = new Vector2(1.3f, 1.3f);
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[ModUpdater] Failed to build update button: {ex.Message}");
            }
        }

        private static void UpdateMod()
        {
            if (_btnObj == null) return;

            // Instantiate popup
            if (_popupComponent == null)
            {
                var man = TwitchManager.Instance;
                if (man != null && man.TwitchPopup != null)
                {
                    var popupPrefab = man.TwitchPopup.gameObject;
                    var popupObject = Object.Instantiate(popupPrefab);
                    _popupComponent = popupObject.GetComponent<GenericPopup>();
                    _popupComponent.TextAreaTMP.enableAutoSizing = true;
                }
            }

            if (_popupComponent == null) return;

            var confirmButton = _popupComponent.transform.Find("ExitGame")?.gameObject;
            if (confirmButton != null) confirmButton.SetActive(false);

            _popupComponent.Show("<color=#00FFFF>Checking for updates...</color>");
            _btnObj.SetActive(false);

            Coroutines.Start(CoCheckAndUpdate(confirmButton));
        }

        private static IEnumerator CoCheckAndUpdate(GameObject? confirmButton)
        {
            // 1. Fetch release info from GitHub
            var url = "https://api.github.com/repos/HekerB/TownOfUsMegaChujoweExtension/releases/latest?t=" + DateTime.UtcNow.Ticks;
            var request = UnityWebRequest.Get(url);
            request.SetRequestHeader("User-Agent", "TouMegaChujoweExtension-Updater");
            yield return request.SendWebRequest();

            if (request.result is UnityWebRequest.Result.ConnectionError or UnityWebRequest.Result.ProtocolError)
            {
                _popupComponent!.Show($"<color=red>Update failed!</color>\nFailed to fetch update info:\n{request.error}");
                if (confirmButton != null) confirmButton.SetActive(true);
                _btnObj?.SetActive(true);
                yield break;
            }

            string tag = "";
            string? downloadUrl = null;

            try
            {
                using var doc = JsonDocument.Parse(request.downloadHandler.text);
                tag = doc.RootElement.GetProperty("tag_name").GetString() ?? "";
                
                if (doc.RootElement.TryGetProperty("assets", out var assets))
                {
                    foreach (var asset in assets.EnumerateArray())
                    {
                        var assetName = asset.GetProperty("name").GetString();
                        if (assetName != null && assetName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                        {
                            downloadUrl = asset.GetProperty("browser_download_url").GetString();
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _popupComponent!.Show($"<color=red>Update failed!</color>\nFailed to parse release info:\n{ex.Message}");
                if (confirmButton != null) confirmButton.SetActive(true);
                _btnObj?.SetActive(true);
                yield break;
            }

            if (string.IsNullOrEmpty(tag) || string.IsNullOrEmpty(downloadUrl))
            {
                _popupComponent!.Show("<color=red>Update failed!</color>\nNo valid DLL release asset found on GitHub.");
                if (confirmButton != null) confirmButton.SetActive(true);
                _btnObj?.SetActive(true);
                yield break;
            }

            // 2. Normalize and check if already up-to-date
            var cleanTag = tag.TrimStart('v', 'V', ' ').Trim();
            var cleanCurrent = TouMegaChujoweExtensionPlugin.Version.TrimStart('v', 'V', ' ').Trim();

            if (cleanTag == cleanCurrent)
            {
                _popupComponent!.Show($"<color=green>You are already up-to-date!</color>\nCurrent Version: {TouMegaChujoweExtensionPlugin.Version}\nLatest Release: {tag}");
                if (confirmButton != null) confirmButton.SetActive(true);
                _btnObj?.SetActive(true);
                yield break;
            }

            // 3. Download the new DLL!
            _popupComponent!.Show($"<color=#00FFFF>Downloading update {tag}...</color>");
            
            var dlRequest = UnityWebRequest.Get(downloadUrl);
            dlRequest.SetRequestHeader("User-Agent", "TouMegaChujoweExtension-Updater");
            yield return dlRequest.SendWebRequest();

            if (dlRequest.result is UnityWebRequest.Result.ConnectionError or UnityWebRequest.Result.ProtocolError)
            {
                _popupComponent!.Show($"<color=red>Download failed!</color>\n{dlRequest.error}");
                if (confirmButton != null) confirmButton.SetActive(true);
                _btnObj?.SetActive(true);
                yield break;
            }

            // 4. Install the update by renaming the current running DLL to .old and writing the new DLL
            try
            {
                var currentDllPath = GetRunningDllPath();
                if (string.IsNullOrEmpty(currentDllPath))
                {
                    throw new Exception("Could not find the target directory to save the downloaded mod file.");
                }

                var backupPath = currentDllPath + "." + DateTime.UtcNow.Ticks + ".old";

                // Rename loaded dll to unique backup path (allowed on Windows!)
                if (File.Exists(currentDllPath))
                {
                    File.Move(currentDllPath, backupPath);
                }

                // Write the new downloaded DLL in place!
                var bytes = dlRequest.downloadHandler.data;
                File.WriteAllBytes(currentDllPath, bytes);

                _popupComponent!.Show($"<color=green>Update successfully completed!</color>\nUpdated to version {tag}!\n<b>Please restart Among Us to apply changes.</b>");
                if (confirmButton != null) confirmButton.SetActive(true);
                
                // Keep the update button hidden so they don't click it again
                _btnObj?.SetActive(false);
            }
            catch (Exception ex)
            {
                _popupComponent!.Show($"<color=red>Installation failed!</color>\n{ex.Message}\n<i>Please update manually.</i>");
                if (confirmButton != null) confirmButton.SetActive(true);
                _btnObj?.SetActive(true);
            }
        }

        private static string? GetRunningDllPath()
        {
            // 1. Try BepInEx Chainloader
            try
            {
                if (BepInEx.Unity.IL2CPP.IL2CPPChainloader.Instance.Plugins.TryGetValue("toumegachujowe.tou.extension", out var pluginInfo))
                {
                    var loc = pluginInfo?.Location;
                    if (!string.IsNullOrEmpty(loc) && File.Exists(loc))
                    {
                        return Path.GetFullPath(loc);
                    }
                }
            }
            catch {}

            // 2. Try Assembly Location
            try
            {
                var loc = Assembly.GetExecutingAssembly().Location;
                if (!string.IsNullOrEmpty(loc) && File.Exists(loc))
                {
                    return Path.GetFullPath(loc);
                }
            }
            catch {}

            // 3. Scan plugins folder by filename or internal assembly name
            try
            {
                var pluginsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BepInEx", "plugins");
                if (Directory.Exists(pluginsDir))
                {
                    var dllFiles = Directory.GetFiles(pluginsDir, "*.dll", SearchOption.AllDirectories);
                    
                    // Check exact filename match
                    foreach (var file in dllFiles)
                    {
                        if (string.Equals(Path.GetFileName(file), "TouMegaChujoweExtension.dll", StringComparison.OrdinalIgnoreCase))
                        {
                            return Path.GetFullPath(file);
                        }
                    }

                    // Check assembly name match
                    foreach (var file in dllFiles)
                    {
                        try
                        {
                            var internalName = AssemblyName.GetAssemblyName(file).Name;
                            if (string.Equals(internalName, "TouMegaChujoweExtension", StringComparison.OrdinalIgnoreCase))
                            {
                                return Path.GetFullPath(file);
                            }
                        }
                        catch {}
                    }
                }
            }
            catch {}

            return null;
        }
    }

    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
    public static class ButtonPatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            ModUpdater.BuildButton();
        }
    }
}
