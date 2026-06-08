using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TouMegaChujoweExtension.Roles.Classic.Impostor;
using TownOfUs.Assets;
using TownOfUs.Buttons;
using TownOfUs.Modules;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Classic.Impostor;

public sealed class VoodooDollButton : TownOfUsRoleButton<VoodooMasterRole>
{
    public override string Name => "Curse";
    public override BaseKeybind Keybind => Keybinds.SecondaryAction;
    public override Color TextOutlineColor => Palette.ImpostorRed;
    public override float Cooldown => GetEffectCooldown(Role?.SelectedEffect ?? VoodooEffect.Blindness);
    public override int MaxUses => (int)OptionGroupSingleton<VoodooMasterOptions>.Instance.MaxCurses;
    public override bool ZeroIsInfinite { get; set; } = true;
    public override LoadableAsset<Sprite> Sprite => GetEffectSprite(Role?.SelectedEffect ?? VoodooEffect.Blindness);

    private bool _isProcessingClick;

    public override bool CanUse()
    {
        if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data.IsDead)
        {
            return false;
        }

        if (Minigame.Instance || MeetingHud.Instance || HudManager.Instance.Chat.IsOpenOrOpening)
        {
            return false;
        }

        return Timer <= 0f && (!LimitedUses || UsesLeft > 0);
    }

    public override void ClickHandler()
    {
        if (_isProcessingClick)
        {
            return;
        }

        _isProcessingClick = true;

        try
        {
            if (CanUse())
            {
                OnClick();
            }
        }
        finally
        {
            Reactor.Utilities.Coroutines.Start(ResetProcessingFlag());
        }
    }

    private System.Collections.IEnumerator ResetProcessingFlag()
    {
        yield return new WaitForSeconds(0.2f);
        _isProcessingClick = false;
    }

    protected override void OnClick()
    {
        var playerMenu = CustomPlayerMenu.Create();
        playerMenu.Begin(
            player => player != null && !player.HasDied() && player.PlayerId != PlayerControl.LocalPlayer.PlayerId,
            player =>
            {
                playerMenu.ForceClose();

                if (player == null || Role == null)
                {
                    Timer = 0.01f;
                    return;
                }

                VoodooMasterRole.CastVoodooDoll(PlayerControl.LocalPlayer, player, Role.SelectedEffect);
                if (LimitedUses)
                {
                    UsesLeft--;
                }

                Timer = GetEffectCooldown(Role.SelectedEffect);
              });
      }

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        var shouldShow = Role != null && !playerControl.HasDied();

        if (Button != null && Button.gameObject.activeSelf != shouldShow)
        {
            Button.gameObject.SetActive(shouldShow);
        }

        if (shouldShow)
        {
            base.FixedUpdate(playerControl);
            OverrideSprite(GetEffectSprite(Role!.SelectedEffect).LoadAsset());
        }
    }

    private static LoadableAsset<Sprite> GetEffectSprite(VoodooEffect effect)
    {
        return effect switch
        {
            VoodooEffect.Mute => TouImpAssets.BlackmailSprite,
            VoodooEffect.Confuse => TouImpAssets.HerbConfuseSprite,
            _ => TouImpAssets.BlindSprite
        };
    }

    private static float GetEffectCooldown(VoodooEffect effect)
    {
        var options = OptionGroupSingleton<VoodooMasterOptions>.Instance;
        return effect switch
        {
            VoodooEffect.Mute => options.MuteCooldown,
            VoodooEffect.Confuse => options.ConfuseCooldown,
            _ => options.BlindCooldown
        };
    }
}
