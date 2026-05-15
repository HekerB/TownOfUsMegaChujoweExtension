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
        var multiplier = OptionGroupSingleton<SniperOptions>.Instance.KillDistanceMultiplier;
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
            role is ITownOfUsRole { RoleAlignment: RoleAlignment.NeutralKilling };
    }

    public static bool LocalPlayerHasSniper()
    {
        var localPlayer = PlayerControl.LocalPlayer;
        if (localPlayer == null || localPlayer.Data == null || localPlayer.Data.Role == null) return false;
        
        return localPlayer.Data.Role is ITownOfUsRole { RoleAlignment: RoleAlignment.NeutralKilling } &&
            localPlayer.HasModifier<SniperModifier>();
    }

    public static float ApplyRangeMultiplier(float baseDistance)
    {
        if (baseDistance <= 0f)
        {
            return baseDistance;
        }

        var multiplier = OptionGroupSingleton<SniperOptions>.Instance.KillDistanceMultiplier;
        // Instruction: change distance by 1 unit up on each stage (baseDistance + 1.0)
        return Mathf.Min((baseDistance + 1.0f) * multiplier, MaxSniperDistance);
    }
}
