using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Roles.Neutral;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Modifiers.Neutral;

public sealed class GaslighterCursedModifier : BaseModifier
{
    public byte GaslighterId { get; set; }
    public int RoundCast { get; set; }

    public GaslighterCursedModifier(byte gaslighterId, int round)
    {
        GaslighterId = gaslighterId;
        RoundCast = round;
    }

    public override string ModifierName => TouLocale.Get("ExtensionModifierGaslighterCursed", "Gaslight Cursed");
    public override bool HideOnUi => true;
}
