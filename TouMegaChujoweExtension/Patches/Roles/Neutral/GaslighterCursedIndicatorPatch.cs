using HarmonyLib;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using TMPro;
using TownOfUs.Patches;
using TownOfUs.Utilities;
using TouMegaChujoweExtension.Modifiers.Neutral;
using TouMegaChujoweExtension.Roles.Neutral;
using TouMegaChujoweExtension.Assets;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace TouMegaChujoweExtension.Patches.Roles.Neutral;

[HarmonyPatch(typeof(HudManagerPatches), nameof(HudManagerPatches.UpdateRoleNameText))]
public static class GaslighterCursedIndicatorPatch
{
    private static readonly Dictionary<byte, SpriteRenderer> CursedSprites = new();

    [HarmonyPostfix]
    public static void UpdateRoleNameTextPostfix()
    {
        if (PlayerControl.LocalPlayer == null)
        {
            return;
        }

        if (MeetingHud.Instance != null)
        {
            foreach (var playerVA in MeetingHud.Instance.playerStates)
            {
                var player = MiscUtils.PlayerById(playerVA.TargetPlayerId);
                if (player == null || player.Data == null)
                {
                    continue;
                }

                var shouldShow = ShouldShowCursedSprite(player);

                if (shouldShow)
                {
                    EnsureCursedSpriteForMeeting(player, playerVA);
                }
                else
                {
                    HideCursedSprite(player);
                    var meetingKey = (byte)(player.PlayerId + 200);
                    if (CursedSprites.TryGetValue(meetingKey, out var meetingSprite) && meetingSprite != null)
                    {
                        meetingSprite.gameObject.SetActive(false);
                    }
                }
            }
        }
        else
        {
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player == null || player.Data == null || player.cosmetics?.nameText == null)
                {
                    continue;
                }

                var shouldShow = ShouldShowCursedSprite(player);

                if (shouldShow)
                {
                    EnsureCursedSprite(player);
                }
                else
                {
                    HideCursedSprite(player);
                }
            }
        }
    }

    private static bool ShouldShowCursedSprite(PlayerControl player)
    {
        if (player == null || !player.HasModifier<GaslighterCursedModifier>())
        {
            return false;
        }

        var localPlayer = PlayerControl.LocalPlayer;
        if (localPlayer == null)
        {
            return false;
        }

        if (MeetingHud.Instance == null)
        {
            // Only Gaslighter sees it during gameplay
            return localPlayer.IsRole<GaslighterRole>();
        }

        // Everyone sees it during meetings
        return true;
    }

    private static bool IsPlayerNameVisible(PlayerControl player, TextMeshPro nameText)
    {
        if (player == null || nameText == null || PlayerControl.LocalPlayer == null)
        {
            return false;
        }

        return nameText.gameObject.activeInHierarchy && nameText.enabled && nameText.color.a > 0.01f;
    }

    private static void EnsureCursedSprite(PlayerControl player)
    {
        if (player == null || player.cosmetics?.nameText == null)
        {
            return;
        }

        var playerId = player.PlayerId;
        var nameText = player.cosmetics.nameText;
        var nameTextGameObject = nameText.gameObject;

        nameText.ForceMeshUpdate();

        var shouldShow = IsPlayerNameVisible(player, nameText);

        if (!CursedSprites.TryGetValue(playerId, out var spriteRenderer) || spriteRenderer == null)
        {
            var spriteObj = new GameObject($"GaslighterCursedSprite_{playerId}");

            spriteObj.transform.SetParent(nameTextGameObject.transform, false);
            spriteObj.transform.localPosition = Vector3.zero;

            float textWidth = 0f;
            if (nameText.textBounds.size.x > 0)
            {
                textWidth = nameText.textBounds.size.x / 2f;
            }
            else if (nameText.preferredWidth > 0)
            {
                textWidth = nameText.preferredWidth / 2f;
            }

            spriteObj.transform.localPosition += new Vector3(textWidth + 0.15f, 0f, -0.1f);

            spriteRenderer = spriteObj.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = TouExtensionAssets.HexedSprite.LoadAsset();
            spriteRenderer.sortingOrder = nameText.sortingOrder + 1;
            spriteRenderer.transform.localScale = Vector3.one * 0.4f;
            spriteRenderer.color = TouExtensionColors.Gaslighter;

            spriteObj.layer = nameTextGameObject.layer;

            CursedSprites[playerId] = spriteRenderer;
        }
        else
        {
            if (spriteRenderer.transform.parent != nameTextGameObject.transform)
            {
                spriteRenderer.transform.SetParent(nameTextGameObject.transform, false);
            }

            float textWidth = 0f;
            if (nameText.textBounds.size.x > 0)
            {
                textWidth = nameText.textBounds.size.x / 2f;
            }
            else if (nameText.preferredWidth > 0)
            {
                textWidth = nameText.preferredWidth / 2f;
            }

            spriteRenderer.transform.localPosition = new Vector3(textWidth + 0.15f, 0f, -0.1f);
            spriteRenderer.color = TouExtensionColors.Gaslighter;
            spriteRenderer.sortingOrder = nameText.sortingOrder + 1;
        }

        spriteRenderer.enabled = shouldShow;

        if (shouldShow)
        {
            var spriteColor = spriteRenderer.color;
            spriteColor.a = nameText.color.a;
            spriteRenderer.color = spriteColor;
        }
        else
        {
            var spriteColor = spriteRenderer.color;
            spriteColor.a = 0f;
            spriteRenderer.color = spriteColor;
        }
    }

    private static void EnsureCursedSpriteForMeeting(PlayerControl player, PlayerVoteArea playerVA)
    {
        if (player == null || playerVA == null || playerVA.NameText == null)
        {
            return;
        }

        var playerId = player.PlayerId;
        var nameText = playerVA.NameText;

        nameText.ForceMeshUpdate();

        var meetingKey = (byte)(playerId + 200);

        if (!CursedSprites.TryGetValue(meetingKey, out var spriteRenderer) || spriteRenderer == null)
        {
            var spriteObj = new GameObject($"GaslighterCursedSprite_Meeting_{playerId}");
            spriteObj.transform.SetParent(playerVA.transform);

            if (playerVA.Megaphone != null)
            {
                spriteObj.layer = playerVA.Megaphone.gameObject.layer;
            }

            float textWidth = 0f;
            if (nameText.textBounds.size.x > 0)
            {
                textWidth = nameText.textBounds.size.x / 2f;
            }
            else if (nameText.preferredWidth > 0)
            {
                textWidth = nameText.preferredWidth / 2f;
            }

            var nameTextLocalPos = nameText.transform.localPosition;
            spriteObj.transform.localPosition = new Vector3(nameTextLocalPos.x + textWidth + 0.15f, nameTextLocalPos.y, -1f);

            spriteRenderer = spriteObj.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = TouExtensionAssets.HexedSprite.LoadAsset();
            spriteRenderer.transform.localScale = Vector3.one * 0.4f;
            spriteRenderer.color = TouExtensionColors.Gaslighter;

            CursedSprites[meetingKey] = spriteRenderer;
        }
        else
        {
            float textWidth = 0f;
            if (nameText.textBounds.size.x > 0)
            {
                textWidth = nameText.textBounds.size.x / 2f;
            }
            else if (nameText.preferredWidth > 0)
            {
                textWidth = nameText.preferredWidth / 2f;
            }

            var nameTextLocalPos = nameText.transform.localPosition;
            spriteRenderer.transform.localPosition = new Vector3(nameTextLocalPos.x + textWidth + 0.15f, nameTextLocalPos.y, -1f);
            spriteRenderer.color = TouExtensionColors.Gaslighter;

            if (spriteRenderer.transform.parent != playerVA.transform)
            {
                spriteRenderer.transform.SetParent(playerVA.transform);
            }
        }

        spriteRenderer.gameObject.SetActive(true);
    }

    private static void HideCursedSprite(PlayerControl player)
    {
        if (player == null)
        {
            return;
        }

        var playerId = player.PlayerId;
        if (CursedSprites.TryGetValue(playerId, out var spriteRenderer) && spriteRenderer != null)
        {
            spriteRenderer.gameObject.SetActive(false);
        }
    }
}
