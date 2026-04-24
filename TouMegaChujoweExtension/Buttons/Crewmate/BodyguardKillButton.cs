using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Keybinds;
using MiraAPI.Modifiers;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Modifiers;
using TouMegaChujoweExtension.Options.Roles.Crewmate;
using TouMegaChujoweExtension.Roles.Crewmate;
using TownOfUs.Assets;
using TownOfUs.Buttons;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Crewmate;

public sealed class BodyguardKillButton : TownOfUsRoleButton<BodyguardRole, PlayerControl>
{
    public override string Name => TouLocale.GetParsed("ExtensionRoleBodyguardKill", "Kill");
    public override BaseKeybind Keybind => Keybinds.PrimaryAction;

    // light green (#77B962)
    public override Color TextOutlineColor => new Color32(0x77, 0xB9, 0x62, 0xFF);

    public override float Cooldown => 0.001f;
    public override float InitialCooldown => 0.001f;

    public override LoadableAsset<Sprite> Sprite => TouNeutAssets.GlitchKillSprite;

    public override bool Enabled(RoleBehaviour? role)
    {
        return base.Enabled(role) && Role is { KillModeActive: true };
    }

    private static PlayerControl? GetShieldedByLocalBodyguard()
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null) return null;

        foreach (var pc in PlayerControl.AllPlayerControls)
        {
            if (pc == null) continue;

            if (pc.TryGetModifier<BodyguardShieldModifier>(out var shield)
                && shield.Bodyguard != null
                && shield.Bodyguard.PlayerId == local.PlayerId)
            {
                return pc;
            }
        }

        return null;
    }

    public override bool CanUse()
    {
        if (HudManager.Instance.Chat.IsOpenOrOpening || MeetingHud.Instance)
            return false;

        if (Role == null || !Role.KillModeActive || PlayerControl.LocalPlayer.HasDied())
            return false;

        return GetTarget() != null;
    }

    public override bool CanClick()
    {
        return Role is { KillModeActive: true } && GetTarget() != null;
    }

    public override void ClickHandler()
    {
        if (Role is { KillModeActive: true })
        {
            var target = GetTarget();
            if (target != null)
            {
                Target = target;
                OnClick();
                return;
            }
        }

        base.ClickHandler();
    }

    public override PlayerControl? GetTarget()
    {
        if (Role == null || !Role.KillModeActive) return null;

        var protectedTarget = GetShieldedByLocalBodyguard();
        var onlyAttacker = OptionGroupSingleton<BodyguardOptions>.Instance.OnlyTargetAttacker;

        if (onlyAttacker && Role.LastAttacker != null && !Role.LastAttacker.HasDied())
        {
            if (protectedTarget != null && Role.LastAttacker.PlayerId == protectedTarget.PlayerId)
                return null;

            var dist = Vector2.Distance(
                PlayerControl.LocalPlayer.transform.position,
                Role.LastAttacker.transform.position);

            return dist <= Distance ? Role.LastAttacker : null;
        }

        return PlayerControl.LocalPlayer.GetClosestLivingPlayer(
            true,
            Distance,
            false,
            x => protectedTarget == null || x.PlayerId != protectedTarget.PlayerId
        );
    }

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        base.FixedUpdate(playerControl);

        if (Role == null) return;

        if (Role.KillModeActive && Button != null)
        {
            Button.SetEnabled();
            Timer = -1f;

            var remaining = Role.KillModeTimer;
            var total = OptionGroupSingleton<BodyguardOptions>.Instance.KillWindow;

            if (remaining > 0f)
            {
                try
                {
                    Button.SetFillUp(remaining, total);
                    Button.cooldownTimerText.text = Mathf.Ceil(remaining)
                        .ToString("F0", System.Globalization.CultureInfo.InvariantCulture);
                    Button.cooldownTimerText.gameObject.SetActive(true);
                }
                catch
                {
                    // ignore
                }
            }
        }
    }

    protected override void OnClick()
    {
        if (Target == null)
        {
            Error("Bodyguard Kill: Target is null");
            return;
        }

        BodyguardRole.RpcBodyguardKill(PlayerControl.LocalPlayer, Target);
    }
}