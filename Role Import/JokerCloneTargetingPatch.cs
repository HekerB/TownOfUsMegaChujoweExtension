using TouMegaChujoweExtension.Modules;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Joker;

public static class JokerCloneTargetingPatch
{
    public static bool IsCloneCloserThanPlayer(PlayerControl source, PlayerControl target)
    {
        if (source == null || target == null) return false;

        var sourcePos = (Vector2)source.GetTruePosition();
        var targetPos = (Vector2)target.GetTruePosition();
        var distToTarget = Vector2.Distance(sourcePos, targetPos);

        var clones = JokerCloneSystem.Clones;
        for (var i = 0; i < clones.Count; i++)
		{
			var clone = clones[i];
			if (clone == null || clone.IsPreview) continue;
			if (clone.AppearancePlayerId != target.PlayerId) continue;

			Vector2 clonePos;
			if (clone.Fake?.body != null)
				clonePos = (Vector2)clone.Fake.body.transform.position;
			else
				clonePos = new Vector2(clone.WorldPosition.x, clone.WorldPosition.y);

			var distToClone = Vector2.Distance(sourcePos, clonePos);

			if (distToClone <= distToTarget)
				return true;
		}
        return false;
    }
}