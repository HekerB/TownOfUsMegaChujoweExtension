using MiraAPI.Modifiers;
using MiraAPI.Modifiers.Types;
using TouMegaChujoweExtension.Assets;
using TownOfUs.Modules.Localization;
using UnityEngine;

namespace TouMegaChujoweExtension.Modifiers.Crewmate;

public sealed class GardenerProtectedModifier : TimedModifier
{
    public override string ModifierName => TouLocale.Get("ExtensionModifierGardenerProtected", "Garden Protection");
    public override bool HideOnUi => true;
    public override float Duration => -1f;
}
