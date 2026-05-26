using AmongUs.GameOptions;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Roles;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Roles.Classic.Impostor;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using TouMegaChujoweExtension.Utilities;
using TownOfUs.Extensions;
using TownOfUs.Roles.Neutral;
using TownOfUs.Utilities;

namespace TouMegaChujoweExtension.Modifiers.Impostor;

public sealed class LonerModifier : BaseModifier, ICachedRole
{
    public override string ModifierName => "Loner";
    public override bool HideOnUi => true;

    public bool ShowCurrentRoleFirst => true;

    public bool Visible => Player.AmOwner ||
                           PlayerControl.LocalPlayer.HasDied() ||
                           PlayerControl.LocalPlayer.IsImpostor() ||
                           FairyRole.FairySeesRoleVisibilityFlag(Player) ||
                           LawyerSeesClientRole();

    public CacheRoleGuess GuessMode => CacheRoleGuess.ActiveOrCachedRole;

    public RoleBehaviour CachedRole => RoleManager.Instance.GetRole((RoleTypes)RoleId.Get<LonerRole>());

    public string CachedRoleName => "Loner";

    private bool LawyerSeesClientRole()
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || !local.IsRole<LawyerRole>())
        {
            return false;
        }

        var options = OptionGroupSingleton<LawyerOptions>.Instance;
        return options?.CanSeeClientRole == true && LawyerUtils.HasLawyerClientRelationship(local, Player);
    }
}
