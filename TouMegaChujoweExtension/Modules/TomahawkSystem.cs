using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using System.Collections.Generic;
using System.Linq;
using MiraAPI.GameOptions;
using TouMegaChujoweExtension.Networking;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TouMegaChujoweExtension.Roles.Classic.Impostor;
using TownOfUs.Extensions;
using TownOfUs.Utilities;
using MiraAPI.Utilities;
using TownOfUs.Networking;
using UnityEngine;

namespace TouMegaChujoweExtension.Modules;

public sealed class TomahawkAxe : MonoBehaviour
{
    private PlayerControl? _owner;
    private Vector2 _velocity;
    private float _radius;
    private float _rotationSpeed = 720f;
    private SpriteRenderer? _renderer;
    private HashSet<byte> _hitPlayers = new();

    public void Initialize(PlayerControl owner, Vector2 direction, float speed, float radius)
    {
        _owner = owner;
        _velocity = direction * speed;
        _radius = radius;

        _renderer = gameObject.AddComponent<SpriteRenderer>();
        _renderer.sprite = TouExtensionAssets.AxeSprite.LoadAsset(); 
        _renderer.color = Color.white;
        _renderer.transform.localScale = Vector3.one * 0.5f;
        _renderer.sortingLayerName = "Player";
        _renderer.sortingOrder = 32767;

        Destroy(gameObject, 10f); 
    }

    private void Update()
    {
        var dt = Time.deltaTime;
        transform.position += (Vector3)(_velocity * dt);
        transform.Rotate(0, 0, _rotationSpeed * dt);

        if (_owner != null && _owner.AmOwner)
        {
            CheckCollisions();
        }

        if (Vector2.Distance(transform.position, Vector2.zero) > 100f)
        {
            Destroy(gameObject);
        }
    }

    private void CheckCollisions()
    {
        if (ShipStatus.Instance == null) return;

        var pos = (Vector2)transform.position;
        var players = Helpers.GetClosestPlayers(pos, _radius);

        foreach (var pc in players)
        {
            if (pc == null || pc.Data == null || pc.Data.IsDead) continue;
            if (_owner != null && pc.PlayerId == _owner.PlayerId) continue;
            if (_hitPlayers.Contains(pc.PlayerId)) continue;

            _hitPlayers.Add(pc.PlayerId);
            Info($"[Tomahawk] Hit {pc.Data.PlayerName}");
            
            // Use RpcSpecialMurder for custom cause
            _owner.RpcSpecialMurder(pc, createDeadBody: true, teleportMurderer: false, causeOfDeath: "Tomahawk");
        }
    }
}

public static class TomahawkSystem
{
    public static void Update()
    {
    }

    public static void ThrowTomahawk(PlayerControl sender, Vector2 direction)
    {
        var opts = OptionGroupSingleton<TomahawkOptions>.Instance;
        var go = new GameObject("TomahawkAxe");
        go.transform.position = sender.GetTruePosition();
        var axe = go.AddComponent<TomahawkAxe>();
        axe.Initialize(sender, direction, opts.Speed, opts.KillRadius);
        Info($"[Tomahawk] {sender.Data.PlayerName} threw tomahawk in direction {direction}");
    }
}
