using MiraAPI.Roles;
using TownOfUs.Roles;

namespace TouMegaChujoweExtension.Roles;

/// <summary>
/// MiraAPI 0.5.0 builds a role's vanilla StringName/BlurbName ids from <see cref="ICustomRole.IdPrefix"/> and
/// <see cref="ICustomRole.IdPart"/>, and TOU-Mira 1.7.3 pins the prefix to <c>TownOfUsMira.Role</c> for every
/// <see cref="ITownOfUsRole"/> - ids no extension string uses. This points them at the extension's
/// <c>ExtensionRole{LocaleKey}</c> keys instead. TOU also takes <see cref="ICustomRole.IdPart"/> as the default
/// cause of death (<c>DiedTo{IdPart}</c>).
/// </summary>
public interface IExtensionRole : ITownOfUsRole
{
    string LocaleKey { get; }

    string ICustomRole.IdPrefix => "TouMegaChujoweExtension.Role";

    string ICustomRole.IdPart => LocaleKey;

    string ICustomRole.RoleNameLocale => $"ExtensionRole{LocaleKey}";

    string ICustomRole.RoleDescriptionLocale => $"ExtensionRole{LocaleKey}IntroBlurb";

    string ICustomRole.RoleMedDescriptionLocale => $"ExtensionRole{LocaleKey}IntroBlurb";

    string ICustomRole.RoleLongDescriptionLocale => $"ExtensionRole{LocaleKey}TabDescription";

    string ICustomRole.RoleWikiDescriptionLocale => $"ExtensionRole{LocaleKey}WikiDescription";

    // The role options menu shows the medium description; the extension has none, the intro blurb fits.
    string ICustomRole.RoleMedDescription => RoleDescription;
}
