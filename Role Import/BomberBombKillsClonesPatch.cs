using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Reactor.Utilities;
using MiraAPI.GameOptions;
using Reactor.Utilities.Extensions;
using TownOfUs.Modules;
using TownOfUs.Options.Roles.Impostor;
using TownOfUs.Utilities;
using TownOfUs.Networking;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Roles.Neutral;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Joker;

[HarmonyPatch(typeof(Bomb), nameof(Bomb.Detonate))]
public static class BomberBombKillsClonesPatch
{
    private static readonly AccessTools.FieldRef<Bomb, PlayerControl?> BomberRef =
        AccessTools.FieldRefAccess<Bomb, PlayerControl?>("_bomber");

    private static readonly AccessTools.FieldRef<Bomb, GameObject?> ObjRef =
        AccessTools.FieldRefAccess<Bomb, GameObject?>("_obj");

    [HarmonyPrefix]
    public static bool Prefix(Bomb __instance)
    {
        Coroutines.Start(CoDetonateWithClones(__instance));
        return false;
    }

    private static IEnumerator CoDetonateWithClones(Bomb bomb)
    {
        yield return new WaitForSeconds(0.1f);

        var obj = ObjRef(bomb);
        var bomber = BomberRef(bomb);

        if (obj == null) yield break;

        if (MeetingHud.Instance || ExileController.Instance)
        {
            obj.Destroy();
            yield break;
        }

        var opts = OptionGroupSingleton<BomberOptions>.Instance;
        var radius = opts.DetonateRadius * ShipStatus.Instance.MaxLightRadius;
        var maxKills = (int)opts.MaxKillsInDetonation;
        var pos2d = (Vector2)obj.transform.position;

        var cloneVictims = new List<int>();
        for (int i = 0; i < JokerCloneSystem.Clones.Count; i++)
        {
            var c = JokerCloneSystem.Clones[i];
            if (c == null || c.IsPreview || c.Fake?.body == null) continue;
            if (Vector2.Distance(pos2d, (Vector2)c.Fake.body.transform.position) <= radius)
                cloneVictims.Add(i);
        }

        var hits = Physics2D.OverlapCircleAll(pos2d, radius, Constants.PlayersOnlyMask);
        var targetList = hits
            .Select(h => h.GetComponent<PlayerControl>())
            .Where(p => p != null && !p.HasDied())
            .Take(Mathf.Max(0, maxKills - cloneVictims.Count))
            .ToList();

        if (bomber != null && targetList.Count > 0)
            bomber.RpcSpecialMultiMurder(targetList, MeetingCheck.OutsideMeeting, true,
                teleportMurderer: false, causeOfDeath: "BomberBomb");

        if (bomber != null)
        {
            cloneVictims.Sort();
            for (int k = cloneVictims.Count - 1; k >= 0; k--)
{
    var idx = cloneVictims[k];
    if (idx < 0 || idx >= JokerCloneSystem.Clones.Count) continue;

    var clone = JokerCloneSystem.Clones[idx];
    if (clone == null || clone.IsPreview) continue;

    JokerRole.RpcJokerCloneKilled(
        bomber,
        clone.JokerId,
        clone.WorldPosition.x,
        clone.WorldPosition.y
    );
}
        }

        obj.Destroy();
    }
}