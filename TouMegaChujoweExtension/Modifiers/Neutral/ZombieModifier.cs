using MiraAPI.Modifiers;
using MiraAPI.Utilities.Assets;
using UnityEngine;

namespace TouMegaChujoweExtension.Modifiers.Neutral;

public sealed class ZombieModifier : BaseModifier
{
    public override string ModifierName => "Zombie";
    public override bool HideOnUi => false;
    public override LoadableAsset<Sprite>? ModifierIcon => TownOfUs.Assets.TouRoleIcons.Vampire;
}

public sealed class PatientZeroModifier : BaseModifier
{
    public override string ModifierName => "Patient Zero";
    public override bool HideOnUi => false;
    public override LoadableAsset<Sprite>? ModifierIcon => TownOfUs.Assets.TouRoleIcons.Vampire;
}
