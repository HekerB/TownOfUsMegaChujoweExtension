using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Roles.Classic.Impostor;

namespace TouMegaChujoweExtension.Modifiers.Impostor;

public sealed class VoodooScheduledCurseModifier(VoodooEffect curseType) : BaseModifier
{
    public VoodooEffect CurseType { get; } = curseType;
    public override string ModifierName => "Voodoo Curse Marked";
    public override bool HideOnUi => true;
}
