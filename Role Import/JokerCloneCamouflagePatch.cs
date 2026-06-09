using HarmonyLib;
using TouMegaChujoweExtension.Modules;
using TownOfUs.Patches;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Joker
{
    [HarmonyPatch(typeof(HudManagerPatches), nameof(HudManagerPatches.UpdateCamouflageComms))]
    public static class JokerCloneCamouflagePatch
    {
        private static bool _lastActive;

        [HarmonyPostfix]
        public static void Postfix()
        {
            var isActive = HudManagerPatches.CommsSaboActive();
            if (isActive == _lastActive) return;
            _lastActive = isActive;

            foreach (var clone in JokerCloneSystem.Clones)
            {
                if (clone == null) continue;
                if (clone.Fake?.body == null || clone.IsPreview) continue;

                var renderers = clone.Fake.body.GetComponentsInChildren<SpriteRenderer>(true);

                if (isActive)
                {
                    foreach (var r in renderers)
                    {
                        if (r == null) continue;
                        PlayerMaterial.SetColors(Color.grey, r);
                    }

                    if (clone.Fake.pc?.cosmetics != null)
                    {
                        clone.Fake.pc.cosmetics.SetHat("hat_NoHat", 0);
                        clone.Fake.pc.cosmetics.SetVisor("visor_EmptyVisor", 0);
                        clone.Fake.pc.cosmetics.SetSkin("skin_None", 0);
                    }

                    if (clone.Fake.Pet != null)
                        clone.Fake.Pet.gameObject.SetActive(false);

                    var names = clone.Fake.body.transform.Find("Names");
                    if (names != null) names.gameObject.SetActive(false);
                }
                else
                {
                    var appearancePlayer = MiscUtils.PlayerById(clone.AppearancePlayerId);
                    if (appearancePlayer != null && clone.Fake.pc?.cosmetics != null)
                    {
                        var colorId = clone.Fake.ColorId;

                        // body color
                        var body = clone.Fake.pc.cosmetics.currentBodySprite?.BodySprite;
                        if (body != null)
                            PlayerMaterial.SetColors(colorId, body);

                        // cosmetics restore
                        clone.Fake.pc.cosmetics.SetHat(appearancePlayer.Data.DefaultOutfit.HatId, colorId);
                        clone.Fake.pc.cosmetics.SetVisor(appearancePlayer.Data.DefaultOutfit.VisorId, colorId);
                        clone.Fake.pc.cosmetics.SetSkin(appearancePlayer.Data.DefaultOutfit.SkinId, colorId);
                    }

                    if (clone.Fake.Pet != null)
                        clone.Fake.Pet.gameObject.SetActive(true);

                    var names = clone.Fake.body.transform.Find("Names");
                    if (names != null) names.gameObject.SetActive(true);
                }
            }
        }
    }
}