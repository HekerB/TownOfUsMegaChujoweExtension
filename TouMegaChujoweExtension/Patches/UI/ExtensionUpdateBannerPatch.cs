using System;
using System.Collections;
using System.Text.Json;
using System.Text.Json.Serialization;
using HarmonyLib;
using Reactor.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

namespace TouMegaChujoweExtension.Patches.UI;

[HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
public static class ExtensionUpdateBannerPatch
{
    private const string LatestReleaseApiUrl = "https://api.github.com/repos/HekerB/TownOfUsMegaChujoweExtension/releases/latest";
    private const string ReleasesUrl = "https://github.com/HekerB/TownOfUsMegaChujoweExtension/releases/latest";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static bool _checkedForUpdate;
    private static GameObject? _banner;
    private static Sprite? _bannerSprite;
    private static Sprite? _bannerBorderSprite;
    private static readonly Color BannerNormalColor = Color.white;
    private static readonly Color BannerHoverColor = new(1.35f, 1.18f, 1.45f, 1f);

    public static void Postfix(MainMenuManager __instance)
    {
        if (_checkedForUpdate)
            return;

        _checkedForUpdate = true;
        Coroutines.Start(CheckForUpdate(__instance));
    }

    private static IEnumerator CheckForUpdate(MainMenuManager mainMenu)
    {
        var request = UnityWebRequest.Get(LatestReleaseApiUrl);
        request.SetRequestHeader("Accept", "application/vnd.github+json");
        request.SetRequestHeader("User-Agent", $"TouMegaChujoweExtension/{TouMegaChujoweExtensionPlugin.Version}");

        yield return request.SendWebRequest();

        if (request.result is UnityWebRequest.Result.ConnectionError or UnityWebRequest.Result.ProtocolError)
            yield break;

        ExtensionGitHubRelease? release;
        try
        {
            release = JsonSerializer.Deserialize<ExtensionGitHubRelease>(request.downloadHandler.text, JsonOptions);
        }
        catch
        {
            yield break;
        }

        if (release == null || string.IsNullOrWhiteSpace(release.TagName))
            yield break;

        if (!IsNewerThanCurrent(release.TagName))
            yield break;

        ShowBanner(mainMenu, release);
    }

    private static void ShowBanner(MainMenuManager mainMenu, ExtensionGitHubRelease release)
    {
        if (_banner != null)
            UnityEngine.Object.Destroy(_banner);

        _banner = new GameObject("TouMceUpdateBanner");
        _banner.transform.SetParent(mainMenu.transform);
        _banner.transform.localPosition = new Vector3(2.95f, 2.22f, -20f);
        _banner.transform.localScale = Vector3.one;
        _banner.layer = LayerMask.NameToLayer("UI");

        var border = new GameObject("Border");
        border.transform.SetParent(_banner.transform);
        border.transform.localPosition = new Vector3(0f, 0f, 0.01f);
        border.transform.localScale = new Vector3(1.018f, 1.075f, 1f);
        border.layer = LayerMask.NameToLayer("UI");

        var borderRenderer = border.AddComponent<SpriteRenderer>();
        borderRenderer.sprite = GetBannerBorderSprite();
        borderRenderer.color = new Color(1f, 0.92f, 1f, 1f);
        borderRenderer.sortingOrder = 49;

        var bg = new GameObject("Background");
        bg.transform.SetParent(_banner.transform);
        bg.transform.localPosition = new Vector3(0f, 0f, -0.01f);
        bg.layer = LayerMask.NameToLayer("UI");

        var renderer = bg.AddComponent<SpriteRenderer>();
        renderer.sprite = GetBannerSprite();
        renderer.color = BannerNormalColor;
        renderer.sortingOrder = 50;
        bg.transform.localScale = new Vector3(0.98f, 0.92f, 1f);

        AddIcon(_banner.transform, "JokerIcon", TouExtensionIcons.JokerRoleIcon.LoadAsset(),
            new Vector3(-1.18f, 0.03f, -0.03f), 0.25f);
        AddIcon(_banner.transform, "JesterIcon", TouRoleIcons.Jester.LoadAsset(),
            new Vector3(1.18f, 0.03f, -0.03f), 0.25f);

        AddText(_banner.transform, "New Version!", new Vector3(0f, 0.16f, -0.03f),
            1.78f, new Color(1f, 0.78f, 1f, 1f), FontStyles.Bold);

        AddText(_banner.transform, "Worth downloading - click", new Vector3(0f, -0.05f, -0.03f),
            0.96f, Color.white, FontStyles.Bold);

        var current = NormalizeVersion(TouMegaChujoweExtensionPlugin.Version);
        var latest = NormalizeVersion(release.TagName);
        AddText(_banner.transform, $"{current} -> {latest}", new Vector3(0f, -0.23f, -0.03f),
            0.92f, new Color(1f, 0.86f, 1f, 1f), FontStyles.Bold);

        var collider = _banner.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(3.45f, 0.78f);

        var button = _banner.AddComponent<PassiveButton>();
        button.Colliders = new[] { (Collider2D)collider };
        button.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
        button.OnClick.AddListener((Action)(() =>
        {
            Application.OpenURL(string.IsNullOrWhiteSpace(release.HtmlUrl) ? ReleasesUrl : release.HtmlUrl);
        }));
        button.OnMouseOver = new UnityEngine.Events.UnityEvent();
        button.OnMouseOver.AddListener((Action)(() =>
        {
            renderer.color = BannerHoverColor;
            borderRenderer.color = Color.white;
            bg.transform.localScale = new Vector3(1.01f, 0.98f, 1f);
            border.transform.localScale = new Vector3(1.035f, 1.11f, 1f);
        }));
        button.OnMouseOut = new UnityEngine.Events.UnityEvent();
        button.OnMouseOut.AddListener((Action)(() =>
        {
            renderer.color = BannerNormalColor;
            borderRenderer.color = new Color(1f, 0.92f, 1f, 1f);
            bg.transform.localScale = new Vector3(0.98f, 0.92f, 1f);
            border.transform.localScale = new Vector3(1.018f, 1.075f, 1f);
        }));
    }

    private static void AddIcon(Transform parent, string name, Sprite sprite, Vector3 localPosition, float scale)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent);
        obj.transform.localPosition = localPosition;
        obj.transform.localScale = new Vector3(scale, scale, 1f);
        obj.layer = LayerMask.NameToLayer("UI");

        var renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 56;
    }

    private static void AddText(Transform parent, string text, Vector3 localPosition, float fontSize, Color color, FontStyles style)
    {
        var obj = new GameObject("Text");
        obj.transform.SetParent(parent);
        obj.transform.localPosition = localPosition;
        obj.layer = LayerMask.NameToLayer("UI");

        var tmp = obj.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.sortingOrder = 55;
        tmp.outlineWidth = 0.12f;
        tmp.outlineColor = Color.black;
        tmp.rectTransform.sizeDelta = new Vector2(2.65f, 0.38f);
        ApplyFont(tmp);
    }

    private static void ApplyFont(TextMeshPro tmp)
    {
        var font = TouExtensionFonts.ChewyFont;
        if (font != null)
            tmp.font = font;
    }

    private static Sprite GetBannerSprite()
    {
        if (_bannerSprite != null)
            return _bannerSprite;

        var tex = new Texture2D(640, 144, TextureFormat.ARGB32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        var topColor = new Color(0.46f, 0.10f, 0.48f, 0.96f);
        var bottomColor = new Color(0.18f, 0.04f, 0.28f, 0.96f);

        for (var y = 0; y < tex.height; y++)
        {
            for (var x = 0; x < tex.width; x++)
            {
                if (!IsInsideRoundedRect(x, y, tex.width, tex.height, 30))
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }

                var t = y / (float)(tex.height - 1);
                var color = Color.Lerp(bottomColor, topColor, t);
                var stripe = (x + y * 2) % 116;
                if (stripe is > 8 and < 26)
                    color = Color.Lerp(color, new Color(0.88f, 0.24f, 0.70f, color.a), 0.28f);

                tex.SetPixel(x, y, color);
            }
        }

        tex.Apply();
        _bannerSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 200f);
        return _bannerSprite;
    }

    private static Sprite GetBannerBorderSprite()
    {
        if (_bannerBorderSprite != null)
            return _bannerBorderSprite;

        var tex = new Texture2D(640, 144, TextureFormat.ARGB32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        for (var y = 0; y < tex.height; y++)
        {
            for (var x = 0; x < tex.width; x++)
            {
                tex.SetPixel(x, y, IsInsideRoundedRect(x, y, tex.width, tex.height, 32)
                    ? Color.white
                    : Color.clear);
            }
        }

        tex.Apply();
        _bannerBorderSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 200f);
        return _bannerBorderSprite;
    }

    private static bool IsInsideRoundedRect(int x, int y, int width, int height, int radius, int inset = 0)
    {
        var localX = x - inset;
        var localY = y - inset;
        var localWidth = width - inset * 2;
        var localHeight = height - inset * 2;
        if (localX < 0 || localY < 0 || localX >= localWidth || localY >= localHeight)
            return false;

        var cx = localX < radius ? radius : localX >= localWidth - radius ? localWidth - radius - 1 : localX;
        var cy = localY < radius ? radius : localY >= localHeight - radius ? localHeight - radius - 1 : localY;
        var dx = localX - cx;
        var dy = localY - cy;
        return dx * dx + dy * dy <= radius * radius;
    }

    private static bool IsNewerThanCurrent(string latestVersion)
        => CompareVersions(latestVersion, TouMegaChujoweExtensionPlugin.Version) > 0;

    private static int CompareVersions(string left, string right)
    {
        var leftParts = SplitVersion(left);
        var rightParts = SplitVersion(right);
        var count = Math.Max(leftParts.Length, rightParts.Length);
        for (var i = 0; i < count; i++)
        {
            var a = i < leftParts.Length ? leftParts[i] : 0;
            var b = i < rightParts.Length ? rightParts[i] : 0;
            if (a != b)
                return a.CompareTo(b);
        }

        return 0;
    }

    private static int[] SplitVersion(string value)
    {
        value = NormalizeVersion(value);
        var suffix = value.IndexOfAny(['-', '+']);
        if (suffix >= 0)
            value = value[..suffix];

        var parts = value.Split('.', StringSplitOptions.RemoveEmptyEntries);
        var numbers = new int[Math.Min(parts.Length, 4)];
        for (var i = 0; i < numbers.Length; i++)
            numbers[i] = int.TryParse(parts[i], out var parsed) ? parsed : 0;

        return numbers;
    }

    private static string NormalizeVersion(string value)
    {
        value = (value ?? string.Empty).Trim();
        return value.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? value[1..] : value;
    }

    private sealed class ExtensionGitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; } = string.Empty;

        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; } = string.Empty;
    }
}
