using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TownOfUs.Assets;
using TownOfUs.Events;
using TownOfUs.Modules.Localization;
using UnityEngine;

namespace TouMegaChujoweExtension.Modifiers.Neutral;

public sealed class JackalShieldModifier : BaseModifier
{
    public override string ModifierName
    {
        get
        {
            var title = TouLocale.Get("ExtensionJackalShieldModifierTitle");
            var remaining = RemainingRounds();
            return remaining > 0 ? $"{title} ({remaining} R)" : title;
        }
    }

    public override bool HideOnUi => false;
    public override LoadableAsset<Sprite> ModifierIcon => TouRoleIcons.Jackal;

    public string LocaleKey => "JackalShield";

    public override string GetDescription()
    {
        var desc = TouLocale.Get("ExtensionJackalShieldModifierDesc");
        var options = OptionGroupSingleton<JackalOptions>.Instance;
        if (options == null || (int)options.ShieldRounds <= 0)
        {
            return desc;
        }

        var remaining = RemainingRounds();
        var roundText = remaining > 0 ? remaining.ToString(TouMegaChujoweExtensionPlugin.Culture) : "0";
        return desc + "\n" + TouLocale.GetParsed("ExtensionJackalShieldRemainingRounds").Replace("<count>", roundText);
    }

    public override Color FreeplayFileColor => TouExtensionColors.Jackal;

    /// <summary>Rounds the shield still has left, or 0 once it has run out.</summary>
    private static int RemainingRounds()
    {
        var options = OptionGroupSingleton<JackalOptions>.Instance;
        if (options == null)
        {
            return 0;
        }

        var maxRounds = (int)options.ShieldRounds;
        if (maxRounds <= 0)
        {
            return 0;
        }

        var remaining = maxRounds - ExtensionDeathInfo.CurrentRound + 1;
        return remaining > 0 ? remaining : 0;
    }
}
