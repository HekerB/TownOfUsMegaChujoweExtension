using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Roles.Classic.Crewmate;
using TouMegaChujoweExtension.Assets;
using TownOfUs.Buttons;
using TownOfUs.Extensions;
using TownOfUs.Utilities;
using UnityEngine;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Buttons.Classic.Crewmate;

public sealed class BuilderCycleButton : TownOfUsRoleButton<BuilderRole>
{
    public override string Name => TouLocale.Get("ExtensionRoleBuilderCycle", "Cycle");
    public override BaseKeybind Keybind => Keybinds.TertiaryAction;
    public override Color TextOutlineColor => Palette.CrewmateBlue;
    public override float Cooldown => 0.5f;
    public override LoadableAsset<Sprite> Sprite => TouExtensionImpAssets.BuilderButtonSprite;
    public override int MaxUses => 0;

    public override bool CanUse()
    {
        if (MeetingHud.Instance || ExileController.Instance) return false;
        if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.HasDied()) return false;
        
        var role = PlayerControl.LocalPlayer.GetRole<BuilderRole>();
        if (role == null) return false;

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

        // Cycle through all 4 structure types
        int nextType = ((int)role.CurrentStructureType + 1) % 4;
        role.CurrentStructureType = (BuilderStructureType)nextType;

        // Notify local player on their screen using premium Vanisher-style notification
        MiraAPI.Utilities.Helpers.CreateAndShowNotification(
            $"Active Structure: {role.CurrentStructureType}", 
            Color.white, 
            new Vector3(0f, 1f, -20f), 
            spr: TouExtensionIcons.BuilderRoleIcon.LoadAsset())?.AdjustNotification();

        Timer = Cooldown;
    }
}
