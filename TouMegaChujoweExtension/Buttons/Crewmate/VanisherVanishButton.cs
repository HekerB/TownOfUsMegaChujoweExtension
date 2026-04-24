using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Modifiers;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Modifiers;
using TouMegaChujoweExtension.Options.Roles.Crewmate;
using TouMegaChujoweExtension.Roles.Crewmate;
using TownOfUs.Buttons;
using TownOfUs.Assets;
using TownOfUs.Modifiers;
using TownOfUs.Modifiers.Neutral;
using TownOfUs.Modules.Localization;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Crewmate;

public sealed class VanisherVanishButton : TownOfUsRoleButton<VanisherRole>
{
    public override Color TextOutlineColor => TouExtensionColors.Vanisher;
    public override string Name => TouLocale.GetParsed("ExtensionRoleVanisherVanish", "Vanish");
    public override BaseKeybind Keybind => Keybinds.SecondaryAction;
    public override float Cooldown => Math.Clamp(OptionGroupSingleton<VanisherOptions>.Instance.VanishCooldown + MapCooldown, 5f, 120f);
    public override float EffectDuration => OptionGroupSingleton<VanisherOptions>.Instance.VanishDuration;
    public override int MaxUses => (int)OptionGroupSingleton<VanisherOptions>.Instance.MaxVanishes;
    public override LoadableAsset<Sprite> Sprite => TouCrewAssets.CrewSwoopSprite;

    public override bool ZeroIsInfinite { get; set; } = true;

    private bool _lastMeetingState;

    public override void ClickHandler()
    {
        if (!CanUse())
        {
            return;
        }

        OnClick();
        Button?.SetDisabled();
        if (EffectActive)
        {
            Timer = Cooldown;
            EffectActive = false;
        }
        else if (HasEffect)
        {
            EffectActive = true;
            Timer = EffectDuration;
        }
        else
        {
            Timer = Cooldown;
        }
    }

    public override bool CanUse()
    {
        if (HudManager.Instance.Chat.IsOpenOrOpening || MeetingHud.Instance)
        {
            return false;
        }

        if (PlayerControl.LocalPlayer.HasModifier<GlitchHackedModifier>() || PlayerControl.LocalPlayer
                .GetModifiers<DisabledModifier>().Any(x => !x.CanUseAbilities))
        {
            return false;
        }

        return ((Timer <= 0 && !EffectActive && (!LimitedUses || UsesLeft > 0)) ||
                (EffectActive && Timer <= EffectDuration - 2f));
    }

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        base.FixedUpdate(playerControl);

        var inMeeting = MeetingHud.Instance != null;
        if (_lastMeetingState && !inMeeting)
        {
            UsesLeft = MaxUses;
            if (LimitedUses)
            {
                Button?.SetUsesRemaining(UsesLeft);
            }
        }
        _lastMeetingState = inMeeting;
    }

    protected override void OnClick()
    {
        if (!EffectActive)
        {
            PlayerControl.LocalPlayer.RpcAddModifier<VanishModifier>();
            UsesLeft--;
            if (LimitedUses)
            {
                Button?.SetUsesRemaining(UsesLeft);
            }
        }
        else
        {
            OnEffectEnd();
        }
    }

    public override void OnEffectEnd()
    {
        if (!PlayerControl.LocalPlayer.HasModifier<VanishModifier>())
        {
            return;
        }

        PlayerControl.LocalPlayer.RpcRemoveModifier<VanishModifier>();
    }
}