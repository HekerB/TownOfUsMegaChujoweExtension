using MiraAPI.Modifiers;
using MiraAPI.Modifiers.Types;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Modules;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities.Appearances;
using UnityEngine;

namespace TouMegaChujoweExtension.Modifiers.Impostor;

public sealed class InverterDisorientedModifier : TimedModifier, IVisualAppearance
{
    private readonly float duration;

    public InverterDisorientedModifier(float duration)
    {
        this.duration = duration;
    }

    public override string ModifierName => TouLocale.Get("ExtensionModifierDisoriented", "Disoriented");
    public override bool HideOnUi => true;
    public override LoadableAsset<Sprite>? ModifierIcon => null;
    public override float Duration => duration;
    public override bool AutoStart => true;

    public override void OnActivate()
    {
        Player.RawSetAppearance(this);
        if (Player.AmOwner)
        {
            // A task, the cameras, vitals or the map open while the screen turns stayed upside down with it;
            // the flipped player is taken out of whatever they had open first.
            CloseOpenInterfaces();
            InverterCameraBehaviour.Apply();
        }
    }

    public override void OnDeactivate()
    {
        Player?.ResetAppearance(fullReset: true);
        if (Player != null && Player.AmOwner)
        {
            InverterCameraBehaviour.ResetCamera();

            // A task opened during the flip was turned with it (InverterMinigameBeginPatch).
            if (Minigame.Instance != null)
            {
                var euler = Minigame.Instance.transform.localEulerAngles;
                Minigame.Instance.transform.localEulerAngles = new Vector3(euler.x, euler.y, 0f);
            }
        }
    }

    private static void CloseOpenInterfaces()
    {
        try
        {
            if (Minigame.Instance != null)
            {
                Minigame.Instance.Close();
            }
        }
        catch
        {
            // Some minigames tear themselves down while closing; nothing to undo here.
        }

        try
        {
            if (MapBehaviour.Instance != null && MapBehaviour.Instance.IsOpen)
            {
                MapBehaviour.Instance.Close();
            }
        }
        catch
        {
            // The map may already be closing.
        }
    }

    public override void OnMeetingStart()
    {
        if (Player.AmOwner)
        {
            InverterCameraBehaviour.ResetCamera();
        }

        Player.RemoveModifier(this);
    }

    public VisualAppearance GetVisualAppearance()
    {
        return Player.GetDefaultAppearance();
    }
}
