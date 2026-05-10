using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace TouMegaChujoweExtension.Assets;

public static class TouExtensionAnims
{
    private static Sprite[]? _rcCarFrames;

    public static Sprite[] RcCarFrames => _rcCarFrames ??= LoadSpriteSheet(
        "TouMegaChujoweExtension.Resources.Anims.RC_Anim.png",
        3, 8, 300f);

    public static Sprite[] LoadSpriteSheet(string resourcePath, int cols, int rows, float pixelsPerUnit = 100f)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourcePath);
        if (stream == null)
        {
            Error($"[TouAnims] Sprite sheet not found: {resourcePath}");
            return System.Array.Empty<Sprite>();
        }

        using var ms = new MemoryStream();
        stream.CopyTo(ms);

        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        tex.hideFlags = HideFlags.DontUnloadUnusedAsset;
        ImageConversion.LoadImage(tex, ms.ToArray());

        var fw = tex.width / cols;
        var fh = tex.height / rows;

        Info($"[TouAnims] Loaded {resourcePath}: {tex.width}x{tex.height}px, grid {cols}x{rows}, frame {fw}x{fh}px");

        var sprites = new List<Sprite>();

        for (var r = 0; r < rows; r++)
        for (var c = 0; c < cols; c++)
        {
            var rx = c * fw;
            var ry = (rows - 1 - r) * fh;

            if (IsFrameEmpty(tex, rx, ry, fw, fh))
                continue;

            var rect = new Rect(rx, ry, fw, fh);
            var spr = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), pixelsPerUnit);
            spr.hideFlags = HideFlags.DontUnloadUnusedAsset;
            sprites.Add(spr);
        }

        Info($"[TouAnims] {sprites.Count} non-empty frames out of {cols * rows} slots");

        return sprites.ToArray();
    }

    private static bool IsFrameEmpty(Texture2D tex, int x, int y, int w, int h)
    {
        for (var dy = 0; dy < 3; dy++)
        for (var dx = 0; dx < 3; dx++)
        {
            var sx = x + (dx + 1) * w / 4;
            var sy = y + (dy + 1) * h / 4;
            if (sx >= tex.width || sy >= tex.height) continue;
            var pixel = tex.GetPixel(sx, sy);
            if (pixel.a > 0.01f) return false;
        }
        return true;
    }
	private static Sprite[]? _guardAnimFront;
	private static Sprite[]? _guardAnimBack;
	
	public static Sprite[] GuardAnimFront => _guardAnimFront ??= LoadSpriteSheet(
		"TouMegaChujoweExtension.Resources.Anims.Guard_Anim_Front.png",
		3, 2, 150f);

	public static Sprite[] GuardAnimBack => _guardAnimBack ??= LoadSpriteSheet(
		"TouMegaChujoweExtension.Resources.Anims.Guard_Anim_Back.png",
		3, 2, 150f);
		

/*	private static Sprite[]? _poisonKillFrames;

    public static Sprite[] PoisonKillFrames => _poisonKillFrames ??= LoadSpriteSheet(
        "TouMegaChujoweExtension.Resources.Anims.Poison_Kill_Anim.png",
        3, 10,
        190f);
*/
	
	private static Sprite[]? _jokerPiPBorderFrames;

	public static Sprite[] JokerPiPBorderFrames => _jokerPiPBorderFrames ??= LoadSpriteSheet(
    "TouMegaChujoweExtension.Resources.Anims.Joker_PIP_Border.png",
    3, 1, 100f);
}










