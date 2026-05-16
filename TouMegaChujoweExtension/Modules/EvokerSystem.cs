using MiraAPI.Events;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Utilities;
using Random = UnityEngine.Random;
using TownOfUs.Extensions;
using TownOfUs.Roles;
using TownOfUs.Utilities.Appearances;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Modules;

public static class EvokerSystem
{
    public static bool IsBlindActive { get; private set; }
    public static float BlindTimeRemaining { get; private set; }
    public static byte? EvokerPlayerId { get; private set; }
    public static bool HysteriaApplied { get; private set; }

    public static Dictionary<byte, bool> VerifiedPlayers { get; } = [];

    private static readonly Dictionary<byte, int> _playerEffects = [];

    public static bool IsLocalPlayerBlocked()
    {
        if (!IsBlindActive) return false;
        var local = PlayerControl.LocalPlayer;
        return local != null && !local.HasDied() && IsBlindTarget(local);
    }

    public static void StartBlind(byte evokerPlayerId, float duration)
    {
        var evoker = MiscUtils.PlayerById(evokerPlayerId);
        if (evoker == null || evoker.HasDied()) return;

        UnityEngine.Debug.Log($"[TOUMCE] Evoker Blind STARTED for {duration}s by player {evokerPlayerId}");
        IsBlindActive = true;
        BlindTimeRemaining = duration;
        EvokerPlayerId = evokerPlayerId;
        _playerEffects.Clear();

        // Apply modifier to all blind targets
        foreach (var player in PlayerControl.AllPlayerControls.ToArray())
        {
            if (player == null || player.HasDied()) continue;
            if (!IsBlindTarget(player)) continue;

            if (!player.HasModifier<EvokerBlindedModifier>())
            {
                player.AddModifier<EvokerBlindedModifier>();
            }
        }

        if (IsBlindTarget(PlayerControl.LocalPlayer))
        {
            ApplyHysteria();
        }
    }

    public static void EndBlind()
    {
        if (!IsBlindActive)
        {
            return;
        }

        UnityEngine.Debug.Log("[TOUMCE] Evoker Blind ENDED");

        if (PlayerControl.LocalPlayer?.Data?.Role is EvokerRole)
        {
            foreach (var kvp in VerifiedPlayers)
            {
                var target = MiscUtils.PlayerById(kvp.Key);
                target?.cosmetics.SetOutline(false, new Il2CppSystem.Nullable<Color>());
            }
        }

        // Remove modifier from all players
        foreach (var player in PlayerControl.AllPlayerControls.ToArray())
        {
            if (player == null) continue;
            if (player.TryGetModifier<EvokerBlindedModifier>(out var mod))
            {
                player.RemoveModifier(mod);
            }
        }

        if (HysteriaApplied)
        {
            RemoveHysteria();
        }

        IsBlindActive = false;
        BlindTimeRemaining = 0f;
        EvokerPlayerId = null;
        _playerEffects.Clear();
    }

    public static void Update()
    {
        if (!IsBlindActive)
        {
            return;
        }

        if (EvokerPlayerId.HasValue)
        {
            var evoker = MiscUtils.PlayerById(EvokerPlayerId.Value);
            if (evoker == null || evoker.HasDied())
            {
                EndBlind();
                return;
            }
        }
        
        if (Mathf.Approximately(BlindTimeRemaining, 0f) || BlindTimeRemaining < 0f)
        {
            EndBlind();
            return;
        }

        BlindTimeRemaining -= Time.deltaTime;
        if (BlindTimeRemaining <= 0f)
        {
            EndBlind();
        }
    }

    public static bool IsBlindTarget(PlayerControl? player)
    {
        if (player == null || player.Pointer == System.IntPtr.Zero || player.Data == null)
        {
            return false;
        }

        if (player.HasDied())
        {
            return false;
        }

        var role = player.Data.Role;
        if (role == null) return false;

        if (role.IsImpostor)
        {
            return true;
        }

        if (player.Is(RoleAlignment.NeutralKilling))
        {
            return true;
        }

        if (OptionGroupSingleton<EvokerOptions>.Instance != null && 
            OptionGroupSingleton<EvokerOptions>.Instance.CrewmateKillersBlinded != null &&
            OptionGroupSingleton<EvokerOptions>.Instance.CrewmateKillersBlinded.Value &&
            player.Is(RoleAlignment.CrewmateKilling))
        {
            return true;
        }

        return false;
    }

    public static void OnRoundStart()
    {
        EndBlind();
        VerifiedPlayers.Clear();
    }

    public static void AddVerified(byte playerId, bool isKiller)
    {
        VerifiedPlayers[playerId] = isKiller;
    }

    private static void ApplyHysteria()
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || local.HasDied())
        {
            return;
        }

        var blindType = OptionGroupSingleton<EvokerOptions>.Instance.BlindType.Value;
        _playerEffects.Clear();

        var players = PlayerControl.AllPlayerControls.ToArray()
            .Where(x => !x.HasDied() && x != local).ToList();

        var localAppearance = local.GetDefaultModifiedAppearance();
        foreach (var player in players)
        {
            if (blindType == EvokerBlindType.ShowOnlySelf)
            {
                _playerEffects[player.PlayerId] = 3;
                ApplyEffectToPlayer(player, 3, localAppearance);
            }
            else
            {
                var effect = Random.RandomRangeInt(0, 3);
                _playerEffects[player.PlayerId] = effect;
                ApplyEffectToPlayer(player, effect, localAppearance);
            }
        }

        if (local.AmOwner)
        {
            var notif = Helpers.CreateAndShowNotification(
                $"<b>{Palette.ImpostorRed.ToTextColor()}You have been blinded by an Evoker!</color></b>",
                Color.white, new Vector3(0f, 1f, -20f),
                spr: TouExtensionIcons.EvokerRoleIcon.LoadAsset());
            notif.AdjustNotification();
        }

        HysteriaApplied = true;
    }

    private static void ApplyEffectToPlayer(PlayerControl player, int effect, VisualAppearance localAppearance)
    {
        switch (effect)
        {
            case 0:
                var morph = new VisualAppearance(localAppearance, TownOfUsAppearances.Morph);
                player.RawSetAppearance(morph);
                break;
            case 1:
                player.SetCamouflage();
                break;
            case 2:
                var swoop = new VisualAppearance(player.GetDefaultModifiedAppearance(), TownOfUsAppearances.Swooper)
                {
                    HatId = string.Empty,
                    SkinId = string.Empty,
                    VisorId = string.Empty,
                    PlayerName = string.Empty,
                    PetId = string.Empty,
                    RendererColor = new Color(0f, 0f, 0f, 0.1f),
                    NameColor = Color.clear,
                    ColorBlindTextColor = Color.clear
                };
                player.RawSetAppearance(swoop);
                break;
            case 3:
                var selfMorph = new VisualAppearance(localAppearance, TownOfUsAppearances.Morph);
                player.RawSetAppearance(selfMorph);
                break;
        }

        player.cosmetics.ToggleNameVisible(false);
    }

    private static void RemoveHysteria()
    {
        if (!HysteriaApplied)
        {
            return;
        }

        foreach (var player in PlayerControl.AllPlayerControls.ToArray().Where(x => !x.HasDied()))
        {
            player.RawSetAppearance(player.GetDefaultModifiedAppearance());
            player.cosmetics.ToggleNameVisible(true);
        }

        HysteriaApplied = false;
        _playerEffects.Clear();
    }
}



















