using UnityEngine.TextCore;
using UnityEngine;

/*using TMPro;

namespace TouMegaChujoweExtension.Modules;

public static class TMPSpriteHelper
{
    private static TMP_SpriteAsset _toumceSpriteAsset;

    public static TMP_SpriteAsset GetOrCreateSpriteAsset(Sprite sprite, string name = "Hexed")
    {
        if (_toumceSpriteAsset != null)
            return _toumceSpriteAsset;

        var texture = sprite.texture;

        _toumceSpriteAsset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
        _toumceSpriteAsset.name = "TOUMCE_Sprites";
        _toumceSpriteAsset.spriteSheet = texture;
        _toumceSpriteAsset.hashCode = TMP_TextUtilities.GetSimpleHashCode(_toumceSpriteAsset.name);

        var mat = new Material(Shader.Find("TextMeshPro/Sprite"));
        mat.mainTexture = texture;
        _toumceSpriteAsset.material = mat;

        var spriteGlyph = new TMP_SpriteGlyph
        {
            index = 0,
            metrics = new GlyphMetrics(
                sprite.rect.width,
                sprite.rect.height,
                0,
                sprite.rect.height * 0.8f,
                sprite.rect.width),
            glyphRect = new GlyphRect(
                (int)sprite.rect.x,
                (int)sprite.rect.y,
                (int)sprite.rect.width,
                (int)sprite.rect.height),
            scale = 1.0f
        };
        spriteGlyph.sprite = sprite;

        var glyphList = new Il2CppSystem.Collections.Generic.List<TMP_SpriteGlyph>();
        glyphList.Add(spriteGlyph);
        _toumceSpriteAsset.spriteGlyphTable = glyphList;

        var spriteChar = new TMP_SpriteCharacter(0, spriteGlyph)
        {
            name = name,
            scale = 1.0f
        };

        var charList = new Il2CppSystem.Collections.Generic.List<TMP_SpriteCharacter>();
        charList.Add(spriteChar);
        _toumceSpriteAsset.spriteCharacterTable = charList;

        var charLookup = new Il2CppSystem.Collections.Generic.Dictionary<uint, TMP_SpriteCharacter>();
        charLookup.Add(0, spriteChar);
        _toumceSpriteAsset.spriteCharacterLookupTable = charLookup;

        return _toumceSpriteAsset;
    }
}*/
// who would have thought that will not work












