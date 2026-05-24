using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Utilities;
using TownOfUs.Modules.Localization;
using TownOfUs;
using UnityEngine;

namespace TouMegaChujoweExtension.Options.Roles.Crewmate;

public sealed class MirrorCasterExtensionOptions : AbstractOptionGroup
{
    public override string GroupName => TouLocale.Get("TOUMCEBetterRolePrefix") + TouLocale.Get("Mirror Caster");
    public override Color GroupColor => TouExtensionColors.ShieldFlashes.Mirrorcaster;
    public override bool ShowInModifiersMenu => false;
    public override MenuCategory ParentMenu => MenuCategory.CustomOne;
    public override uint GroupPriority => 104;

    [ModdedToggleOption("ExtensionOptionMirrorCasterMoveWhileMenu")]
    public bool MoveWhileMenu { get; set; } = false;
}
