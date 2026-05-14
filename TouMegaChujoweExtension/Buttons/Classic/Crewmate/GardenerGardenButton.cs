using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Options.Roles.Crewmate;
namespace TouMegaChujoweExtension.Buttons.Classic.Crewmate;

using TouMegaChujoweExtension.Roles.Classic.Crewmate;
using TownOfUs.Assets;
using TownOfUs.Buttons;
using TownOfUs.Modules.Localization;
using UnityEngine;

public sealed class GardenerGardenButton : TownOfUsRoleButton<GardenerRole>
{
    public override string Name => TouLocale.Get("ExtensionRoleGardenerGarden", "Garden");
    public override LoadableAsset<Sprite> Sprite => TouRoleIcons.Traitor;
    public override BaseKeybind Keybind => Keybinds.SecondaryAction;
    public override float Cooldown => OptionGroupSingleton<GardenerOptions>.Instance.Cooldown;

    public override bool CanUse()
    {
        return base.CanUse() && Role != null;
    }

    protected override void OnClick()
    {
        if (Role == null) return;

        Role.PlaceGarden(PlayerControl.LocalPlayer.GetTruePosition());
    }
}
