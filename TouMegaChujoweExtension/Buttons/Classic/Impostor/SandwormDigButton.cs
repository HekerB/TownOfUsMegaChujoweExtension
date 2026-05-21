using System;
using System.Linq;
using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TouMegaChujoweExtension.Roles.Classic.Impostor;
using TouMegaChujoweExtension.Assets;
using TownOfUs.Buttons;
using TownOfUs.Extensions;
using TownOfUs.Utilities;
using TownOfUs.Assets;
using UnityEngine;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Buttons.Classic.Impostor;

public sealed class SandwormDigButton : TownOfUsRoleButton<SandwormRole>
{
    public static SandwormDigButton? Instance { get; private set; }

    private bool _isNearWall;
    private bool _isNearVent;
    private float _wallCheckTimer;
    private const float WallDetectRadius = 0.3f;

    public SandwormDigButton()
    {
        Instance = this;
    }
    public override string Name
    {
        get
        {
            if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data == null) return TouLocale.Get("ExtensionRoleSandwormDig", "Dig");
            var role = PlayerControl.LocalPlayer.GetRole<SandwormRole>();
            if (role == null) return TouLocale.Get("ExtensionRoleSandwormDig", "Dig");
            if (role.IsUnderground) return TouLocale.Get("ExtensionRoleSandwormEmerge", "Emerge");
            return TouLocale.Get("ExtensionRoleSandwormDig", "Dig");
        }
    }

    public override BaseKeybind Keybind => Keybinds.SecondaryAction; // This is F
    public override Color TextOutlineColor => TouExtensionColors.Sandworm;
    public override float Cooldown => OptionGroupSingleton<SandwormOptions>.Instance.DigCooldown;
    public override LoadableAsset<Sprite> Sprite => TouExtensionImpAssets.SandwormDigButtonSprite;
    public override int MaxUses => 0;

    public override bool CanUse()
    {
        if (MeetingHud.Instance || ExileController.Instance) return false;
        if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.HasDied()) return false;
        
        var role = PlayerControl.LocalPlayer.GetRole<SandwormRole>();
        if (role == null) return false;

        if (!role.IsUnderground && !role.IsDigging)
        {
            var pos = (Vector2)PlayerControl.LocalPlayer.transform.position;
            if (IsNearWall(pos) || IsNearVent(pos)) return false;
        }

        return role.IsDigging || role.IsUnderground || (Timer <= 0f);
    }

    public override bool CanClick()
    {
        if (PlayerControl.LocalPlayer != null &&
            (PlayerControl.LocalPlayer.inVent || PlayerControl.LocalPlayer.walkingToVent))
            return false;

        var role = PlayerControl.LocalPlayer?.GetRole<SandwormRole>();
        if (role != null && !role.IsUnderground && !role.IsDigging)
        {
            var pos = (Vector2)PlayerControl.LocalPlayer.transform.position;
            if (IsNearWall(pos) || IsNearVent(pos)) return false;
        }

        return CanUse();
    }

    private bool _isProcessingClick;

    public override void ClickHandler()
    {
        Info($"[Sandworm] ClickHandler! CanUse: {CanUse()}");
        if (_isProcessingClick) return;
        _isProcessingClick = true;

        try
        {
            if (!CanUse()) return;
            OnClick();
        }
        finally
        {
            Reactor.Utilities.Coroutines.Start(ResetProcessingFlag());
        }
    }

    private System.Collections.IEnumerator ResetProcessingFlag()
    {
        yield return new WaitForSeconds(0.2f);
        _isProcessingClick = false;
    }

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        if (MeetingHud.Instance || ExileController.Instance) return;

        var role = playerControl.GetRole<SandwormRole>();

        if (Button != null)
        {
            if (Button.usesRemainingText != null) Button.usesRemainingText.gameObject.SetActive(false);
            if (Button.usesRemainingSprite != null) Button.usesRemainingSprite.gameObject.SetActive(false);

            var isUnderground = role != null && role.IsUnderground;
            OverrideName(isUnderground 
                ? TouLocale.Get("ExtensionRoleSandwormEmerge", "Emerge") 
                : TouLocale.Get("ExtensionRoleSandwormDig", "Dig"));
        }

        if (role != null && role.IsDigging)
        {
            // Show duration timer
            float remaining = Mathf.Max(0f, role.DigEndTime - Time.time);
            Button?.SetFillUp(remaining, OptionGroupSingleton<SandwormOptions>.Instance.DigDuration);
            if (Button != null && Button.cooldownTimerText != null)
            {
                Button.cooldownTimerText.text = Mathf.Ceil(remaining).ToString();
                Button.cooldownTimerText.gameObject.SetActive(true);
            }
            return;
        }

        base.FixedUpdate(playerControl);
    }

    protected override void OnClick()
    {
        Info($"[Sandworm] Button clicked! Cooldown: {Timer}");
        var player = PlayerControl.LocalPlayer;
        if (player == null) return;
        var role = player.GetRole<SandwormRole>();
        if (role == null) return;

        if (role.IsUnderground)
        {
            // Manual emergence
            SandwormRole.RpcEmerge(player, player.GetTruePosition());
            Timer = Cooldown;
        }
        else
        {
            var pos = (Vector2)player.transform.position;
            if (IsNearWall(pos) || IsNearVent(pos)) return;

            // Go underground immediately (places first vent and hops in)
            SandwormRole.RpcUnderground(player, player.GetTruePosition());
        }
    }

    private static bool IsNearWall(Vector2 pos)
    {
        return false;
    }

    private static bool IsSandwormVent(Transform t)
    {
        while (t != null)
        {
            if (t.gameObject.name != null && t.gameObject.name.StartsWith("SandwormVent-"))
                return true;
            t = t.parent;
        }
        return false;
    }

    private static bool IsNearVent(Vector2 pos)
    {
        if (ShipStatus.Instance != null && ShipStatus.Instance.AllVents != null)
        {
            foreach (var vent in ShipStatus.Instance.AllVents)
            {
                if (vent == null) continue;
                if (IsSandwormVent(vent.transform)) continue;

                var dist = Vector2.Distance(pos, vent.transform.position);
                if (dist < 1.8f)
                {
                    return true;
                }
            }
        }

        var colliders = Physics2D.OverlapCircleAll(pos, 1.8f);
        foreach (var c in colliders)
        {
            if (c != null && c.name != null && c.name.Contains("Vent", StringComparison.OrdinalIgnoreCase))
            {
                if (IsSandwormVent(c.transform)) continue;
                return true;
            }
        }

        return false;
    }
}
