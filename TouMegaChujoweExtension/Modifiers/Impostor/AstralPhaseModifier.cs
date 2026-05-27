using MiraAPI.Modifiers;
using MiraAPI.GameOptions;
using MiraAPI.Roles;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TownOfUs.Utilities.Appearances;
using TownOfUs.Modifiers;
using TownOfUs.Extensions;
using TownOfUs.Utilities;
using TownOfUs.Interfaces;
using UnityEngine;
using TownOfUs.Roles;
using TownOfUs.Assets;

namespace TouMegaChujoweExtension.Modifiers.Impostor;

public sealed class AstralPhaseModifier : ConcealedModifier, IVisualAppearance
{
    public override string ModifierName => "AstralPhasing";
    public override float Duration => OptionGroupSingleton<AstralOptions>.Instance.PhaseDuration;
    public override bool HideOnUi => true;
    public override bool AutoStart => true;
    public bool VisualPriority => true;

    public VisualAppearance GetVisualAppearance()
    {
        return new VisualAppearance(Player.GetDefaultModifiedAppearance(), TownOfUsAppearances.Swooper)
        {
            HatId = string.Empty,
            SkinId = string.Empty,
            VisorId = string.Empty,
            PlayerName = string.Empty,
            PetId = string.Empty,
            RendererColor = new Color(0f, 0f, 0f, 0.1f),
            NameColor = Color.clear,
            ColorBlindTextColor = Color.clear
        };
    }

    public override void OnActivate()
    {
        Player.RawSetAppearance(this);
        Player.cosmetics.ToggleNameVisible(false);

        // Noclip
        Player.gameObject.layer = LayerMask.NameToLayer("Ghost");

        if (Player.AmOwner)
        {
            TouAudio.PlaySound(TouAudio.SwooperActivateSound);
        }
    }

    public override void OnDeactivate()
    {
        Player.ResetAppearance();
        Player.cosmetics.ToggleNameVisible(true);

        // Restore layer
        Player.gameObject.layer = LayerMask.NameToLayer("Players");

        if (Player.AmOwner)
        {
            TouAudio.PlaySound(TouAudio.SwooperDeactivateSound);
        }
    }

    public override void OnDeath(DeathReason reason)
    {
        Player.RemoveModifier(this);
    }

    public override void OnMeetingStart()
    {
        if (Player.AmOwner && !Player.Data.IsDead)
        {
            var options = OptionGroupSingleton<AstralOptions>.Instance;
            var astralRole = Player.Data.Role as TouMegaChujoweExtension.Roles.Classic.Impostor.AstralRole;
            bool killMade = astralRole != null && astralRole.KillMadeDuringPhase;

            if (options.DieIfNoKillDuringPhase && !killMade)
            {
                Player.RpcSpecialMurder(Player, causeOfDeath: "AstralShatter");
            }
            else
            {
                PlayerControl? otherPlayer = null;
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (pc != null && pc.PlayerId != Player.PlayerId && pc.Data != null && !pc.Data.IsDead && !pc.Data.Disconnected)
                    {
                        otherPlayer = pc;
                        break;
                    }
                }
                if (otherPlayer != null)
                {
                    var pos = (Vector2)otherPlayer.transform.position;
                    Player.transform.position = pos;
                    if (Player.NetTransform != null)
                    {
                        Player.NetTransform.RpcSnapTo(pos);
                    }
                }
            }
        }
        Player.RemoveModifier(this);
    }
}
