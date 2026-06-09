using MiraAPI.Modifiers;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Modifiers.Neutral;

public sealed class FamineStarvedModifier : BaseModifier
{
    public override string ModifierName => TouLocale.Get("ExtensionModifierFamineStarved", "Starving");
    public override bool HideOnUi => true;
}
