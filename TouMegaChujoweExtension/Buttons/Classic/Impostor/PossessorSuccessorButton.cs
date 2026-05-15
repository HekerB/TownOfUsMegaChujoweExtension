using MiraAPI.Hud;
using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Roles.Impostor;
using TownOfUs.Assets;
using TownOfUs.Buttons;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Impostor;

public sealed class PossessorSuccessorButton : TownOfUsButton
{
    public bool Show { get; set; }

    public override string Name => TouLocale.Get("ExtensionRolePossessorChooseSuccessor", "Choose Successor");
    public override BaseKeybind Keybind => Keybinds.PrimaryAction;
    public override float Cooldown => 0.01f;
    public override LoadableAsset<Sprite> Sprite => TouRoleIcons.Traitor;
    public override ButtonLocation Location => ButtonLocation.BottomRight;
    public override Color TextOutlineColor => Palette.ImpostorRed;
    public override bool ShouldPauseInVent => false;
    public override bool UsableInDeath => true;

    public override bool Enabled(RoleBehaviour? role)
    {
        return Show &&
               PlayerControl.LocalPlayer?.Data?.Role is PossessorRole Possessor &&
               Possessor.CompletedAllTasks &&
               !Possessor.SuccessorChosen &&
               !Possessor.Caught;
    }

    public override bool CanUse()
    {
        if (DestroyableSingleton<HudManager>.Instance.Chat.IsOpenOrOpening || MeetingHud.Instance)
        {
            return false;
        }

        return PlayerControl.LocalPlayer?.Data?.Role is PossessorRole Possessor &&
               Possessor.CompletedAllTasks &&
               !Possessor.SuccessorChosen &&
               !Possessor.Caught;
    }

    protected override void OnClick()
    {
        if (!CanUse() || Minigame.Instance != null)
        {
            return;
        }

        var localPlayer = PlayerControl.LocalPlayer;
        if (localPlayer == null)
        {
            return;
        }

        var playerMenu = CustomPlayerMenu.Create();
        playerMenu.transform.FindChild("PhoneUI").GetChild(0).GetComponent<SpriteRenderer>().material =
            localPlayer.cosmetics.currentBodySprite.BodySprite.material;
        playerMenu.transform.FindChild("PhoneUI").GetChild(1).GetComponent<SpriteRenderer>().material =
            localPlayer.cosmetics.currentBodySprite.BodySprite.material;

        playerMenu.Begin(
            IsValidSuccessorTarget,
            target =>
            {
                playerMenu.ForceClose();

                if (target == null)
                {
                    return;
                }

                PossessorRole.RpcPossessorChooseSuccessor(localPlayer, target);
            });
    }

    private static bool IsValidSuccessorTarget(PlayerControl target)
    {
        return target != null
               && target.Data != null
               && !target.Data.Disconnected
               && !target.HasDied()
               && target.PlayerId != PlayerControl.LocalPlayer.PlayerId
               && !target.IsImpostorAligned();
    }
}
