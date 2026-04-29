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
/// Reveals the Cat's role to their partner.
/// </summary>
public sealed class CatRevealModifier : RevealModifier
{
    private readonly RoleBehaviour _role;

    [HideFromIl2Cpp] public bool IsHiddenFromList => true;

    public CatRevealModifier(RoleBehaviour role)
        : base((int)ChangeRoleResult.Nothing, true, role)
    {
        _role = role;
    }

    public override string ModifierName => "Cat Revealed";

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
        if (localPlayer == null)
        {
            Visible = false;
            return;
        }

        // Partner sees the cat's role
        if (Player.IsRole<SchrodingersCatRole>())
        {
            var cat = Player.GetRole<SchrodingersCatRole>();
            Visible = cat.IsAdopted && cat.TeammateId == localPlayer.PlayerId;
        }
        else
        {
            Visible = false;
        }
    }
}
