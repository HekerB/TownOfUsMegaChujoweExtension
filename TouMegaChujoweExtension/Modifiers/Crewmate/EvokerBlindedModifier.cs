using TownOfUs.Modifiers;

namespace TouMegaChujoweExtension.Modifiers.Crewmate;

public sealed class EvokerBlindedModifier : DisabledModifier
{
    public override string ModifierName => "Evoker Blinded";
    public override bool HideOnUi => true;
    public override bool AutoStart => false;

    public override bool CanUseAbilities => false;
    
    public override bool CanUseConsoles => true;
    public override bool CanOpenMap => true;
    
    public override bool CanReport => true;
    public override bool CanBeInteractedWith => true;
    public override bool IsConsideredAlive => true;

    public override float Duration => 0f; // managed by EvokerSystem
}












