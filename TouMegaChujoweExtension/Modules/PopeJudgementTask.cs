using System.Collections;
using AmongUs.Data;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Utilities;
using Reactor.Utilities.Attributes;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TownOfUs;
using TownOfUs.Assets;
using TownOfUs.Modifiers.Game.Universal;
using TownOfUs.Modules.Anims;
using TownOfUs.Utilities;
using TownOfUs.Modules.Localization;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TouMegaChujoweExtension.Modules;

[RegisterInIl2Cpp]
public sealed class PopeJudgementTask(nint cppPtr) : PlayerTask(cppPtr)
{
    public override int TaskStep => !IsComplete ? 0 : 1;
    public override bool IsComplete => _isComplete;
    private bool _isComplete;
    private bool _triggeredJudgement;
    private PopeJudgementSystem? _sabotage;
    private Coroutine? _flash;

    public override bool ValidConsole(Console console) => false;

    private void FixedUpdate()
    {
        if (IsComplete) return;
        if (_sabotage != null && !_sabotage.IsActive) Complete();
    }

    private float _ogShakeAmt;
    private bool _ogShakeEnabled;
    private float _ogShakePeriod;
    private bool _even;

    public override void Initialize()
    {
        _sabotage = ShipStatus.Instance.Systems[(SystemTypes)PopeJudgementSystem.SabotageId]
            .Cast<PopeJudgementSystem>();
        _flash ??= HudManager.Instance.StartCoroutine(CoFlash().WrapToIl2Cpp());

        _ogShakeEnabled = DataManager.Settings.Gameplay.ScreenShake;
        _ogShakeAmt = HudManager.Instance.PlayerCam.shakeAmount;
        _ogShakePeriod = HudManager.Instance.PlayerCam.shakePeriod;
        DataManager.Settings.Gameplay.ScreenShake = true;

        var text = TouLocale.GetParsed("ExtensionRolePopeWarningNotif")
            .Replace("<role>",
                $"{TouExtensionColors.Pope.ToTextColor()}{TouLocale.Get("ExtensionRolePope")}</color>");

        var notif1 = Helpers.CreateAndShowNotification(
            text.Replace("<time>",
                $"{(int)OptionGroupSingleton<PopeOptions>.Instance.JudgementDuration}"),
            Color.white, new Vector3(0f, 1f, -20f),
            spr: TouExtensionIcons.PopeRoleIcon.LoadAsset());
        notif1.AdjustNotification();
    }

    [HideFromIl2Cpp]
    private IEnumerator CoFlash()
    {
        var wait = new WaitForSeconds(1f);
        var playSound = false;

        // Golden color for Pope
        var goldBg = new Color(1f, 0.84f, 0f, 0.37254903f);

        while (_sabotage != null && _sabotage.IsActive)
        {
            var disableBlare = MeetingHud.Instance != null || ExileController.Instance != null;

            // Podczas meetingu/exile: wyłącz wszystkie efekty wizualne i dźwiękowe
            if (disableBlare && _sabotage.Stage != PopeJudgementStage.Finished && _sabotage.Stage != PopeJudgementStage.Ending)
            {
                HudManager.Instance.FullScreen.gameObject.SetActive(false);
                SoundManager.Instance.StopSound(TouExtensionAudio.PopeAlarmSound.LoadAsset());
                HudManager.Instance.PlayerCam.shakeAmount = _ogShakeAmt;
                HudManager.Instance.PlayerCam.shakePeriod = _ogShakePeriod;
                yield return wait;
                continue;
            }

            if (_sabotage.Stage == PopeJudgementStage.Countdown)
            {
                HudManager.Instance.FullScreen.color = new Color(0.5f, 0.4f, 0f, playSound ? 0.18f : 0.34f);
                HudManager.Instance.FullScreen.gameObject.SetActive(true);
                HudManager.Instance.PlayerCam.shakeAmount = 0.03f;
                HudManager.Instance.PlayerCam.shakePeriod = 16f;

                playSound = !playSound;
                if (playSound && !disableBlare)
                {
                    SoundManager.Instance.StopSound(TouExtensionAudio.PopeAlarmSound.LoadAsset());
                    SoundManager.Instance.PlaySound(TouExtensionAudio.PopeAlarmSound.LoadAsset(), false, 3f);
                }
            }
            else if (_sabotage.Stage == PopeJudgementStage.PopeDead)
            {
                if (!_sabotage.BombFinished)
                {
                    HudManager.Instance.FullScreen.color = new Color(Palette.CrewmateBlue.r,
                        Palette.CrewmateBlue.g, Palette.CrewmateBlue.b, playSound ? 0.18f : 0.34f);
                    HudManager.Instance.FullScreen.gameObject.SetActive(true);
                    HudManager.Instance.PlayerCam.shakeAmount = 0f;
                    HudManager.Instance.PlayerCam.shakePeriod = 1f;

                    playSound = !playSound;
                    if (playSound)
                    {
                        SoundManager.Instance.StopSound(TouExtensionAudio.PopeAlarmSound.LoadAsset());
                        SoundManager.Instance.PlaySound(TouExtensionAudio.PopeAlarmSound.LoadAsset(), false, 0.1f);
                    }
                }
            }
            else if (_sabotage.Stage == PopeJudgementStage.Finished)
            {
                if (!_triggeredJudgement)
                {
                    // Use hexbomb death prefab from Mira/TownOfUs base assets
                    var deathAnim = AnimStore.SpawnAnimBody(PlayerControl.LocalPlayer,
                        TouAssets.HexBombDeathPrefab.LoadAsset());
                    
                    var bgParent = deathAnim != null ? deathAnim.transform.GetParent().transform : HudManager.Instance.FullScreen.transform.parent;
                    var bg = Object.Instantiate(HudManager.Instance.FullScreen, bgParent);
                    
                    bg.gameObject.name = "Gold Background";
                    bg.color = goldBg;
                    bg.transform.localScale *= 20f;

                    if (deathAnim != null)
                    {
                        deathAnim.name = "Judgement Animation";
                        deathAnim.SetActive(false);
                        var deathRend = deathAnim.GetComponent<SpriteRenderer>();
                        deathRend.color = new Color(0f, 0f, 0f, 0.17254903f);
                        
                        deathAnim.transform.localPosition += new Vector3(
                            PlayerControl.LocalPlayer.MyPhysics.FlipX ? 0f : -0.4f, 0.1f,
                            bg.transform.localPosition.z - 100f);
                        
                        deathAnim.gameObject.layer = bg.gameObject.layer;

                        if (PlayerControl.LocalPlayer.HasModifier<GiantModifier>())
                            deathAnim.transform.localPosition += new Vector3(0.5f, 0.2f, 0f);
                        else if (PlayerControl.LocalPlayer.HasModifier<MiniModifier>())
                            deathAnim.transform.localPosition += new Vector3(0f, -0.05f, 0f);

                        SoundManager.Instance.StopSound(TouExtensionAudio.PopeAlarmSound.LoadAsset());
                        SoundManager.Instance.PlaySound(TouExtensionAudio.PopeJudgementSound.LoadAsset(), false, 1f);
                        HudManager.Instance.FullScreen.gameObject.SetActive(true);
                        HudManager.Instance.FullScreen.color = goldBg;
                        deathAnim.SetActive(true);
                        yield return MiscUtils.FadeInDualRenderers(bg, deathRend, 0.01f, 0.03f, 2f);
                    }
                    else
                    {
                        SoundManager.Instance.StopSound(TouExtensionAudio.PopeAlarmSound.LoadAsset());
                        SoundManager.Instance.PlaySound(TouExtensionAudio.PopeJudgementSound.LoadAsset(), false, 1f);
                        HudManager.Instance.FullScreen.gameObject.SetActive(true);
                        HudManager.Instance.FullScreen.color = goldBg;
                        bg.color = new Color(goldBg.r, goldBg.g, goldBg.b, 0.8f);
                    }

                    yield return new WaitForSeconds(6f);
                    if (bg != null) Object.Destroy(bg);
                    if (deathAnim != null) Object.Destroy(deathAnim);
                    _triggeredJudgement = true;
                }
            }
            else if (_sabotage.Stage is PopeJudgementStage.Initiate or PopeJudgementStage.Countdown)
            {
                // Initiate/Countdown stage - golden flashing
                HudManager.Instance.FullScreen.color = goldBg;
                if (!HudManager.Instance.FullScreen.gameObject.activeSelf && !disableBlare)
                {
                    SoundManager.Instance.StopSound(TouExtensionAudio.PopeAlarmSound.LoadAsset());
                    SoundManager.Instance.PlaySound(TouExtensionAudio.PopeAlarmSound.LoadAsset(), false, 3f);
                }
                HudManager.Instance.FullScreen.gameObject.SetActive(
                    !HudManager.Instance.FullScreen.gameObject.activeSelf);
                _triggeredJudgement = false;
            }
            else if (_sabotage.Stage == PopeJudgementStage.Ending)
            {
                HudManager.Instance.FullScreen.gameObject.SetActive(false);
                SoundManager.Instance.StopSound(TouExtensionAudio.PopeAlarmSound.LoadAsset());
                SoundManager.Instance.StopSound(TouExtensionAudio.PopeJudgementSound.LoadAsset());
            }
            yield return wait;
        }
    }

    public override void AppendTaskText(Il2CppSystem.Text.StringBuilder sb)
    {
        if (_sabotage == null) return;
        _even = !_even;
        var color = _even ? TouExtensionColors.Pope : Color.white;
        if (_sabotage.Stage == PopeJudgementStage.Countdown)
        {
            color = _even ? new Color(0.8f, 0.68f, 0f) : Color.white;
        }

        var text = "Divine Judgement has been triggered!";
        switch (_sabotage.Stage)
        {
            case PopeJudgementStage.Initiate:
                text =
                    $"The Pope is unleashing Divine Judgement!\n{(int)_sabotage.TimeRemaining + 1 + (int)PopeJudgementSystem.ConfiguredDuration} seconds left!";
                break;
            case PopeJudgementStage.Countdown:
                text =
                    $"The Pope is unleashing Divine Judgement!\n{(int)_sabotage.TimeRemaining + 1} seconds left!";
                break;
            case PopeJudgementStage.PopeDead:
                color = Palette.CrewmateBlue;
                text = "The Pope has perished!";
                break;
        }

        sb.AppendLine($"{color.ToTextColor()}\n{text}</color>");
    }

    public override void Complete()
    {
        if (_flash != null)
        {
            HudManager.Instance.StopCoroutine(_flash);
            _flash = null;
            HudManager.Instance.FullScreen.gameObject.SetActive(false);
            SoundManager.Instance.StopSound(TouExtensionAudio.PopeAlarmSound.LoadAsset());
        }
        DataManager.Settings.Gameplay.ScreenShake = _ogShakeEnabled;
        HudManager.Instance.PlayerCam.shakeAmount = _ogShakeAmt;
        HudManager.Instance.PlayerCam.shakePeriod = _ogShakePeriod;

        _isComplete = true;
        PlayerControl.LocalPlayer.RemoveTask(this);
    }
}
