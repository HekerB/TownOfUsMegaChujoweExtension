using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using System.Globalization;
using System;
using TownOfUs.Buttons;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Classic.Impostor;

public sealed class HackerJamButton : TownOfUsRoleButton<HackerRole>
{
    public override string Name => TouLocale.GetParsed("ExtensionRoleHackerJam", "Jam");
    public override BaseKeybind Keybind => OptionGroupSingleton<HackerOptions>.Instance.SimpleModeJamOnly
        ? Keybinds.SecondaryAction
        : Keybinds.ModifierAction;
    public override Color TextOutlineColor => TouExtensionColors.Hacker;
    public override float Cooldown
    {
        get
        {
            return Math.Clamp(OptionGroupSingleton<HackerOptions>.Instance.JamCooldownSeconds + MapCooldown, 5f, 120f);
        }
    }
    public override float EffectDuration => OptionGroupSingleton<HackerOptions>.Instance.JamDurationSeconds;
    public override LoadableAsset<Sprite> Sprite => TouExtensionImpAssets.HackerJamButtonSprite;
    public override bool ZeroIsInfinite { get; set; } = true;



    public override bool Enabled(RoleBehaviour? role)
    {
        return base.Enabled(role) && OptionGroupSingleton<HackerOptions>.Instance.JamEnabled;
    }

    public override bool CanUse()
    {
        if (!base.CanUse())
        {
            return false;
        }

        if (HackerSystem.IsJammed)
        {
            return false;
        }

        var player = PlayerControl.LocalPlayer;
        if (player == null)
        {
            return false;
        }

        return Timer <= 0f && HackerSystem.GetJamCharges(player.PlayerId) > 0;
    }

    private bool _chargesInitialized;

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        base.FixedUpdate(playerControl);

        if (Button == null || PlayerControl.LocalPlayer == null)
        {
            return;
        }

        if (!_chargesInitialized)
        {
            var opts = OptionGroupSingleton<HackerOptions>.Instance;
            if (opts.JamEnabled)
            {
                var initialVal = (int)opts.InitialJamCharges;
                byte initial = initialVal >= 11 ? (byte)255 : (byte)Mathf.Clamp(initialVal, 0, 10);
                HackerSystem.SetJamCharges(PlayerControl.LocalPlayer.PlayerId, initial);
            }
            _chargesInitialized = true;
        }

        var charges = HackerSystem.GetJamCharges(PlayerControl.LocalPlayer.PlayerId);
        if (charges == 255)
        {
            Button.usesRemainingText.gameObject.SetActive(false);
            Button.usesRemainingSprite.gameObject.SetActive(false);
        }
        else
        {
            Button.usesRemainingText.gameObject.SetActive(true);
            Button.usesRemainingSprite.gameObject.SetActive(true);
            Button.usesRemainingText.text = charges.ToString(CultureInfo.InvariantCulture);
        }

        if (EffectActive && !HackerSystem.IsJammed)
        {
            EffectActive = false;
            Timer = Cooldown;
        }
    }

    protected override void OnClick()
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null)
        {
            return;
        }

        HackerRole.RpcHackerActivateJam(player);
    }
}
















