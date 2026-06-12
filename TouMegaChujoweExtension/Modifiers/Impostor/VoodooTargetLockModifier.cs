using MiraAPI.Modifiers;

namespace TouMegaChujoweExtension.Modifiers.Impostor;

public sealed class VoodooTargetLockModifier(byte targetId, int meetingsRemaining) : BaseModifier
{
    public byte TargetId { get; } = targetId;
    public int MeetingsRemaining { get; set; } = meetingsRemaining;
    public override string ModifierName => "Voodoo Target Lock";
    public override bool HideOnUi => true;
}
