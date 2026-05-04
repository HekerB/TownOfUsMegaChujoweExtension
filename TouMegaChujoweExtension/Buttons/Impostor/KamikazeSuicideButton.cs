using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using Reactor.Utilities.Extensions;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TouMegaChujoweExtension.Roles.Impostor;
using TownOfUs.Assets;
using TownOfUs.Buttons;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Impostor;

public sealed class KamikazeSuicideButton : TownOfUsRoleButton<KamikazeRole>
{
    private GameObject? _radiusSphere;

    public bool Usable => OptionGroupSingleton<KamikazeOptions>.Instance.CanSuicideFirstRound ||
                          TownOfUs.Events.DeathEventHandlers.CurrentRound > 1 ||
                          TutorialManager.InstanceExists;

    public override string Name => TouLocale.GetParsed("ExtensionRoleKamikazeSuicide", "Suicide");
    public override BaseKeybind Keybind => Keybinds.SecondaryAction;
    public override Color TextOutlineColor => Palette.ImpostorRed;
    public override float Cooldown => OptionGroupSingleton<KamikazeOptions>.Instance.SuicideCooldown;
    public override int MaxUses => 1;
    public override LoadableAsset<Sprite> Sprite => TouExtensionImpAssets.KamikazeSuicideButtonSprite;

    public override bool CanUse()
    {
        return base.CanUse() && Usable;
    }

    protected override void OnClick()
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null || player.HasDied())
        {
            return;
        }

        DestroyRadius();
        KamikazeRole.RpcKamikazeDetonate(player);

        CustomButtonSingleton<KamikazeKillButton>.Instance.SetTimer(
            player.GetKillCooldown());
    }

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        base.FixedUpdate(playerControl);

        var player = PlayerControl.LocalPlayer;
        if (player == null || player.HasDied() || MeetingHud.Instance)
        {
            DestroyRadius();
            return;
        }

        var opts = OptionGroupSingleton<KamikazeOptions>.Instance;
        var radius = opts.DetonateRadius;
        var shouldShow = opts.ShowRadiusIndicator && Timer <= 0f && UsesLeft > 0 && Usable;

        if (shouldShow)
        {
            if (_radiusSphere == null)
            {
                _radiusSphere = MiscUtils.CreateSpherePrimitive(
                    player.transform.position, radius);
                var meshRenderer = _radiusSphere.GetComponent<MeshRenderer>();
                if (meshRenderer != null)
                {
                    var mat = new Material(AuAvengersAnims.IgniteMaterial.LoadAsset());
                    var color = mat.color;
                    color.a = 0.25f;
                    mat.color = color;
                    meshRenderer.material = mat;
                }
            }
            else
            {
                _radiusSphere.transform.position = player.transform.position;
            }
        }
        else
        {
            DestroyRadius();
        }
    }

    private void DestroyRadius()
    {
        if (_radiusSphere != null)
        {
            _radiusSphere.Destroy();
            _radiusSphere = null;
        }
    }
}
