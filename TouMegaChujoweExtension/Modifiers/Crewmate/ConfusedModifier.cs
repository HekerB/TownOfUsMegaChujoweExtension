using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Patches;
using TownOfUs.Modifiers;
using TownOfUs.Utilities.Appearances;
using TownOfUs.Extensions;
using TownOfUs.Utilities;
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

    private readonly System.Collections.Generic.Dictionary<byte, VisualAppearance> _hallucinations = new();

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

    public override void Update()
    {
        if (Player == null || !Player.AmOwner) return;

        foreach (var kvp in _hallucinations)
        {
            var p = MiscUtils.PlayerById(kvp.Key);
            if (p != null && p.Data != null && !p.Data.IsDead)
            {
                if (p.CurrentOutfitType != (PlayerOutfitType)kvp.Value.AppearanceType)
                {
                    p.RawSetAppearance(kvp.Value);
                }
                if (p.cosmetics.nameText.gameObject.activeSelf)
                {
                    p.cosmetics.ToggleNameVisible(false);
                }
            }
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
        _hallucinations.Clear();

        foreach (var player in players)
        {
            VisualAppearance targetAppearance;
            var hidden = Random.Range(0, 3);
            if (hidden == 0)
            {
                var seeker = players[Random.Range(0, players.Count)];
                if (seeker != null && seeker != player)
                {
                    targetAppearance = seeker.GetDefaultModifiedAppearance();
                }
                else
                {
                    targetAppearance = player.GetDefaultModifiedAppearance();
                }
            }
            else if (hidden == 1)
            {
                targetAppearance = new VisualAppearance(player.GetDefaultAppearance(), TownOfUsAppearances.Camouflage)
                {
                    ColorId = player.Data.DefaultOutfit.ColorId,
                    HatId = string.Empty,
                    SkinId = string.Empty,
                    VisorId = string.Empty,
                    PlayerName = string.Empty,
                    PetId = string.Empty,
                    NameVisible = false,
                    PlayerMaterialColor = Color.grey
                };
            }
            else
            {
                targetAppearance = new VisualAppearance(player.GetDefaultModifiedAppearance(), TownOfUsAppearances.Swooper)
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

            _hallucinations[player.PlayerId] = targetAppearance;
            player.RawSetAppearance(targetAppearance);
            player.cosmetics.ToggleNameVisible(false);
        }
    }

    private void RemoveHallucinatoryEffects()
    {
        _hallucinations.Clear();
        foreach (var player in PlayerControl.AllPlayerControls.ToArray().Where(x => x.Data != null && !x.Data.IsDead))
        {
            player.RawSetAppearance(player.GetDefaultModifiedAppearance());
            player.cosmetics.ToggleNameVisible(true);
        }
    }
}
