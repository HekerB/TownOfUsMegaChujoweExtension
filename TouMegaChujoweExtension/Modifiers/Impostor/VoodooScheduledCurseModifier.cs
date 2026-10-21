using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Roles.Classic.Impostor;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Modifiers.Impostor;

/// <param name="curseType">
///     The <see cref="VoodooEffect"/> as a raw byte. It is deliberately not typed as the enum: this
///     modifier is created through <c>RpcAddModifier</c>, which serialises each constructor argument by
///     writing its assembly-qualified type name and having the receiver resolve it with
///     <c>Type.GetType</c> plus a generic deserialiser. Every other modifier in this project that
///     carries an enum-like value converts it first, and this one was the sole exception - which is why
///     the scheduled curse never reached the other clients and Mute silently did nothing while the use
///     was still spent.
/// </param>
public sealed class VoodooScheduledCurseModifier(byte curseType) : BaseModifier
{
    public VoodooEffect CurseType { get; } = (VoodooEffect)curseType;
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
