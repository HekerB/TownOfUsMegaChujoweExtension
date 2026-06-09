using HarmonyLib;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Roles.Neutral;
using TownOfUs.Buttons.Impostor;

namespace TouMegaChujoweExtension.Patches.Joker
{
    [HarmonyPatch]
    public static class JokerCloneWarlockWitchPatch
    {
        private static bool TryKillClone(PlayerControl killer, float distance)
        {
            if (killer == null || killer.Data == null || killer.Data.IsDead) return false;
            if (MeetingHud.Instance) return false;

            if (!JokerCloneSystem.TryGetClosestClone(killer.GetTruePosition(), distance, out var idx, out _))
                return false;

            if (idx < 0 || idx >= JokerCloneSystem.Clones.Count) return false;

			var clone = JokerCloneSystem.Clones[idx];
			if (clone == null || clone.IsPreview) return false;

			JokerRole.RpcJokerCloneKilled(
				killer,
				clone.JokerId,
				clone.WorldPosition.x,
				clone.WorldPosition.y
			);
            return true;
        }

        [HarmonyPatch(typeof(WarlockKillButton), nameof(WarlockKillButton.ClickHandler))]
        [HarmonyPrefix]
        public static bool WarlockPrefix(WarlockKillButton __instance)
        {
            if (__instance == null || !__instance.CanClick()) return true;

            var local = PlayerControl.LocalPlayer;
            var dist = JokerCloneInteractionPatches.GetKillDistanceStatic();

            if (TryKillClone(local, dist))
            {
                try { __instance.SetTimer(__instance.Cooldown); } catch { }
                return false;
            }

            return true;
        }

        [HarmonyPatch(typeof(TouMegaChujoweExtension.Buttons.Impostor.WitchKillButton),
            nameof(TouMegaChujoweExtension.Buttons.Impostor.WitchKillButton.ClickHandler))]
        [HarmonyPrefix]
        public static bool WitchPrefix(TouMegaChujoweExtension.Buttons.Impostor.WitchKillButton __instance)
        {
            if (__instance == null || !__instance.CanClick()) return true;

            var local = PlayerControl.LocalPlayer;
            var dist = JokerCloneInteractionPatches.GetKillDistanceStatic();

            if (TryKillClone(local, dist))
            {
                try { __instance.SetTimer(__instance.Cooldown); } catch { }
                return false;
            }

            return true;
        }
    }
}