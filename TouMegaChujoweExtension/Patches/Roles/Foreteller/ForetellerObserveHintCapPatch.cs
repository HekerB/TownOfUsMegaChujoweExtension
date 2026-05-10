using HarmonyLib;
using MiraAPI.Modifiers;
using MiraAPI.Utilities;
using System.Text;
using TownOfUs.Extensions;
using TownOfUs.Modifiers.Neutral;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules;
using TownOfUs.Roles.Neutral;
using TownOfUs.Utilities;
using TownOfUs;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Roles.Foreteller;

[HarmonyPatch(typeof(DoomsayerRole), "GenerateReport")]
public static class FortellerObserveHintCapPatch
{
    private const int MaxHintRoles = 10;

    [HarmonyPrefix]
    public static bool Prefix(DoomsayerRole __instance)
    {
        var player = __instance.Player;
        if (player == null || !player.AmOwner)
            return false;

        var reportBuilder = new StringBuilder();

        foreach (var playerInfo in GameData.Instance.AllPlayers.ToArray()
                     .Where(x => !x.Object.HasDied() && x.Object.HasModifier<DoomsayerObservedModifier>()))
        {
            var role = playerInfo.Object.Data.Role;
            var doomableRole = role as IDoomable;
            var hintType = DoomableType.Default;

            var cachedMod =
                playerInfo.Object.GetModifiers<MiraAPI.Modifiers.BaseModifier>()
                    .FirstOrDefault(x => x is ICachedRole) as ICachedRole;
            if (cachedMod != null)
            {
                role = cachedMod.CachedRole;
                doomableRole = role as IDoomable;
            }

            var undoomableRole = role as IUnguessable;
            if (undoomableRole != null)
            {
                role = undoomableRole.AppearAs;
                doomableRole = role as IDoomable;
            }

            if (doomableRole != null)
            {
                hintType = doomableRole.DoomHintType;
            }

            var fallback = TouLocale.GetParsed("TouRoleDoomsayerRoleHintDefault");
            var hint = TouLocale.GetParsed($"TouRoleDoomsayerRoleHint{hintType}");

            if (hint.Contains("STRMISS"))
            {
                reportBuilder.AppendLine($"{fallback.Replace("<player>", playerInfo.PlayerName)}\n");
            }
            else
            {
                reportBuilder.AppendLine($"{hint.Replace("<player>", playerInfo.PlayerName)}\n");
            }

            List<RoleBehaviour> roles;
            if (hintType != DoomableType.Default)
            {
                roles = MiscUtils.AllRoles
                    .Where(x => x is IDoomable doomRole && doomRole.DoomHintType == hintType &&
                                x is not IUnguessable)
                    .OrderBy(x => x.GetRoleName()).ToList();
            }
            else
            {
                roles = MiscUtils.AllRegisteredRoles
                    .Where(x => (x is IDoomable doomRole && doomRole.DoomHintType == DoomableType.Default &&
                                 x is not IUnguessable || x is not IDoomable) && !x.IsDead)
                    .OrderBy(x => x.GetRoleName()).ToList();
            }

            // CAP to MaxHintRoles, guaranteeing target's real role is included
            if (roles.Count > MaxHintRoles)
            {
                roles = CapRoles(roles, role, MaxHintRoles);
            }

            if (roles.Count != 0)
            {
                reportBuilder.Append("(");
                for (int i = 0; i < roles.Count; i++)
                {
                    reportBuilder.Append(MiscUtils.GetHyperlinkText(roles[i]));
                    if (i < roles.Count - 1)
                        reportBuilder.Append(", ");
                    else
                        reportBuilder.Append(")");
                }
            }

            playerInfo.Object.RemoveModifier<DoomsayerObservedModifier>();
        }

        var report = reportBuilder.ToString();

        if (HudManager.Instance && report.Length > 0)
        {
            var hexColor = ColorUtility.ToHtmlStringRGBA(TownOfUsColors.Doomsayer);
            var title = $"<color=#{hexColor}>{TouLocale.Get("TouRoleDoomsayerMessageTitle")}</color>";
            MiscUtils.AddFakeChat(player.Data, title, report, false, true);
        }

        return false;
    }

    private static List<RoleBehaviour> CapRoles(List<RoleBehaviour> allRoles, RoleBehaviour targetRole, int max)
    {
        bool targetInList = allRoles.Any(r => r.Role == targetRole.Role);

        if (!targetInList)
        {
            return allRoles.OrderBy(_ => UnityEngine.Random.value).Take(max).OrderBy(x => x.GetRoleName()).ToList();
        }

        var withoutTarget = allRoles.Where(r => r.Role != targetRole.Role).ToList();
        var targetEntry = allRoles.First(r => r.Role == targetRole.Role);

        var picked = withoutTarget
            .OrderBy(_ => UnityEngine.Random.value)
            .Take(max - 1)
            .ToList();

        picked.Add(targetEntry);
        return picked.OrderBy(x => x.GetRoleName()).ToList();
    }
}












