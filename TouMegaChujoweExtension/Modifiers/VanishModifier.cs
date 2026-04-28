using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Modifiers;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Buttons.Crewmate;
using TouMegaChujoweExtension.Options.Roles.Crewmate;
using TownOfUs.Assets;
using TownOfUs.Extensions;
using TownOfUs.Modifiers;
using TownOfUs.Modules.Localization;
using TownOfUs.Patches;
using TownOfUs.Utilities;
using TownOfUs.Utilities.Appearances;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TouMegaChujoweExtension.Modifiers;

public sealed class VanishModifier : ConcealedModifier, IVisualAppearance
{
    public override string ModifierName => "Vanished";
    public override float Duration => OptionGroupSingleton<VanisherOptions>.Instance.VanishDuration;
    public override bool HideOnUi => true;
    public override bool AutoStart => true;
    public override bool VisibleToOthers => false;
    public bool VisualPriority => true;

    private float _notifTimer;

    public VisualAppearance GetVisualAppearance()
    {
        var playerColor = Player.AmOwner
            ? new Color(1f, 1f, 1f, 0.3f)
            : Color.clear;

        return new VisualAppearance(Player.GetDefaultModifiedAppearance(), TownOfUsAppearances.Swooper)
        {
            HatId = string.Empty,
            SkinId = string.Empty,
            VisorId = string.Empty,
            PlayerName = string.Empty,
            PetId = string.Empty,
            RendererColor = playerColor,
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
        _notifTimer = 0f;

        if (Player.AmOwner)
        {
            TouAudio.PlaySound(TouAudio.SwooperActivateSound);
        }

        Player.RawSetAppearance(this);
        Player.cosmetics.ToggleNameVisible(false);

        if (Player.AmOwner)
        {
            var button = CustomButtonSingleton<VanisherVanishButton>.Instance;
            button.OverrideSprite(TouExtensionCrewAssets.UnvanishButtonSprite.LoadAsset());
            button.OverrideName(TouLocale.Get("ExtensionRoleVanisherUnvanish", "Unvanish"));
        }
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        var mushroom = Object.FindObjectOfType<MushroomMixupSabotageSystem>();
        if (mushroom && mushroom.IsActive)
        {
            Player.RawSetAppearance(this);
            Player.cosmetics.ToggleNameVisible(false);
        }

        if (!Player.AmOwner)
        {
            CheckDetection();
        }
    }

    private void CheckDetection()
    {
        if (!OptionGroupSingleton<VanisherOptions>.Instance.DetectionEnabled)
            return;

        var local = PlayerControl.LocalPlayer;
        if (local == null || local.Data == null || local.Data.IsDead || local.Data.Disconnected)
            return;

        if (!local.IsImpostor() && !local.IsNeutral())
            return;

        _notifTimer -= Time.fixedDeltaTime;
        if (_notifTimer > 0f)
            return;

        var radius = OptionGroupSingleton<VanisherOptions>.Instance.DetectionRadius.Value;
        var dist = Vector2.Distance(local.GetTruePosition(), Player.GetTruePosition());

        if (dist <= radius)
        {
            _notifTimer = OptionGroupSingleton<VanisherOptions>.Instance.NotificationCooldown.Value;

            var notif = Helpers.CreateAndShowNotification(
                "You are being watched.....",
                Color.white,
                new Vector3(0f, 1f, -20f),
                spr: TouExtensionIcons.VanisherRoleIcon.LoadAsset());
            notif.AdjustNotification();
        }
    }

    public override void OnDeactivate()
    {
        Player.ResetAppearance();
        Player.cosmetics.ToggleNameVisible(true);

        if (Player.AmOwner)
        {
            var button = CustomButtonSingleton<VanisherVanishButton>.Instance;
            button.OverrideSprite(TouExtensionCrewAssets.VanishButtonSprite.LoadAsset());
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
