using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.Utilities;
using TownOfUs;
using TownOfUs.Modules.Localization;
using UnityEngine;

namespace TouMegaChujoweExtension.Options.Roles.Crewmate;

public sealed class MirrorCasterExtensionOptions : AbstractOptionGroup
{
    public override string GroupName => TouLocale.Get("TOUMCEBetterRolePrefix") + TouLocale.Get("Mirror Caster");
    public override Color GroupColor => TouExtensionColors.ShieldFlashes.MirrorcasterFlash;
    public override bool ShowInModifiersMenu => false;
    public override MenuCategory ParentMenu => MenuCategory.CustomOne;
    public override uint GroupPriority => 103;

    [ModdedToggleOption("ExtensionOptionMirrorCasterMoveWhileMenu")]
    public bool MoveWhileMenu { get; set; } = false;
}
