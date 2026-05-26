using MiraAPI.Modifiers;
using MiraAPI.Roles;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using TownOfUs.Extensions;
using TownOfUs.Modifiers;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Modifiers.Neutral;

public sealed class BakerBreadRevealModifier : BaseRevealModifier
{
    public override string ModifierName => TouLocale.Get("ExtensionModifierBakerBreadReveal", "Bread Mark");
    public override string ExtraNameText { get; set; } = $" {TouExtensionColors.Baker.ToTextColor()}⁂</color>";
    public override Color? NameColor { get; set; } = TouExtensionColors.Baker;

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        var localPlayer = PlayerControl.LocalPlayer;
        var hasBakerMark = Player != null &&
                           !Player.HasDied() &&
                           (Player.HasModifier<BakerBreadModifier>() ||
                            Player.HasModifier<FamineStarvedModifier>());

        Visible = hasBakerMark &&
                  localPlayer != null &&
                  !localPlayer.HasDied() &&
                  (localPlayer.IsRole<BakerRole>() || localPlayer.IsRole<FamineRole>());
    }
}
