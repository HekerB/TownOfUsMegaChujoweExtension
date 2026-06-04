using AmongUs.GameOptions;
using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using MiraAPI.Utilities;
using Reactor.Utilities;
using TownOfUs.Assets;
using TownOfUs.Buttons;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;
using System;
using TouMegaChujoweExtension.Roles.Classic.Crewmate;
using TouMegaChujoweExtension.Options.Roles.Crewmate;
using TouMegaChujoweExtension.Modules;

namespace TouMegaChujoweExtension.Buttons.Classic.Crewmate;

public sealed class EvokerVerifyButton : TownOfUsRoleButton<EvokerRole>
{
    private PlayerControl? _verifyTarget;
    private PlayerControl? _lastOutlined;
    private bool _wasActive;

    public override string Name => TouLocale.Get("ExtensionRoleEvokerVerify", "Verify");
    public override BaseKeybind Keybind => Keybinds.PrimaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Evoker;
    public override LoadableAsset<Sprite> Sprite => TouNeutAssets.InquireSprite;

    public override float Cooldown => Math.Clamp(OptionGroupSingleton<EvokerOptions>.Instance.VerifyCooldown.Value + MapCooldown, 1f, 60f);
    public override float InitialCooldown => 0.001f;

    public override bool Enabled(RoleBehaviour? role)
    {
        return base.Enabled(role) && !OptionGroupSingleton<EvokerOptions>.Instance.CantVerify.Value;
    }

    public override bool CanUse()
    {
        if (!base.CanUse()) return false;

        var player = PlayerControl.LocalPlayer;
        if (player == null || player.HasDied()) return false;

        if (!EvokerSystem.IsBlindActive) return false;

        if (OptionGroupSingleton<EvokerOptions>.Instance.CantVerify.Value) return false;

        var max = (int)OptionGroupSingleton<EvokerOptions>.Instance.MaxVerifications.Value;
        if (max > 0 && Role.VerifiesUsed >= max) return false;

        return _verifyTarget != null;
    }

    protected override void OnClick()
    {
        var player = PlayerControl.LocalPlayer;
        var target = _verifyTarget;
        if (player == null || target == null) return;

        var isKiller = EvokerSystem.IsBlindTarget(target);
        var name = target.Data.PlayerName;

        var color = isKiller ? Palette.ImpostorRed : Palette.CrewmateBlue;
        var text = isKiller
            ? $"<b>{Palette.ImpostorRed.ToTextColor()}{name} is a Killing role!</color></b>"
            : $"<b>{Palette.CrewmateBlue.ToTextColor()}{name} is NOT a Killing role.</color></b>";

        Coroutines.Start(MiscUtils.CoFlash(color));
        var notif = Helpers.CreateAndShowNotification(text, Color.white,
            new Vector3(0f, 1f, -20f), spr: TouExtensionIcons.EvokerRoleIcon.LoadAsset());
        notif.AdjustNotification();

        Role.VerifiesUsed++;
        var max = (int)OptionGroupSingleton<EvokerOptions>.Instance.MaxVerifications.Value;
        if (max > 0)
        {
            var remaining = Mathf.Max(0, max - Role.VerifiesUsed);
            Button?.SetUsesRemaining(remaining);

            if (remaining <= 0)
            {
                _verifyTarget = null;
                ClearOutline();
            }
        }

        var record = isKiller
            ? $"<b>{Palette.ImpostorRed.ToTextColor()}{name} - Killer</color></b>"
            : $"<b>{Palette.CrewmateBlue.ToTextColor()}{name} - Safe</color></b>";
        Role.VerifiedRecords.Add(record);

        EvokerSystem.AddVerified(target.PlayerId, isKiller);
        EvokerRole.RpcEvokerVerify(player, target.PlayerId);
    }

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        if (MeetingHud.Instance)
        {
            ClearOutline();
            _verifyTarget = null;
            return;
        }

        var button = Button;
        if (button == null) return;

        var isActive = EvokerSystem.IsBlindActive;
        if (isActive && !_wasActive)
        {
            Timer = 0f;
            _wasActive = true;
        }
        else if (!isActive)
        {
            _wasActive = false;
        }

        var max = (int)OptionGroupSingleton<EvokerOptions>.Instance.MaxVerifications.Value;
        if (max > 0)
        {
            button.usesRemainingText?.gameObject.SetActive(true);
            button.usesRemainingSprite?.gameObject.SetActive(true);
            button.SetUsesRemaining(Mathf.Max(0, max - Role.VerifiesUsed));
        }
        else
        {
            button.usesRemainingText?.gameObject.SetActive(false);
            button.usesRemainingSprite?.gameObject.SetActive(false);
        }

        if (isActive && !OptionGroupSingleton<EvokerOptions>.Instance.CantVerify.Value && (max <= 0 || Role.VerifiesUsed < max))
        {
            ClearOutline();
            _verifyTarget = playerControl.GetClosestLivingPlayer(true, 1.5f, false,
                p => !EvokerSystem.VerifiedPlayers.ContainsKey(p.PlayerId));

            if (_verifyTarget != null && !_verifyTarget.HasDied())
            {
                _verifyTarget.cosmetics.SetOutline(true,
                    new Il2CppSystem.Nullable<Color>(TouExtensionColors.Evoker));
                _lastOutlined = _verifyTarget;
            }
        }
        else
        {
            _verifyTarget = null;
            ClearOutline();
        }
    }

    private void ClearOutline()
    {
        if (_lastOutlined != null)
        {
            _lastOutlined.cosmetics.SetOutline(false, new Il2CppSystem.Nullable<Color>());
            _lastOutlined = null;
        }
    }

    public void OnDestroy()
    {
        ClearOutline();
    }
}
