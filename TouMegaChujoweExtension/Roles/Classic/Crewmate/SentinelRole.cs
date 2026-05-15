using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using MiraAPI.Utilities;
using System;
using System.Collections.Generic;
using TouMegaChujoweExtension.Options.Roles.Crewmate;
using TownOfUs.Assets;
using TownOfUs.Extensions;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using UnityEngine;
using System.Linq;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using TouMegaChujoweExtension.Networking;
using TouMegaChujoweExtension.Assets;

namespace TouMegaChujoweExtension.Roles.Crewmate;

public sealed class SentinelRole(IntPtr cppPtr) : CrewmateRole(cppPtr), ITownOfUsRole, IWikiDiscoverable
{
    public string LocaleKey => "Sentinel";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}", "Sentinel");
    public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb");
    public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription");
    public Color RoleColor => TouExtensionColors.Sentinel;
    public ModdedRoleTeams Team => ModdedRoleTeams.Crewmate;
    public RoleAlignment RoleAlignment => RoleAlignment.CrewmateKilling;

    public Vector2? PatrolPosition { get; set; }
    public GameObject? PatrolAreaObject { get; set; }

    public CustomRoleConfiguration Configuration => new(this)
    {
        UseVanillaKillButton = false,
        Icon = TouExtensionIcons.SentinelRoleIcon, 
        IntroSound = TouAudio.ScientistIntroSound
    };

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities =>
    [
        new(
            TouLocale.GetParsed("ExtensionRoleSentinelPatrol", "Patrol"),
            TouLocale.GetParsed("ExtensionRoleSentinelPatrolWikiDescription"),
            TouExtensionCrewAssets.SentinelPatrolSprite)
    ];

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);
        PatrolPosition = Modules.SentinelSystem.GetActivePatrolPosition(player.PlayerId);
        if (PatrolPosition != null)
        {
            UpdatePatrolVisual();
        }
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);
        if (PatrolAreaObject != null)
        {
            UnityEngine.Object.Destroy(PatrolAreaObject);
        }
    }

    public void PlacePatrol(Vector2 position)
    {
        RpcPlacePatrol(Player, position);
    }

    [MethodRpc((uint)ExtensionRpc.SentinelPlacePatrol)]
    public static void RpcPlacePatrol(PlayerControl sentinel, Vector2 position)
    {
        if (sentinel == null) return;
        
        Modules.SentinelSystem.SetPatrol(sentinel.PlayerId, position);
        
        if (sentinel.Data?.Role is SentinelRole sentinelRole)
        {
            sentinelRole.PatrolPosition = position;
            if (sentinel.AmOwner)
            {
                sentinelRole.UpdatePatrolVisual();
            }
        }
    }

    public void ClearPatrol()
    {
        PatrolPosition = null;
        if (PatrolAreaObject != null)
        {
            UnityEngine.Object.Destroy(PatrolAreaObject);
            PatrolAreaObject = null;
        }
    }

    private static GameObject? CreateRadiusSphere(Vector3 pos, float radius, Color roleColor, float alpha = 0.35f)
    {
        var sphere = MiscUtils.CreateSpherePrimitive(pos, radius);
        var meshRenderer = sphere.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            try 
            {
                var mat = new Material(AuAvengersAnims.TrapMaterial.LoadAsset());
                meshRenderer.material = mat;
                meshRenderer.material.color = new Color(roleColor.r, roleColor.g, roleColor.b, 0.4f);
            }
            catch 
            {
                meshRenderer.material.color = new Color(roleColor.r, roleColor.g, roleColor.b, alpha);
            }
        }

        return sphere;
    }

    private void UpdatePatrolVisual()
    {
        if (Player == null || !Player.AmOwner || PatrolPosition == null) return;

        if (PatrolAreaObject == null)
        {
            float radius = OptionGroupSingleton<SentinelOptions>.Instance.Radius;
            PatrolAreaObject = CreateRadiusSphere(new Vector3(PatrolPosition.Value.x, PatrolPosition.Value.y, -5f), radius, RoleColor);
            PatrolAreaObject.name = "SentinelPatrolArea";
        }
        else
        {
            PatrolAreaObject.transform.position = new Vector3(PatrolPosition.Value.x, PatrolPosition.Value.y, -5f);
        }
    }
}
