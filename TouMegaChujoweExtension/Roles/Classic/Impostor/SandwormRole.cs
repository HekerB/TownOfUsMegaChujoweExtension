using System.Collections;
using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using Reactor.Networking.Attributes;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Networking;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TownOfUs.Assets;
using TownOfUs.Extensions;
using TownOfUs.Networking;
using TouMegaChujoweExtension.Modifiers;
using TownOfUs.Modules.Localization;
using TownOfUs.Interfaces;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Roles.Impostor;
using TownOfUs.Utilities;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Reactor.Utilities;

namespace TouMegaChujoweExtension.Roles.Impostor;

public sealed class SandwormRole(IntPtr cppPtr) : ImpostorRole(cppPtr), TownOfUs.Roles.ITownOfUsRole, TownOfUs.Modules.Wiki.IWikiDiscoverable
{
    public string LocaleKey => "Sandworm";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}", "Sandworm");
    public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb");
    public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription");

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription") + MiscUtils.AppendOptionsText(GetType());
    }

    public Color RoleColor => TouExtensionColors.Sandworm;
    public ModdedRoleTeams Team => ModdedRoleTeams.Impostor;
    public TownOfUs.Roles.RoleAlignment RoleAlignment => TownOfUs.Roles.RoleAlignment.ImpostorConcealing;

    public CustomRoleConfiguration Configuration => new(this)
    {
        Icon = TouRoleIcons.Miner, // Placeholder
    };

    [HideFromIl2Cpp]
    public List<TownOfUs.Modules.Wiki.CustomButtonWikiDescription> Abilities =>
    [
        new(TouLocale.GetParsed("ExtensionRoleSandwormDig", "Dig"),
            TouLocale.GetParsed("ExtensionRoleSandwormDigWikiDescription"),
            TouRoleIcons.Miner)
    ];

    public bool IsUnderground { get; set; }
    public bool IsDigging { get; set; }
    public float DigEndTime { get; set; }
    public List<Vent> PlacedVents { get; set; } = new();

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);
        IsUnderground = false;
        IsDigging = false;
        PlacedVents.Clear();
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);
        if (targetPlayer.AmOwner)
        {
            CleanupVents();
        }
    }

    private void CleanupVents()
    {
        foreach (var vent in PlacedVents)
        {
            if (vent != null)
            {
                // Remove from ShipStatus
                var allVents = ShipStatus.Instance.AllVents.ToList();
                allVents.Remove(vent);
                ShipStatus.Instance.AllVents = allVents.ToArray();
                UnityEngine.Object.Destroy(vent.gameObject);
            }
        }
        PlacedVents.Clear();
    }

    public void Update()
    {
        if (Player == null || !Player.AmOwner || !IsDigging) return;

        if (Time.time >= DigEndTime)
        {
            SandwormRole.RpcEmerge(Player, Player.GetTruePosition());
        }
    }

    [MethodRpc((uint)ExtensionRpc.SandwormUnderground)]
    public static void RpcUnderground(PlayerControl player, Vector2 position)
    {
        if (player.Data.Role is not SandwormRole role) return;
        role.IsUnderground = true;
        role.IsDigging = true; // Combine phases for immediate movement
        role.DigEndTime = Time.time + OptionGroupSingleton<SandwormOptions>.Instance.DigDuration;
        
        // Spawn entrance vent
        var vent = PlaceVent(player, position);
        if (vent != null)
        {
            role.PlacedVents.Add(vent);
            if (player.AmOwner)
            {
                player.MyPhysics.RpcEnterVent(vent.Id);
                // Immediately exit to allow WASD movement while invisible/fast
                Coroutines.Start(CoExitAndInvis(player));
            }
        }
    }

    private static IEnumerator CoExitAndInvis(PlayerControl player)
    {
        yield return new WaitForSeconds(0.2f);
        player.MyPhysics.RpcExitVent(0);
        player.AddModifier<SandwormInvisibleModifier>();
        player.AddModifier<SandwormSpeedModifier>(OptionGroupSingleton<SandwormOptions>.Instance.UndergroundSpeed);
    }

    private static IEnumerator CoFinalExit(PlayerControl player)
    {
        yield return new WaitForSeconds(0.2f);
        player.MyPhysics.RpcExitVent(0);
    }

    [MethodRpc((uint)ExtensionRpc.SandwormEmerge)]
    public static void RpcEmerge(PlayerControl player, Vector2 position)
    {
        if (player.Data.Role is not SandwormRole role) return;
        role.IsUnderground = false;
        role.IsDigging = false;
        
        if (player.AmOwner)
        {
            player.RemoveModifier<SandwormInvisibleModifier>();
            player.RemoveModifier<SandwormSpeedModifier>();
            // Play emergence vent visual
            var vent = PlaceVent(player, position);
            if (vent != null)
            {
                player.MyPhysics.RpcEnterVent(vent.Id);
                Coroutines.Start(CoFinalExit(player));
            }
        }

        // Spawn exit vent
        var exitVent = PlaceVent(player, position);
        if (exitVent != null)
        {
            role.PlacedVents.Add(exitVent);
            
            // Link ONLY the last two vents placed by this sandworm
            if (role.PlacedVents.Count >= 2)
            {
                var v1 = role.PlacedVents[role.PlacedVents.Count - 2];
                var v2 = role.PlacedVents[role.PlacedVents.Count - 1];
                if (v1 != null && v2 != null)
                {
                    v1.Left = v2;
                    v1.Right = v2;
                    v2.Left = v1;
                    v2.Right = v1;
                }
            }
        }

        // Passive kill
        if (AmongUsClient.Instance.AmHost)
        {
            var options = OptionGroupSingleton<SandwormOptions>.Instance;
            var radius = options.EmergeKillRadius;
            
            foreach (var victim in PlayerControl.AllPlayerControls)
            {
                if (victim == null || victim.HasDied() || victim.PlayerId == player.PlayerId) continue;
                // Killing everyone! Per user: "zabijając każdego"
                // But usually we don't kill fellow impostors. I'll stick to non-impostors.
                if (victim.IsImpostorAligned()) continue;

                if (Vector2.Distance(victim.GetTruePosition(), position) <= radius)
                {
                    CustomTouMurderRpcs.RpcSpecialMurder(player, victim, causeOfDeath: "Mangled");
                }
            }
        }
    }

    private static Vent? PlaceVent(PlayerControl player, Vector2 position)
    {
        var ventPrefab = ShipStatus.Instance.AllVents.FirstOrDefault(v => v != null);
        if (ventPrefab == null) return null;

        var vent = UnityEngine.Object.Instantiate(ventPrefab, ventPrefab.transform.parent);
        int ventId = 5000 + player.PlayerId * 10 + (player.Data.Role as SandwormRole)?.PlacedVents.Count ?? 0;
        vent.name = $"SandwormVent-{player.PlayerId}-{ventId}";
        vent.Id = ventId;
        vent.transform.position = new Vector3(position.x, position.y, position.y / 1000f + 0.01f);
        
        // Isolate vent
        vent.Left = null;
        vent.Right = null;
        // Some versions of Among Us use these:
        // vent.Center = null; 
        
        // Add to global list so it can be used
        var allVents = ShipStatus.Instance.AllVents.ToList();
        allVents.Add(vent);
        ShipStatus.Instance.AllVents = allVents.ToArray();
        
        return vent;
    }
}
