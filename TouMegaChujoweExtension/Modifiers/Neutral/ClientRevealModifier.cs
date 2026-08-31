using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using TownOfUs.Modifiers;
using TownOfUs.Modules;
using TownOfUs.Utilities;

namespace TouMegaChujoweExtension.Modifiers.Neutral;

/// <summary>
/// Reveal modifier for client - makes client's role visible to their lawyer (if setting is enabled).
/// </summary>
public sealed class ClientRevealModifier : RevealModifier
{
    private readonly RoleBehaviour _role;

    [HideFromIl2Cpp] public bool IsHiddenFromList => true;

    public ClientRevealModifier(RoleBehaviour role)
        : base((int)ChangeRoleResult.Nothing, true, role)
    {
        _role = role;
    }

    public override string ModifierName => "Client Revealed";

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

        // A client's role is not fixed for the game - a Traitor conversion, an Amnesiac remembering off
        // them, an Arcanist card or a Shifter swap all change it - and this was pinned once at
        // activation, so the Lawyer kept reading a role their client no longer had. Resolved live, and
        // through GetRoleWhenAlive so a dead client still shows what they were rather than the ghost
        // role vanilla swapped in.
        // Unity's implicit bool, not a null check: an `is not null` here teaches the compiler that
        // Player may be null and makes every later use of it a nullability warning.
        if (RevealRole && Player)
        {
            var current = Player.GetRoleWhenAlive();
            if (current != null)
            {
                ShownRole = current;
            }
        }

        var localPlayer = PlayerControl.LocalPlayer;
        if (localPlayer == null)
        {
            Visible = false;
            return;
        }

        if (!localPlayer.IsRole<LawyerRole>())
        {
            Visible = false;
            return;
        }

        if (!LawyerUtils.HasLawyerClientRelationship(localPlayer, Player))
        {
            Visible = false;
            return;
        }

        var options = OptionGroupSingleton<LawyerOptions>.Instance;
        Visible = options?.CanSeeClientRole == true;
    }
}














