using HarmonyLib;
using MiraAPI.Hud;
using System;
using System.Reflection;
using System.Linq;
using TownOfUs.Buttons;
using TownOfUs.Roles;

namespace TouMegaChujoweExtension.Patches.Buttons;

public static class CustomButtonEnabledPatch
{
    public static void Apply(Harmony harmony)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var buttonTypes = assembly.GetTypes()
            .Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(CustomActionButton)))
            .ToList();

        var prefixMethod = typeof(CustomButtonEnabledPatch).GetMethod(nameof(EnabledPrefix), BindingFlags.NonPublic | BindingFlags.Static);

        foreach (var type in buttonTypes)
        {
            // Only patch buttons declared in our extension assembly
            if (type.Assembly != assembly) continue;

            // We specifically want to intercept the virtual Enabled call
            var enabledMethod = type.GetMethod("Enabled", BindingFlags.Public | BindingFlags.Instance);
            if (enabledMethod != null)
            {
                try
                {
                    harmony.Patch(enabledMethod, prefix: new HarmonyMethod(prefixMethod));
                }
                catch (Exception)
                {
                    // Ignore patching issues for specific types if they arise
                }
            }
        }
    }

    private static bool EnabledPrefix(CustomActionButton __instance, ref bool __result, RoleBehaviour? role)
    {
        if (__instance.GetType().Assembly != typeof(TouMegaChujoweExtensionPlugin).Assembly)
        {
            return true; // Let other buttons use their normal logic
        }

        // If the button type overrides Enabled itself (like BodySwapperDecoyButton), we can let it run its own logic.
        // We check if the concrete class overrides Enabled.
        var buttonType = __instance.GetType();
        var enabledMethod = buttonType.GetMethod("Enabled", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        if (enabledMethod != null)
        {
            return true; // Let the overridden method run
        }

        // Find the TownOfUsRoleButton or TownOfUsKillRoleButton base class to get the TRole type
        Type? roleType = null;
        var baseType = buttonType.BaseType;
        while (baseType != null && baseType != typeof(object))
        {
            if (baseType.IsGenericType)
            {
                var genType = baseType.GetGenericTypeDefinition();
                if (genType.Name.StartsWith("TownOfUsRoleButton") || genType.Name.StartsWith("TownOfUsKillRoleButton"))
                {
                    roleType = baseType.GetGenericArguments()[0];
                    break;
                }
            }
            baseType = baseType.BaseType;
        }

        if (roleType != null)
        {
            if (role == null)
            {
                __result = false;
                return false;
            }

            var button = __instance as TownOfUsButton;
            bool isDisabled = button != null && button.Disabled;

            // Perform direct, concrete type check to bypass IL2CPP shared generic type check issues
            __result = !isDisabled && roleType.IsAssignableFrom(role.GetType());
            return false; // Skip the original generic method call
        }

        return true; // Fallback to original
    }
}
