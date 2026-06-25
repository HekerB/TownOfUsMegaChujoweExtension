using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Roles.Classic.Impostor;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Modifiers.Impostor;

public sealed class VoodooScheduledCurseModifier(VoodooEffect curseType) : BaseModifier
{
    public VoodooEffect CurseType { get; } = curseType;
    public override string ModifierName => "Voodoo Curse Marked";
    public override bool HideOnUi => true;

    public override void OnActivate()
    {
        base.OnActivate();

        if (CurseType != VoodooEffect.Mute || Player == null || !Player.AmOwner)
        {
            return;
        }

        var message = TouLocale.GetParsed(
            "ExtensionVoodooMuteScheduledAlert",
            "The Voodoo Master has charmed you. Only you know about this curse. During the next meeting you cannot chat, but you can still vote.");
        var title =
            $"{Palette.ImpostorRed.ToTextColor()}{TouLocale.Get("ExtensionVoodooMuteScheduledTitle", "Voodoo Charm")}</color>";

        RoleAlertUtils.ShowRoleAlert(
            $"<b>{Palette.ImpostorRed.ToTextColor()}{message.Replace("\n", " ")}</color></b>",
            Color.white,
            TouExtensionIcons.VoodooRoleIcon.LoadAsset(),
            $"VoodooMuteScheduled-{Player.PlayerId}");

        MiscUtils.AddFakeChat(Player.Data, title, message, false, true);
    }
}
