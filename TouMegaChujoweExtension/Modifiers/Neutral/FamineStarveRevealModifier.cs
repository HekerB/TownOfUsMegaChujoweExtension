using MiraAPI.Modifiers;
using MiraAPI.Roles;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using TownOfUs.Extensions;
using TownOfUs.Modifiers;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Modifiers.Neutral;

public sealed class FamineStarveRevealModifier : BaseRevealModifier
{
    public override string ModifierName => TouLocale.Get("ExtensionModifierFamineStarveReveal", "Starvation Mark");
    public override string ExtraNameText { get; set; } = $" {TouExtensionColors.Famine.ToTextColor()}†</color>";
    public override Color? NameColor { get; set; } = TouExtensionColors.Famine;

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        var localPlayer = PlayerControl.LocalPlayer;
        var hasStarveMark = Player != null &&
                           !Player.HasDied() &&
                           Player.HasModifier<FamineStarvedModifier>();

        Visible = hasStarveMark &&
                  localPlayer != null &&
                  !localPlayer.HasDied() &&
                  localPlayer.IsRole<FamineRole>();
    }
}
