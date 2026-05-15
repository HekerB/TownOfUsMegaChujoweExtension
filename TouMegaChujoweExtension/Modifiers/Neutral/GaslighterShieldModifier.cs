using MiraAPI.Modifiers;
using MiraAPI.Utilities.Assets;
using TownOfUs.Modifiers;
using TownOfUs.Assets;
using TownOfUs.Modules.Anims;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using MiraAPI.Utilities;
using UnityEngine;
using Reactor.Utilities.Extensions;
using System.Linq;

namespace TouMegaChujoweExtension.Modifiers.Neutral;

public sealed class GaslighterShieldModifier : BaseShieldModifier
{
    public override string ModifierName => TouLocale.Get("ExtensionModifierGaslighterShield", "Gaslight Shield");
    public override LoadableAsset<Sprite>? ModifierIcon => TouRoleIcons.Medic;
    public override float Duration => 2.5f;

    public override string ShieldDescription =>
        "You are shielded by the Gaslighter!\nYou may not die to other players.";

    public GameObject? ShieldVisual { get; set; }

    public override bool HideOnUi => false;
    public override bool VisibleSymbol => true;

    public override void OnActivate()
    {
        base.OnActivate();
        ShieldVisual = AnimStore.SpawnAnimBody(Player, TouAssets.MedicShield.LoadAsset(), false, -1.1f, -0.1f, 1.5f);
    }

    public override void OnDeactivate()
    {
        if (ShieldVisual != null)
        {
            ShieldVisual.Destroy();
        }
        base.OnDeactivate();
    }

    public override void Update()
    {
        if (Player == null)
        {
            ModifierComponent?.RemoveModifier(this);
            return;
        }

        if (!MeetingHud.Instance && ShieldVisual != null)
        {
            ShieldVisual.SetActive(!Player.IsConcealed() && IsVisible);
        }
    }
}
