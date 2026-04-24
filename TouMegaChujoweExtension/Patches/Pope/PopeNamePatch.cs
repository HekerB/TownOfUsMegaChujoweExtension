using HarmonyLib;
using MiraAPI.Modifiers;
using TMPro;
using TouMegaChujoweExtension.Modifiers;
using TouMegaChujoweExtension.Roles.Neutral;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TouMegaChujoweExtension.Patches.Pope;

[HarmonyPatch]
public static class PopeNamePatch
{
    private static readonly Dictionary<byte, GameObject> ThetaObjects = new();
    private static string _goldHex;

    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    [HarmonyPostfix]
    public static void HudUpdatePostfix()
    {
        var localPlayer = PlayerControl.LocalPlayer;
        if (localPlayer?.Data == null)
        {
            CleanupAll();
            return;
        }

        var isPope = localPlayer.Data.Role is PopeRole;
        var isDeadAndCanSee = localPlayer.Data.IsDead;

        // Dead pope (role changed to ghost) - check if was pope
        if (!isPope && isDeadAndCanSee)
        {
            // Check if any player has canonized modifier - if so, there was a pope
            // and dead players should see the symbols
            var anyCanonized = false;
            foreach (var p in PlayerControl.AllPlayerControls)
            {
                if (p != null && p.HasModifier<PopeCanonizedModifier>())
                {
                    anyCanonized = true;
                    break;
                }
            }

            if (!anyCanonized)
            {
                CleanupAll();
                return;
            }
        }
        else if (!isPope)
        {
            CleanupAll();
            return;
        }

        _goldHex ??= ColorUtility.ToHtmlStringRGBA(TouExtensionColors.Pope);

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player?.cosmetics?.nameText == null) continue;

            var id = player.PlayerId;
            var isCanonized = player.HasModifier<PopeCanonizedModifier>();

            if (isCanonized)
            {
                if (!ThetaObjects.TryGetValue(id, out var thetaObj) || thetaObj == null)
                {
                    thetaObj = CreateThetaLabel(player);
                    ThetaObjects[id] = thetaObj;
                }

                UpdateThetaPosition(player, thetaObj);
                thetaObj.SetActive(player.cosmetics.nameText.gameObject.activeSelf && !MeetingHud.Instance);
            }
            else if (ThetaObjects.TryGetValue(id, out var thetaObj) && thetaObj)
            {
                thetaObj.SetActive(false);
            }
        }
    }

    private static GameObject CreateThetaLabel(PlayerControl player)
    {
        var nameText = player.cosmetics.nameText;

        var go = Object.Instantiate(nameText.gameObject, nameText.transform.parent);
        go.name = $"PopeTheta_{player.PlayerId}";

        for (var i = go.transform.childCount - 1; i >= 0; i--)
        {
            Object.Destroy(go.transform.GetChild(i).gameObject);
        }

        var tmp = go.GetComponent<TextMeshPro>();
        tmp.text = $"<color=#{_goldHex}>Θ</color>";
        go.transform.localScale = nameText.transform.localScale;

        return go;
    }

    private static void UpdateThetaPosition(PlayerControl player, GameObject thetaObj)
    {
        var nameText = player.cosmetics.nameText;
        var namePos = nameText.transform.localPosition;
        var nameWidth = nameText.GetRenderedValues(false).x;

        thetaObj.transform.localPosition = new Vector3(
            namePos.x + nameWidth * 0.5f + 0.15f,
            namePos.y,
            namePos.z);
    }

    public static void CleanupAll()
    {
        foreach (var kvp in ThetaObjects)
        {
            if (kvp.Value) Object.Destroy(kvp.Value);
        }
        ThetaObjects.Clear();
    }

    [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.OnDestroy))]
    [HarmonyPostfix]
    public static void IntroCleanup()
    {
        CleanupAll();
    }
}