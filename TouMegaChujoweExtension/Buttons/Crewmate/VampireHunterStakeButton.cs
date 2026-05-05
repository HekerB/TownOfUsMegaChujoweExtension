using System.Collections.Generic;
using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Networking;
using MiraAPI.Utilities;
using MiraAPI.Utilities.Assets;
using MiraAPI.Modifiers;
using Reactor.Utilities;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Options.Roles.Crewmate;
using TouMegaChujoweExtension.Roles.Crewmate;
using TownOfUs.Buttons;
using TownOfUs.Modules.Localization;
using TownOfUs.Roles.Neutral;
using TownOfUs.Utilities;
using TownOfUs.Events;
using TownOfUs.Options;
using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using Il2CppInterop.Runtime;
using UnityEngine;
using TouMegaChujoweExtension.Utilities;

namespace TouMegaChujoweExtension.Buttons.Crewmate;

public sealed class StakeButton : TownOfUsRoleButton<VampireHunterRole>, IKillButton

{
    private PlayerControl? _target;
    private PlayerControl? _lastOutlined;
    private static bool _firstRound = true;
    public static List<byte> HunterKilledVictims = new();

    public override string Name => TouLocale.Get("ExtensionRoleVampireHunterStake", "Stake");
    public override BaseKeybind Keybind => Keybinds.PrimaryAction;
    public override Color TextOutlineColor => TouExtensionColors.VampireHunter;
    public override LoadableAsset<Sprite> Sprite => TouExtensionCrewAssets.StakeButtonIcon;

    public override float Cooldown =>
        Math.Clamp(OptionGroupSingleton<VampireHunterOptions>.Instance.StakeCooldown + MapCooldown, 5f, 120f);

    public override int MaxUses => (int)OptionGroupSingleton<VampireHunterOptions>.Instance.MaxFailedStakes;
    public override bool ZeroIsInfinite { get; set; } = true;

    public override bool CanUse()
    {
        if (!base.CanUse()) return false;
        if (_firstRound && !OptionGroupSingleton<VampireHunterOptions>.Instance.CanStakeRoundOne) return false;
        if (MaxUses > 0 && UsesLeft <= 0) return false;

        return _target != null;
    }

    public override void ClickHandler()
    {
        if (!CanClick()) return;
        if (_target == null) return;

        var player = PlayerControl.LocalPlayer;
        if (player == null) return;

        // Use centralized shield interaction handler
        if (ShieldUtils.HandleButtonShieldClick(this, _target))
        {
            return;
        }

        OnClick();
        Timer = Cooldown;
    }

    protected override void OnClick()
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null || _target == null) return;

        var role = player.Data.Role as VampireHunterRole;
        if (role == null) return;

        var isVampire = _target.Data.Role is VampireRole;

        if (isVampire)
        {
            player.RpcCustomMurder(_target);
            role.SuccessfulStakes++;

            if (!OptionGroupSingleton<VampireHunterOptions>.Instance.CanSelfReport)
            {
                HunterKilledVictims.Add(_target.PlayerId);
            }
        }
        else
        {
            role.FailedStakes++;
            
            if (MaxUses > 0)
            {
                UsesLeft--;
                SetUses(UsesLeft);
            }

            Coroutines.Start(MiscUtils.CoFlash(new Color(1f, 0f, 0f, 0.3f)));

            Coroutines.Start(CheckSelfKill(player, role));
        }
    }

    private static System.Collections.IEnumerator CheckSelfKill(PlayerControl player, VampireHunterRole role)
    {
        yield return new WaitForSeconds(0.3f);
        if (player == null || player.HasDied()) yield break;

        if (!role.HasStakesLeft &&
            role.SuccessfulStakes == 0 &&
            OptionGroupSingleton<VampireHunterOptions>.Instance.SelfKillOnFailure)
        {
            player.RpcCustomMurder(player);
        }
    }

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        base.FixedUpdate(playerControl);

        if (MeetingHud.Instance)
        {
            ClearOutline();
            _target = null;
            return;
        }

        Button?.gameObject.SetActive(
            HudManager.Instance.UseButton.isActiveAndEnabled ||
            HudManager.Instance.PetButton.isActiveAndEnabled);

        if (Button == null) return;

        ClearOutline();
        _target = playerControl.GetClosestLivingPlayer(true, 1.5f, false);

        if (Button != null)
        {
            if (CanUse())
            {
                Button.SetEnabled();
            }
            else
            {
                Button.SetDisabled();
            }
        }

        if (_target != null && !_target.HasDied())
        {
            _target.cosmetics.SetOutline(true,
                new Il2CppSystem.Nullable<Color>(TouExtensionColors.VampireHunter));
            _lastOutlined = _target;
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

    public static void ResetFirstRound() => _firstRound = true;
    public static void EndFirstRound() => _firstRound = false;
}
