using HarmonyLib;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Lawyer;

/// <summary>
/// Cleans up Objection icons when chat bubbles are reused by the pool.
/// </summary>
[HarmonyPatch(typeof(ChatBubble), nameof(ChatBubble.SetCosmetics))]
public static class LawyerChatBubbleCleanupPatch
{
    [HarmonyPrefix]
    public static void Prefix(ChatBubble __instance)
    {
        if (__instance == null)
        {
            return;
        }

        // Re-enable PoolablePlayer if it was hidden by objection
        if (__instance.Player != null && !__instance.Player.gameObject.activeSelf)
        {
            __instance.Player.gameObject.SetActive(true);
        }

        // Destroy any leftover objection icons
        if (__instance.Player != null && __instance.Player.transform.parent != null)
        {
            var parent = __instance.Player.transform.parent;
            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                if (child.name == "ObjectionChatIcon")
                {
                    UnityEngine.Object.Destroy(child.gameObject);
                }
            }
        }
    }
}
