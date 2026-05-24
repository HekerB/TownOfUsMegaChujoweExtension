using AmongUs.GameOptions;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Roles;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TownOfUs.Extensions;
using TownOfUs.Roles.Neutral;
using TownOfUs.Utilities;

namespace TouMegaChujoweExtension.Modifiers.Impostor;

public sealed class GunGameModifier : BaseModifier, ICachedRole
{
    public override string ModifierName => "Gun Game";
    public override bool HideOnUi => true;

    public bool ShowCurrentRoleFirst => true;

    public bool Visible => Player.AmOwner ||
                           PlayerControl.LocalPlayer.HasDied() ||
                           PlayerControl.LocalPlayer.IsImpostor() ||
                           FairyRole.FairySeesRoleVisibilityFlag(Player);

    public CacheRoleGuess GuessMode => (CacheRoleGuess)OptionGroupSingleton<GunGameOptions>.Instance.GunGameGuess.Value;

    public RoleBehaviour CachedRole => RoleManager.Instance.GetRole((RoleTypes)RoleId.Get<TouMegaChujoweExtension.Roles.Classic.Impostor.GunGameRole>());

    public string CachedRoleName => "Gun Game";
}
