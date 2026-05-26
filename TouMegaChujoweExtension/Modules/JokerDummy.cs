using TMPro;
using TownOfUs.Utilities;
using TownOfUs.Utilities.Appearances;
using UnityEngine;

namespace TouMegaChujoweExtension.Modules;

public sealed class JokerDummy
{
    public GameObject? Body { get; private set; }

    public JokerDummy(PlayerControl target)
    {
        if (target == null || target.Data == null)
        {
            return;
        }

        var appearance = target.GetAppearance();
        var prefab = AmongUsClient.Instance.PlayerPrefab.gameObject;
        Body = UnityEngine.Object.Instantiate(prefab, target.transform.position, Quaternion.identity);
        Body.name = "JokerDummy_" + appearance.PlayerName;

        var player = Body.GetComponent<PlayerControl>();
        if (player != null)
        {
            PlayerControl.AllPlayerControls.Remove(player);
            player.enabled = false;
            if (player.MyPhysics != null)
            {
                player.MyPhysics.enabled = false;
            }

            player.transform.localScale = appearance.Size;
            player.cosmetics.SetHat(appearance.HatId, appearance.ColorId);
            player.cosmetics.SetVisor(appearance.VisorId, appearance.ColorId);
            player.cosmetics.SetSkin(appearance.SkinId, appearance.ColorId);
            player.SetColor(appearance.ColorId);

            if (target.cosmetics?.currentBodySprite?.BodySprite != null &&
                player.cosmetics?.currentBodySprite?.BodySprite != null)
            {
                PlayerMaterial.SetColors(appearance.ColorId, player.cosmetics.currentBodySprite.BodySprite);
                player.cosmetics.currentBodySprite.BodySprite.color = target.cosmetics.currentBodySprite.BodySprite.color;
            }
        }

        var networkTransform = Body.GetComponent<CustomNetworkTransform>();
        if (networkTransform != null)
        {
            networkTransform.enabled = false;
        }

        foreach (var collider in Body.GetComponentsInChildren<Collider2D>())
        {
            UnityEngine.Object.Destroy(collider);
        }

        CopyNames(target, appearance);
    }

    private void CopyNames(PlayerControl target, VisualAppearance appearance)
    {
        if (Body == null)
        {
            return;
        }

        var targetNames = target.transform.Find("Names");
        var cloneNames = Body.transform.Find("Names");
        if (targetNames == null || cloneNames == null)
        {
            return;
        }

        cloneNames.localPosition = targetNames.localPosition;

        var cloneNameText = cloneNames.Find("NameText_TMP")?.GetComponent<TextMeshPro>();
        var targetNameText = targetNames.Find("NameText_TMP")?.GetComponent<TextMeshPro>();
        if (cloneNameText != null && targetNameText != null)
        {
            cloneNameText.text = appearance.PlayerName;
            cloneNameText.color = targetNameText.color;
            cloneNameText.font = targetNameText.font;
            cloneNameText.fontSize = targetNameText.fontSize;
            cloneNameText.transform.localPosition = targetNameText.transform.localPosition;
        }

        var cloneColorblind = cloneNames.Find("ColorblindName_TMP")?.GetComponent<TextMeshPro>();
        var targetColorblind = targetNames.Find("ColorblindName_TMP")?.GetComponent<TextMeshPro>();
        if (cloneColorblind != null && targetColorblind != null)
        {
            cloneColorblind.text = targetColorblind.text;
            cloneColorblind.font = targetColorblind.font;
            cloneColorblind.fontSize = targetColorblind.fontSize;
            cloneColorblind.color = targetColorblind.color;
            cloneColorblind.transform.localPosition = targetColorblind.transform.localPosition;
            cloneColorblind.gameObject.SetActive(targetColorblind.gameObject.activeSelf);
        }

        var info = cloneNames.Find("Info");
        if (info != null)
        {
            UnityEngine.Object.Destroy(info.gameObject);
        }
    }

    public void Destroy()
    {
        if (Body != null)
        {
            UnityEngine.Object.Destroy(Body);
        }
    }
}
