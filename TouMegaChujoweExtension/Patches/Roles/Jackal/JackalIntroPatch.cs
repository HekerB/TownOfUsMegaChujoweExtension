using HarmonyLib;
using MiraAPI.Modifiers;
using MiraAPI.Utilities;
using TownOfUs.Modules.Localization;
using TouMegaChujoweExtension.Modifiers.Neutral;
using TouMegaChujoweExtension.Assets;
using TownOfUs.Extensions;
using TownOfUs.Utilities;
using UnityEngine;
using System.Collections;
using Reactor.Utilities;

namespace TouMegaChujoweExtension.Patches.Roles.Jackal;

[HarmonyPatch]
public static class JackalIntroPatch
{
    [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.BeginCrewmate))]
    [HarmonyPostfix]
    public static void BeginCrewmatePostfix(IntroCutscene __instance)
    {
        AddRecruitBlurb(__instance);
    }

    [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.BeginImpostor))]
    [HarmonyPostfix]
    public static void BeginImpostorPostfix(IntroCutscene __instance)
    {
        AddRecruitBlurb(__instance);
    }

    private static void AddRecruitBlurb(IntroCutscene instance)
    {
        var localPlayer = PlayerControl.LocalPlayer;
        if (localPlayer == null || instance == null) return;

        if (localPlayer.TryGetModifier<SidekickModifier>(out _))
        {
            ShowRecruitIntroText(instance);
            return;
        }

        if (JackalStartPatch.PendingAssignments.ContainsKey(localPlayer.PlayerId))
        {
            ShowRecruitIntroText(instance);
            return;
        }

        Coroutines.Start(CoDelayedRecruitNotification(localPlayer));
    }

    public static void UpdateIntroCutscene(IntroCutscene instance)
    {
        ShowRecruitIntroText(instance);
    }

    private static void ShowRecruitIntroText(IntroCutscene instance)
    {
        if (instance == null) return;

        var headerText = TouLocale.Get("ExtensionJackalSidekickIntroHeader");
        var jackalInfo = TouLocale.GetParsed("SidekickIntroBlurb");
        var recruitName = SidekickModifier.ShortName;
        
        if (instance.YouAreText != null && !string.IsNullOrEmpty(headerText))
        {
            instance.YouAreText.text = headerText;
        }

        if (instance.RoleText != null && !string.IsNullOrEmpty(recruitName))
        {
            instance.RoleText.text = recruitName;
            instance.RoleText.color = TouExtensionColors.Jackal;
        }

        if (instance.BackgroundBar != null)
        {
            instance.BackgroundBar.material.color = TouExtensionColors.Jackal;
        }

        if (!string.IsNullOrEmpty(jackalInfo) && instance.RoleBlurbText != null && !instance.RoleBlurbText.text.Contains(jackalInfo))
        {
            instance.RoleBlurbText.text += $"\n<size=2.5><color=#{ColorUtility.ToHtmlStringRGBA(TouExtensionColors.Jackal)}>{jackalInfo}</color></size>";
        }
    }

    private static IEnumerator CoDelayedRecruitNotification(PlayerControl localPlayer)
    {
        float elapsed = 0f;
        const float maxWait = 4f;
        const float pollInterval = 0.5f;

        while (elapsed < maxWait)
        {
            yield return new WaitForSeconds(pollInterval);
            elapsed += pollInterval;

            if (localPlayer == null || localPlayer.Pointer == System.IntPtr.Zero) yield break;

            if (localPlayer.TryGetModifier<SidekickModifier>(out var mod) || JackalStartPatch.PendingAssignments.ContainsKey(localPlayer.PlayerId))
            {
                var activeIntro = UnityEngine.Object.FindObjectOfType<IntroCutscene>();
                if (activeIntro != null)
                {
                    ShowRecruitIntroText(activeIntro);
                }

                if (mod != null && !mod.WasNotified)
                {
                    mod.WasNotified = true;
                    try
                    {
                        var notification = Helpers.CreateAndShowNotification(
                            TouLocale.Get("ExtensionSidekickRecruitedAlert"),
                            TouExtensionColors.Jackal,
                            new Vector3(0f, 1f, -20f),
                            spr: TouExtensionIcons.SidekickModifierIcon.LoadAsset()
                        );
                        if (notification != null)
                        {
                            notification.AdjustNotification();
                        }
                        Reactor.Utilities.Coroutines.Start(MiscUtils.CoFlash(TouExtensionColors.Jackal));
                    }
                    catch (System.Exception ex)
                    {
                        UnityEngine.Debug.LogError($"[TOUMCE] Error showing Sidekick recruited notification in intro: {ex}");
                    }
                }

                yield break;
            }
        }

        if (localPlayer != null && localPlayer.Pointer != System.IntPtr.Zero && localPlayer.TryGetModifier<SidekickModifier>(out var lateMod) && !lateMod.WasNotified)
        {
            lateMod.WasNotified = true;
            try
            {
                var notification = Helpers.CreateAndShowNotification(
                    TouLocale.Get("ExtensionSidekickRecruitedAlert"),
                    TouExtensionColors.Jackal,
                    new Vector3(0f, 1f, -20f),
                    spr: TouExtensionIcons.SidekickModifierIcon.LoadAsset()
                );
                if (notification != null)
                {
                    notification.AdjustNotification();
                }
                Reactor.Utilities.Coroutines.Start(MiscUtils.CoFlash(TouExtensionColors.Jackal));
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"[TOUMCE] Error showing late Sidekick recruited notification: {ex}");
            }
        }
    }
}
