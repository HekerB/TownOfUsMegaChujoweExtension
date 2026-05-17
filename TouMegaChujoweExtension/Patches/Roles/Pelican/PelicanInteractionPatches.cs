using HarmonyLib;
using MiraAPI.Hud;
using MiraAPI.Modifiers;
using Reactor.Utilities;
using TownOfUs.Buttons;
using TownOfUs.Events;
using TownOfUs.Modifiers;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using TownOfUs.Modifiers.Game.Universal;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using TouMegaChujoweExtension.Modifiers.Game;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Roles.Pelican;

[HarmonyPatch]
public static class PelicanInteractionPatches
{
    // ==================== BLOCK ALL CUSTOM BUTTONS FOR SWALLOWED PLAYERS ====================

    [HarmonyPatch(typeof(TownOfUsButton), nameof(TownOfUsButton.CanUse))]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    public static bool TownOfUsButtonCanUsePrefix(ref bool __result)
    {
        var local = PlayerControl.LocalPlayer;
        if (local != null && PelicanSystem.IsSwallowed(local.PlayerId))
        {
            __result = false;
            return false;
        }
        return true;
    }

    private static bool _wasSwallowedLastFrame;
    
    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    [HarmonyPostfix]
    public static void HudManagerUpdatePostfix()
    {
        var local = PlayerControl.LocalPlayer;
        bool isSwallowed = local != null && PelicanSystem.IsSwallowed(local.PlayerId);

        if (isSwallowed)
        {
            if (HudManager.Instance != null && HudManager.Instance.ShadowQuad != null && HudManager.Instance.ShadowQuad.gameObject.activeSelf)
            {
                HudManager.Instance.ShadowQuad.gameObject.SetActive(false);
            }

            if (!_wasSwallowedLastFrame)
            {
                foreach (var button in CustomButtonManager.Buttons)
                {
                    if (button?.Button != null && button.Button.gameObject.activeSelf)
                    {
                        button.Button.gameObject.SetActive(false);
                    }
                }
            }
        }
        else if (_wasSwallowedLastFrame)
        {
            if (HudManager.Instance != null && HudManager.Instance.ShadowQuad != null && !HudManager.Instance.ShadowQuad.gameObject.activeSelf)
            {
                HudManager.Instance.ShadowQuad.gameObject.SetActive(true);
            }
        }
        
        _wasSwallowedLastFrame = isSwallowed;
    }

    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.CalculateLightRadius))]
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    public static void CalculateLightRadiusPostfix(ShipStatus __instance, NetworkedPlayerInfo player, ref float __result)
    {
        var localPlayer = PlayerControl.LocalPlayer;
        if (localPlayer != null && PelicanSystem.IsSwallowed(localPlayer.PlayerId))
        {
            __result = 100f; // Extremely large vision radius (super-torch) to illuminate the entire map
        }
    }

    [HarmonyPatch(typeof(TownOfUsButton), nameof(TownOfUsButton.ClickHandler))]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    public static bool TownOfUsButtonClickPrefix()
    {
        var local = PlayerControl.LocalPlayer;
        if (local != null && PelicanSystem.IsSwallowed(local.PlayerId)) return false;
        return true;
    }

    // ==================== BLOCK SWALLOWED PLAYER ABILITIES ====================

    [HarmonyPatch(typeof(KillButton), nameof(KillButton.DoClick))]
    [HarmonyPrefix]
    public static bool KillButtonPrefix()
    {
        var local = PlayerControl.LocalPlayer;
        if (local != null && PelicanSystem.IsSwallowed(local.PlayerId)) return false;
        return true;
    }

    [HarmonyPatch(typeof(UseButton), nameof(UseButton.DoClick))]
    [HarmonyPrefix]
    public static bool UseButtonPrefix()
    {
        var local = PlayerControl.LocalPlayer;
        if (local != null && PelicanSystem.IsSwallowed(local.PlayerId)) return false;
        return true;
    }

    [HarmonyPatch(typeof(ReportButton), nameof(ReportButton.DoClick))]
    [HarmonyPrefix]
    public static bool ReportButtonDoClickPrefix()
    {
        var local = PlayerControl.LocalPlayer;
        if (local != null && PelicanSystem.IsSwallowed(local.PlayerId)) return false;
        return true;
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CmdReportDeadBody))]
    [HarmonyPrefix]
    public static bool CmdReportBodyPrefix(PlayerControl __instance)
    {
        if (PelicanSystem.IsSwallowed(__instance.PlayerId)) return false;
        return true;
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ReportDeadBody))]
    [HarmonyPrefix]
    public static bool ReportDeadBodyPrefix(PlayerControl __instance)
    {
        if (PelicanSystem.IsSwallowed(__instance.PlayerId)) return false;
        return true;
    }

    [HarmonyPatch(typeof(Vent), nameof(Vent.CanUse))]
    [HarmonyPrefix]
    public static bool VentCanUsePrefix(ref float __result, [HarmonyArgument(0)] NetworkedPlayerInfo playerInfo)
    {
        if (playerInfo?.Object != null && PelicanSystem.IsSwallowed(playerInfo.PlayerId))
        {
            __result = float.MaxValue;
            return false;
        }
        return true;
    }

    // ==================== BLOCK TARGETING SWALLOWED PLAYERS ====================

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckMurder))]
    [HarmonyPrefix]
    public static bool CheckMurderPrefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
    {
        if (target != null && PelicanSystem.IsSwallowed(target.PlayerId)) return false;
        return true;
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
    [HarmonyPrefix]
    public static bool MurderPlayerPrefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
    {
        if (target == null || !PelicanSystem.IsSwallowed(target.PlayerId)) return true;

        var pelicanId = PelicanSystem.GetPelicanOf(target.PlayerId);
        if (pelicanId.HasValue && __instance.PlayerId == pelicanId.Value) return true;
        if (PelicanSystem.IsPendingDigest(target.PlayerId)) return true;

        return false;
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Die))]
    [HarmonyPrefix]
    public static bool DiePrefix(PlayerControl __instance, [HarmonyArgument(0)] DeathReason reason)
    {
        if (!PelicanSystem.IsSwallowed(__instance.PlayerId)) return true;
        if (PelicanSystem.IsPendingDigest(__instance.PlayerId)) return true;
        if (PelicanSystem.IsDigestKillVictim(__instance.PlayerId)) return true;

        return false;
    }

    // ==================== PUPPETEER BLOCK ====================

    [HarmonyPatch(typeof(TownOfUs.Roles.Impostor.PuppeteerRole), nameof(TownOfUs.Roles.Impostor.PuppeteerRole.RpcPuppeteerControl))]
    [HarmonyPrefix]
    public static bool PuppeteerControlPrefix(PlayerControl target)
    {
        if (target != null && PelicanSystem.IsSwallowed(target.PlayerId)) return false;
        return true;
    }

    // ==================== POSITION SYNC + TRACKING + PRE-WIN DIGEST ====================

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
    [HarmonyPostfix]
    public static void PlayerFixedUpdatePostfix(PlayerControl __instance)
    {
        if (__instance == null || __instance.Data == null) return;

        if (PelicanSystem.IsSwallowed(__instance.PlayerId) && !__instance.HasDied())
        {
            if (__instance.Visible) __instance.Visible = false;
            if (__instance.moveable) __instance.moveable = false;

            var col = __instance.GetComponent<UnityEngine.Collider2D>();
            if (col != null && col.enabled) col.enabled = false;

            var pelicanId = PelicanSystem.GetPelicanOf(__instance.PlayerId);
            if (pelicanId.HasValue)
            {
                var pelican = MiscUtils.PlayerById(pelicanId.Value);
                if (pelican != null && !pelican.HasDied())
                {
                    var pelicanPos = pelican.GetTruePosition();
                    __instance.transform.position = new Vector3(pelicanPos.x, pelicanPos.y, __instance.transform.position.z);
                    if (__instance.MyPhysics != null && __instance.MyPhysics.body != null)
                    {
                        __instance.MyPhysics.body.position = pelicanPos;
                        __instance.MyPhysics.body.velocity = Vector2.zero;
                    }
                }
            }
        }
    }

    // ==================== EXILE CLEANUP ====================

    [HarmonyPatch(typeof(ExileController), nameof(ExileController.WrapUp))]
    [HarmonyPostfix]
    public static void ExileWrapUpPostfix()
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null) return;

        if (local.HasDied())
        {
            if (PelicanSystem.IsSwallowed(local.PlayerId)) PelicanSystem.ReleaseAll(PelicanSystem.GetPelicanOf(local.PlayerId) ?? 0);
            local.moveable = true;
            local.Visible = true;
        }

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null || player.Data == null) continue;

            if (PelicanSystem.IsSwallowed(player.PlayerId) && player.HasDied())
            {
                var pelicanId = PelicanSystem.GetPelicanOf(player.PlayerId);
                if (pelicanId.HasValue) PelicanSystem.ClearForPelican(pelicanId.Value);
            }
        }
    }

    // ==================== ZABICIE PRZED SPOTKANIEM ====================

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ReportDeadBody))]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    public static void ReportDeadBodyDigestPrefix()
    {
        DigestAllPelicans();
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.StartMeeting))]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    public static void StartMeetingPrefix()
    {
        DigestAllPelicans();
    }

    private static void DigestAllPelicans()
    {
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null) continue;
            if (PelicanSystem.GetSwallowedByPelican(player.PlayerId).Count > 0)
            {
                PelicanSystem.DigestAll(player.PlayerId);
            }
        }
    }



    // ==================== CLEANUP NA START GRY ====================

    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Start))]
    [HarmonyPrefix]
    public static void ShipStatusStartPrefix()
    {
        PelicanSystem.ClearAll();
    }

    // ==================== CLEANUP NA LOBBY ====================

    [HarmonyPatch(typeof(LobbyBehaviour), nameof(LobbyBehaviour.Start))]
    [HarmonyPostfix]
    public static void LobbyStartPostfix()
    {
        PelicanSystem.ForceResetAllPlayers();
    }

    // ==================== CLEANUP NA KONIEC GRY ====================

    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameEnd))]
    [HarmonyPostfix]
    public static void OnGameEndPostfix()
    {
        PelicanSystem.ForceResetAllPlayers();
    }

    // ==================== MODIFIER EXCLUSIONS ====================
    // Commented out due to signature mismatch in this TOU version
    /*
    [HarmonyPatch(typeof(MiniModifier), nameof(MiniModifier.IsModifierValidOn))]
    [HarmonyPostfix]
    public static void MiniModifierIsModifierValidOnPostfix(ref bool __result, RoleBehaviour role)
    {
        if (role is PelicanRole) __result = false;
    }

    [HarmonyPatch(typeof(ChildModifier), nameof(ChildModifier.IsModifierValidOn))]
    [HarmonyPostfix]
    public static void ChildModifierIsModifierValidOnPostfix(ref bool __result, RoleBehaviour role)
    {
        if (role is PelicanRole) __result = false;
    }
    */
}