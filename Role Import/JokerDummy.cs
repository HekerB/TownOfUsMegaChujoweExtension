using System;
using TMPro;
using UnityEngine;
using TownOfUs.Utilities;
using TownOfUs.Utilities.Appearances;

namespace TouMegaChujoweExtension.Modules
{
    public class JokerDummy
    {
        public GameObject body;
        public PlayerControl pc;
        public PetBehaviour Pet;

        public int ColorId;

        public Vector2 PetBaseOffset = new(-0.3f, -0.2f);

        public Vector3 PetWorldScale = Vector3.one;

        public JokerDummy(PlayerControl target)
        {
            if (target == null || target.Data == null) return;

            var appearance = target.GetAppearance();
            ColorId = appearance.ColorId;

            var prefab = AmongUsClient.Instance.PlayerPrefab.gameObject;
            body = UnityEngine.Object.Instantiate(prefab, target.transform.position, Quaternion.identity);
            body.name = "JokerDummy_" + appearance.PlayerName;

            pc = body.GetComponent<PlayerControl>();

            PlayerControl.AllPlayerControls.Remove(pc);
            pc.enabled = false;
            if (pc.MyPhysics != null) pc.MyPhysics.enabled = false;

            var cnt = body.GetComponent<CustomNetworkTransform>();
            if (cnt != null) cnt.enabled = false;

            foreach (var col in body.GetComponentsInChildren<Collider2D>())
                UnityEngine.Object.Destroy(col);

            pc.transform.localScale = appearance.Size;

            pc.cosmetics.SetHat(appearance.HatId, ColorId);
            pc.cosmetics.SetVisor(appearance.VisorId, ColorId);
            pc.cosmetics.SetSkin(appearance.SkinId, ColorId);
            pc.SetColor(ColorId);

            if (target.cosmetics?.currentBodySprite?.BodySprite != null)
            {
                PlayerMaterial.SetColors(ColorId, pc.cosmetics.currentBodySprite.BodySprite);
                pc.cosmetics.currentBodySprite.BodySprite.color = target.cosmetics.currentBodySprite.BodySprite.color;
            }

            bool flipX = target.cosmetics?.currentBodySprite?.BodySprite?.flipX ?? false;

            EnsurePetFrom(target, flipX);

            // Names
            var targetNames = target.transform.Find("Names");
            var myNames = body.transform.Find("Names");

            if (targetNames != null && myNames != null)
            {
                myNames.localPosition = targetNames.localPosition;

                var myNameText = myNames.Find("NameText_TMP")?.GetComponent<TextMeshPro>();
                var targetNameText = targetNames.Find("NameText_TMP")?.GetComponent<TextMeshPro>();
                if (myNameText != null && targetNameText != null)
                {
                    myNameText.text = appearance.PlayerName;
                    myNameText.color = targetNameText.color;
                    myNameText.font = targetNameText.font;
                    myNameText.fontSize = targetNameText.fontSize;
                    myNameText.transform.localPosition = targetNameText.transform.localPosition;
                }

                var myCb = myNames.Find("ColorblindName_TMP")?.GetComponent<TextMeshPro>();
                var targetCb = targetNames.Find("ColorblindName_TMP")?.GetComponent<TextMeshPro>();
                if (myCb != null && targetCb != null)
                {
                    myCb.text = targetCb.text;
                    myCb.font = targetCb.font;
                    myCb.fontSize = targetCb.fontSize;
                    myCb.color = targetCb.color;
                    myCb.transform.localPosition = targetCb.transform.localPosition;
                    myCb.gameObject.SetActive(targetCb.gameObject.activeSelf);
                }

                var info = myNames.Find("Info");
                if (info != null) UnityEngine.Object.Destroy(info.gameObject);
            }
        }

        public void EnsurePetFrom(PlayerControl target, bool flipX)
        {
            if (Pet != null || body == null || target == null) return;

            var emptyPet = body.transform.Find("EmptyPet(Clone)");
            if (emptyPet != null) UnityEngine.Object.Destroy(emptyPet.gameObject);

            // CurrentPet
            try
            {
                var srcPet = target.cosmetics?.currentPet;
                var srcName = srcPet != null ? srcPet.name : "null";

                if (srcPet != null && !srcName.Contains("Empty"))
                {
                    PetWorldScale = srcPet.transform.lossyScale;

                    PetBaseOffset = ComputeSafeOffset(srcPet.transform.position - target.transform.position);

                    var petGo = UnityEngine.Object.Instantiate(srcPet.gameObject);
                    var pb = petGo.GetComponent<PetBehaviour>();
                    if (pb != null)
                    {
                        AttachPet(pb, ColorId, flipX);
                        Warning($"[JokerDummy] Pet cloned from currentPet: {srcName} offset={PetBaseOffset}");
                        return;
                    }
                }
                else
                {
                    Warning($"[JokerDummy] target.currentPet null/Empty: {srcName}");
                }
            }
            catch (Exception e)
            {
                Warning($"[JokerDummy] Clone from currentPet EXCEPTION: {e}");
            }

            // Fallback: cache
            try
            {
                var petId = target.Data?.DefaultOutfit?.PetId;
                if (string.IsNullOrEmpty(petId) || petId == PetData.EmptyId)
                {
                    Warning("[JokerDummy] No petId on target");
                    return;
                }

                if (ShipStatus.Instance?.CosmeticsCache == null)
                {
                    Warning("[JokerDummy] No CosmeticsCache");
                    return;
                }

                var prefab = ShipStatus.Instance.CosmeticsCache.GetPet(petId);
                if (prefab == null || prefab.name.Contains("Empty"))
                {
                    Warning($"[JokerDummy] Pet empty/null from cache: {prefab?.name} (petId='{petId}')");
                    return;
                }

                var pb2 = UnityEngine.Object.Instantiate(prefab);
                PetWorldScale = pb2.transform.lossyScale;

                PetBaseOffset = new Vector2(-0.3f, -0.2f);

                AttachPet(pb2, ColorId, flipX);
                Warning($"[JokerDummy] Pet OK from cache: {pb2.name}");
            }
            catch (Exception e)
            {
                Warning($"[JokerDummy] EnsurePetFrom(cache) EXCEPTION: {e}");
            }
        }

        private static Vector2 ComputeSafeOffset(Vector3 rawWorldOffset)
        {

            var x = rawWorldOffset.x;
            var y = rawWorldOffset.y;

            if (rawWorldOffset.sqrMagnitude > 1.2f * 1.2f)
                return new Vector2(-0.3f, -0.2f);

            var absX = Mathf.Clamp(Mathf.Abs(x), 0.18f, 0.38f);
            var clampedY = Mathf.Clamp(y, -0.35f, -0.05f);

            return new Vector2(-absX, clampedY);
        }

        private void AttachPet(PetBehaviour petBehaviour, int colorId, bool flipX)
        {
            if (petBehaviour == null || body == null) return;

            petBehaviour.transform.SetParent(null, true);

            if (PetWorldScale != default && PetWorldScale.sqrMagnitude > 0.001f)
                petBehaviour.transform.localScale = PetWorldScale;

            petBehaviour.SetCrewmateColor(colorId);
            petBehaviour.FlipX = flipX;

            foreach (var col in petBehaviour.GetComponentsInChildren<Collider2D>())
                UnityEngine.Object.Destroy(col);
            var ownCol = petBehaviour.GetComponent<Collider2D>();
            if (ownCol != null) UnityEngine.Object.Destroy(ownCol);

            petBehaviour.enabled = true;
            petBehaviour.gameObject.SetActive(true);

            var off = GetBaseOffsetForFlip(flipX);
            var b = body.transform.position;
            petBehaviour.transform.position = new Vector3(b.x + off.x, b.y + off.y, b.y / 1000f - 0.1f);

            Pet = petBehaviour;
        }

        public Vector2 GetBaseOffsetForFlip(bool flipX)
        {
            var x = Mathf.Abs(PetBaseOffset.x);
            return new Vector2(flipX ? x : -x, PetBaseOffset.y);
        }

        public void Destroy()
        {
            if (Pet != null) UnityEngine.Object.Destroy(Pet.gameObject);
            if (body != null) UnityEngine.Object.Destroy(body);
        }
    }
}