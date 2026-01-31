using MiraAPI.Hud;
using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using TouMiraRolesExtension.Assets;
using TouMiraRolesExtension.Modules;
using TouMiraRolesExtension.Options.Roles.Impostor;
using TouMiraRolesExtension.Roles.Impostor;
using TownOfUs.Assets;
using TownOfUs.Buttons;
using TownOfUs.Modules;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TouMiraRolesExtension.Buttons.Impostor;

public sealed class CharlatanDeceiveButton : TownOfUsRoleButton<CharlatanRole, DeadBody>
{
    public override string Name => TouLocale.GetParsed("ExtensionRoleCharlatanDeceive", "Deceive");
    public override BaseKeybind Keybind => Keybinds.SecondaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Charlatan;
    public override float Cooldown => 0.01f;
    public override LoadableAsset<Sprite> Sprite => TouExtensionImpAssets.DeceiveButtonSprite;
    public override float Distance => float.MaxValue;

    public override bool ZeroIsInfinite { get; set; } = true;

    public override DeadBody? GetTarget()
    {
        if (PlayerControl.LocalPlayer == null)
        {
            return null;
        }

        var charlatan = PlayerControl.LocalPlayer;
        if (charlatan.Data?.Role is not CharlatanRole)
        {
            return null;
        }

        var allBodies = Object.FindObjectsOfType<DeadBody>();
        foreach (var body in allBodies)
        {
            if (CharlatanDeceiveSystem.CanDeceiveReport(charlatan.PlayerId, body.ParentId))
            {
                return body;
            }
        }

        return null;
    }

    public override bool IsTargetValid(DeadBody? target)
    {
        if (target == null || PlayerControl.LocalPlayer == null)
        {
            return false;
        }

        var charlatan = PlayerControl.LocalPlayer;
        if (charlatan.Data?.Role is not CharlatanRole)
        {
            return false;
        }

        return CharlatanDeceiveSystem.CanDeceiveReport(charlatan.PlayerId, target.ParentId);
    }

    public override void ClickHandler()
    {
        if (!CanClick())
        {
            return;
        }

        if (Target == null)
        {
            return;
        }

        var player = PlayerControl.LocalPlayer;
        if (player == null)
        {
            return;
        }

        var bodyPlayer = MiscUtils.PlayerById(Target.ParentId);
        if (bodyPlayer != null)
        {
            player.CmdReportDeadBody(bodyPlayer.Data);
        }
        Button?.SetDisabled();
    }

    protected override void OnClick()
    {
        ClickHandler();
    }
}
