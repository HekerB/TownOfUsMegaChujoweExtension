using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Roles.Classic.Impostor;

namespace TouMegaChujoweExtension.Modifiers.Impostor;

public sealed class VoodooScheduledCurseModifier : BaseModifier
{
    public VoodooEffect CurseType { get; set; }

    public VoodooScheduledCurseModifier(VoodooEffect curseType)
    {
        CurseType = curseType;
    }

    public override string ModifierName => "Voodoo Curse Marked";
    public override bool HideOnUi => true;
}
