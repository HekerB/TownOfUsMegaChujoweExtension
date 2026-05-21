using HarmonyLib;
using TownOfUs.Modifiers.Crewmate;
using TownOfUs.Extensions;
using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Roles.Classic.Impostor;
using System.Collections.Generic;

namespace TouMegaChujoweExtension.Patches.Roles.Crewmate;

[HarmonyPatch(typeof(ClericCleanseModifier), "CleansePlayer")]
public static class ClericCleansePatch
{
    [HarmonyPostfix]
    public static void Postfix(ClericCleanseModifier __instance)
    {
        if (__instance.Player != null)
        {
            bool isVine = PoisonSystem.IsTargetVined(__instance.Player.PlayerId);
            bool isPoison = PoisonSystem.IsTargetPoisoned(__instance.Player.PlayerId);
            bool isWitchSpell = __instance.Player.HasModifier<WitchSpellboundModifier>();

            var effects = new List<string>();
            if (isVine) effects.Add("Vine");
            if (isPoison) effects.Add("Poison");
            if (isWitchSpell) effects.Add("Witch Spell");

            if (effects.Count > 0)
            {
                if (__instance.Cleric.AmOwner)
                {
                    ClericCleanseOnMeetingStartPatch.CleansedPoisonPlayers[__instance.Player.PlayerId] = string.Join(", ", effects);
                }

                if (__instance.Player.AmOwner)
                {
                    if (isVine || isPoison)
                    {
                        PoisonerRole.RpcPoisonerCleanse(__instance.Player, __instance.Player.PlayerId);
                    }
                    if (isWitchSpell)
                    {
                        WitchRole.RpcWitchClearSpellboundPlayer(__instance.Player, __instance.Player.PlayerId);
                    }
                }
            }
        }
    }
}

[HarmonyPatch(typeof(ClericCleanseModifier), nameof(ClericCleanseModifier.OnMeetingStart))]
public static class ClericCleanseOnMeetingStartPatch
{
    public static readonly Dictionary<byte, string> CleansedPoisonPlayers = new();

    [HarmonyPrefix]
    public static bool Prefix(ClericCleanseModifier __instance)
    {
        if (__instance.Cleric.AmOwner && CleansedPoisonPlayers.TryGetValue(__instance.Player.PlayerId, out var effectType))
        {
            var text = new System.Text.StringBuilder($"Cleansed effects on {__instance.Player.Data.PlayerName}:");

            foreach (var effect in __instance.Effects)
            {
                text.Append(TownOfUs.TownOfUsPlugin.Culture, $" {effect.ToString()},");
            }

            text.Append($" {effectType},");
            text = text.Remove(text.Length - 1, 1);

            var title = $"<color=#{UnityEngine.ColorUtility.ToHtmlStringRGBA(TownOfUs.TownOfUsColors.Cleric)}>Cleric Feedback</color>";
            TownOfUs.Utilities.MiscUtils.AddFakeChat(PlayerControl.LocalPlayer.Data, title, text.ToString(), false, true);
            return false; // Skip original method
        }
        return true;
    }
}
