using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Modifiers;
using MiraAPI.Utilities;
using Object = UnityEngine.Object;
using TownOfUs.Assets;
using TownOfUs.Extensions;
using TownOfUs.Modifiers;
using TownOfUs.Modules.Localization;
using TownOfUs.Patches;
using TownOfUs.Utilities.Appearances;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Modifiers.Crewmate;

public sealed class VanishModifier : ConcealedModifier, IVisualAppearance
{
    public override string ModifierName => "Vanished";
    public override float Duration => OptionGroupSingleton<VanisherOptions>.Instance.VanishDuration;
    public override bool HideOnUi => true;
    public override bool AutoStart => true;
    public override bool VisibleToOthers => false;
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
            RendererColor = Player.AmOwner ? new Color(0f, 0f, 0f, 0.1f) : Color.clear,
            NameColor = Color.clear,
            ColorBlindTextColor = Color.clear
        };
    }

    public override void OnDeath(DeathReason reason)
    {
        Player.RemoveModifier(this);
    }

    public override void OnMeetingStart()
    {
        Player.RemoveModifier(this);
    }

    public override void OnActivate()
    {
        if (Player.AmOwner)
        {
            TouAudio.PlaySound(TouAudio.SwooperActivateSound);
        }

        Player.RawSetAppearance(this);
        Player.cosmetics.ToggleNameVisible(false);

        if (Player.AmOwner)
        {
            var button = CustomButtonSingleton<VanisherVanishButton>.Instance;
            button.OverrideSprite(TouCrewAssets.CrewUnswoopSprite.LoadAsset());
            button.OverrideName(TouLocale.Get("ExtensionRoleVanisherUnvanish", "Unvanish"));
        }
    }

    private MushroomMixupSabotageSystem? _cachedMushroom;

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        if (_cachedMushroom == null)
        {
            _cachedMushroom = Object.FindObjectOfType<MushroomMixupSabotageSystem>();
        }

        if (_cachedMushroom && _cachedMushroom.IsActive)
        {
            Player.RawSetAppearance(this);
            Player.cosmetics.ToggleNameVisible(false);
        }

    }

    public override void OnDeactivate()
    {
        Player.ResetAppearance();
        Player.cosmetics.ToggleNameVisible(true);

        if (Player.AmOwner)
        {
            var button = CustomButtonSingleton<VanisherVanishButton>.Instance;
            button.OverrideSprite(TouCrewAssets.CrewSwoopSprite.LoadAsset());
            button.OverrideName(TouLocale.Get("ExtensionRoleVanisherVanish", "Vanish"));

            if (MeetingHud.Instance == null)
            {
                TouAudio.PlaySound(TouAudio.SwooperDeactivateSound);
            }
        }

        if (HudManagerPatches.CamouflageCommsEnabled)
        {
            Player.cosmetics.ToggleNameVisible(false);
        }

        var mushroom = Object.FindObjectOfType<MushroomMixupSabotageSystem>();
        if (mushroom && mushroom.IsActive)
        {
            SwoopMushroomHelper(mushroom, Player);
        }
    }

    public static void SwoopMushroomHelper(MushroomMixupSabotageSystem instance, PlayerControl player)
    {
        if (player != null && !player.Data.IsDead && instance.currentMixups.ContainsKey(player.PlayerId))
        {
            var condensedOutfit = instance.currentMixups[player.PlayerId];
            var playerOutfit = instance.ConvertToPlayerOutfit(condensedOutfit);
            playerOutfit.NamePlateId = player.Data.DefaultOutfit.NamePlateId;
            player.MixUpOutfit(playerOutfit);
        }
    }
}















