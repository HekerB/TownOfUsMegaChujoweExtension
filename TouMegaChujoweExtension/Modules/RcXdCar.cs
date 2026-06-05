using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Networking;
using MiraAPI.Utilities;
using Object = UnityEngine.Object;
using Reactor.Utilities.Extensions;
using Reactor.Utilities;
using System.Collections;
using System;
using TownOfUs.Assets;
using TownOfUs.Modifiers;
using TownOfUs.Utilities;
using UnityEngine;
using System.Linq;
using TouMegaChujoweExtension.Assets;

namespace TouMegaChujoweExtension.Modules;

public sealed class RcXdCar : IDisposable
{
    private PlayerControl? _owner;
    private GameObject? _go;
    private SpriteRenderer? _renderer;
    private AudioSource? _audio;

    private bool _detonated;
    private bool _isOwner;

    private float _speed;
    private Vector2 _velocity;

    private Transform? _lightTransformParent;
    private Vector3 _lightOriginalLocalPos;
    private GameObject? _petObject;

    private Sprite[]? _frames;
    private int _frameIndex;
    private float _frameTimer;

    // --- NET / REMOTE SMOOTHING ---
    private Vector2 _netPos;
    private bool _netFlip;
    private Vector2 _netVel;
    private Vector2 _netPrevPos;
    private float _netPrevTime;
    private float _netLastRecvTime;

    private bool _renderConfigured;

    private const float TurnSpeed = 12f;
    private const float AudioHearRadius = 4.5f;

    private const float NetSendInterval = 0.066f; // ~15 Hz

    private const float SnapDistance = 1.25f;

    public bool IsDetonated => _detonated;
    public bool IsDriving => !_detonated && _go != null;
    public Vector2 Position => _go != null ? (Vector2)_go.transform.position : Vector2.zero;

    private static bool IsInWall(Vector2 pos)
    {
        var cols = Physics2D.OverlapCircleAll(pos, 0.05f, Constants.ShipAndAllObjectsMask);
        foreach (var c in cols)
        {
            if (c != null && !c.isTrigger) return true;
        }
        return false;
    }

    private static bool IsPathBlocked(Vector2 from, Vector2 to)
    {
        var dir = to - from;
        var dist = dir.magnitude;
        if (dist < 0.001f) return false;

        var hit = Physics2D.RaycastAll(from, dir.normalized, dist + 0.05f, Constants.ShipAndAllObjectsMask);
        return hit.Any(h => h.collider != null && !h.collider.isTrigger);
    }

    private static SpriteRenderer? FindBestMaskedRendererOnLocalPlayer()
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null) return null;

        var direct = local.GetComponent<SpriteRenderer>();
        if (direct != null) return direct;

        var rends = local.GetComponentsInChildren<SpriteRenderer>(true);
        if (rends == null || rends.Length == 0) return null;

        SpriteRenderer? best = null;
        var bestScore = int.MinValue;

        for (int i = 0; i < rends.Length; i++)
        {
            var r = rends[i];
            if (r == null) continue;

            int score = 0;

            if (r.maskInteraction != SpriteMaskInteraction.None) score += 100;

            var sl = r.sortingLayerName ?? string.Empty;
            if (sl.Contains("Player", StringComparison.OrdinalIgnoreCase)) score += 50;
            if (sl.Contains("UI", StringComparison.OrdinalIgnoreCase)) score -= 200;

            var nm = r.name ?? string.Empty;
            if (nm.Contains("Name", StringComparison.OrdinalIgnoreCase)) score -= 100;

            score += Mathf.Clamp(r.sortingOrder, -50, 50);

            if (score > bestScore)
            {
                bestScore = score;
                best = r;
            }
        }

        return best;
    }

    private bool TryConfigureRendererOnce()
    {
        if (_renderer == null || _go == null) return false;

        if (ShipStatus.Instance != null && _go.transform.parent != ShipStatus.Instance.transform)
            _go.transform.SetParent(ShipStatus.Instance.transform, true);

        var src = FindBestMaskedRendererOnLocalPlayer();
        if (src == null)
        {
            var vent = Object.FindObjectOfType<Vent>();
            if (vent != null) src = vent.GetComponent<SpriteRenderer>();
        }

        if (src == null) return false;

        _renderer.maskInteraction = src.maskInteraction;
        _renderer.sortingLayerID = src.sortingLayerID;

        _renderer.sortingOrder = src.sortingOrder;

        _go.layer = src.gameObject.layer;

        return true;
    }

    public static RcXdCar Create(PlayerControl owner, Vector2 position)
    {
        var opts = OptionGroupSingleton<RcXdOptions>.Instance;
        RcXdCar car = new()
        {
            _owner = owner,
            _isOwner = owner.AmOwner,
            _speed = opts.CarSpeed,
            _velocity = Vector2.zero,

            _netPos = position,
            _netPrevPos = position,
            _netVel = Vector2.zero,
            _netPrevTime = 0f,
            _netLastRecvTime = 0f
        };

        car._go = new GameObject("RC-XD_Car");
        car._go.transform.position = new Vector3(position.x, position.y, position.y / 1000f);

        car._renderer = car._go.AddComponent<SpriteRenderer>();

        var frames = TouExtensionAnims.RcCarFrames;
        car._frames = frames.Length > 1 ? frames : null;
        car._renderer.sprite = frames.Length > 0 ? frames[0] : TouExtensionImpAssets.RcCarSprite.LoadAsset();

        car._renderConfigured = car.TryConfigureRendererOnce();
        if (!car._renderConfigured)
        {
            car._renderer.sortingOrder = 0;
            car._renderer.maskInteraction = SpriteMaskInteraction.None;
        }

        var localPlayer = PlayerControl.LocalPlayer;
        car._renderer.enabled = localPlayer != null;

        car._audio = car._go.AddComponent<AudioSource>();
        
        // Pre-load audio to avoid lag/bugs during gameplay
        car._audio.clip = TouExtensionAudio.RcSound.LoadAsset();
        TouExtensionAudio.RcExplosionSound.LoadAsset();
        TouExtensionAudio.RcSound.LoadAsset();

        car._audio.loop = true;
        car._audio.spatialBlend = 0f;
        car._audio.volume = 0f;
        car._audio.Play();

        if (owner.AmOwner)
        {
            var light = owner.lightSource;
            if (light != null)
            {
                car._lightTransformParent = light.transform.parent;
                car._lightOriginalLocalPos = light.transform.localPosition;
            }

            var cam = Camera.main?.GetComponent<FollowerCamera>();
            if (cam != null) cam.enabled = false;

            if (owner.cosmetics?.currentPet?.gameObject != null)
            {
                car._petObject = owner.cosmetics.currentPet.gameObject;
                car._petObject.SetActive(false);
            }

            Coroutines.Start(car.CoDrive());
        }
        else
        {
            Coroutines.Start(car.CoRemoteSim());
        }

        return car;
    }

    private IEnumerator CoRemoteSim()
    {
        while (_go != null && !_detonated)
        {
            if (!_renderConfigured)
                _renderConfigured = TryConfigureRendererOnce();

            if (MeetingHud.Instance)
            {
                if (_renderer != null) _renderer.enabled = false;
                if (_audio != null) _audio.volume = 0f;
                yield return null;
                continue;
            }

            var local = PlayerControl.LocalPlayer;
            bool shouldRender = local != null;
            if (_renderer != null) _renderer.enabled = shouldRender;

            var cur = (Vector2)_go.transform.position;

            var dist = Vector2.Distance(cur, _netPos);
            Vector2 next;
            if (dist > SnapDistance)
                next = _netPos;
            else
                next = Vector2.Lerp(cur, _netPos, 20f * Time.deltaTime);

            _go.transform.position = new Vector3(next.x, next.y, next.y / 1000f);

            // flip
            if (_renderer != null) _renderer.flipX = _netFlip;

            var speedMag = _netVel.magnitude;
            var moving = speedMag > 0.10f && (Time.time - _netLastRecvTime) < 0.35f;

            UpdateFrameAnimation(Time.deltaTime, moving, speedMag);
            UpdateRemotePitch();
            UpdateAudio();

            yield return null;
        }
    }

    private void UpdateFrameAnimation(float dt, bool moving, float speedMag)
    {
        if (_renderer == null) return;

        if (_frames != null && _frames.Length > 1 && moving)
        {
            var ratio = _speed <= 0.001f ? 0f : Mathf.Clamp01(speedMag / _speed);
            var fps = Mathf.Lerp(5f, 15f, ratio);

            _frameTimer += dt;
            if (_frameTimer >= 1f / fps)
            {
                _frameTimer -= 1f / fps;
                _frameIndex = (_frameIndex + 1) % _frames.Length;
                _renderer.sprite = _frames[_frameIndex];
            }
        }
        else if (_frames != null && _frames.Length > 0)
        {
            _frameTimer = 0f;
            _frameIndex = 0;
            _renderer.sprite = _frames[0];
        }
    }

    private void UpdateRemotePitch()
    {
        if (_audio == null) return;
        _audio.pitch = 1.0f;
    }

    private IEnumerator CoDrive()
    {
        yield return null;

        if (!_renderConfigured)
            _renderConfigured = TryConfigureRendererOnce();

        // deploy SFX
        var deployClip = TouAudio.TrackerActivateSound.LoadAsset();
        if (deployClip != null && _audio != null)
        {
            _audio.volume = 1f;
            _audio.PlayOneShot(deployClip, 1.0f);
        }

        var syncTimer = 0f;

        while (!_detonated && _go != null)
        {
            if (MeetingHud.Instance || ExileController.Instance)
            {
                Detonate();
                yield break;
            }

            if (_owner == null || _owner.Data == null || _owner.Data.IsDead || _owner.Data.Disconnected)
            {
                DoDestroy();
                yield break;
            }

            var dt = Time.fixedDeltaTime;

            var inputDir = AdvancedMovementUtilities.GetRegularDirection();

            var gameSpeed = GameOptionsManager.Instance != null ? GameOptionsManager.Instance.currentNormalGameOptions.PlayerSpeedMod : 1f;
            var targetSpeed = _speed * gameSpeed;

            if (inputDir != Vector2.zero)
            {
                var minSpeed = Mathf.Min(0.5f * gameSpeed, targetSpeed);
                if (_velocity.magnitude < minSpeed)
                {
                    _velocity = inputDir * minSpeed;
                }

                var targetVelocity = inputDir * targetSpeed;
                var accelRate = 3.0f * gameSpeed;
                _velocity = Vector2.MoveTowards(_velocity, targetVelocity, accelRate * dt);

                var currentSpeed = _velocity.magnitude;
                if (currentSpeed > 0.05f)
                {
                    var newDir = Vector2.Lerp(_velocity.normalized, inputDir, TurnSpeed * dt).normalized;
                    _velocity = newDir * currentSpeed;
                }
            }
            else
            {
                _velocity = Vector2.zero;
            }

            var pos = (Vector2)_go.transform.position;

            if (_velocity.sqrMagnitude > 0.001f)
            {
                var newPos = pos;
                var moveX = _velocity.x * dt;
                var moveY = _velocity.y * dt;

                if (Mathf.Abs(moveX) > 0.001f)
                {
                    var testX = new Vector2(newPos.x + moveX, newPos.y);
                    if (!IsPathBlocked(newPos, testX) && !IsInWall(testX))
                        newPos.x += moveX;
                    else
                        _velocity.x = 0f;
                }

                if (Mathf.Abs(moveY) > 0.001f)
                {
                    var testY = new Vector2(newPos.x, newPos.y + moveY);
                    if (!IsPathBlocked(new Vector2(newPos.x, newPos.y), testY) && !IsInWall(testY))
                        newPos.y += moveY;
                    else
                        _velocity.y = 0f;
                }

                if (!IsInWall(newPos))
                    pos = newPos;
                else
                    _velocity = Vector2.zero;

                _go.transform.position = new Vector3(pos.x, pos.y, pos.y / 1000f);
            }

            // anim (owner)
            UpdateFrameAnimation(dt, _velocity.magnitude > 0.10f, _velocity.magnitude);

            if (_renderer != null)
            {
                if (_velocity.x > 0.05f) _renderer.flipX = true;
                else if (_velocity.x < -0.05f) _renderer.flipX = false;
            }

            if (_owner != null && _owner.lightSource != null)
            {
                _owner.lightSource.transform.position = new Vector3(pos.x, pos.y,
                    _owner.lightSource.transform.position.z);
            }

            var mainCam = Camera.main;
            if (mainCam != null)
            {
                var cp = mainCam.transform.position;
                mainCam.transform.position = new Vector3(pos.x, pos.y, cp.z);
            }

            if (_audio != null)
            {
                _audio.pitch = 1.0f;
            }

            UpdateAudio();

            // net sync
            syncTimer += dt;
            if (syncTimer >= NetSendInterval && _owner != null)
            {
                syncTimer = 0f;
                var flip = _renderer != null && _renderer.flipX;
                RcXdRole.RpcUpdateCarPosition(_owner, pos.x, pos.y, flip ? 1f : 0f);
            }

            yield return new WaitForFixedUpdate();
        }
    }

    private void UpdateAudio()
    {
        if (_audio == null || _go == null) return;

        var local = PlayerControl.LocalPlayer;
        if (local == null)
        {
            _audio.volume = 0f;
            return;
        }



        bool isMoving = _isOwner ? _velocity.magnitude > 0.05f : _netVel.magnitude > 0.05f && (Time.time - _netLastRecvTime) < 0.35f;

        if (_isOwner)
        {
            _audio.volume = isMoving ? 0.7f : 0f;
            return;
        }

        var dist = Vector2.Distance(_go.transform.position, local.transform.position);
        float baseVol = isMoving ? 0.7f : 0f;
        _audio.volume = dist > AudioHearRadius ? 0f : Mathf.Clamp01(1f - (dist / AudioHearRadius)) * baseVol;
    }

    public void UpdatePosition(Vector2 pos, bool flipX)
    {
        if (_isOwner) return;

        _netFlip = flipX;

        var now = Time.time;
        if (_netPrevTime > 0f)
        {
            var dt = Mathf.Max(0.001f, now - _netPrevTime);
            _netVel = (pos - _netPrevPos) / dt;
        }

        _netPrevPos = pos;
        _netPrevTime = now;
        _netLastRecvTime = now;

        _netPos = pos;

        if (_go != null)
        {
            var cur = (Vector2)_go.transform.position;
            if (Vector2.Distance(cur, pos) > SnapDistance * 2f)
                _go.transform.position = new Vector3(pos.x, pos.y, pos.y / 1000f);
        }
    }

    public void Detonate()
    {
        if (_detonated) return;
        _detonated = true;
        _velocity = Vector2.zero;
        if (_renderer != null) _renderer.enabled = false;
        StopAudio();

        if (_isOwner && _owner != null)
            Coroutines.Start(CoDetonate());
        else
        {
            PlayExplosionSound();
            if (_go != null)
            {
                var opts = OptionGroupSingleton<RcXdOptions>.Instance;
                var sphere = MiscUtils.CreateSpherePrimitive(_go.transform.position, opts.DetonateRadius);
                Coroutines.Start(DestroyObjAfter(sphere, 0.5f));
            }
            DoDestroy();
        }
    }

    private IEnumerator CoDetonate()
    {
        if (_go == null)
        {
            DoDestroy();
            yield break;
        }

        var bombPos = _go.transform.position;
        var opts = OptionGroupSingleton<RcXdOptions>.Instance;

        if (_owner != null)
        {
            var radius = opts.DetonateRadius * ShipStatus.Instance.MaxLightRadius;
            var allNear = Helpers.GetClosestPlayers(bombPos, radius);
            
            // Filter for valid targets FIRST (including teammates!)
            var validTargets = allNear.Where(x => 
                x != null && 
                !x.HasDied() && 
                // x.PlayerId != _owner.PlayerId && // Removed to allow RC-XD to kill themselves
                !(x.HasModifier<BaseShieldModifier>() && x.AmOwner) && 
                !(x.HasModifier<FirstDeadShield>() && x.AmOwner)
            ).ToList();

            validTargets.Shuffle();

            // Take up to MaxKills from the VALID targets
            var targetsToKill = validTargets.Take((int)opts.MaxKillsInDetonation).ToList();

            foreach (var target in targetsToKill)
            {
                _owner.RpcCustomMurder(target,
                    createDeadBody: true, teleportMurderer: false,
                    showKillAnim: false, playKillSound: false);
            }
        }

        PlayExplosionSound();
        var bomb = MiscUtils.CreateSpherePrimitive(bombPos, opts.DetonateRadius);

        yield return new WaitForSeconds(0.5f);
        bomb?.Destroy();

        RestorePet();
        RestoreLight();
        RestoreCamera();
        PlayDisconnectSound();

        yield return new WaitForSeconds(0.3f);
        FinalDestroy();
    }

    private void PlayExplosionSound()
    {
        if (_go == null) return;

        var listenerPos = (Vector2)(Camera.main?.transform.position ?? Vector3.zero);
        var explosionPos = (Vector2)_go.transform.position;
        var dist = Vector2.Distance(explosionPos, listenerPos);

        const float maxDist = 15f;
        if (dist <= maxDist || _isOwner)
        {
            var clip = TouExtensionAudio.RcExplosionSound.LoadAsset();
            if (clip == null) return;

            // Using SoundManager for 2D \"Stereo\" feel, basing volume on camera position
            var volume = _isOwner ? 1.0f : Mathf.Clamp01(1f - (dist / maxDist)) * 0.9f;
            SoundManager.Instance.PlaySound(clip, false, volume);
        }
    }

    private void PlayDisconnectSound()
    {
        if (!_isOwner) return;
        SoundManager.Instance.PlaySound(TouAudio.TrackerDeactivateSound.LoadAsset(), false, 1.0f);
    }

    private static IEnumerator DestroyObjAfter(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        obj?.Destroy();
    }

    private void RestoreLight()
    {
        if (!_isOwner || _owner == null || _owner.lightSource == null) return;
        if (_lightTransformParent != null)
        {
            _owner.lightSource.transform.parent = _lightTransformParent;
            _owner.lightSource.transform.localPosition = _lightOriginalLocalPos;
        }
    }

    private void RestorePet()
    {
        if (!_isOwner) return;
        if (_petObject != null)
        {
            _petObject.SetActive(true);
            _petObject = null;
        }
    }

    private void StopAudio()
    {
        try
        {
            if (_audio != null)
            {
                _audio.Stop();
                _audio.volume = 0f;
                _audio.clip = null;
                UnityEngine.Object.Destroy(_audio);
            }
        }
        catch { /* ignored */ }
        _audio = null;
    }

    public void DoDestroy()
    {
        if (_go == null && _detonated) return;
        _detonated = true;
        _velocity = Vector2.zero;
        if (_renderer != null) _renderer.enabled = false;
        RestorePet();
        RestoreLight();
        RestoreCamera();
        PlayDisconnectSound();
        StopAudio();
        if (_owner?.Data?.Role is RcXdRole role && role.ActiveCar == this)
            role.ActiveCar = null;
        try { _go?.Destroy(); } catch { /* ignored */ }
        _go = null;
        _renderer = null;
    }

    private void FinalDestroy()
    {
        RestorePet();
        StopAudio();
        if (_renderer != null) _renderer.enabled = false;
        _renderer = null;
        if (_owner?.Data?.Role is RcXdRole role && role.ActiveCar == this)
            role.ActiveCar = null;
        try { _go?.Destroy(); } catch { /* ignored */ }
        _go = null;
    }

    private void RestoreCamera()
    {
        if (!_isOwner) return;
        var cam = Camera.main?.GetComponent<FollowerCamera>();
        if (cam != null) cam.enabled = true;
    }

    public void Dispose()
    {
        DoDestroy();
    }
}
