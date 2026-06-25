using System.Linq;
using MiraAPI.Hud;
using MiraAPI.Keybinds;
using MiraAPI.Modifiers;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using TownOfUs.Assets;
using TownOfUs.Buttons;
using TownOfUs.Modifiers;
using TownOfUs.Modifiers.Neutral;
using TownOfUs.Modules.Localization;
using TownOfUs.Networking;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Classic.Neutral;

public sealed class InnocentHauntButton : TownOfUsButton
{
    public static bool ShowThisRound { get; set; }

    public override string Name => TouLocale.Get("ExtensionRoleInnocentHaunt", "Haunt");
    public override BaseKeybind Keybind => Keybinds.SecondaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Innocent;
    public override float Cooldown => 0.01f;
    public override LoadableAsset<Sprite> Sprite => TouNeutAssets.JesterHauntSprite;
    public override ButtonLocation Location => ButtonLocation.BottomRight;
    public override bool ShouldPauseInVent => false;
    public override bool UsableInDeath => true;

    public override bool Enabled(RoleBehaviour? role)
    {
        return ShowThisRound &&
               role is InnocentRole &&
               ModifierUtils.GetActiveModifiers<MisfortuneTargetModifier>().Any();
    }

    public override bool CanUse()
    {
        if (HudManager.Instance.Chat.IsOpenOrOpening || MeetingHud.Instance)
        {
            return false;
        }

        return ShowThisRound && ModifierUtils.GetActiveModifiers<MisfortuneTargetModifier>().Any();
    }

    protected override void OnClick()
    {
        if (Minigame.Instance)
        {
            return;
        }

        var playerMenu = CustomPlayerMenu.Create();
        playerMenu.Begin(
            player => player != null &&
                      !player.HasDied() &&
                      player.HasModifier<MisfortuneTargetModifier>() &&
                      !player.HasModifier<InvulnerabilityModifier>() &&
                      !player.AmOwner,
            player =>
            {
                playerMenu.ForceClose();

                if (player == null || !ModifierUtils.GetActiveModifiers<MisfortuneTargetModifier>().Any())
                {
                    return;
                }

                if (PlayerControl.LocalPlayer.HasDied())
                {
                    PlayerControl.LocalPlayer.RpcGhostRoleMurder(player);
                }
                else
                {
                    PlayerControl.LocalPlayer.RpcSpecialMurder(
                        player,
                        ignoreShield: false,
                        createDeadBody: true,
                        teleportMurderer: false,
                        showKillAnim: false,
                        playKillSound: true,
                        causeOfDeath: "InnocentHaunt");
                }

                ClearRoundHaunts();
            });
    }

    public static void ClearRoundHaunts()
    {
        foreach (var mod in ModifierUtils.GetActiveModifiers<MisfortuneTargetModifier>().ToArray())
        {
            mod.ModifierComponent?.RemoveModifier(mod);
        }

        ShowThisRound = false;
    }
}
