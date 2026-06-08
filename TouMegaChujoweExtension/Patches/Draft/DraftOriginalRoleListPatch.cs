using HarmonyLib;
using TownOfUs.Patches;

namespace TouMegaChujoweExtension.Patches.Draft;

[HarmonyPatch(typeof(HudManagerPatches), nameof(HudManagerPatches.UpdateRoleList))]
public static class DraftOriginalRoleListPatch
{
    [HarmonyPriority(Priority.Last)]
    [HarmonyPostfix]
    public static void UpdateRoleListPostfix()
    {
        var draftEnabled = DraftSystem.IsEnabled && LobbyBehaviour.Instance;
        SetOriginalRoleListHoverEnabled(!draftEnabled);

        if (!draftEnabled)
        {
            return;
        }

        if (HudManagerPatches.RoleListTextComp)
        {
            HudManagerPatches.RoleListTextComp.text = string.Empty;
        }

        if (HudManagerPatches.RoleList)
        {
            HudManagerPatches.RoleList.SetActive(false);
        }
    }

    private static void SetOriginalRoleListHoverEnabled(bool enabled)
    {
        if (!HudManager.Instance)
        {
            return;
        }

        var hover = HudManager.Instance.GetComponent<RoleListHoverComponent>();
        if (hover)
        {
            hover.enabled = enabled;
        }
    }
}
