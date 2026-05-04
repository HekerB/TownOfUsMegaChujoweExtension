using System;
using System.Collections;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Networking;
using MiraAPI.Utilities;
using Reactor.Utilities;
using Reactor.Utilities.Extensions;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TouMegaChujoweExtension.Roles.Impostor;
using TownOfUs.Assets;
using TownOfUs.Modifiers;
using TownOfUs.Utilities;
using UnityEngine;
using Object = UnityEngine.Object;

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
    private float _acceleration;
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
    private const float AudioHearRadius = 8f;

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
        foreach (var h in hit)
        {
            if (h.collider != null && !h.collider.isTrigger) return true;
        }
        return false;
    }

    // --- VISION MASK CONFIG ---
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
            if (sl.IndexOf("Player", StringComparison.OrdinalIgnoreCase) >= 0) score += 50;
            if (sl.IndexOf("UI", StringComparison.OrdinalIgnoreCase) >= 0) score -= 200;

            var nm = r.name ?? string.Empty;
            if (nm.IndexOf("Name", StringComparison.OrdinalIgnoreCase) >= 0) score -= 100;

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

        var car = new RcXdCar
        {
            _owner = owner,
            _isOwner = owner.AmOwner,
            _speed = opts.CarSpeed,
            _acceleration = opts.CarAcceleration,
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

        car._renderer.enabled = true;

        car._audio = car._go.AddComponent<AudioSource>();
        car._audio.clip = TouExtensionAudio.RcSound.LoadAsset();
        car._audio.loop = true;
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

            if (_renderer != null) _renderer.enabled = true;

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
            UpdateRemotePitch(speedMag);
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

    private void UpdateRemotePitch(float speedMag)
    {
        if (_audio == null) return;
        var ratio = _speed <= 0.001f ? 0f : Mathf.Clamp01(speedMag / _speed);
        _audio.pitch = Mathf.Lerp(0.6f, 1.3f, ratio);
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
        var deceleration = _acceleration * 0.8f;

        while (!_detonated && _go != null)
        {
            if (_owner == null || _owner.Data == null || _owner.Data.IsDead || _owner.Data.Disconnected)
            {
                DoDestroy();
                yield break;
            }

            var dt = Time.fixedDeltaTime;

            var inputDir = Vector2.zero;
            if (Input.GetKey(KeyCode.UpArrow)) inputDir.y += 1f;
            if (Input.GetKey(KeyCode.DownArrow)) inputDir.y -= 1f;
            if (Input.GetKey(KeyCode.LeftArrow)) inputDir.x -= 1f;
            if (Input.GetKey(KeyCode.RightArrow)) inputDir.x += 1f;
            if (inputDir != Vector2.zero) inputDir = inputDir.normalized;

            if (inputDir != Vector2.zero)
            {
                var currentDir = _velocity.magnitude > 0.1f ? _velocity.normalized : inputDir;
                var newDir = Vector2.Lerp(currentDir, inputDir, TurnSpeed * dt).normalized;
                var currentSpeed = _velocity.magnitude;
                var newSpeed = Mathf.MoveTowards(currentSpeed, _speed, _acceleration * dt);
                _velocity = newDir * newSpeed;
            }
            else
            {
                var currentSpeed = _velocity.magnitude;
                var newSpeed = Mathf.MoveTowards(currentSpeed, 0f, deceleration * dt);
                _velocity = newSpeed > 0.02f ? _velocity.normalized * newSpeed : Vector2.zero;
            }

            if (_velocity.magnitude < 0.02f)
                _velocity = Vector2.zero;

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
                var speedRatio = _speed <= 0.001f ? 0f : Mathf.Clamp01(_velocity.magnitude / _speed);
                _audio.pitch = Mathf.Lerp(0.6f, 1.3f, speedRatio);
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

        if (_isOwner)
        {
            _audio.volume = 0.4f;
            return;
        }

        if (PlayerControl.LocalPlayer == null)
        {
            _audio.volume = 0f;
            return;
        }

        var dist = Vector2.Distance(_go.transform.position, PlayerControl.LocalPlayer.transform.position);
        _audio.volume = dist > AudioHearRadius ? 0f : Mathf.Clamp01(1f - (dist / AudioHearRadius)) * 0.35f;
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
                var local = PlayerControl.LocalPlayer;
                if (local?.Data?.Role?.IsImpostor == true || local?.Data?.IsDead == true)
                {
                    var opts = OptionGroupSingleton<RcXdOptions>.Instance;
                    var sphere = MiscUtils.CreateSpherePrimitive(_go.transform.position, opts.DetonateRadius);
                    Coroutines.Start(DestroyObjAfter(sphere, 0.5f));
                }
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

        if (!MeetingHud.Instance && !ExileController.Instance && _owner != null)
        {
            var radius = opts.DetonateRadius * ShipStatus.Instance.MaxLightRadius;
            var allNear = Helpers.GetClosestPlayers(bombPos, radius);
            
            // Filter for valid targets FIRST (including teammates!)
            var validTargets = allNear.Where(x => 
                x != null && 
                !x.HasDied() && 
                x.PlayerId != _owner.PlayerId && // Don't kill the owner themselves
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
        if (_go == null || PlayerControl.LocalPlayer == null) return;

        var explosionPos = (Vector2)_go.transform.position;
        var localPos = (Vector2)PlayerControl.LocalPlayer.transform.position;
        var dist = Vector2.Distance(explosionPos, localPos);

        var opts = OptionGroupSingleton<RcXdOptions>.Instance;
        var blastRadius = opts.DetonateRadius * ShipStatus.Instance.MaxLightRadius;

        var isInBlastRadius = dist <= blastRadius;
        var isImpostorNearby = PlayerControl.LocalPlayer.Data.Role.IsImpostor && dist <= blastRadius * 3f;

        if (isInBlastRadius || isImpostorNearby || _isOwner)
        {
            var volume = isInBlastRadius || _isOwner
                ? 0.8f
                : Mathf.Clamp01(1f - (dist / (blastRadius * 3f))) * 0.6f;
            SoundManager.Instance.PlaySound(TouExtensionAudio.RcExplosionSound.LoadAsset(), false, volume);
        }
    }

    private void PlayDisconnectSound()
    {
        if (!_isOwner) return;
        SoundManager.Instance.PlaySound(TouAudio.TrackerDeactivateSound.LoadAsset(), false, 0.7f);
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
