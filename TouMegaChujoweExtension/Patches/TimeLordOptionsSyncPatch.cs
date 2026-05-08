using HarmonyLib;
using TownOfUs.Options.Roles.Crewmate;
using TownOfUs.Utilities;
using MiraAPI.GameOptions;
using MiraAPI.GameOptions.OptionTypes;
using TownOfUs.Modules;
using TownOfUs.Roles.Crewmate;

namespace TouMegaChujoweExtension.Patches;

public static class TimeLordOptionsSyncHelper
{
    public static void InjectOptions()
    {
        try
        {
            var options = MiscUtils.GetModdedOptionsForRole(typeof(TimeLordOptions));
            if (options == null) return;

            var instance = OptionGroupSingleton<TimeLordOptions>.Instance;
            if (instance == null) return;

            foreach (var opt in options)
            {
                var key = opt.StringName.ToString();
                if (opt is ModdedNumberOption numOpt)
                {
                    if (key == "TouOptionTimeLordRewindCooldown")
                    {
                        var field = AccessTools.Field(typeof(TimeLordOptions), "<RewindCooldown>k__BackingField");
                        field?.SetValue(instance, numOpt.Value);
                    }
                    else if (key == "TouOptionTimeLordRewindHistory")
                    {
                        var field = AccessTools.Field(typeof(TimeLordOptions), "<RewindHistorySeconds>k__BackingField");
                        field?.SetValue(instance, numOpt.Value);
                    }
                }
                else if (opt is ModdedToggleOption toggleOpt)
                {
                    if (key == "TouOptionTimeLordCanUseVitals")
                    {
                        var field = AccessTools.Field(typeof(TimeLordOptions), "<CanUseVitals>k__BackingField");
                        field?.SetValue(instance, toggleOpt.Value);
                    }
                    else if (key == "TouOptionTimeLordReviveOnRewind")
                    {
                        var field = AccessTools.Field(typeof(TimeLordOptions), "<ReviveOnRewind>k__BackingField");
                        field?.SetValue(instance, toggleOpt.Value);
                    }
                    else if (key == "TouOptionTimeLordUndoTasksOnRewind")
                    {
                        var field = AccessTools.Field(typeof(TimeLordOptions), "<UndoTasksOnRewind>k__BackingField");
                        field?.SetValue(instance, toggleOpt.Value);
                    }
                    else if (key == "TouOptionTimeLordUncleanBodiesOnRewind")
                    {
                        var field = AccessTools.Field(typeof(TimeLordOptions), "<UncleanBodiesOnRewind>k__BackingField");
                        field?.SetValue(instance, toggleOpt.Value);
                    }
                    else if (key == "TouOptionTimeLordNotifyOnRevive")
                    {
                        var field = AccessTools.Field(typeof(TimeLordOptions), "<NotifyOnRevive>k__BackingField");
                        field?.SetValue(instance, toggleOpt.Value);
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogError($"[TimeLordOptionsSyncHelper] Error injecting options: {ex.Message}");
        }
    }
}

[HarmonyPatch(typeof(TimeLordRole), nameof(TimeLordRole.RpcStartRewind))]
public static class TimeLordOptionsRpcPatch
{
    [HarmonyPrefix]
    public static void Prefix()
    {
        TimeLordOptionsSyncHelper.InjectOptions();
    }
}

[HarmonyPatch(typeof(TimeLordRewindSystem), nameof(TimeLordRewindSystem.StartRewind))]
public static class TimeLordOptionsStartRewindPatch
{
    [HarmonyPrefix]
    public static void Prefix()
    {
        TimeLordOptionsSyncHelper.InjectOptions();
    }
}

[HarmonyPatch(typeof(TimeLordRewindSystem), nameof(TimeLordRewindSystem.RecordLocalSnapshot))]
public static class TimeLordOptionsRecordSnapshotPatch
{
    [HarmonyPrefix]
    public static void Prefix()
    {
        TimeLordOptionsSyncHelper.InjectOptions();
    }
}
