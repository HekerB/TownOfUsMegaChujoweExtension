using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Options.Roles.Crewmate;
namespace TouMegaChujoweExtension.Buttons.Classic.Crewmate;

using TouMegaChujoweExtension.Roles.Classic.Crewmate;
using TownOfUs.Assets;
using TownOfUs.Buttons;
using TownOfUs.Modules.Localization;
using UnityEngine;

public sealed class GardenerGardenButton : TownOfUsRoleButton<GardenerRole>
{
    public override Color TextOutlineColor => TouExtensionColors.Gardener;
    public override string Name => TouLocale.Get("ExtensionRoleGardenerGarden", "Garden");
    public override LoadableAsset<Sprite> Sprite => TouExtensionCrewAssets.GardenerButtonSprite;
    public override BaseKeybind Keybind => Keybinds.SecondaryAction;
    public override float Cooldown => OptionGroupSingleton<GardenerOptions>.Instance.TrapCooldown;
    public override float InitialCooldown => 10f;
    public override int MaxUses => (int)OptionGroupSingleton<GardenerOptions>.Instance.MaxTraps;
    public int ExtraUses { get; set; }

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);
        if (Button != null)
        {
            Button.usesRemainingSprite.sprite = TouAssets.AbilityCounterBasicSprite.LoadAsset();
            Button.usesRemainingSprite.gameObject.SetActive(MaxUses > 0);
        }
    }

    private bool _lastMeetingState;

    public override bool CanUse()
    {
        return base.CanUse() && Role != null;
    }

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        base.FixedUpdate(playerControl);

        bool inMeeting = MeetingHud.Instance != null;
        if (_lastMeetingState && !inMeeting)
        {
            Timer = 20f;
        }
        _lastMeetingState = inMeeting;
    }

    protected override void OnClick()
    {
        if (Role == null) return;

        Role.PlaceGarden(PlayerControl.LocalPlayer.GetTruePosition());
        TownOfUs.Assets.TouAudio.PlaySound(TownOfUs.Assets.TouAudio.TrapperPlaceSound);
    }
}
