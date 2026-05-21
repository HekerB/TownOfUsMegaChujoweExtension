using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Options.Roles.Crewmate;
using TouMegaChujoweExtension.Roles.Classic.Crewmate;
using TouMegaChujoweExtension.Assets;
using TownOfUs.Buttons;
using TownOfUs.Extensions;
using TownOfUs.Utilities;
using UnityEngine;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Buttons.Classic.Crewmate;

public sealed class BuilderPlaceButton : TownOfUsRoleButton<BuilderRole>
{
    public static BuilderPlaceButton? Instance { get; private set; }

    public BuilderPlaceButton()
    {
        Instance = this;
    }

    public override string Name => TouLocale.Get("ExtensionRoleBuilderPlace", "Place");
    public override BaseKeybind Keybind => Keybinds.SecondaryAction; // Keybind F
    public override Color TextOutlineColor => Palette.CrewmateBlue;
    public override float Cooldown => OptionGroupSingleton<BuilderOptions>.Instance.Cooldown;
    public override LoadableAsset<Sprite> Sprite => TouExtensionImpAssets.BuilderButtonSprite;
    public override int MaxUses => 0;

    public override bool CanUse()
    {
        if (MeetingHud.Instance || ExileController.Instance) return false;
        if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.HasDied()) return false;
        
        var role = PlayerControl.LocalPlayer.GetRole<BuilderRole>();
        if (role == null) return false;

        // Can build if cooldown is done and not inside vent
        return Timer <= 0f && !PlayerControl.LocalPlayer.inVent;
    }

    public override bool CanClick()
    {
        return CanUse();
    }

    private bool _isProcessingClick;

    public override void ClickHandler()
    {
        if (_isProcessingClick) return;
        _isProcessingClick = true;

        try
        {
            if (!CanUse()) return;
            OnClick();
        }
        finally
        {
            Reactor.Utilities.Coroutines.Start(ResetProcessingFlag());
        }
    }

    private System.Collections.IEnumerator ResetProcessingFlag()
    {
        yield return new WaitForSeconds(0.2f);
        _isProcessingClick = false;
    }

    protected override void OnClick()
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null) return;

        var role = player.GetRole<BuilderRole>();
        if (role == null) return;

        role.PlaceStructureLocal();
        Timer = Cooldown;
    }
}
