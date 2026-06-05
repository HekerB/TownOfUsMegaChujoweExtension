using System;
using System.Collections;
using System.Collections.Generic;
using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using Reactor.Networking.Attributes;
using Reactor.Utilities;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Buttons.Classic.Impostor;
using TouMegaChujoweExtension.Modifiers.Impostor;
using TouMegaChujoweExtension.Networking;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TownOfUs.Assets;
using TownOfUs.Extensions;
using TownOfUs.Interfaces;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Roles.Impostor;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Roles.Classic.Impostor;

public sealed class BurrowerRole(IntPtr cppPtr) : ImpostorRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IDoomable
{
    public DoomableType DoomHintType => DoomableType.Trickster;
    public string LocaleKey => "Burrower";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}", "Burrower");
    public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb");
    public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription");

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription") + MiscUtils.AppendOptionsText(GetType());
    }

    public Color RoleColor => TouExtensionColors.Burrower;
    public ModdedRoleTeams Team => ModdedRoleTeams.Impostor;
    public RoleAlignment RoleAlignment => RoleAlignment.ImpostorConcealing;

    public CustomRoleConfiguration Configuration => new(this)
    {
        Icon = TouExtensionIcons.BurrowerRoleIcon,
        OptionsScreenshot = TouBanners.ImpostorRoleBanner,
        IntroSound = TouAudio.MineSound
    };

    [HideFromIl2Cpp]
    public List<Type> RoleButtons => [typeof(BurrowerDigButton)];

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities =>
    [
        new(TouLocale.GetParsed("ExtensionRoleBurrowerDig", "Dig"),
            TouLocale.GetParsed("ExtensionRoleBurrowerDigWikiDescription"),
            TouImpAssets.MineSprite)
    ];

    public bool IsUnderground { get; set; }
    public bool IsDigging { get; set; }
    public bool IsPreparingDig { get; set; }
    public float BurrowStartTime { get; set; }
    public float PrepareDigEndTime { get; set; }
    public float DigEndTime { get; set; }
    public float EmergeTime { get; set; }
    public int NextVentIndex { get; set; }
    private const float DigCancelLockDuration = 1f;
    private const float UndergroundInitialSpeed = 0.3f;
    private const float BurrowSoundRadius = 15f;
    private const float BurrowSoundVolume = 0.8f;

    [HideFromIl2Cpp]
    public Vent? FirstVent { get; set; }

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);
        IsUnderground = false;
        IsDigging = false;
        IsPreparingDig = false;
        BurrowStartTime = 0f;
        PrepareDigEndTime = 0f;
        EmergeTime = -999f;
        FirstVent = null;
        NextVentIndex = 0;
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);
        Modules.BurrowerSystem.Reset();
    }

    public void Update()
    {
        if (Player == null || !Player.AmOwner || !IsDigging)
        {
            return;
        }

        if (Time.time >= DigEndTime)
        {
            if (!Modules.BurrowerSystem.TryFindVentPlacementPosition(Player, Player.GetTruePosition(), out var emergePosition))
            {
                return;
            }

            RpcEmerge(Player, emergePosition);
        }
    }

    public float GetUndergroundSpeedMultiplier()
    {
        var maxSpeed = OptionGroupSingleton<BurrowerOptions>.Instance.UndergroundSpeed;
        var digDuration = OptionGroupSingleton<BurrowerOptions>.Instance.DigDuration;
        var accelerationDuration = Mathf.Clamp(digDuration * 0.45f, 2f, 6f);
        var acceleration = Mathf.Clamp01((Time.time - BurrowStartTime) / accelerationDuration);
        return Mathf.Lerp(UndergroundInitialSpeed, maxSpeed, acceleration);
    }

    public bool CanCancelUndergroundDig()
    {
        if (!IsUnderground || !IsDigging)
        {
            return false;
        }

        return Time.time - BurrowStartTime >= DigCancelLockDuration;
    }

    public bool CanCancelPreparingDig()
    {
        if (!IsPreparingDig)
        {
            return false;
        }

        var enterDelay = OptionGroupSingleton<BurrowerOptions>.Instance.EnterDelay;
        var startTime = PrepareDigEndTime - enterDelay;
        return Time.time - startTime >= 1f;
    }

    [MethodRpc((uint)ExtensionRpc.BurrowerUnderground)]
    public static void RpcUnderground(PlayerControl player, Vector2 position)
    {
        if (!player.IsRole<BurrowerRole>())
        {
            return;
        }

        var role = player.GetRole<BurrowerRole>();
        if (role == null || role.IsUnderground || role.IsPreparingDig)
        {
            return;
        }

        if (!Modules.BurrowerSystem.TryFindVentPlacementPosition(player, position, out var ventPosition))
        {
            return;
        }

        var enterDelay = OptionGroupSingleton<BurrowerOptions>.Instance.EnterDelay;
        if (enterDelay <= 0f)
        {
            EnterUnderground(player, role, ventPosition);
            return;
        }

        role.IsPreparingDig = true;
        role.PrepareDigEndTime = Time.time + enterDelay;
        Coroutines.Start(CoEnterUndergroundAfterDelay(player, ventPosition));
    }

    private static IEnumerator CoEnterUndergroundAfterDelay(PlayerControl player, Vector2 ventPosition)
    {
        yield return new WaitForSeconds(OptionGroupSingleton<BurrowerOptions>.Instance.EnterDelay);

        if (player == null || player.HasDied() || !player.IsRole<BurrowerRole>())
        {
            yield break;
        }

        var role = player.GetRole<BurrowerRole>();
        if (role == null || !role.IsPreparingDig || role.IsUnderground || MeetingHud.Instance || ExileController.Instance)
        {
            if (role != null)
            {
                role.IsPreparingDig = false;
                role.PrepareDigEndTime = 0f;
            }

            yield break;
        }

        EnterUnderground(player, role, ventPosition);
    }

    private static void EnterUnderground(PlayerControl player, BurrowerRole role, Vector2 ventPosition)
    {
        role.IsPreparingDig = false;
        role.PrepareDigEndTime = 0f;
        role.IsUnderground = true;
        role.IsDigging = true;
        role.BurrowStartTime = Time.time;
        role.DigEndTime = Time.time + OptionGroupSingleton<BurrowerOptions>.Instance.DigDuration;

        var ventId = 5200 + player.PlayerId * 100 + role.NextVentIndex * 2;
        var vent = Modules.BurrowerSystem.SpawnVent(player, ventId, ventPosition);
        role.FirstVent = vent;

        player.AddModifier<BurrowerInvisibleModifier>();
        player.AddModifier<BurrowerSpeedModifier>(OptionGroupSingleton<BurrowerOptions>.Instance.UndergroundSpeed);
        PlayBurrowSound(ventPosition);

        if (player.MyPhysics != null)
        {
            if (player.AmOwner)
            {
                player.MyPhysics.RpcEnterVent(ventId);
            }
            else
            {
                try
                {
                    vent.EnterVent(player);
                }
                catch (System.Exception ex)
                {
                    UnityEngine.Debug.LogError($"[Burrower] Failed to EnterVent locally for remote client: {ex}");
                }
            }
        }
    }

    [MethodRpc((uint)ExtensionRpc.BurrowerEmerge)]
    public static void RpcEmerge(PlayerControl player, Vector2 position)
    {
        if (!player.IsRole<BurrowerRole>())
        {
            return;
        }

        var role = player.GetRole<BurrowerRole>();
        if (role == null || !role.IsUnderground)
        {
            return;
        }

        if (!Modules.BurrowerSystem.TryFindVentPlacementPosition(player, position, out var ventPosition))
        {
            return;
        }

        role.IsUnderground = false;
        role.IsDigging = false;

        var ventId = 5200 + player.PlayerId * 100 + role.NextVentIndex * 2 + 1;
        var vent = Modules.BurrowerSystem.SpawnVent(player, ventId, ventPosition);
        role.NextVentIndex++;
        PlayBurrowSound(ventPosition);

        var firstVent = role.FirstVent;
        if (firstVent != null)
        {
            firstVent.Left = vent;
            firstVent.Right = vent;
            vent.Left = firstVent;
            vent.Right = firstVent;
        }

        player.RemoveModifier<BurrowerInvisibleModifier>();
        player.RemoveModifier<BurrowerSpeedModifier>();

        if (player.MyPhysics != null)
        {
            if (player.AmOwner)
            {
                role.EmergeTime = Time.time;
                player.MyPhysics.RpcExitVent(ventId);
            }
            else
            {
                try
                {
                    vent.ExitVent(player);
                }
                catch (System.Exception ex)
                {
                    UnityEngine.Debug.LogError($"[Burrower] Failed to ExitVent locally for remote client: {ex}");
                }
            }
        }

        if (player.AmOwner && BurrowerDigButton.Instance != null)
        {
            BurrowerDigButton.Instance.Timer = BurrowerDigButton.Instance.Cooldown;
        }
    }

    [MethodRpc((uint)ExtensionRpc.BurrowerCancel)]
    public static void RpcCancel(PlayerControl player)
    {
        if (!player.IsRole<BurrowerRole>())
        {
            return;
        }

        var role = player.GetRole<BurrowerRole>();
        if (role == null || (!role.IsPreparingDig && (!role.IsUnderground || !role.IsDigging)))
        {
            return;
        }

        var firstVent = role.FirstVent;
        role.IsPreparingDig = false;
        role.IsUnderground = false;
        role.IsDigging = false;
        role.PrepareDigEndTime = 0f;
        role.FirstVent = null;

        player.RemoveModifier<BurrowerInvisibleModifier>();
        player.RemoveModifier<BurrowerSpeedModifier>();

        if (player.MyPhysics != null)
        {
            if (player.AmOwner)
            {
                role.EmergeTime = Time.time;

                if (firstVent != null)
                {
                    player.MyPhysics.RpcExitVent(firstVent.Id);
                }
            }
            else
            {
                if (firstVent != null)
                {
                    try
                    {
                        firstVent.ExitVent(player);
                    }
                    catch (System.Exception ex)
                    {
                        UnityEngine.Debug.LogError($"[Burrower] Failed to ExitVent on cancel: {ex}");
                    }
                }
            }
        }

        if (player.AmOwner && BurrowerDigButton.Instance != null)
        {
            BurrowerDigButton.Instance.Timer = BurrowerDigButton.Instance.Cooldown;
        }

        Modules.BurrowerSystem.RemoveVent(firstVent);
    }

    private static void PlayBurrowSound(Vector2 position)
    {
        var localPlayer = PlayerControl.LocalPlayer;
        if (localPlayer == null || SoundManager.Instance == null)
        {
            return;
        }

        var distance = Vector2.Distance(localPlayer.GetTruePosition(), position);
        if (distance > BurrowSoundRadius)
        {
            return;
        }

        var volume = Mathf.Clamp01(1f - distance / BurrowSoundRadius) * BurrowSoundVolume;
        SoundManager.Instance.PlaySound(TouAudio.MineSound.LoadAsset(), false, volume);
    }
}
