using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Utilities;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Options.Modifiers;
using TownOfUs.Interfaces;
using TownOfUs.Modifiers.Game;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Modifiers.Neutral;

public sealed class SniperModifier : TouGameModifier, IColoredModifier, IWikiDiscoverable
{
    public static readonly Color SniperColor = TouExtensionColors.Sniper;
    public const float MaxSniperDistance = 3.5f;

    public override string ModifierName => TouLocale.Get("ExtensionModifierSniper", "Sniper");
    public override string LocaleKey => "Sniper";
    public override ModifierFaction FactionType => ModifierFaction.NeutralPassive;
    public override Color FreeplayFileColor => SniperColor;
    public Color ModifierColor => SniperColor;
    public override LoadableAsset<Sprite>? ModifierIcon => TownOfUs.Assets.TouRoleIcons.Miner;

    public override string GetDescription()
    {
        var multiplier = OptionGroupSingleton<TouMegaChujoweExtension.Options.Modifiers.SniperOptions>.Instance.KillDistanceMultiplier;
        return TouLocale.GetParsed($"ExtensionModifier{LocaleKey}Desc", $"Kill and teleport to the body. Kill range is increased by 1.0 and multiplied by {multiplier:0.0}x.");
    }

    public string GetAdvancedDescription() => GetDescription() + MiscUtils.AppendOptionsText(GetType());

    public override int GetAssignmentChance() =>
        (int)OptionGroupSingleton<NeutralModifierOptions>.Instance.SniperChance.Value;

    public override int GetAmountPerGame() =>
        (int)OptionGroupSingleton<NeutralModifierOptions>.Instance.SniperAmount;

    public override bool IsModifierValidOn(RoleBehaviour role)
    {
        return base.IsModifierValidOn(role) &&
            role is ITownOfUsRole { RoleAlignment: RoleAlignment.NeutralKilling or RoleAlignment.ImpostorConcealing or RoleAlignment.ImpostorPower or RoleAlignment.ImpostorSupport or RoleAlignment.ImpostorKilling };
    }

    public static bool LocalPlayerHasSniper()
    {
        var localPlayer = PlayerControl.LocalPlayer;
        if (localPlayer == null || localPlayer.Data == null || localPlayer.Data.Role == null) return false;
        
        return localPlayer.Data.Role is ITownOfUsRole { RoleAlignment: RoleAlignment.NeutralKilling or RoleAlignment.ImpostorConcealing or RoleAlignment.ImpostorPower or RoleAlignment.ImpostorSupport or RoleAlignment.ImpostorKilling } &&
            localPlayer.HasModifier<SniperModifier>();
    }

    /// <summary>
    /// Applies additive kill distance upgrade:
    /// Short (0) -> Medium (1), Medium (1) -> Long (2).
    /// Returns the upgraded kill distance value.
    /// </summary>
    public static float ApplyRangeMultiplier(float baseDistance)
    {
        if (baseDistance <= 0f) return baseDistance;

        var killDistances = GameOptionsManager.Instance.currentNormalGameOptions
            .GetFloatArray(AmongUs.GameOptions.FloatArrayOptionNames.KillDistances);
        var currentIdx = GameOptionsManager.Instance.currentNormalGameOptions.KillDistance;

        // Upgrade by one step: short(0)->medium(1), medium(1)->long(2), long(2) stays long
        var upgradedIdx = System.Math.Min(currentIdx + 1, killDistances.Length - 1);
        return killDistances[upgradedIdx];
    }
}
