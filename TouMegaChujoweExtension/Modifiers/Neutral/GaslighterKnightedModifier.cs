using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TownOfUs.Assets;
using TownOfUs.Modules.Localization;
using UnityEngine;

namespace TouMegaChujoweExtension.Modifiers.Neutral;

public sealed class GaslighterKnightedModifier : BaseModifier
{
    public override string ModifierName => TouLocale.Get("ExtensionModifierGaslighterKnighted", "Gaslight Knighted");
    public override bool HideOnUi => false;
    public override LoadableAsset<Sprite>? ModifierIcon => TouRoleIcons.Monarch;
    public override bool Unique => false;

    public override string GetDescription()
    {
        return $"You were 'knighted' by the Gaslighter. You gained 1 extra vote.";
    }
}
