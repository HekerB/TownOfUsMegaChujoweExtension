using MiraAPI.Modifiers;
using UnityEngine;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Assets;
using TownOfUs.Modules.Localization;
using TownOfUs.Assets;

namespace TouMegaChujoweExtension.Modifiers.Neutral;

public sealed class JackalShieldModifier : BaseModifier
{
    public override string ModifierName => TouLocale.Get("ExtensionJackalShieldModifierTitle");
    public override bool HideOnUi => false;
    public override LoadableAsset<Sprite> ModifierIcon => TouRoleIcons.Jackal;


    // Use property instead of override if BaseModifier doesn't have it
    public string LocaleKey => "JackalShield";
    
    // BaseModifier usually uses GetDescription() for UI
    public override string GetDescription() => TouLocale.Get("ExtensionJackalShieldModifierDesc");

    public override Color FreeplayFileColor => TouExtensionColors.Jackal;
}
