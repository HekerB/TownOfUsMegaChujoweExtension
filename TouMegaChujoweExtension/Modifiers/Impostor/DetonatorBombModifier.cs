using MiraAPI.Modifiers;
using MiraAPI.Modifiers.Types;
using TouMegaChujoweExtension.Networking;
using UnityEngine;
using TownOfUs.Utilities;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TownOfUs.Assets;

namespace TouMegaChujoweExtension.Modifiers.Impostor;

public sealed class DetonatorBombModifier : BaseModifier
{
    private PlayerControl _detonator;

    public override string ModifierName => "Bomb Attached";

    public DetonatorBombModifier(PlayerControl detonator)
    {
        _detonator = detonator;
    }

    public override void OnActivate()
    {
        base.OnActivate();
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();
        
        if (Player == null || Player.HasDied() || MeetingHud.Instance || _detonator == null || _detonator.HasDied())
        {
            if (Player != null) Player.RemoveModifier(this);
            return;
        }
    }

}
