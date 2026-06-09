using MiraAPI.Modifiers;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Modifiers.Neutral;

public sealed class GrimReaperMarkedModifier : BaseModifier
{
    public byte GrimReaperId { get; set; }

    public GrimReaperMarkedModifier(byte grimReaperId)
    {
        GrimReaperId = grimReaperId;
    }

    public override string ModifierName => TouLocale.Get("ExtensionModifierGrimReaperMarked", "Grim Reaper Marked");
    public override bool HideOnUi => true;
}
