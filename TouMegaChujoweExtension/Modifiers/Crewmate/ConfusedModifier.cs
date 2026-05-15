using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Patches;
using TownOfUs.Modifiers;
using TownOfUs.Utilities.Appearances;
using TownOfUs.Extensions;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TouMegaChujoweExtension.Modifiers;

public sealed class ConfusedModifier : DisabledModifier, IVisualAppearance
{
    private readonly float _duration;
    public float SpeedFactor => 0.7f;
    public float VisionPerc => 0.7f;

    public ConfusedModifier(float duration)
    {
        _duration = duration;
    }

    public override string ModifierName => "Confused";
    public override bool HideOnUi => false;
    public override bool CanReport => false;
    public override float Duration => _duration;
    public override bool AutoStart => true;
    public override bool CanUseAbilities => true;

    public override void OnActivate()
    {
        if (!Player.AmOwner) return;
        Player.RawSetAppearance(this);
        ApplyHallucinatoryEffects();
    }

    public override void OnDeactivate()
    {
        base.OnDeactivate();
        if (Player != null && Player.AmOwner)
        {
            RemoveHallucinatoryEffects();
            Player.ResetAppearance(fullReset: true);
        }
    }

    public VisualAppearance GetVisualAppearance()
    {
        var appearance = Player.GetDefaultModifiedAppearance();
        appearance.Speed = SpeedFactor;
        return appearance;
    }

    private void ApplyHallucinatoryEffects()
    {
        var players = PlayerControl.AllPlayerControls.ToArray().Where(x => x.Data != null && !x.Data.IsDead && x != Player).ToList();

        foreach (var player in players)
        {
            var hidden = Random.Range(0, 3);
            if (hidden == 0)
            {
                var seeker = players[Random.Range(0, players.Count)];
                if (seeker != null && seeker != player)
                {
                    var seekerAppearance = seeker.GetDefaultModifiedAppearance();
                    player.RawSetAppearance(seekerAppearance);
                }
            }
            else if (hidden == 1)
            {
                player.SetCamouflage();
            }
            else
            {
                var swoop = new VisualAppearance(player.GetDefaultModifiedAppearance(), TownOfUsAppearances.Swooper)
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
                player.RawSetAppearance(swoop);
            }
            player?.cosmetics.ToggleNameVisible(false);
        }
    }

    private void RemoveHallucinatoryEffects()
    {
        foreach (var player in PlayerControl.AllPlayerControls.ToArray().Where(x => x.Data != null && !x.Data.IsDead))
        {
            player.RawSetAppearance(player.GetDefaultModifiedAppearance());
            player.cosmetics.ToggleNameVisible(true);
        }
    }
}
