using System.Collections.Generic;
using System.Linq;
using Reactor.Utilities.Extensions;
using TMPro;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using TownOfUs.Extensions;
using TownOfUs.Patches;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Modules;

public static class JokerCloneSystem
{
    public sealed class CloneData(byte jokerId, byte appearancePlayerId, JokerDummy fake, Vector3 worldPosition, int placedAtMeeting)
    {
        public byte JokerId { get; } = jokerId;
        public byte AppearancePlayerId { get; } = appearancePlayerId;
        public JokerDummy Fake { get; } = fake;
        public Vector3 WorldPosition { get; } = worldPosition;
        public int PlacedAtMeeting { get; } = placedAtMeeting;
        public bool IsPreview { get; set; }
    }

    public readonly record struct JokerCloneSummary(
        int ActiveCount,
        int FirstActiveIndex,
        int LastActiveIndex,
        int PreviewIndex);

    private static readonly List<CloneData> ActiveClones = [];
    private static int _currentMeetingCount;
    private static int _localOutlinedCloneIndex = -1;

    public static int KilledCloneCount { get; private set; }
    public static IReadOnlyList<CloneData> Clones => ActiveClones;
    public static int CurrentMeetingCount => _currentMeetingCount;

    public static int GetActiveCloneCountForJoker(byte jokerId)
    {
        return ActiveClones.Count(clone => clone.JokerId == jokerId && !clone.IsPreview);
    }

    public static JokerCloneSummary GetCloneSummaryForJoker(byte jokerId)
    {
        var activeCount = 0;
        var firstActiveIndex = -1;
        var lastActiveIndex = -1;
        var previewIndex = -1;

        for (var i = 0; i < ActiveClones.Count; i++)
        {
            var clone = ActiveClones[i];
            if (clone.JokerId != jokerId)
            {
                continue;
            }

            if (clone.IsPreview)
            {
                previewIndex = i;
                continue;
            }

            activeCount++;
            lastActiveIndex = i;
            if (firstActiveIndex < 0)
            {
                firstActiveIndex = i;
            }
        }

        return new JokerCloneSummary(activeCount, firstActiveIndex, lastActiveIndex, previewIndex);
    }

    public static void IncrementMeetingCount()
    {
        _currentMeetingCount++;
    }

    public static void AddKill()
    {
        KilledCloneCount++;
    }

    public static int PlaceClone(byte jokerId, PlayerControl appearanceSource, Vector3 worldPos, bool isPreview = false)
    {
        if (isPreview)
        {
            RemovePreviewClonesForJoker(jokerId);
        }

        return PlaceCloneInternal(jokerId, appearanceSource, worldPos, _currentMeetingCount, isPreview);
    }

    public static void RemovePreviewClonesForJoker(byte jokerId)
    {
        for (var i = ActiveClones.Count - 1; i >= 0; i--)
        {
            if (ActiveClones[i].JokerId == jokerId && ActiveClones[i].IsPreview)
            {
                DestroyAndRemoveAt(i);
            }
        }
    }

    private static int PlaceCloneInternal(byte jokerId, PlayerControl appearanceSource, Vector3 worldPos, int placedAtMeeting, bool isPreview)
    {
        if (appearanceSource == null)
        {
            return -1;
        }

        var fake = new JokerDummy(appearanceSource);
        if (fake.Body == null)
        {
            fake.Destroy();
            return -1;
        }

        fake.Body.transform.position = worldPos;
        SetAlpha(fake.Body, isPreview ? 0.35f : 1f);
        fake.SetCamouflaged(HudManagerPatches.CamouflageCommsEnabled);
        SetAlpha(fake.Body, isPreview ? 0.35f : 1f);

        var control = fake.Body.AddComponent<JokerCloneControlComponent>();
        control.OwnerId = jokerId;
        control.AppearanceId = appearanceSource.PlayerId;

        ActiveClones.Add(new CloneData(jokerId, appearanceSource.PlayerId, fake, worldPos, placedAtMeeting)
        {
            IsPreview = isPreview
        });

        return ActiveClones.Count - 1;
    }

    public static void SyncCamouflageComms()
    {
        SetCloneCamouflage(HudManagerPatches.CamouflageCommsEnabled);
    }

    private static void SetCloneCamouflage(bool camouflaged)
    {
        foreach (var clone in ActiveClones)
        {
            if (clone.Fake.Body == null)
            {
                continue;
            }

            clone.Fake.SetCamouflaged(camouflaged);
            SetAlpha(clone.Fake.Body, clone.IsPreview ? 0.35f : 1f);
        }
    }

    public static bool TryGetClosestClone(Vector2 from, float maxDistance, out int cloneIndex, out Vector2 clonePos)
    {
        cloneIndex = -1;
        clonePos = default;
        var maxDistanceSq = maxDistance * maxDistance;
        var bestDistanceSq = float.MaxValue;

        for (var i = 0; i < ActiveClones.Count; i++)
        {
            var clone = ActiveClones[i];
            if (clone.IsPreview)
            {
                continue;
            }

            var position = clone.Fake.Body != null
                ? (Vector2)clone.Fake.Body.transform.position
                : (Vector2)clone.WorldPosition;
            var distanceSq = (from - position).sqrMagnitude;
            if (distanceSq <= maxDistanceSq && distanceSq < bestDistanceSq)
            {
                bestDistanceSq = distanceSq;
                cloneIndex = i;
                clonePos = position;
            }
        }

        return cloneIndex >= 0;
    }

    public static bool TryTriggerClosestClone(PlayerControl killer, Vector2 from, float maxDistance)
    {
        if (!TryGetClosestClone(from, maxDistance, out var cloneIndex, out _))
        {
            return false;
        }

        return TryTriggerClone(killer, cloneIndex);
    }

    public static int TriggerClonesInRadius(PlayerControl killer, Vector2 center, float radius, int maxClones = int.MaxValue)
    {
        if (killer == null || killer.HasDied() || MeetingHud.Instance || maxClones <= 0)
        {
            return 0;
        }

        var radiusSq = radius * radius;
        var cloneIndices = ActiveClones
            .Select((clone, index) => new
            {
                Clone = clone,
                Index = index,
                DistanceSq = (center - GetClonePosition(clone)).sqrMagnitude
            })
            .Where(entry => !entry.Clone.IsPreview && entry.DistanceSq <= radiusSq)
            .OrderBy(entry => entry.DistanceSq)
            .Take(maxClones)
            .Select(entry => entry.Index)
            .OrderByDescending(index => index)
            .ToList();

        foreach (var cloneIndex in cloneIndices)
        {
            TryTriggerClone(killer, cloneIndex);
        }

        return cloneIndices.Count;
    }

    public static bool TryTriggerClone(PlayerControl killer, int cloneIndex)
    {
        if (killer == null || cloneIndex < 0 || cloneIndex >= ActiveClones.Count)
        {
            return false;
        }

        var clone = ActiveClones[cloneIndex];
        var joker = MiscUtils.PlayerById(clone.JokerId);
        if (joker == null || joker.HasDied() || !joker.IsRole<JokerRole>())
        {
            return false;
        }

        JokerRole.RpcJokerCloneKilled(killer, clone.JokerId, (byte)cloneIndex);
        return true;
    }

    private static Vector2 GetClonePosition(CloneData clone)
    {
        return clone.Fake.Body != null
            ? clone.Fake.Body.transform.position
            : clone.WorldPosition;
    }

    public static bool TryRemoveClone(int index, out CloneData removed)
    {
        if (index < 0 || index >= ActiveClones.Count)
        {
            removed = null!;
            return false;
        }

        removed = ActiveClones[index];
        DestroyAndRemoveAt(index);
        return true;
    }

    public static void RemoveClonesForJoker(byte jokerId)
    {
        for (var i = ActiveClones.Count - 1; i >= 0; i--)
        {
            if (ActiveClones[i].JokerId == jokerId)
            {
                DestroyAndRemoveAt(i);
            }
        }
    }

    public static void ClearClones(bool includePreviews = true)
    {
        for (var i = ActiveClones.Count - 1; i >= 0; i--)
        {
            if (!includePreviews && ActiveClones[i].IsPreview)
            {
                continue;
            }

            DestroyAndRemoveAt(i);
        }
    }

    private static void DestroyAndRemoveAt(int index)
    {
        var clone = ActiveClones[index];
        if (_localOutlinedCloneIndex == index)
        {
            ClearCloneOutline(clone);
            _localOutlinedCloneIndex = -1;
        }
        else if (_localOutlinedCloneIndex > index)
        {
            _localOutlinedCloneIndex--;
        }

        clone.Fake.Destroy();
        ActiveClones.RemoveAt(index);
    }

    public static void ClearAll()
    {
        ClearClones();
        KilledCloneCount = 0;
        _currentMeetingCount = 0;
    }

    public static void UpdateLocalOutline(Vector2 from, float maxDistance, Color color)
    {
        if (!TryGetClosestClone(from, maxDistance, out var index, out _))
        {
            ClearLocalOutline();
            return;
        }

        UpdateLocalOutline(index, color);
    }

    public static void UpdateLocalOutline(int cloneIndex, Color color)
    {
        if (cloneIndex < 0 || cloneIndex >= ActiveClones.Count)
        {
            ClearLocalOutline();
            return;
        }

        if (_localOutlinedCloneIndex >= 0 && _localOutlinedCloneIndex != cloneIndex)
        {
            ClearLocalOutline();
        }

        var clone = ActiveClones[cloneIndex];
        if (clone.Fake.Body == null)
        {
            return;
        }

        var cosmetics = clone.Fake.Body.GetComponentInChildren<CosmeticsLayer>(true);
        var body = cosmetics?.currentBodySprite?.BodySprite;
        if (cosmetics == null || body == null)
        {
            return;
        }

        try
        {
            cosmetics.SetOutline(true, new Il2CppSystem.Nullable<Color>(color));
            body.SetOutline(color);
            _localOutlinedCloneIndex = cloneIndex;
        }
        catch
        {
            // visual-only fallback
        }
    }

    public static void ClearLocalOutline()
    {
        if (_localOutlinedCloneIndex >= 0 && _localOutlinedCloneIndex < ActiveClones.Count)
        {
            ClearCloneOutline(ActiveClones[_localOutlinedCloneIndex]);
            _localOutlinedCloneIndex = -1;
            return;
        }

        _localOutlinedCloneIndex = -1;
    }

    private static void ClearCloneOutline(CloneData clone)
    {
        if (clone.Fake.Body == null)
        {
            return;
        }

        try
        {
            var cosmetics = clone.Fake.Body.GetComponentInChildren<CosmeticsLayer>(true);
            var body = cosmetics?.currentBodySprite?.BodySprite;
            cosmetics?.SetOutline(false, new Il2CppSystem.Nullable<Color>(Color.clear));
            body?.SetOutline(null);
        }
        catch
        {
            // visual-only fallback
        }
    }

    public static void SetAlpha(GameObject root, float alpha)
    {
        if (root == null)
        {
            return;
        }

        foreach (var spriteRenderer in root.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (spriteRenderer == null)
            {
                continue;
            }

            var color = spriteRenderer.color;
            color.a = Mathf.Clamp01(alpha);
            spriteRenderer.color = color;
        }

        foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text == null)
            {
                continue;
            }

            var color = text.color;
            color.a = Mathf.Clamp01(alpha);
            text.color = color;
        }
    }
}
