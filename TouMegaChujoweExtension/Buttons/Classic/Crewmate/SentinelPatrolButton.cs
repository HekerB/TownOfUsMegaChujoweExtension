using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using TouMegaChujoweExtension.Options.Roles.Crewmate;
using TouMegaChujoweExtension.Roles.Crewmate;
using TownOfUs.Assets;
using TownOfUs.Buttons;
using TownOfUs.Modules.Localization;
using UnityEngine;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Assets;

namespace TouMegaChujoweExtension.Buttons.Crewmate;

public sealed class SentinelPatrolButton : TownOfUsRoleButton<SentinelRole>
{
    public override string Name => TouLocale.Get("ExtensionRoleSentinelPatrol", "Patrol");
    public override LoadableAsset<Sprite> Sprite => TouExtensionCrewAssets.SentinelPatrolSprite; 
    public override BaseKeybind Keybind => Keybinds.SecondaryAction;
    public override float Cooldown => OptionGroupSingleton<SentinelOptions>.Instance.Cooldown;
    
    public override bool CanUse()
    {
        return base.CanUse() && PlayerControl.LocalPlayer.Data.Role is SentinelRole;
    }

    protected override void OnClick()
    {
        if (PlayerControl.LocalPlayer.Data.Role is SentinelRole sentinel)
        {
            sentinel.PlacePatrol(PlayerControl.LocalPlayer.transform.position);
        }
    }
}
