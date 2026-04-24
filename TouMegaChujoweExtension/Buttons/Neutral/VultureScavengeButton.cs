using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Roles.Neutral;
using TownOfUs.Buttons;
using TownOfUs.Modules.Localization;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Neutral;

public sealed class VultureScavengeButton : TownOfUsRoleButton<VultureRole>
{
    public override string Name => TouLocale.GetParsed("ExtensionRoleVultureScavenge", "Scavenge");
    public override BaseKeybind Keybind => Keybinds.ModifierAction;
    public override Color TextOutlineColor => TouExtensionColors.Vulture;
    public override float Cooldown => Math.Clamp(OptionGroupSingleton<VultureOptions>.Instance.ScavengeCooldown.Value + MapCooldown, 5f, 120f);
    public override float EffectDuration => OptionGroupSingleton<VultureOptions>.Instance.ScavengeDuration.Value;
    public override LoadableAsset<Sprite> Sprite => TouExtensionNeuAssets.VultureScavengeButtonSprite;

    public override bool ZeroIsInfinite { get; set; } = true;

    public override bool Enabled(RoleBehaviour? role)
    {
        return base.Enabled(role) && OptionGroupSingleton<VultureOptions>.Instance.ScavengeEnabled;
    }

    protected override void OnClick()
    {
        if (PlayerControl.LocalPlayer == null)
        {
            return;
        }

        VultureRole.RpcVultureScavenge(PlayerControl.LocalPlayer);
        ResetCooldownAndOrEffect();
    }
}
