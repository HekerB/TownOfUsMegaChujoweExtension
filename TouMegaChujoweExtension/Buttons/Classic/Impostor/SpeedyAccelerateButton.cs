using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Modifiers;
using MiraAPI.Utilities.Assets;
using TownOfUs.Assets;
using TownOfUs.Buttons;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;
using TouMegaChujoweExtension.Roles.Classic.Impostor;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Modifiers.Impostor;

namespace TouMegaChujoweExtension.Buttons.Classic.Impostor;

public sealed class SpeedyAccelerateButton : TownOfUsRoleButton<SpeedyRole>
{
    public override string Name => TouLocale.GetParsed("ExtensionRoleSpeedyAccelerate", "Accelerate");
    public override BaseKeybind Keybind => Keybinds.SecondaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Speedy;
    public override float Cooldown
    {
        get
        {
            return Math.Clamp(OptionGroupSingleton<SpeedyOptions>.Instance.AccelerateCooldown + MapCooldown, 5f, 120f);
        }
    }
    public override float EffectDuration => OptionGroupSingleton<SpeedyOptions>.Instance.AccelerateDuration;
    public override LoadableAsset<Sprite> Sprite => TouExtensionImpAssets.SpeedyAbilitySprite;

    public override bool HasEffect => true;
    public override bool ZeroIsInfinite { get; set; } = true;

    public override bool CanUse()
    {
        if (HudManager.Instance.Chat.IsOpenOrOpening || MeetingHud.Instance)
        {
            return false;
        }

        if (Role != null && Role.KillsCount <= 0)
        {
            return false;
        }

        return (Timer <= 0 && !EffectActive) || (EffectActive && Timer <= EffectDuration - 0.5f);
    }

    protected override void OnClick()
    {
        if (!EffectActive)
        {
            PlayerControl.LocalPlayer.RpcAddModifier<SpeedyAccelerateModifier>();
        }
        else
        {
            OnEffectEnd();
        }
    }

    public override void OnEffectEnd()
    {
        if (PlayerControl.LocalPlayer.HasModifier<SpeedyAccelerateModifier>())
        {
            PlayerControl.LocalPlayer.RpcRemoveModifier<SpeedyAccelerateModifier>();
        }
        
        EffectActive = false;
        Timer = Cooldown;
    }

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        if (playerControl == null || playerControl.Data.IsDead)
        {
            if (Button != null) Button.gameObject.SetActive(false);
            return;
        }

        bool killMade = Role != null && Role.KillsCount > 0;
        if (Button != null)
        {
            Button.gameObject.SetActive(killMade && (HudManager.Instance.UseButton.isActiveAndEnabled || 
                                                     HudManager.Instance.PetButton.isActiveAndEnabled));
        }

        if (!killMade) return;

        base.FixedUpdate(playerControl);

        if (Button == null) return;

        if (Button.graphic != null)
        {
            Button.graphic.color = Color.white;
            Button.graphic.material.SetFloat("_Desat", 0f);
        }

        if (Button.buttonLabelText != null)
        {
            if (EffectActive)
            {
                Button.buttonLabelText.color = Color.white;
                Button.buttonLabelText.alpha = 1f;
            }
            else
            {
                Button.buttonLabelText.color = Color.white;
                Button.buttonLabelText.alpha = Timer > 0 ? 0.3f : 1f;
            }
        }
    }
}
