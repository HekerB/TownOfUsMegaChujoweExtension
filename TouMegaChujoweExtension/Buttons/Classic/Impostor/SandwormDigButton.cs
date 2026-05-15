using System;
using System.Linq;
using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TouMegaChujoweExtension.Roles.Impostor;
using TouMegaChujoweExtension.Assets;
using TownOfUs.Buttons;
using TownOfUs.Extensions;
using TownOfUs.Utilities;
using TownOfUs.Assets;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Impostor;

public sealed class SandwormButton : TownOfUsRoleButton<SandwormRole>
{
    public override string Name
    {
        get
        {
            var role = PlayerControl.LocalPlayer.GetRole<SandwormRole>();
            if (role == null) return "Dig";
            if (role.IsUnderground) return "Emerge";
            return "Dig";
        }
    }

    public override BaseKeybind Keybind => Keybinds.PrimaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Sandworm;
    public override float Cooldown => OptionGroupSingleton<SandwormOptions>.Instance.KillCooldown;
    public override LoadableAsset<Sprite> Sprite => TouRoleIcons.Miner;

    public override bool CanUse()
    {
        if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.HasDied()) return false;
        
        var role = PlayerControl.LocalPlayer.GetRole<SandwormRole>();
        if (role == null) return false;
        
        // While underground or digging, button is always usable (to transition or emerge)
        if (role.IsDigging || role.IsUnderground) return true;

        return Timer <= 0f;
    }

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        var role = playerControl.GetRole<SandwormRole>();
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
        var player = PlayerControl.LocalPlayer;
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
            // Enter Ground phase
            SandwormRole.RpcUnderground(player, player.GetTruePosition());
        }
    }
}
