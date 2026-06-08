using MiraAPI.GameOptions;
using MiraAPI.Modifiers.Types;
using MiraAPI.Utilities;
using MiraAPI.Utilities.Assets;
using Reactor.Utilities.Extensions;
using TouMegaChujoweExtension.Assets;
using TownOfUs.Patches;
using TownOfUs.Utilities.Appearances;
using UnityEngine;

namespace TouMegaChujoweExtension.Modifiers.Impostor;

public sealed class VoodooConfusedModifier(PlayerControl voodooMaster, float duration) : TimedModifier
{
    public PlayerControl VoodooMaster { get; } = voodooMaster;

    public override string ModifierName => "Voodoo Confused";
    public override LoadableAsset<Sprite>? ModifierIcon => TouExtensionIcons.VoodooRoleIcon;
    public override float Duration => duration;
    public override bool AutoStart => true;
    public override bool HideOnUi => true;

    public override void OnActivate()
    {
        if (!Player.AmOwner)
        {
            return;
        }

        List<string> hats = [];
        List<string> skins = [];
        List<string> visors = [];
        List<string> pets = [];
        List<int> colors = [];

        foreach (var plr in Helpers.GetAlivePlayers())
        {
            hats.Add(plr.Data.DefaultOutfit.HatId);
            skins.Add(plr.Data.DefaultOutfit.SkinId);
            visors.Add(plr.Data.DefaultOutfit.VisorId);
            pets.Add(plr.Data.DefaultOutfit.PetId);
            colors.Add(plr.Data.DefaultOutfit.ColorId);
        }

        foreach (var plr in Helpers.GetAlivePlayers())
        {
            var randomSize = UnityEngine.Random.RandomRangeInt(3, 5) * 0.2f;
            var morph = new VisualAppearance(Player.GetDefaultAppearance(), TownOfUsAppearances.Morph)
            {
                HatId = hats.Random(),
                SkinId = skins.Random(),
                VisorId = visors.Random(),
                PetId = pets.Random(),
                ColorId = colors.Random(),
                NameColor = Color.clear,
                ColorBlindTextColor = Color.clear,
                Size = new Vector3(randomSize, randomSize, 1f)
            };

            plr.RawSetAppearance(morph);
            plr.cosmetics.ToggleNameVisible(false);
        }
    }

    public override void OnDeath(DeathReason reason)
    {
        ModifierComponent?.RemoveModifier(this);
    }

    public override void OnMeetingStart()
    {
        ModifierComponent?.RemoveModifier(this);
    }

    public override void OnDeactivate()
    {
        foreach (var player in Helpers.GetAlivePlayers())
        {
            player.ResetAppearance();
            player.cosmetics.ToggleNameVisible(true);

            if (HudManagerPatches.CamouflageCommsEnabled)
            {
                player.cosmetics.ToggleNameVisible(false);
            }

            if (VanillaSystemCheckPatches.ShroomSabotageSystem != null &&
                VanillaSystemCheckPatches.ShroomSabotageSystem.IsActive)
            {
                MushroomMixUp(VanillaSystemCheckPatches.ShroomSabotageSystem, player);
            }
        }
    }

    private static void MushroomMixUp(MushroomMixupSabotageSystem instance, PlayerControl player)
    {
        if (player == null || player.Data.IsDead || !instance.currentMixups.ContainsKey(player.PlayerId))
        {
            return;
        }

        var condensedOutfit = instance.currentMixups[player.PlayerId];
        var playerOutfit = instance.ConvertToPlayerOutfit(condensedOutfit);
        playerOutfit.NamePlateId = player.Data.DefaultOutfit.NamePlateId;
        player.MixUpOutfit(playerOutfit);
    }
}
