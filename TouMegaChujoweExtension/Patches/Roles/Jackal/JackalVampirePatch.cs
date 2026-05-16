using HarmonyLib;
using MiraAPI.GameOptions;
using MiraAPI.Roles;
using TownOfUs.Roles.Neutral;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Options;

namespace TouMegaChujoweExtension.Patches.Roles.Jackal;

[HarmonyPatch(typeof(VampireRole), nameof(VampireRole.Configuration), MethodType.Getter)]
public static class JackalVampirePatch
{
    public static void Postfix(ref CustomRoleConfiguration __result)
    {
        var generalOptions = OptionGroupSingleton<ExtensionGeneralOptions>.Instance;

        if (generalOptions != null && generalOptions.PreventVampiresWithJackal)
        {
            __result.MaxRoleCount = 0;
        }
    }
}
