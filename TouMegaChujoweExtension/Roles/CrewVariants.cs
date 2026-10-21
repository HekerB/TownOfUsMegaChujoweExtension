using AmongUs.GameOptions;
using MiraAPI.Roles;

namespace TouMegaChujoweExtension.Roles;

/// <summary>
/// The crew role an Imitator turns into when it imitates a dead extension impostor or neutral
/// (<c>ICrewVariant</c>); without one TOU-Mira leaves that player out of the Imitator's choices.
/// </summary>
public static class CrewVariants
{
    public static RoleBehaviour Of<T>()
        where T : RoleBehaviour, ICustomRole
    {
        return RoleManager.Instance.GetRole((RoleTypes)RoleId.Get<T>());
    }
}
