using MiraAPI.GameOptions;
using Object = UnityEngine.Object;
using TownOfUs.Modifiers;
using UnityEngine;

namespace TouMegaChujoweExtension.Modifiers.Neutral;

public sealed class BountyHunterArrowModifier(PlayerControl owner, Color color)
    : ArrowTargetModifier(owner, color, 0)
{
    public override string ModifierName => "Bounty Hunter Arrow";

    private const float ArrowVisibleRange = 10f;

    public override void OnActivate()
    {
        base.OnActivate();

        var popup = GameManagerCreator.Instance.HideAndSeekManagerPrefab.DeathPopupPrefab;
        var item = Object.Instantiate(popup, HudManager.Instance.transform.parent);
        item.Show(Player, 0);
        if (item.text.transform.TryGetComponent<TextTranslatorTMP>(out var tmp))
        {
            tmp.defaultStr = "YOUR NEXT TARGET IS";
            tmp.TargetText = StringNames.None;
            tmp.ResetText();
        }
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        if (Arrow == null) return;

        var local = PlayerControl.LocalPlayer;
        if (local == null || Player == null)
        {
            Arrow.gameObject.SetActive(false);
            return;
        }

        if (!OptionGroupSingleton<BountyHunterOptions>.Instance.ShowArrow)
        {
            Arrow.gameObject.SetActive(false);
            return;
        }

        var distance = Vector2.Distance(
            (Vector2)local.transform.position,
            (Vector2)Player.transform.position);

        Arrow.gameObject.SetActive(distance <= ArrowVisibleRange);
    }

    public override void OnMeetingStart()
    {
        base.OnMeetingStart();
        ModifierComponent!.RemoveModifier(this);
    }
}














