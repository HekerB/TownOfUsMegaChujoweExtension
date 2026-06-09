using System.Collections;
using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using Reactor.Utilities;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Roles.Neutral;
using TownOfUs.Buttons;
using TownOfUs.Utilities;
using TownOfUs.Modules.Components;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Neutral
{
    public sealed class JokerPlaceCloneButton : TownOfUsRoleButton<JokerRole>
    {
        private enum Stage
        {
            Select,
            Preview,
            ActiveLocked,
            Active
        }

        private const float PostPlaceLockSeconds = 3f;

        private Stage _stage = Stage.Select;
        private int _ghostCloneIndex = -1;

        private bool _isProcessingClick;

        private float _removeUnlockAt;

        private bool _cooldownArmed;
        private bool _hadActiveLastTick;

        // shake
        private bool _isShaking;

        public static JokerPlaceCloneButton? LocalInstance { get; private set; }

        public override string Name => "Place Clone";
        public override BaseKeybind Keybind => Keybinds.SecondaryAction;
        public override Color TextOutlineColor => TouExtensionColors.Joker;
        public override float Cooldown => OptionGroupSingleton<JokerOptions>.Instance.CloneCooldown + MapCooldown;
        public override float EffectDuration => 0f;
        public override int MaxUses => 0;
        public override LoadableAsset<Sprite> Sprite => TouExtensionNeuAssets.JokerPlaceCloneButtonSprite;

        public override bool CanUse()
        {
            if (MeetingHud.Instance || HudManager.Instance.Chat.IsOpenOrOpening) return false;

            var player = PlayerControl.LocalPlayer;
            if (player == null || player.HasDied()) return false;

            return _stage switch
            {
                Stage.Select => Timer <= 0f,
                Stage.Preview => true,
                Stage.ActiveLocked => Time.time >= _removeUnlockAt,
                Stage.Active => true,
                _ => false
            };
        }

        protected override void FixedUpdate(PlayerControl playerControl)
        {
            base.FixedUpdate(playerControl);

            var lp = PlayerControl.LocalPlayer;
            if (lp == null) return;

            LocalInstance = this;

            int activeIdx = FindMyActiveCloneIndex(lp.PlayerId);
            int previewIdx = FindMyPreviewCloneIndex(lp.PlayerId);

            bool hasActive = activeIdx >= 0;
            bool hasPreview = previewIdx >= 0;

            if (_hadActiveLastTick && !hasActive && _cooldownArmed)
            {
                _cooldownArmed = false;
                Timer = Cooldown;
            }
            _hadActiveLastTick = hasActive;

            if (hasActive)
            {
                _stage = Time.time < _removeUnlockAt ? Stage.ActiveLocked : Stage.Active;

                Timer = 0f;

                OverrideName("Cancel");
                return;
            }

            if (hasPreview)
            {
                _stage = Stage.Preview;
                _ghostCloneIndex = previewIdx;

                Timer = 0f;
                OverrideName("Confirm");
                return;
            }

            _stage = Stage.Select;
            _ghostCloneIndex = -1;

            OverrideName("Place Clone");
        }

        public override void ClickHandler()
        {
            if (_stage == Stage.ActiveLocked && Time.time < _removeUnlockAt)
            {
                DoShake();
                return;
            }

            if (_isProcessingClick) return;
            _isProcessingClick = true;

            try
            {
                if (!CanUse())
                {
                    if (_stage == Stage.Select && Timer > 0f) DoShake();
                    return;
                }

                OnClick();
            }
            finally
            {
                Coroutines.Start(ResetProcessingFlag());
            }
        }

        private IEnumerator ResetProcessingFlag()
        {
            yield return new WaitForSeconds(0.2f);
            _isProcessingClick = false;
        }

        protected override void OnClick()
        {
            var player = PlayerControl.LocalPlayer;
            if (player == null) return;

            // CANCEL
            if (_stage == Stage.Active)
            {
                var idx = FindMyActiveCloneIndex(player.PlayerId);
                if (idx >= 0)
                {
                    JokerRole.RpcJokerDestroyClone(player, idx);
                }
                return;
            }

            // SELECT -> menu -> preview
            if (_stage == Stage.Select)
			{
				if (IsNearWall(player.transform.position))
				{
					DoShake();
					return;
				}

				var menu = CustomPlayerMenu.Create();
                menu.Begin(
                    p => p != null && !p.Data.Disconnected && p.PlayerId != player.PlayerId,
                    selectedPlayer =>
                    {
                        if (selectedPlayer != null)
                        {
                            _ghostCloneIndex = JokerCloneSystem.PlaceClone(
                                player.PlayerId,
                                selectedPlayer,
                                player.transform.position,
                                true);

                            if (_ghostCloneIndex >= 0)
                            {
                                SetCloneAlpha(_ghostCloneIndex, 0.35f);
                                _stage = Stage.Preview;
                                OverrideName("Confirm");
                            }
                        }
                        menu.Close();
                    }
                );
                return;
            }

            // PREVIEW -> CONFIRM
            if (_stage == Stage.Preview)
            {
                if (_ghostCloneIndex < 0 || _ghostCloneIndex >= JokerCloneSystem.Clones.Count)
                    _ghostCloneIndex = FindMyPreviewCloneIndex(player.PlayerId);

                if (_ghostCloneIndex < 0)
                {
                    _stage = Stage.Select;
                    OverrideName("Place Clone");
                    return;
                }

                var clone = JokerCloneSystem.Clones[_ghostCloneIndex];

                var appPlayerId = clone.AppearancePlayerId;
                var px = clone.Fake.body.transform.position.x;
                var py = clone.Fake.body.transform.position.y;
                var pz = clone.Fake.body.transform.position.z;

                JokerCloneSystem.TryRemoveClone(_ghostCloneIndex, out _);
                _ghostCloneIndex = -1;

                JokerRole.RpcJokerPlaceClone(player, appPlayerId, px, py, pz);

                _removeUnlockAt = Time.time + PostPlaceLockSeconds;
                _stage = Stage.ActiveLocked;

                _cooldownArmed = true;
                Timer = 0f;

                OverrideName("Cancel");
            }
        }

        public void ResetStage()
        {
            if (_ghostCloneIndex >= 0)
            {
                JokerCloneSystem.TryRemoveClone(_ghostCloneIndex, out _);
                _ghostCloneIndex = -1;
            }

            var lp = PlayerControl.LocalPlayer;
            if (lp != null)
            {
                for (int i = JokerCloneSystem.Clones.Count - 1; i >= 0; i--)
                {
                    var c = JokerCloneSystem.Clones[i];
                    if (!c.IsPreview) continue;

                    var body = c.Fake?.body;
                    if (body == null) continue;

                    var ctrl = body.GetComponent<JokerCloneControlComponent>();
                    if (ctrl != null && ctrl.OwnerId == lp.PlayerId)
                        JokerCloneSystem.TryRemoveClone(i, out _);
                }
            }

            _stage = Stage.Select;
            _removeUnlockAt = 0f;

            OverrideName("Place Clone");
        }

        private static int FindMyPreviewCloneIndex(byte ownerId)
        {
            for (int i = 0; i < JokerCloneSystem.Clones.Count; i++)
            {
                var c = JokerCloneSystem.Clones[i];
                if (!c.IsPreview) continue;

                var body = c.Fake?.body;
                if (body == null) continue;

                var ctrl = body.GetComponent<JokerCloneControlComponent>();
                if (ctrl != null && ctrl.OwnerId == ownerId)
                    return i;
            }
            return -1;
        }

        private static int FindMyActiveCloneIndex(byte ownerId)
        {
            for (int i = 0; i < JokerCloneSystem.Clones.Count; i++)
            {
                var c = JokerCloneSystem.Clones[i];
                if (c.IsPreview) continue;

                var body = c.Fake?.body;
                if (body == null) continue;

                var ctrl = body.GetComponent<JokerCloneControlComponent>();
                if (ctrl != null && ctrl.OwnerId == ownerId)
                    return i;
            }
            return -1;
        }

        private static void SetCloneAlpha(int cloneIndex, float alpha)
        {
            if (cloneIndex < 0 || cloneIndex >= JokerCloneSystem.Clones.Count) return;

            var clone = JokerCloneSystem.Clones[cloneIndex];
            if (clone.Fake?.body == null) return;

            foreach (var sr in clone.Fake.body.GetComponentsInChildren<SpriteRenderer>(true))
            {
                var c = sr.color;
                c.a = Mathf.Clamp01(alpha);
                sr.color = c;
            }
        }

        private void DoShake()
        {
            if (_isShaking) return;
            if (Button == null) return;
            Coroutines.Start(CoShake());
        }

        private IEnumerator CoShake()
        {
            _isShaking = true;

            var t = Button.transform;
            var basePos = t.localPosition;

            const float dur = 0.14f;
            const float amp = 1.6f;
            float time = 0f;

            while (time < dur)
            {
                time += Time.deltaTime;
                float s = Mathf.Sin(time * 70f) * amp;
                t.localPosition = basePos + new Vector3(s, 0f, 0f);
                yield return null;
            }

            t.localPosition = basePos;
            _isShaking = false;
        }
		private static bool IsNearWall(Vector2 pos)
        {
            var cols = Physics2D.OverlapCircleAll(
                pos,
                0.25f,
                Constants.ShipAndAllObjectsMask);

            foreach (var c in cols)
                if (c != null && !c.isTrigger)
                    return true;

            return false;
        }
    }
}
