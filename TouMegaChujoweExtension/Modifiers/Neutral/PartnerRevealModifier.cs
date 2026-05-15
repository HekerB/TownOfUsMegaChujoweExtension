using Il2CppInterop.Runtime.Attributes;
using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Roles.Neutral;
using TouMegaChujoweExtension.Utilities;
using TownOfUs.Modifiers;
using TownOfUs.Extensions;
using MiraAPI.Roles;
using TownOfUs.Utilities;

namespace TouMegaChujoweExtension.Modifiers;

/// <summary>
/// Reveals the partner's role to the Cat.
/// </summary>
public sealed class PartnerRevealModifier : RevealModifier
{
    private readonly RoleBehaviour _role;

    [HideFromIl2Cpp] public bool IsHiddenFromList => true;

    public PartnerRevealModifier(RoleBehaviour role)
        : base((int)ChangeRoleResult.Nothing, true, role)
    {
        _role = role;
    }

    public override string ModifierName => "Partner Revealed";

    public override void OnActivate()
    {
        base.OnActivate();
        if (RevealRole && ShownRole == null)
        {
            ShownRole = _role ?? (Player.Data?.Role);
        }
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        var localPlayer = PlayerControl.LocalPlayer;
        if (localPlayer == null || !localPlayer.IsRole<SchrodingersCatRole>())
        {
            Visible = false;
            return;
        }

        var cat = localPlayer.GetRole<SchrodingersCatRole>();
        Visible = cat.IsAdopted && cat.TeammateId == Player.PlayerId;
    }
}
