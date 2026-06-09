using System;
using System.Linq;
using Il2CppInterop.Runtime.Attributes;
using Reactor.Utilities.Attributes;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Roles.Neutral;
using UnityEngine;
using TownOfUs.Utilities;
using MiraAPI.GameOptions;

namespace TouMegaChujoweExtension.Modules
{
    [RegisterInIl2Cpp]
    public sealed class JokerCloneControlComponent : MonoBehaviour
    {
        public JokerCloneControlComponent(IntPtr ptr) : base(ptr) { }

        public byte OwnerId;
        public byte AppearanceId;

        private Vector2 _targetNetworkPos;
        private Vector2 _currentVelocity;
        private Vector2 _logicalPosition;
        private float _syncTimer;
        private bool _flipX;

        private JokerDummy _dummy;
        private PetBehaviour _petBehaviour;

        private bool _netIsMoving;

        // pet smoothing
        private bool _petInit;
        private Vector3 _petPos;
        private Vector3 _petVel;

        // anim state
        private bool _petWasMoving;
		
		private float _netLastRecvTime;

		private const float SnapDistance = 4f;

	private void Start()
	{
		_logicalPosition = transform.position;
		_targetNetworkPos = transform.position;
		_flipX = false;

		//temp debug
		/*for (int i = 0; i < 32; i++)
		{
			var name = LayerMask.LayerToName(i);
			if (!string.IsNullOrEmpty(name))
				Warning($"Layer {i}: {name}");
		}*/

		var myCloneData = JokerCloneSystem.Clones.FirstOrDefault(c => c.Fake?.body == this.gameObject);
		if (myCloneData != null)
		{
			_dummy = myCloneData.Fake;
			_petBehaviour = _dummy?.Pet;
		}
	}

private void FixedUpdate()
{
    if (MeetingHud.Instance || _dummy == null || _dummy.pc == null) return;

    var local = PlayerControl.LocalPlayer;
    var isOwner = local != null && local.PlayerId == OwnerId;
    var speed = OptionGroupSingleton<JokerOptions>.Instance.CloneSpeed * 2.5f;

    if (isOwner)
    {
        Vector2 input = Vector2.zero;
        if (Input.GetKey(KeyCode.UpArrow)) input.y += 1f;
        if (Input.GetKey(KeyCode.DownArrow)) input.y -= 1f;
        if (Input.GetKey(KeyCode.LeftArrow)) input.x -= 1f;
        if (Input.GetKey(KeyCode.RightArrow)) input.x += 1f;

        Vector2 actualMovement = Vector2.zero;

        if (input != Vector2.zero)
        {
            input.Normalize();
            var dt = Time.fixedDeltaTime;
            var moveX = input.x * speed * dt;
            var moveY = input.y * speed * dt;

            if (Mathf.Abs(moveX) > 0.001f)
            {
                var testX = new Vector2(_logicalPosition.x + moveX, _logicalPosition.y);
                if (!IsInWall(testX))
                {
                    _logicalPosition.x += moveX;
                    actualMovement.x = moveX;
                }
            }

            if (Mathf.Abs(moveY) > 0.001f)
            {
                var testY = new Vector2(_logicalPosition.x, _logicalPosition.y + moveY);
                if (!IsInWall(testY))
                {
                    _logicalPosition.y += moveY;
                    actualMovement.y = moveY;
                }
            }

            if (Mathf.Abs(input.x) > 0.05f)
                _flipX = input.x < 0f;

            _currentVelocity = actualMovement.sqrMagnitude > 0f ? input * speed : Vector2.zero;
        }
        else
        {
            _currentVelocity = Vector2.zero;
        }

        _netIsMoving = _currentVelocity.sqrMagnitude > 0.01f;

        _syncTimer += Time.fixedDeltaTime;
        if (_syncTimer >= 0.05f)
        {
            var jokerPlayer = MiscUtils.PlayerById(OwnerId);
            if (jokerPlayer != null)
                JokerRole.RpcJokerSyncClone(jokerPlayer, _logicalPosition, _flipX, _netIsMoving);

            _syncTimer = 0f;
        }
    }
    else
    {
        _logicalPosition = Vector2.Lerp(
            _logicalPosition,
            _targetNetworkPos,
            15f * Time.fixedDeltaTime
        );

        if (_dummy.pc?.MyPhysics?.body != null)
        {
            Vector2 visualVelocity =
                (_logicalPosition - (Vector2)_dummy.body.transform.position) / Time.fixedDeltaTime;

            // kill micro jitter with generous deadzone
            if (Mathf.Abs(visualVelocity.x) < 0.15f)
                visualVelocity.x = 0f;
            if (Mathf.Abs(visualVelocity.y) < 0.15f)
                visualVelocity.y = 0f;

            if (visualVelocity.sqrMagnitude > 0.001f)
                _dummy.pc.MyPhysics.body.velocity = visualVelocity;
            else
                _dummy.pc.MyPhysics.body.velocity = Vector2.zero;

            // HandleAnimation first, then SetFlipX so network flip always wins
            _dummy.pc.MyPhysics.HandleAnimation(false);
            _dummy.pc.cosmetics?.SetFlipX(_flipX);
        }
    }

    _dummy.body.transform.position =
        new Vector3(_logicalPosition.x, _logicalPosition.y, _logicalPosition.y / 1000f);

    if (isOwner && _dummy.pc?.MyPhysics?.body != null)
    {
        _dummy.pc.MyPhysics.body.velocity = _currentVelocity;
        // HandleAnimation first, then SetFlipX for consistency
        _dummy.pc.MyPhysics.HandleAnimation(false);
        _dummy.pc.cosmetics?.SetFlipX(_flipX);
    }
}

        private void LateUpdate()
        {
            if (MeetingHud.Instance || _dummy == null) return;

            if (_petBehaviour == null) _petBehaviour = _dummy.Pet;
            UpdatePet();
        }

        private void UpdatePet()
        {
            if (_dummy?.body == null || _petBehaviour == null) return;

            if (_petBehaviour.transform.parent != null)
                _petBehaviour.transform.SetParent(null, true);

            _petBehaviour.FlipX = _flipX;

            var bodyPos = _dummy.body.transform.position;
            var baseOff = _dummy.GetBaseOffsetForFlip(_flipX);

            Vector2 behind = Vector2.zero;

            var local = PlayerControl.LocalPlayer;
            var isOwner = local != null && local.PlayerId == OwnerId;

            if (_netIsMoving)
            {
                if (isOwner && _currentVelocity.sqrMagnitude > 0.01f)
                {
                    var dir = _currentVelocity.normalized;
                    behind = -dir * 0.12f;
                }
                else
                {
                    behind = new Vector2(_flipX ? 0.12f : -0.12f, 0f);
                }
            }

            var desired = new Vector3(
                bodyPos.x + baseOff.x + behind.x,
                bodyPos.y + baseOff.y + behind.y,
                bodyPos.y / 1000f - 0.1f
            );

            if (!_petInit)
            {
                _petInit = true;
                _petPos = desired;
            }

            _petPos = Vector3.SmoothDamp(_petPos, desired, ref _petVel, 0.06f, Mathf.Infinity, Time.deltaTime);
            _petBehaviour.transform.position = _petPos;

            if (_netIsMoving != _petWasMoving)
            {
                _petWasMoving = _netIsMoving;
                try
                {
                    if (_netIsMoving) _petBehaviour.StartWalkAnim();
                    else _petBehaviour.SetIdle();
                }
                catch { }
            }
        }

	private static bool IsInWall(Vector2 pos)
	{
		var checkPos = pos + new Vector2(0f, -0.1f);
    
		var cols = Physics2D.OverlapCircleAll(
			checkPos, 
			0.12f,
			LayerMask.GetMask("Ship", "Objects", "ShortObjects"));
    
		foreach (var c in cols)
			if (c != null && !c.isTrigger) return true;
		return false;
	}
	
[HideFromIl2Cpp]
public void ReceiveSync(Vector2 pos, bool flipX, bool isMoving)
{
    _netIsMoving = isMoving;

    if (flipX != _flipX)
        _flipX = flipX;

    _targetNetworkPos = pos;
    _netLastRecvTime = Time.time;

    if (Vector2.Distance(_logicalPosition, pos) > 6f)
        _logicalPosition = pos;
}
}
}