using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace TouMegaChujoweExtension.Assets;

public static class TouExtensionFonts
{
    private static TMP_FontAsset _chewyFont;

    public static TMP_FontAsset ChewyFont
    {
        get
        {
            if (_chewyFont != null) return _chewyFont;
            _chewyFont = LoadChewyFont();
            return _chewyFont;
        }
    }

    private static TMP_FontAsset LoadChewyFont()
    {
        try
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var resourceName = "TouMegaChujoweExtension.Resources.Fonts.Chewy-Regular.ttf";

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                // // BepInEx.Logging.Logger.CreateLogSource("ExtensionFonts").LogWarning("Chewy font resource not found");
                return null;
            }

            var fontData = new byte[stream.Length];
            stream.Read(fontData, 0, fontData.Length);

            // Write to temp file and load via Font constructor
            var tempPath = Path.Combine(Path.GetTempPath(), "Chewy-Regular.ttf");
            File.WriteAllBytes(tempPath, fontData);

            var font = new Font(tempPath);
            font.hideFlags = HideFlags.DontUnloadUnusedAsset;

            var tmpFont = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024);
            tmpFont.hideFlags = HideFlags.DontUnloadUnusedAsset;
            tmpFont.name = "Chewy SDF";

            // Add fallback to default game font
            var defaultFont = TMP_Settings.defaultFontAsset;
            if (defaultFont != null)
            {
                var fallbackList = new Il2CppSystem.Collections.Generic.List<TMP_FontAsset>();
                fallbackList.Add(defaultFont);
                tmpFont.fallbackFontAssetTable = fallbackList;
            }

            // // BepInEx.Logging.Logger.CreateLogSource("ExtensionFonts").LogInfo("Chewy font loaded successfully");
            return tmpFont;
        }
        catch (System.Exception)
        {
            // // BepInEx.Logging.Logger.CreateLogSource("ExtensionFonts").LogError($"Failed to load Chewy font");
            return null;
        }
    }
}
