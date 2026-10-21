namespace TouMegaChujoweExtension.Utilities;

/// <summary>
/// The extension switches the vision shadows off for its own camera effects (Falcon zoom, Sniper aim, Poisoner
/// vine, Pelican stomach). Turning them back on must land where TOU-Mira would have them - on for a living
/// player, off for a ghost or while TOU's own zoom is out - not simply "on".
/// </summary>
public static class ShadowQuadUtils
{
    public static bool TouWantsShadows
    {
        get
        {
            var local = PlayerControl.LocalPlayer;
            return local != null && local.Data != null && !local.Data.IsDead &&
                   !TownOfUs.Patches.HudManagerPatches.Zooming;
        }
    }

    public static void Restore()
    {
        if (!HudManager.InstanceExists || HudManager.Instance.ShadowQuad == null)
        {
            return;
        }

        var local = PlayerControl.LocalPlayer;
        var swallowed = local != null && PelicanSystem.IsSwallowed(local.PlayerId);
        HudManager.Instance.ShadowQuad.gameObject.SetActive(TouWantsShadows && !swallowed);
    }
}
