using HarmonyLib;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Modifiers.Neutral;
using TownOfUs.Modifiers;
using TownOfUs.Options;
using TownOfUs.Utilities;

namespace TouMegaChujoweExtension.Patches.DeathNote;

[HarmonyPatch]
public static class DeathNoteCursedMarkPatch
{
    private static byte _lastDeathNoteVictim = byte.MaxValue;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerRoleTextExtensions), nameof(PlayerRoleTextExtensions.UpdateStatusSymbols), typeof(string), typeof(PlayerControl), typeof(bool))]
    public static void AddDeathNoteSymbol(ref string __result, PlayerControl player, bool hidden)
    {
        if (player == null) return;
        var local = PlayerControl.LocalPlayer;
        if (local == null || local.Data == null) return;

        var genOpt = OptionGroupSingleton<GeneralOptions>.Instance;
        bool isGhost = local.Data.IsDead;
        bool deadKnow = isGhost && genOpt.TheDeadKnow && !hidden;

        // Check if we are the Death Note
        bool isDeathNote = local.TryGetModifier<DeathNoteModifier>(out var localDnMod);
        if (isDeathNote && localDnMod.CursedTarget != null && player.PlayerId == localDnMod.CursedTarget.PlayerId)
        {
            if (!__result.Contains("✶"))
            {
                __result += "<color=#8B00FF> ✶</color>";
            }
            return;
        }

        // Check if we are a ghost and "The Dead Know" is on
        if (deadKnow)
        {
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (pc.TryGetModifier<DeathNoteModifier>(out var dnMod))
                {
                    if (dnMod.CursedTarget != null && player.PlayerId == dnMod.CursedTarget.PlayerId)
                    {
                        if (!__result.Contains("✶"))
                        {
                            __result += "<color=#8B00FF> ✶</color>";
                        }
                        return;
                    }
                }
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.OnDestroy))]
    public static void ResetOnGameStart()
    {
        _lastDeathNoteVictim = byte.MaxValue;
    }
}
