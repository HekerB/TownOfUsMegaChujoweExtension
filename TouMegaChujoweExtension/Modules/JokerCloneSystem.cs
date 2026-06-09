using System.Collections.Generic;
using System.Linq;
using Reactor.Utilities.Extensions;
using TMPro;
using TownOfUs.Modules;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Modules;

public static class JokerCloneSystem
{
    public sealed class CloneData
    {
        public byte JokerId { get; }
        public byte AppearancePlayerId { get; }
        public JokerDummy Fake { get; }
        public Vector3 WorldPosition { get; }
        public int PlacedAtMeeting { get; }
        public bool IsPreview { get; set; }

        public CloneData(byte jokerId, byte appearancePlayerId, JokerDummy fake, Vector3 worldPosition, int placedAtMeeting)
        {
            JokerId = jokerId;
            AppearancePlayerId = appearancePlayerId;
            Fake = fake;
            WorldPosition = worldPosition;
            PlacedAtMeeting = placedAtMeeting;
            IsPreview = false;
        }
    }

    private static readonly List<CloneData> ActiveClones = new();
    private static int _currentMeetingCount;

    public static int KilledCloneCount { get; private set; }
    public static IReadOnlyList<CloneData> Clones => ActiveClones;

    public static int GetCloneCountForJoker(byte jokerId) => ActiveClones.Count(c => c.JokerId == jokerId);
    public static int GetActiveCloneCountForJoker(byte jokerId) => ActiveClones.Count(c => c.JokerId == jokerId && !c.IsPreview);

    public static void IncrementMeetingCount() => _currentMeetingCount++;
    public static int CurrentMeetingCount => _currentMeetingCount;
    public static void AddKill() => KilledCloneCount++;

    public static int PlaceClone(byte jokerId, PlayerControl appearanceSource, Vector3 worldPos, bool isPreview = false)
    {
        return PlaceCloneInternal(jokerId, appearanceSource, worldPos, _currentMeetingCount, isPreview);
    }

    public static void RespawnClone(byte jokerId, PlayerControl appearanceSource, Vector3 worldPos, int originalPlacedAtMeeting)
    {
        PlaceCloneInternal(jokerId, appearanceSource, worldPos, originalPlacedAtMeeting, false);
    }

    private static int PlaceCloneInternal(byte jokerId, PlayerControl appearanceSource, Vector3 worldPos, int placedAtMeeting, bool isPreview)
    {
        if (appearanceSource == null) return -1;

        var fake = new JokerDummy(appearanceSource);
        if (fake.body == null)
        {
            fake.Destroy();
            return -1;
        }

        fake.body.transform.position = worldPos;
        SetAlpha(fake.body, isPreview ? 0.35f : 1f);

        var controlComp = fake.body.AddComponent<JokerCloneControlComponent>();
        controlComp.OwnerId = jokerId;
        controlComp.AppearanceId = appearanceSource.PlayerId;

        var cloneData = new CloneData(jokerId, appearanceSource.PlayerId, fake, worldPos, placedAtMeeting)
        {
            IsPreview = isPreview
        };

        ActiveClones.Add(cloneData);
        return ActiveClones.Count - 1;
    }

    public static bool TryGetClosestClone(Vector2 from, float maxDistance, out int cloneIndex, out Vector2 clonePos)
    {
        cloneIndex = -1;
        clonePos = default;
        var bestDist = float.MaxValue;

        for (var i = 0; i < ActiveClones.Count; i++)
        {
            var clone = ActiveClones[i];
            if (clone.IsPreview) continue;

            var p = clone.Fake?.body != null
                ? new Vector2(clone.Fake.body.transform.position.x, clone.Fake.body.transform.position.y)
                : new Vector2(clone.WorldPosition.x, clone.WorldPosition.y);
            var d = Vector2.Distance(from, p);
            if (d <= maxDistance && d < bestDist)
            {
                bestDist = d;
                cloneIndex = i;
                clonePos = p;
            }
        }

        return cloneIndex >= 0;
    }

    public static bool TryRemoveClone(int index, out CloneData removed)
    {
        if (index < 0 || index >= ActiveClones.Count)
        {
            removed = null!;
            return false;
        }

        removed = ActiveClones[index];
        removed.Fake?.Destroy();
        ActiveClones.RemoveAt(index);
        return true;
    }

    public static void RemoveClonesByMeetingLifespan(int maxMeetings)
    {
        if (maxMeetings <= 0) return;

        for (var i = ActiveClones.Count - 1; i >= 0; i--)
        {
            var meetingsSurvived = _currentMeetingCount - ActiveClones[i].PlacedAtMeeting;
            if (meetingsSurvived >= maxMeetings)
            {
                ActiveClones[i].Fake?.Destroy();
                ActiveClones.RemoveAt(i);
            }
        }
    }

    public static void RemoveClonesForJoker(byte jokerId)
    {
        for (var i = ActiveClones.Count - 1; i >= 0; i--)
        {
            if (ActiveClones[i].JokerId == jokerId)
            {
                ActiveClones[i].Fake?.Destroy();
                ActiveClones.RemoveAt(i);
            }
        }
    }

    public static void ClearClonesWithoutDestroying() => ActiveClones.Clear();

    public static void ClearAll()
    {
        foreach (var clone in ActiveClones) clone.Fake?.Destroy();
        ActiveClones.Clear();
        KilledCloneCount = 0;
        _currentMeetingCount = 0;
    }

    public static void UpdateLocalOutline(Vector2 from, float maxDistance, Color color)
    {
        if (!TryGetClosestClone(from, maxDistance, out var idx, out _)) return;

        var clone = ActiveClones[idx];
        if (clone.Fake?.body == null) return;

        var cosmetics = clone.Fake.body.GetComponentInChildren<CosmeticsLayer>(true);
        if (cosmetics == null) return;

        var body = cosmetics.currentBodySprite?.BodySprite;
        if (body == null) return;

        try
        {
            cosmetics.SetOutline(true, new Il2CppSystem.Nullable<Color>(color));
            body.SetOutline(color);
        }
        catch { }
    }

    public static void ClearLocalOutline()
    {
        foreach (var clone in ActiveClones)
        {
            if (clone.Fake?.body == null) continue;
            try
            {
                var cosmetics = clone.Fake.body.GetComponentInChildren<CosmeticsLayer>(true);
                if (cosmetics != null)
                {
                    cosmetics.SetOutline(false, new Il2CppSystem.Nullable<Color>(Color.clear));
                    var body = cosmetics.currentBodySprite?.BodySprite;
                    body?.SetOutline(null);
                }
            }
            catch { }
        }
    }

    public static void SetAlpha(GameObject root, float alpha)
    {
        if (root == null) return;

        foreach (var sr in root.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sr == null) continue;
            var c = sr.color;
            c.a = Mathf.Clamp01(alpha);
            sr.color = c;
        }

        foreach (var tmp in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if (tmp == null) continue;
            var c = tmp.color;
            c.a = Mathf.Clamp01(alpha);
            tmp.color = c;
        }
    }
}
