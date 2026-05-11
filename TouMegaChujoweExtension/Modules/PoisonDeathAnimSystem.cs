using Il2CppInterop.Runtime;
using MiraAPI.GameOptions;
using PowerTools;
using Reactor.Utilities;
using System.Collections.Generic;
using System.Collections;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Modules;

public static class PoisonDeathAnimSystem
{
    private static readonly Dictionary<byte, GameObject> ActiveClones = new();

    public static void TriggerDeathAnimation(byte targetId)
    {
        Coroutines.Start(CoPlayDeathAnimation(targetId));
    }

    private static IEnumerator CoPlayDeathAnimation(byte targetId)
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        var target = MiscUtils.PlayerById(targetId);
        if (target == null) yield break;

        var realBody = GetBodyById(targetId);
        if (realBody == null) yield break;

        if (GameManager.Instance == null || GameManager.Instance.deadBodyPrefab == null || GameManager.Instance.deadBodyPrefab.Length < 2) yield break;
        
        var viperPrefab = GameManager.Instance.deadBodyPrefab[1].Cast<ViperDeadBody>();
        if (viperPrefab == null) yield break;

        bool wasActive = viperPrefab.gameObject.activeSelf;
        viperPrefab.gameObject.SetActive(false);
        
        var visualClone = UnityEngine.Object.Instantiate(viperPrefab.gameObject, realBody.transform);
        
        viperPrefab.gameObject.SetActive(wasActive);

        UnityEngine.Object.DestroyImmediate(visualClone.GetComponent<ViperDeadBody>());
        UnityEngine.Object.DestroyImmediate(visualClone.GetComponent<DeadBody>());
        UnityEngine.Object.DestroyImmediate(visualClone.GetComponent<Collider2D>());

        visualClone.transform.localPosition = new Vector3(0f, 0f, -0.01f);
        visualClone.transform.localScale = Vector3.one;


        foreach (var r in realBody.GetComponentsInChildren<SpriteRenderer>())
        {
            if (r.transform.IsChildOf(visualClone.transform)) continue;
            
            r.color = Color.clear;
        }

        var fiolet = new Color(0.6f, 0f, 1f, 1f);
        foreach (var r in visualClone.GetComponentsInChildren<SpriteRenderer>())
        {
            r.color = fiolet;
        }

        var spriteAnim = visualClone.GetComponent<SpriteAnim>();
        if (spriteAnim != null && viperPrefab.dissolveAnims != null && viperPrefab.dissolveAnims.Length > 0)
        {
            int stageIndex = System.Math.Min(2, viperPrefab.dissolveAnims.Length - 1);
            spriteAnim.Play(viperPrefab.dissolveAnims[stageIndex]);
        }

        visualClone.SetActive(true);
        
        if (ActiveClones.TryGetValue(targetId, out var old) && old != null)
        {
            UnityEngine.Object.Destroy(old);
        }
        ActiveClones[targetId] = visualClone;
    }

    public static void RestoreBodyRenderers(byte playerId)
    {
        if (ActiveClones.TryGetValue(playerId, out var clone))
        {
            if (clone != null)
            {
                UnityEngine.Object.Destroy(clone);
            }
            ActiveClones.Remove(playerId);
        }

        var body = GetBodyById(playerId);
        if (body == null) return;

        foreach (var r in body.GetComponentsInChildren<SpriteRenderer>())
        {
            r.color = Color.white;
        }
    }

    public static void CleanupAll()
    {
        foreach (var clone in ActiveClones.Values.Where(clone => clone != null))
        {
            UnityEngine.Object.Destroy(clone);
        }
        ActiveClones.Clear();
    }

    private static DeadBody? GetBodyById(byte playerId)
    {
        return UnityEngine.Object.FindObjectsOfType<DeadBody>().FirstOrDefault(body => body.ParentId == playerId);
    }
}












