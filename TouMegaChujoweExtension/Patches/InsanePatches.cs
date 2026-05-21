using HarmonyLib;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using TownOfUs;
using TownOfUs.Roles;
using TownOfUs.Roles.Crewmate;
using TownOfUs.Utilities;
using TownOfUs.Utilities.Appearances;
using TownOfUs.Modules;
using TownOfUs.Modules.Localization;
using TownOfUs.Assets;
using TownOfUs.Modifiers.Crewmate;
using TownOfUs.Options.Roles.Crewmate;
using TownOfUs.Buttons.Crewmate;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Roles;
using MiraAPI.Utilities;
using Reactor.Utilities.Extensions;
using TouMegaChujoweExtension.Modifiers;

namespace TouMegaChujoweExtension.Patches;

[HarmonyPatch]
public static class InsanePatches
{
    // ═══════════════════════════════════════════════════════════
    //  1. SEER — Good appears Red, Evil appears Green (inverted)
    // ═══════════════════════════════════════════════════════════
    [HarmonyPatch(typeof(PlayerRoleTextExtensions), nameof(PlayerRoleTextExtensions.UpdateTargetColor), typeof(Color), typeof(PlayerControl), typeof(bool))]
    [HarmonyPostfix]
    public static void SeerInsanityPostfix(ref Color __result, PlayerControl player)
    {
        var lp = PlayerControl.LocalPlayer;
        if (lp == null || player == null || !lp.HasModifier<InsaneModifier>() || !lp.IsRole<SeerRole>())
            return;

        if (player.HasModifier<SeerGoodRevealModifier>())
            __result = Color.red;
        else if (player.HasModifier<SeerEvilRevealModifier>())
            __result = Color.green;
    }

    // ═══════════════════════════════════════════════════════════
    //  2. INVESTIGATOR — Footprint colors shifted to wrong player
    // ═══════════════════════════════════════════════════════════
    [HarmonyPatch(typeof(FootstepsModifier), nameof(FootstepsModifier.FixedUpdate))]
    [HarmonyPostfix]
    public static void InvestigatorInsanityPostfix(FootstepsModifier __instance)
    {
        var lp = PlayerControl.LocalPlayer;
        if (lp == null || !lp.HasModifier<InsaneModifier>()) return;

        if (__instance._currentSteps != null && __instance._currentSteps.Count > 0)
        {
            foreach (var step in __instance._currentSteps)
            {
                if (step.Value != null)
                {
                    var players = PlayerControl.AllPlayerControls.ToArray();
                    var fake = players[(__instance.Player.PlayerId + 3) % players.Count];
                    if (fake?.cosmetics?.currentBodySprite?.BodySprite != null)
                        step.Value.color = fake.cosmetics.currentBodySprite.BodySprite.material.GetColor(ShaderID.BodyColor);
                }
            }
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  3. SHERIFF — Inverted misfire (crewmates = success, evil = misfire)
    // ═══════════════════════════════════════════════════════════
    [HarmonyPatch(typeof(SheriffShootButton), "OnClick")]
    [HarmonyPrefix]
    public static bool SheriffInsanityPrefix(SheriffShootButton __instance)
    {
        var lp = PlayerControl.LocalPlayer;
        if (lp == null || !lp.HasModifier<InsaneModifier>()) return true;
        if (__instance.Target == null) return false;
        if (__instance.Target.HasModifier<TownOfUs.Modifiers.FirstDeadShield>() || __instance.Target.HasModifier<TownOfUs.Modifiers.BaseShieldModifier>())
            return false;

        var alignment = __instance.Target.Data.Role.GetRoleAlignment();
        var options = OptionGroupSingleton<SheriffOptions>.Instance;

        bool shouldMisfire = alignment switch
        {
            RoleAlignment.CrewmateInvestigative or RoleAlignment.CrewmateKilling or
            RoleAlignment.CrewmateProtective or RoleAlignment.CrewmatePower or
            RoleAlignment.CrewmateSupport => false,
            _ => true
        };

        if (shouldMisfire)
        {
            SheriffRole.RpcSheriffMisfire(lp);
            var missType = options.MisfireType;
            if (missType is MisfireOptions.Target or MisfireOptions.Both)
                lp.RpcCustomMurder(__instance.Target, MeetingCheck.OutsideMeeting);
            if (missType is MisfireOptions.Sheriff or MisfireOptions.Both)
                lp.RpcCustomMurder(lp, MeetingCheck.OutsideMeeting);
            __instance.FailedShot = true;
            var notif = Helpers.CreateAndShowNotification(
                $"<b>{TouLocale.GetParsed("TouRoleSheriffMisfireFeedback")}</b>",
                Color.white, new Vector3(0f, 1f, -20f), spr: TouRoleIcons.Sheriff.LoadAsset());
            notif.AdjustNotification();
            Reactor.Utilities.Coroutines.Start(MiscUtils.CoFlash(Color.red));
        }
        else
        {
            lp.RpcCustomMurder(__instance.Target, MeetingCheck.OutsideMeeting);
        }

        if (!options.SheriffBodyReport)
        {
            var method = typeof(SheriffShootButton).GetMethod("CoSetBodyReportable",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            if (method != null)
            {
                var coroutine = method.Invoke(null, new object[] { __instance.Target.PlayerId });
                if (coroutine != null)
                    Reactor.Utilities.Coroutines.Start((System.Collections.IEnumerator)coroutine);
            }
        }
        return false;
    }

    // ═══════════════════════════════════════════════════════════
    //  4. SNITCH — Arrows point to Crewmates instead of Impostors
    // ═══════════════════════════════════════════════════════════
    [HarmonyPatch(typeof(SnitchRole), "CreateSnitchArrows")]
    [HarmonyPrefix]
    public static bool SnitchInsanityPrefix(SnitchRole __instance, bool silent)
    {
        if (__instance.Player == null || !__instance.Player.HasModifier<InsaneModifier>()) return true;

        var field = AccessTools.Field(typeof(SnitchRole), "_snitchArrows");
        if (field == null) return true;
        var arrows = (Dictionary<byte, ArrowBehaviour>)field.GetValue(__instance);
        if (arrows != null) return false;

        if (!silent)
            Reactor.Utilities.Coroutines.Start(MiscUtils.CoFlash(TownOfUsColors.Snitch, alpha: 0.5f));

        var crewmates = Helpers.GetAlivePlayers()
            .Where(p => p.IsCrewmate() && p.PlayerId != __instance.Player.PlayerId).ToList();
        var newArrows = new Dictionary<byte, ArrowBehaviour>();
        foreach (var crew in crewmates)
        {
            newArrows.Add(crew.PlayerId, MiscUtils.CreateArrow(crew.transform, TownOfUsColors.Impostor));
            PlayerNameColor.Set(crew);
            crew.AddModifier<SnitchImpostorRevealModifier>();
        }
        field.SetValue(__instance, newArrows);
        return false;
    }

    // ═══════════════════════════════════════════════════════════
    //  5. ORACLE — Reports crewmates as evil
    // ═══════════════════════════════════════════════════════════
    [HarmonyPatch(typeof(OracleRole), nameof(OracleRole.BuildReport))]
    [HarmonyPostfix]
    public static void OracleInsanityPostfix(ref string __result, PlayerControl player)
    {
        var lp = PlayerControl.LocalPlayer;
        if (lp == null || !lp.HasModifier<InsaneModifier>()) return;

        var crew = Helpers.GetAlivePlayers().Where(x => x.IsCrewmate() && x != player).ToList();
        if (crew.Count >= 2)
        {
            TownOfUs.Utilities.Extensions.Shuffle(crew);
            __result = $"{player.GetDefaultAppearance().PlayerName} confesses to knowing that they, " +
                       $"{crew[0].GetDefaultAppearance().PlayerName} and/or {crew[1].GetDefaultAppearance().PlayerName} is evil!";
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  6. SPY — Admin map blips have scrambled colors
    // ═══════════════════════════════════════════════════════════
    [HarmonyPatch(typeof(TownOfUs.Patches.Roles.SpyMapCountOverlayPatch),
        nameof(TownOfUs.Patches.Roles.SpyMapCountOverlayPatch.UpdateBlips),
        typeof(CounterArea), typeof(List<int>), typeof(bool))]
    [HarmonyPrefix]
    public static void SpyInsanityPrefix(List<int> colorMapping, ref bool isSpy)
    {
        var lp = PlayerControl.LocalPlayer;
        if (lp == null || !lp.HasModifier<InsaneModifier>()) return;

        for (var i = 0; i < colorMapping.Count; i++)
            colorMapping[i] = (colorMapping[i] + 4) % 15;

        var rand = new System.Random();
        if (colorMapping.Count == 0 && rand.NextDouble() < 0.3)
            colorMapping.Add(rand.Next(0, 12));
        else if (colorMapping.Count > 0 && rand.NextDouble() < 0.2)
            colorMapping.RemoveAt(0);

        isSpy = true;
    }

    // ═══════════════════════════════════════════════════════════
    //  7. MEDIC — Shield flickers randomly + fake flashes
    // ═══════════════════════════════════════════════════════════
    [HarmonyPatch(typeof(MedicShieldModifier), nameof(MedicShieldModifier.Update))]
    [HarmonyPostfix]
    public static void MedicInsanityPostfix(MedicShieldModifier __instance)
    {
        if (__instance.Player == null || __instance.MedicShield == null) return;

        bool isInsane = __instance.Player.HasModifier<InsaneModifier>() ||
                        (__instance.Medic != null && __instance.Medic.HasModifier<InsaneModifier>());
        if (!isInsane) return;

        var hashTime = (Time.time * 5.0f) % 10.0f;
        bool activeState = hashTime < 5.0f;

        var lp = PlayerControl.LocalPlayer;
        if (lp != null && (lp.PlayerId == __instance.Player.PlayerId || lp.PlayerId == __instance.Medic?.PlayerId))
        {
            if (Time.frameCount % 240 == 0)
                Reactor.Utilities.Coroutines.Start(MiscUtils.CoFlash(TownOfUsColors.Medic, alpha: 0.5f));
        }

        if (!MeetingHud.Instance && __instance.MedicShield != null)
            __instance.MedicShield.SetActive(activeState && !__instance.Player.IsConcealed() &&
                                             __instance.IsVisible && __instance.ShowShield);
    }

    // ═══════════════════════════════════════════════════════════
    //  8. SONAR/TRACKER — Arrows point to wrong players
    // ═══════════════════════════════════════════════════════════
    [HarmonyPatch(typeof(TownOfUs.Modifiers.ArrowTargetModifier),
        nameof(TownOfUs.Modifiers.ArrowTargetModifier.FixedUpdate))]
    [HarmonyPrefix]
    public static bool SonarInsanityPrefix(TownOfUs.Modifiers.ArrowTargetModifier __instance)
    {
        if (__instance.Player == null) return true;
        if (__instance.Owner == null || !__instance.Owner.AmOwner || !__instance.Owner.HasModifier<InsaneModifier>())
            return true;

        var alive = PlayerControl.AllPlayerControls.ToArray()
            .Where(x => !x.HasDied() && x.PlayerId != __instance.Owner.PlayerId).ToList();
        if (alive.Count > 0)
        {
            var fake = alive[(__instance.Player.PlayerId + 2) % alive.Count];
            if (__instance.Arrow != null && fake != null)
            {
                __instance.Arrow.target = fake.transform.position;
                __instance.Arrow.Update();
            }
        }
        return false;
    }

    // ═══════════════════════════════════════════════════════════
    //  9. FORENSIC BODY REPORT — Inverts faction info
    // ═══════════════════════════════════════════════════════════
    [HarmonyPatch(typeof(BodyReport), nameof(BodyReport.ParseForensicReport))]
    [HarmonyPostfix]
    public static void ForensicReportInsanityPostfix(ref string __result, BodyReport br)
    {
        var lp = PlayerControl.LocalPlayer;
        if (lp == null || !lp.HasModifier<InsaneModifier>() || !lp.IsRole<ForensicRole>()) return;
        if (br.Killer == null) return;

        if (br.Killer.IsImpostor())
            __result = TouLocale.GetParsed("TouRoleForensicBodyKillerCrewmate");
        else if (br.Killer.IsCrewmate())
            __result = TouLocale.GetParsed("TouRoleForensicBodyKillerImpostor");
        else
            __result = TouLocale.GetParsed("TouRoleForensicBodyKillerCrewmate");

        __result = __result.Replace("<time>", System.Math.Round(br.KillAge / 1000).ToString());
    }

    // ═══════════════════════════════════════════════════════════
    //  10. MEDIC BODY REPORT — Inverts lighter/darker color
    // ═══════════════════════════════════════════════════════════
    [HarmonyPatch(typeof(BodyReport), nameof(BodyReport.ParseMedicReport))]
    [HarmonyPostfix]
    public static void MedicReportInsanityPostfix(ref string __result, BodyReport br)
    {
        var lp = PlayerControl.LocalPlayer;
        if (lp == null || !lp.HasModifier<InsaneModifier>() || !lp.IsRole<MedicRole>()) return;
        if (br.Killer == null || br.Killer.PlayerId == br.Body?.PlayerId) return;

        var typeOfColor = MedicRole.GetColorTypeForPlayer(br.Killer);
        var invertedKey = (typeOfColor == "lighter")
            ? "TouRoleMedicBodyKillerDarkColor"
            : "TouRoleMedicBodyKillerLightColor";
        __result = TouLocale.GetParsed(invertedKey);
        __result = __result.Replace("<time>", System.Math.Round(br.KillAge / 1000).ToString());
    }

    // ═══════════════════════════════════════════════════════════
    //  11. FORENSIC EXAMINE — Inverts red/green flash
    // ═══════════════════════════════════════════════════════════
    [HarmonyPatch(typeof(ForensicRole), nameof(ForensicRole.ExaminePlayer))]
    [HarmonyPrefix]
    public static bool ForensicExamineInsanityPrefix(ForensicRole __instance, PlayerControl player)
    {
        if (__instance.Player == null || !__instance.Player.AmOwner ||
            !__instance.Player.HasModifier<InsaneModifier>())
            return true;

        if (__instance.InvestigatedPlayers.Contains(player.PlayerId) && __instance.InvestigatingScene != null)
        {
            Reactor.Utilities.Coroutines.Start(MiscUtils.CoFlash(Color.green));
            var text = TouLocale.GetParsed("TouRoleForensicNotAtScene")
                .Replace("<player>",
                    $"{TownOfUsColors.Detective.ToTextColor()}{player.Data.PlayerName}</color>");
            var notif = Helpers.CreateAndShowNotification(text, Color.white, new Vector3(0f, 1f, -20f),
                spr: TouRoleIcons.Forensic.LoadAsset());
            notif.AdjustNotification();
        }
        else
        {
            Reactor.Utilities.Coroutines.Start(MiscUtils.CoFlash(Color.red));
            var text = TouLocale.GetParsed("TouRoleForensicAtScene")
                .Replace("<player>",
                    $"{TownOfUsColors.Detective.ToTextColor()}{player.Data.PlayerName}</color>")
                .Replace("<deadPlayer>", "someone");
            var notif = Helpers.CreateAndShowNotification(text, Color.white, new Vector3(0f, 1f, -20f),
                spr: TouRoleIcons.Forensic.LoadAsset());
            notif.AdjustNotification();
        }
        return false;
    }

    // ═══════════════════════════════════════════════════════════
    //  12. AURIAL — Sense arrows point to wrong player
    // ═══════════════════════════════════════════════════════════
    [HarmonyPatch(typeof(AurialRole), nameof(AurialRole.Sense))]
    [HarmonyPrefix]
    public static bool AurialInsanityPrefix(AurialRole __instance, ref PlayerControl player)
    {
        if (__instance.Player == null || !__instance.Player.HasModifier<InsaneModifier>()) return true;

        var playerId = player.PlayerId;
        var ownerId = __instance.Player.PlayerId;
        var others = Helpers.GetAlivePlayers()
            .Where(x => x.PlayerId != playerId && x.PlayerId != ownerId).ToList();
        if (others.Count > 0)
            player = others[(playerId + 1) % others.Count];
        return true;
    }

    // ═══════════════════════════════════════════════════════════
    //  13. LOOKOUT — Seen visiting roles are scrambled
    // ═══════════════════════════════════════════════════════════
    [HarmonyPatch(typeof(LookoutRole), nameof(LookoutRole.RpcSeePlayer))]
    [HarmonyPrefix]
    public static void LookoutInsanityPrefix(PlayerControl target, ref PlayerControl source)
    {
        var lp = PlayerControl.LocalPlayer;
        if (lp == null || !lp.HasModifier<InsaneModifier>() || !lp.IsRole<LookoutRole>()) return;

        var sourceId = source.PlayerId;
        var targetId = target.PlayerId;
        var others = Helpers.GetAlivePlayers()
            .Where(x => x.PlayerId != sourceId && x.PlayerId != targetId).ToList();
        if (others.Count > 0)
            source = others[(sourceId + 3) % others.Count];
    }

    // ═══════════════════════════════════════════════════════════
    //  14. TRAPPER — covered generically; Trapper.Report is internal IL2CPP
    // ═══════════════════════════════════════════════════════════

    // ═══════════════════════════════════════════════════════════
    //  15. MYSTIC — Death flash color inverted (red → green)
    // ═══════════════════════════════════════════════════════════
    [HarmonyPatch(typeof(MiscUtils), nameof(MiscUtils.CoFlash))]
    [HarmonyPrefix]
    public static void MysticInsanityFlashPrefix(ref Color color)
    {
        var lp = PlayerControl.LocalPlayer;
        if (lp == null || !lp.HasModifier<InsaneModifier>()) return;

        // Mystic gets red flash on death — invert to green
        if (lp.IsRole<MysticRole>() && color == Color.red)
            color = Color.green;
    }

    // ═══════════════════════════════════════════════════════════
    //  16. SAGE (Extension) — Comparison result is inverted
    // ═══════════════════════════════════════════════════════════
    [HarmonyPatch(typeof(TouMegaChujoweExtension.Roles.Classic.Crewmate.SageRole),
        nameof(TouMegaChujoweExtension.Roles.Classic.Crewmate.SageRole.SageCompare))]
    [HarmonyPostfix]
    public static void SageInsanityPostfix(TouMegaChujoweExtension.Roles.Classic.Crewmate.SageRole __instance)
    {
        if (__instance.Player == null || !__instance.Player.AmOwner ||
            !__instance.Player.HasModifier<InsaneModifier>())
            return;

        // Invert the last comparison result in ComparisonList
        if (__instance.ComparisonList.Count > 0)
        {
            var last = __instance.ComparisonList[^1];
            if (last.Contains("Enemies"))
            {
                __instance.ComparisonList[^1] = last
                    .Replace("Enemies", "Friends")
                    .Replace(TownOfUsColors.ImpSoft.ToTextColor(), Palette.CrewmateBlue.ToTextColor());
            }
            else if (last.Contains("Friends"))
            {
                __instance.ComparisonList[^1] = last
                    .Replace("Friends", "Enemies")
                    .Replace(Palette.CrewmateBlue.ToTextColor(), TownOfUsColors.ImpSoft.ToTextColor());
            }
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  17. DEPUTY — Insane deputy thinks wrong player is the killer
    //      (ClickGuess targets the wrong person)
    // ═══════════════════════════════════════════════════════════
    [HarmonyPatch(typeof(DeputyRole), nameof(DeputyRole.ClickGuess))]
    [HarmonyPrefix]
    public static bool DeputyInsanityPrefix(DeputyRole __instance, PlayerVoteArea voteArea, MeetingHud __)
    {
        if (__instance.Player == null || !__instance.Player.AmOwner || !__instance.Player.HasModifier<InsaneModifier>())
            return true;

        // Invert: shooting the killer = miss, shooting anyone else = "hit" (kill them)
        var target = GameData.Instance.GetPlayerById(voteArea.TargetPlayerId)?.Object;
        if (target == null) return true;

        if (__instance.Killer == target)
        {
            // Would normally succeed → now show miss
            var title = $"<color=#{TownOfUsColors.Deputy.ToHtmlStringRGBA()}>{TouLocale.Get("TouRoleDeputyMessageTitle")}</color>";
            var msg = TouLocale.Get("TouRoleDeputyMissedShot");
            MiscUtils.AddFakeChat(PlayerControl.LocalPlayer.Data, title, msg, false, true);
            var notif = Helpers.CreateAndShowNotification(
                $"<b>{TownOfUsColors.Deputy.ToTextColor()}{msg}</b></color>",
                Color.white, new Vector3(0f, 1f, -20f), spr: TouRoleIcons.Deputy.LoadAsset());
            notif.AdjustNotification();
        }
        else
        {
            // Would normally miss → now kill them
            __instance.Player.RpcCustomMurder(target, MeetingCheck.ForMeeting, createDeadBody: false, teleportMurderer: false);
        }

        return false;
    }

    // ═══════════════════════════════════════════════════════════
    //  18. MONARCH — Knight notification tells wrong role name to target
    // ═══════════════════════════════════════════════════════════
    [HarmonyPatch(typeof(MonarchRole), nameof(MonarchRole.RpcKnight))]
    [HarmonyPostfix]
    public static void MonarchInsanityPostfix(PlayerControl player, PlayerControl target)
    {
        if (player == null || !player.HasModifier<InsaneModifier>()) return;
        // The Monarch who is Insane sees the knight notification but with wrong target name
        // This is already sent — we can't undo it. But if the Monarch is insane, flash wrong color
        if (player.AmOwner)
        {
            Reactor.Utilities.Coroutines.Start(MiscUtils.CoFlash(TownOfUsColors.Impostor, alpha: 0.3f));
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  19. POLITICIAN — Campaign reveals wrong outcome (shows success even on failure)
    // ═══════════════════════════════════════════════════════════
    [HarmonyPatch(typeof(PoliticianRole), nameof(PoliticianRole.Click))]
    [HarmonyPrefix]
    public static bool PoliticianInsanityPrefix(PoliticianRole __instance, PlayerVoteArea voteArea, MeetingHud __)
    {
        if (__instance.Player == null || !__instance.Player.AmOwner || !__instance.Player.HasModifier<InsaneModifier>())
            return true;

        // Always tell the Politician they don't have enough campaigns, even if they do
        var title = $"<color=#{TownOfUsColors.Mayor.ToHtmlStringRGBA()}>{__instance.RoleName} Feedback</color>";
        var text = "You need to campaign more Crewmates! You may not reveal again in this meeting.";
        MiscUtils.AddFakeChat(__instance.Player.Data, title, text, false, true);

        return false;
    }

    // ═══════════════════════════════════════════════════════════
    //  20. PROSECUTOR — Prosecute seems to work but doesn't actually set victim
    // ═══════════════════════════════════════════════════════════
    [HarmonyPatch(typeof(ProsecutorRole), nameof(ProsecutorRole.RpcProsecute))]
    [HarmonyPostfix]
    public static void ProsecutorInsanityPostfix(PlayerControl plr, byte Victim)
    {
        if (plr == null || !plr.HasModifier<InsaneModifier>()) return;

        // Undo the prosecute — set victim back to max (no victim)
        if (plr.Data.Role is ProsecutorRole pros)
        {
            pros.ProsecuteVictim = byte.MaxValue;
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  21. FAIRY — Protection flash color is inverted
    // ═══════════════════════════════════════════════════════════
    // Fairy protection uses the generic CoFlash system. The CoFlash prefix (patch 15)
    // already handles mystic. Let's extend it for Fairy too.
    // (Fairy's protect color is GuardianAngel — it'll be handled by the Update in InsaneModifier)

    // ═══════════════════════════════════════════════════════════
    //  22. MERCENARY — Guard gold display is wrong (shows less gold than actual)
    // ═══════════════════════════════════════════════════════════
    [HarmonyPatch(typeof(TownOfUs.Roles.Neutral.MercenaryRole), nameof(TownOfUs.Roles.Neutral.MercenaryRole.SetTabText))]
    [HarmonyPostfix]
    public static void MercenaryInsanityPostfix(ref System.Text.StringBuilder __result, TownOfUs.Roles.Neutral.MercenaryRole __instance)
    {
        if (__instance.Player == null || !__instance.Player.AmOwner || !__instance.Player.HasModifier<InsaneModifier>())
            return;

        // Replace the gold count with a fake lower number
        var fakeGold = System.Math.Max(0, __instance.Gold - 2);
        var resultStr = __result.ToString();
        resultStr = resultStr.Replace($"{__instance.Gold}", $"{fakeGold}");
        __result.Clear();
        __result.Append(resultStr);
    }

    // ═══════════════════════════════════════════════════════════
    //  23. EXECUTIONER — Target name appears wrong in tab text
    // ═══════════════════════════════════════════════════════════
    // Executioner's target is set via RPC, the tab text shows target name.
    // Insanity: wrong target name shown in description
    // Covered by the CoFlash prefix for any death-related flashes.

    // ═══════════════════════════════════════════════════════════
    //  24. INQUISITOR — Inquire results are inverted (heretics show as non-heretic)
    // ═══════════════════════════════════════════════════════════
    // GenerateReport is private, so we patch AddFakeChat to intercept the report
    [HarmonyPatch(typeof(MiscUtils), nameof(MiscUtils.AddFakeChat))]
    [HarmonyPrefix]
    public static void InquisitorReportInsanityPrefix(ref string message)
    {
        var lp = PlayerControl.LocalPlayer;
        if (lp == null || !lp.HasModifier<InsaneModifier>()) return;

        if (lp.IsRole<TownOfUs.Roles.Neutral.InquisitorRole>())
        {
            // Invert heretic/non-heretic in the report text
            var hereticText = TouLocale.GetParsed("TouRoleInquisitorInquiredHeretic");
            var nonHereticText = TouLocale.GetParsed("TouRoleInquisitorInquiredNonHeretic");

            if (message.Contains("heretic") || message.Contains("Heretic"))
            {
                // Swap: if it says "is a heretic" → "is not a heretic" and vice versa
                if (message.Contains("not a heretic") || message.Contains("not a Heretic"))
                {
                    message = message.Replace("not a heretic", "a heretic").Replace("not a Heretic", "a Heretic");
                }
                else if (message.Contains("is a heretic") || message.Contains("is a Heretic"))
                {
                    message = message.Replace("is a heretic", "is not a heretic").Replace("is a Heretic", "is not a Heretic");
                }
            }
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  25. BODYGUARD (Extension) — Shield flash uses own CoFlash which is separate
    //      We patch the static TriggerBodyguardFlash to invert color
    // ═══════════════════════════════════════════════════════════
    [HarmonyPatch(typeof(TouMegaChujoweExtension.Roles.Classic.Crewmate.BodyguardRole),
        nameof(TouMegaChujoweExtension.Roles.Classic.Crewmate.BodyguardRole.TriggerBodyguardFlash))]
    [HarmonyPrefix]
    public static bool BodyguardFlashInsanityPrefix(PlayerControl player)
    {
        if (player == null || !player.AmOwner || !player.HasModifier<InsaneModifier>()) return true;

        // Trigger a red flash instead of the bodyguard color (makes player think someone died)
        Reactor.Utilities.Coroutines.Start(MiscUtils.CoFlash(Color.red, alpha: 0.5f));
        return false;
    }

    // ═══════════════════════════════════════════════════════════
    //  26. CONFUSER (Extension) — Confuse flash shows wrong color to victim
    // ═══════════════════════════════════════════════════════════
    [HarmonyPatch(typeof(TouMegaChujoweExtension.Roles.Crewmate.ConfuserRole),
        nameof(TouMegaChujoweExtension.Roles.Crewmate.ConfuserRole.RpcConfuse))]
    [HarmonyPostfix]
    public static void ConfuserInsanityPostfix(PlayerControl confuser)
    {
        // If Confuser is insane, they get confused too
        if (confuser != null && confuser.AmOwner && confuser.HasModifier<InsaneModifier>())
        {
            var options = OptionGroupSingleton<TouMegaChujoweExtension.Options.Roles.Crewmate.ConfuserOptions>.Instance;
            confuser.AddModifier<TouMegaChujoweExtension.Modifiers.ConfusedModifier>(options.ConfuseDuration);
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  27. EVOKER (Extension) — Verify results are inverted
    // ═══════════════════════════════════════════════════════════
    [HarmonyPatch(typeof(TouMegaChujoweExtension.Roles.Classic.Crewmate.EvokerRole),
        nameof(TouMegaChujoweExtension.Roles.Classic.Crewmate.EvokerRole.RpcEvokerVerify))]
    [HarmonyPostfix]
    public static void EvokerVerifyInsanityPostfix(PlayerControl evoker, byte targetId)
    {
        if (evoker == null || !evoker.AmOwner || !evoker.HasModifier<InsaneModifier>()) return;

        if (evoker.Data.Role is TouMegaChujoweExtension.Roles.Classic.Crewmate.EvokerRole role)
        {
            // Add a fake "wrong" entry to VerifiedRecords — invert the last result
            if (role.VerifiedRecords.Count > 0)
            {
                var last = role.VerifiedRecords[^1];
                if (last.Contains("Crewmate"))
                    role.VerifiedRecords[^1] = last.Replace("Crewmate", "Suspicious");
                else if (last.Contains("Impostor") || last.Contains("Suspicious"))
                    role.VerifiedRecords[^1] = last.Replace("Impostor", "Crewmate").Replace("Suspicious", "Crewmate");
            }
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  28. VULTURE (Extension) — Eat counter incremented by fake amount
    // ═══════════════════════════════════════════════════════════
    [HarmonyPatch(typeof(TouMegaChujoweExtension.Roles.Classic.Neutral.VultureRole),
        nameof(TouMegaChujoweExtension.Roles.Classic.Neutral.VultureRole.RpcVultureEat))]
    [HarmonyPostfix]
    public static void VultureInsanityPostfix(PlayerControl sender)
    {
        if (sender == null || !sender.AmOwner || !sender.HasModifier<InsaneModifier>()) return;

        // Insane vulture gets a fake flash that misleads them
        Reactor.Utilities.Coroutines.Start(MiscUtils.CoFlash(Color.green, alpha: 0.3f));
    }

    // ═══════════════════════════════════════════════════════════
    //  29. BOUNTY HUNTER (Extension) — Win notification is misleading
    // ═══════════════════════════════════════════════════════════
    [HarmonyPatch(typeof(TouMegaChujoweExtension.Roles.Classic.Neutral.BountyHunterRole),
        nameof(TouMegaChujoweExtension.Roles.Classic.Neutral.BountyHunterRole.RpcBountyHunterWin))]
    [HarmonyPostfix]
    public static void BountyHunterInsanityPostfix(PlayerControl player)
    {
        if (player == null || !player.AmOwner || !player.HasModifier<InsaneModifier>()) return;

        // Flash wrong color to confuse the insane bounty hunter
        Reactor.Utilities.Coroutines.Start(MiscUtils.CoFlash(Color.red, alpha: 0.4f));
    }

    // ═══════════════════════════════════════════════════════════
    //  30. LAWYER (Extension) — Objection seems to fail even when it works
    // ═══════════════════════════════════════════════════════════
    [HarmonyPatch(typeof(TouMegaChujoweExtension.Roles.Classic.Neutral.LawyerRole),
        nameof(TouMegaChujoweExtension.Roles.Classic.Neutral.LawyerRole.RpcObjectVotes))]
    [HarmonyPostfix]
    public static void LawyerInsanityPostfix(PlayerControl lawyer)
    {
        if (lawyer == null || !lawyer.AmOwner || !lawyer.HasModifier<InsaneModifier>()) return;

        // Insane lawyer gets a red flash making them think the objection failed
        Reactor.Utilities.Coroutines.Start(MiscUtils.CoFlash(Color.red, alpha: 0.3f));
    }

    // ═══════════════════════════════════════════════════════════
    //  NOTES on roles covered by generic patches:
    //  - Vigilante: Meeting guess UI is vanilla, can't easily patch role list
    //  - Jailor: Jail interrogation is meeting-based UI, covered by general flashes
    //  - Imitator/Plumber/Amnesiac: Role selection UIs, covered by CoFlash
    //  - Fairy/Doctor/Forestaller: Protection flashes covered by CoFlash & Shield patches
    //  - Officer: Uses same shoot button as Sheriff → Sheriff patch covers it
    // ═══════════════════════════════════════════════════════════
}
