using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Keybinds;
using MiraAPI.Modifiers;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Modifiers;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Roles.Neutral;
using TownOfUs.Buttons;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Neutral;

public sealed class PopeCanonizeButton : TownOfUsRoleButton<PopeRole, PlayerControl>
{
    public override string Name => TouLocale.GetParsed("ExtensionRolePopeCanonize", "Canonize");
    public override BaseKeybind Keybind => Keybinds.SecondaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Pope;
    public override float Cooldown => Math.Clamp(OptionGroupSingleton<PopeOptions>.Instance.CanonizeCooldown + MapCooldown, 5f, 120f);
    public override int MaxUses => (int)OptionGroupSingleton<PopeOptions>.Instance.MaxCanonizations;
    public override LoadableAsset<Sprite> Sprite => TouExtensionNeuAssets.PopeCanonizeButtonSprite;

    public override bool Enabled(RoleBehaviour? role)
    {
        return base.Enabled(role) && !PopeRole.EveryoneCanonized();
    }

    public override PlayerControl? GetTarget()
    {
        return PlayerControl.AllPlayerControls.ToArray()
            .Where(x => !x.Data.IsDead
                      && x.PlayerId != PlayerControl.LocalPlayer.PlayerId
                      && !x.HasModifier<PopeCanonizedModifier>()
                      && Vector2.Distance(PlayerControl.LocalPlayer.GetTruePosition(), x.GetTruePosition()) <= Distance)
            .OrderBy(x => Vector2.Distance(PlayerControl.LocalPlayer.GetTruePosition(), x.GetTruePosition()))
            .FirstOrDefault();
    }

    protected override void OnClick()
    {
        if (Target == null) return;

        Target.RpcAddModifier<PopeCanonizedModifier>(PlayerControl.LocalPlayer);
    }
}
