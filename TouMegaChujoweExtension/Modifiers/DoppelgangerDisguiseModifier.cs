using TownOfUs.Modifiers;
using TownOfUs.Utilities.Appearances;

namespace TouMegaChujoweExtension.Modifiers;

public sealed class DoppelgangerDisguiseModifier(PlayerControl target) : ConcealedModifier, IVisualAppearance
{
    public override float Duration => 9999f;
    public override string ModifierName => "Doppelganger Disguise";
    public override bool HideOnUi => true;
    public override bool AutoStart => true;
    public override bool VisibleToOthers => true;
    public bool VisualPriority => true;

    public PlayerControl Target { get; private set; } = target;

    public void UpdateTarget(PlayerControl newTarget)
    {
        Target = newTarget;
        Player.RawSetAppearance(this);
    }

    public VisualAppearance GetVisualAppearance()
    {
        return new VisualAppearance(Target.GetDefaultModifiedAppearance(), TownOfUsAppearances.Mimic);
    }

    public override void OnActivate()
    {
        Player.RawSetAppearance(this);
    }

    public override void OnDeath(DeathReason reason)
    {
        ModifierComponent!.RemoveModifier(this);
    }

    public override void OnDeactivate()
    {
        Player.ResetAppearance();
    }
}
