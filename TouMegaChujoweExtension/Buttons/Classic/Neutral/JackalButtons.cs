using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Modifiers;
using MiraAPI.Networking;
using MiraAPI.Utilities.Assets;
using Reactor.Utilities;
using System;
using System.Linq;
using TouMegaChujoweExtension.Modifiers.Neutral;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using TownOfUs.Assets;
using TownOfUs.Buttons;
using TownOfUs.Extensions;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Classic.Neutral;

public sealed class JackalKillButton : TownOfUsKillRoleButton<JackalRole, PlayerControl>, IDiseaseableButton, IKillButton
{
    public override string Name => TouLocale.Get("ExtensionRoleJackalAssassination", "Assassinate");
    public override BaseKeybind Keybind => Keybinds.PrimaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Jackal;
    public override LoadableAsset<Sprite> Sprite => TouNeutAssets.PestKillSprite;

    public override float Cooldown => Math.Clamp(
        OptionGroupSingleton<JackalOptions>.Instance.KillCooldown + MapCooldown, 5f, 120f);

    public override float Distance
    {
        get
        {
            return OptionGroupSingleton<JackalOptions>.Instance.KillDistance switch
            {
                0 => 1.25f,
                1 => 1.75f,
                2 => 2.5f,
                _ => 1.75f
            };
        }
    }

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);
        Coroutines.Start(MiscUtils.CoMoveButtonIndex(this, false));
    }

    public void SetDiseasedTimer(float multiplier)
    {
        SetTimer(Cooldown * multiplier);
    }

    public override bool CanUse()
    {
        return base.CanUse() && !AreSidekicksAlive();
    }

    protected override void OnClick()
    {
        if (Target == null)
        {
            Error("Jackal Kill: Target is null");
            return;
        }

        PlayerControl.LocalPlayer.RpcCustomMurder(Target, MeetingCheck.OutsideMeeting);
    }

    public override PlayerControl? GetTarget()
    {
        if (AreSidekicksAlive()) return null;

        return PlayerControl.LocalPlayer.GetClosestLivingPlayer(false, Distance, false, x => IsTargetValid(x));
    }

    public override bool IsTargetValid(PlayerControl? target)
    {
        if (!base.IsTargetValid(target) || target == null) return false;

        var local = PlayerControl.LocalPlayer;
        if (local == null) return false;

        if (target.TryGetModifier<SidekickModifier>(out var mod) && mod != null && mod.JackalId == local.PlayerId)
            return false;

        return true;
    }


    protected override void FixedUpdate(PlayerControl playerControl)
    {
        base.FixedUpdate(playerControl);

        if (Button == null) return;

        if (AreSidekicksAlive())
        {
            Button.gameObject.SetActive(false);
        }

        OverrideName(TouLocale.Get("ExtensionRoleJackalAssassination", "Assassinate"));
    }

    private static bool AreSidekicksAlive()
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null) return false;

        return PlayerControl.AllPlayerControls.ToArray()
            .Any(p => p != null && p.Pointer != IntPtr.Zero && p.Data != null && !p.Data.IsDead &&
                 p.TryGetModifier<SidekickModifier>(out var m) && m != null && m.JackalId == local.PlayerId);
    }
}
