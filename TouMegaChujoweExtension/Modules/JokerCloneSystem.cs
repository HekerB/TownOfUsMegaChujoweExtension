using System.Collections.Generic;
using System.Linq;
using Reactor.Utilities.Extensions;
using TMPro;
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

    private static readonly List<CloneData> ActiveClones = [];
    private static int _currentMeetingCount;

    public static int KilledCloneCount { get; private set; }
    public static IReadOnlyList<CloneData> Clones => ActiveClones;
    public static int CurrentMeetingCount => _currentMeetingCount;

    public static int GetActiveCloneCountForJoker(byte jokerId)
    {
        return ActiveClones.Count(clone => clone.JokerId == jokerId && !clone.IsPreview);
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
                ActiveClones[i].Fake.Destroy();
                ActiveClones.RemoveAt(i);
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

        var control = fake.Body.AddComponent<JokerCloneControlComponent>();
        control.OwnerId = jokerId;
        control.AppearanceId = appearanceSource.PlayerId;

        ActiveClones.Add(new CloneData(jokerId, appearanceSource.PlayerId, fake, worldPos, placedAtMeeting)
        {
            IsPreview = isPreview
        });

        return ActiveClones.Count - 1;
    }

    public static bool TryGetClosestClone(Vector2 from, float maxDistance, out int cloneIndex, out Vector2 clonePos)
    {
        cloneIndex = -1;
        clonePos = default;
        var bestDistance = float.MaxValue;

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
            var distance = Vector2.Distance(from, position);
            if (distance <= maxDistance && distance < bestDistance)
            {
                bestDistance = distance;
                cloneIndex = i;
                clonePos = position;
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
        removed.Fake.Destroy();
        ActiveClones.RemoveAt(index);
        return true;
    }

    public static void RemoveClonesForJoker(byte jokerId)
    {
        for (var i = ActiveClones.Count - 1; i >= 0; i--)
        {
            if (ActiveClones[i].JokerId == jokerId)
            {
                ActiveClones[i].Fake.Destroy();
                ActiveClones.RemoveAt(i);
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

            ActiveClones[i].Fake.Destroy();
            ActiveClones.RemoveAt(i);
        }
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
            return;
        }

        var clone = ActiveClones[index];
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
        }
        catch
        {
            // visual-only fallback
        }
    }

    public static void ClearLocalOutline()
    {
        foreach (var clone in ActiveClones)
        {
            if (clone.Fake.Body == null)
            {
                continue;
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
