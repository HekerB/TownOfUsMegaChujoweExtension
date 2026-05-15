using MiraAPI.Modifiers;
using MiraAPI.Modifiers.Types;
using TownOfUs.Utilities.Appearances;

namespace TouMegaChujoweExtension.Modifiers.Impostor;

public sealed class ZapperZapModifier : TimedModifier
{
    public override string ModifierName => "Electrocuted";
    public override bool HideOnUi => true;
    public override float Duration => 1.2f;

    public override void OnActivate()
    {
        base.OnActivate();
        ResumeTimer();
    }

    public override void OnDeactivate()
    {
        base.OnDeactivate();
        Player?.ResetAppearance();
    }
}
