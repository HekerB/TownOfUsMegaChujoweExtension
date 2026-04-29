using System.Collections;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Modifiers.Types;
using MiraAPI.Utilities.Assets;
using Reactor.Utilities;
using Reactor.Utilities.Extensions;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Options.Roles.Crewmate;
using TouMegaChujoweExtension.Events.Crewmate;
using TownOfUs.Modifiers;
using TownOfUs.Options;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Modifiers.Crewmate;

public sealed class DoctorShieldModifier : TimedModifier
{
    public PlayerControl Doctor { get; }
    public override string ModifierName => "Doctor Shield";
    public override bool HideOnUi => true;

    private float _duration;
    private DoctorEffectDurationType _durationType;

    public DoctorShieldModifier(PlayerControl doctor, float duration, DoctorEffectDurationType durationType)
    {
        Doctor = doctor;
        _duration = duration;
        _durationType = durationType;
    }

    public override float Duration
    {
        get
        {
            return _durationType switch
            {
                DoctorEffectDurationType.AllRound => -1f,
                DoctorEffectDurationType.AllGame => -1f,
                DoctorEffectDurationType.SetTime => _duration,
                _ => _duration
            };
        }
    }
    
    private GameObject? _holderFront;
    private GameObject? _holderBack;
    private SpriteRenderer? _frontSr;
    private SpriteRenderer? _backSr;
    private IEnumerator? _animCoroutineRef;

    private bool CanLocalSeeShield()
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || Player == null || Doctor == null)
            return false;

        var isTarget = local.PlayerId == Player.PlayerId;
        var isDoctor = local.PlayerId == Doctor.PlayerId;

        var options = OptionGroupSingleton<DoctorOptions>.Instance;
        
        if (isTarget && options.TargetSeesShield) return true;
        if (isDoctor && options.DoctorSeesShieldFlash) return true;
        
        return false;
    }

    public override void OnActivate()
    {
        if (Player == null) return;

        var cosmeticsLayer = Player.transform.GetChild(2);
        var playerLayer = Player.gameObject.layer;

        var spriteMat = new Material(Shader.Find("Sprites/Default"));

        var frontFrames = TouExtensionAnims.GuardAnimFront;
        var backFrames = TouExtensionAnims.GuardAnimBack;

        var bodyRenderer = Player.cosmetics.currentBodySprite?.BodySprite;
        var bodyMask = bodyRenderer != null ? bodyRenderer.maskInteraction : SpriteMaskInteraction.None;

        var doctorColor = TouExtensionColors.Doctor;

        // === BACK SHIELD ===
        if (backFrames.Length > 0)
        {
            _holderBack = new GameObject("DoctorShield_Back");
            _holderBack.transform.SetParent(cosmeticsLayer, false);
            _holderBack.transform.localPosition = new Vector3(0f, 0.1f, 0.04f);

            var backObj = new GameObject("Shield_Back_Sprite");
            backObj.transform.SetParent(_holderBack.transform, false);
            backObj.transform.localScale = new Vector3(1.1f, 1.1f, 1f);
            backObj.transform.localPosition = Vector3.zero;

            _backSr = backObj.AddComponent<SpriteRenderer>();
            _backSr.material = new Material(spriteMat);
            _backSr.sprite = backFrames[0];
            _backSr.color = doctorColor;
            _backSr.maskInteraction = bodyMask;

            SetLayerRecursive(_holderBack, playerLayer);
        }

        // === FRONT SHIELD ===
        if (frontFrames.Length > 0)
        {
            _holderFront = new GameObject("DoctorShield_Front");
            _holderFront.transform.SetParent(cosmeticsLayer, false);
            _holderFront.transform.localPosition = new Vector3(0f, 0.1f, -0.04f);

            var frontObj = new GameObject("Shield_Front_Sprite");
            frontObj.transform.SetParent(_holderFront.transform, false);
            frontObj.transform.localScale = new Vector3(2.1f, 2.1f, 1f);
            frontObj.transform.localPosition = Vector3.zero;

            _frontSr = frontObj.AddComponent<SpriteRenderer>();
            _frontSr.material = new Material(spriteMat);
            _frontSr.sprite = frontFrames[0];
            _frontSr.color = doctorColor;
            _frontSr.maskInteraction = bodyMask;

            SetLayerRecursive(_holderFront, playerLayer);
        }

        if (bodyRenderer != null)
        {
            var baseOrder = bodyRenderer.sortingOrder;
            if (_frontSr != null)
            {
                _frontSr.sortingLayerID = bodyRenderer.sortingLayerID;
                _frontSr.sortingOrder = baseOrder;
            }
            if (_backSr != null)
            {
                _backSr.sortingLayerID = bodyRenderer.sortingLayerID;
                _backSr.sortingOrder = baseOrder;
            }
        }

        _animCoroutineRef = AnimateShield(frontFrames, backFrames, 6f);
        Coroutines.Start(_animCoroutineRef);
    }

    private static void SetLayerRecursive(GameObject obj, int layer)
    {
        obj.layer = layer;
        for (var i = 0; i < obj.transform.childCount; i++)
            SetLayerRecursive(obj.transform.GetChild(i).gameObject, layer);
    }

    private IEnumerator AnimateShield(Sprite[] frontFrames, Sprite[] backFrames, float fps)
    {
        var frameTime = 1f / fps;
        var maxFrames = Mathf.Max(frontFrames.Length, backFrames.Length);
        if (maxFrames == 0) yield break;

        var index = 0;
        while (true)
        {
            if (_frontSr != null && frontFrames.Length > 0)
                _frontSr.sprite = frontFrames[index % frontFrames.Length];
            if (_backSr != null && backFrames.Length > 0)
                _backSr.sprite = backFrames[index % backFrames.Length];

            index = (index + 1) % maxFrames;
            yield return new WaitForSeconds(frameTime);
        }
    }

    public override void OnDeactivate()
    {
        if (_animCoroutineRef != null)
        {
            Coroutines.Stop(_animCoroutineRef);
            _animCoroutineRef = null;
        }

        _frontSr = null;
        _backSr = null;

        if (_holderFront != null) _holderFront.Destroy();
        if (_holderBack != null) _holderBack.Destroy();

        _holderFront = null;
        _holderBack = null;
    }

    public override void FixedUpdate()
    {
        if (Player == null || Doctor == null)
        {
            return;
        }

        if (MeetingHud.Instance)
        {
            if (_holderFront != null) _holderFront.SetActive(false);
            if (_holderBack != null) _holderBack.SetActive(false);
            return;
        }

        var canSee = CanLocalSeeShield();
        var isVisible = canSee && !Player.IsConcealed();

        if (_holderFront != null) _holderFront.SetActive(isVisible);
        if (_holderBack != null) _holderBack.SetActive(isVisible);

        if (!isVisible)
            return;

        var bodyRenderer = Player.cosmetics.currentBodySprite?.BodySprite;
        if (bodyRenderer == null)
            return;

        var bodyAlpha = bodyRenderer.color.a;
        var baseOrder = bodyRenderer.sortingOrder;

        if (_frontSr != null)
        {
            _frontSr.maskInteraction = bodyRenderer.maskInteraction;
            _frontSr.sortingLayerID = bodyRenderer.sortingLayerID;
            _frontSr.sortingOrder = baseOrder;
            var fc = _frontSr.color;
            _frontSr.color = new Color(fc.r, fc.g, fc.b, bodyAlpha);
        }

        if (_backSr != null)
        {
            _backSr.maskInteraction = bodyRenderer.maskInteraction;
            _backSr.sortingLayerID = bodyRenderer.sortingLayerID;
            _backSr.sortingOrder = baseOrder;
            var bc = _backSr.color;
            _backSr.color = new Color(bc.r, bc.g, bc.b, bodyAlpha);
        }
    }
}
