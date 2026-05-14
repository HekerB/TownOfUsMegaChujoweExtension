using MiraAPI.Modifiers;
using UnityEngine;

namespace TouMegaChujoweExtension.Modifiers.Neutral;

public sealed class PopeCanonizedModifier(PlayerControl pope) : BaseModifier
{
    public PlayerControl Pope { get; } = pope;
    private readonly Color _color = TouExtensionColors.Pope;

    public override string ModifierName => "Canonized";
    public override bool HideOnUi => true;

    public override void FixedUpdate()
    {
        if (PlayerControl.LocalPlayer.Data?.Role is not PopeRole || Player == null)
            return;

        if (Player != PlayerControl.LocalPlayer)
        {
            Player.cosmetics.SetOutline(true, new Il2CppSystem.Nullable<Color>(_color));
        }
    }

    public override void OnDeactivate()
    {
        if (Player == null) return;
        Player.cosmetics.SetOutline(false, new Il2CppSystem.Nullable<Color>(_color));
    }
}














