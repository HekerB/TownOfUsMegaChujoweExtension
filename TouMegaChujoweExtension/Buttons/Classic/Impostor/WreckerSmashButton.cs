using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Roles.Classic.Impostor;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TownOfUs.Buttons;
using TownOfUs.Modules.Localization;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Classic.Impostor;

/// <summary>
/// Lights up while the Wrecker stands at a wall that leads somewhere, and smashes a hole through it.
/// </summary>
public sealed class WreckerSmashButton : TownOfUsRoleButton<WreckerRole>
{
    private const float CheckInterval = 0.2f;

    private float _nextCheck;
    private bool _hasBreach;
    private WallBreach _breach;

    public override string Name => TouLocale.GetParsed("ExtensionRoleWreckerSmash", "Smash");
    public override BaseKeybind Keybind => Keybinds.SecondaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Wrecker;
    public override float Cooldown => Math.Clamp(OptionGroupSingleton<WreckerOptions>.Instance.SmashCooldown + MapCooldown, 5f, 120f);
    public override int MaxUses => (int)OptionGroupSingleton<WreckerOptions>.Instance.MaxSmashes;
    public override bool ZeroIsInfinite { get; set; } = true;
    public override LoadableAsset<Sprite> Sprite => TouImpAssets.MineSprite;

    public override bool CanUse()
    {
        // No point looking for a wall while the smash is still cooling down.
        if (!base.CanUse() || Timer > 0f)
        {
            return false;
        }

        var local = PlayerControl.LocalPlayer;
        if (local == null || local.inVent || local.walkingToVent || local.onLadder || local.inMovingPlat ||
            Minigame.Instance != null)
        {
            return false;
        }

        if (Time.time >= _nextCheck)
        {
            _nextCheck = Time.time + CheckInterval;
            _hasBreach = FindBreach(local);
        }

        return _hasBreach;
    }

    public override void ClickHandler()
    {
        // Look again at the moment of the click, so the hole opens where the Wrecker stands now.
        var local = PlayerControl.LocalPlayer;
        if (local != null)
        {
            _hasBreach = FindBreach(local);
            _nextCheck = Time.time + CheckInterval;
        }

        base.ClickHandler();
    }

    protected override void OnClick()
    {
        if (!_hasBreach)
        {
            return;
        }

        WreckerRole.RpcSmash(PlayerControl.LocalPlayer, _breach.Origin, _breach.Direction, _breach.Depth);
        _hasBreach = false;
        _nextCheck = Time.time + 0.5f;
    }

    private bool FindBreach(PlayerControl local)
    {
        var maxThickness = OptionGroupSingleton<WreckerOptions>.Instance.MaxWallThickness;
        return WallBreakSystem.TryFindBreach(local, maxThickness, out _breach);
    }
}
