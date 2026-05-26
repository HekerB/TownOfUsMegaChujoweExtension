using MiraAPI.Modifiers;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Modifiers.Neutral;

public sealed class BakerBreadModifier : BaseModifier
{
    public override string ModifierName => TouLocale.Get("ExtensionModifierBakerBread", "Has Bread");
    public override bool HideOnUi => true;
}
