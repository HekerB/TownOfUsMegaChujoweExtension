using Il2CppInterop.Runtime.Attributes;
using Reactor.Utilities.Attributes;
using UnityEngine;

namespace TouMegaChujoweExtension.Modules;

[RegisterInIl2Cpp]
public class DeathNoteUIController(IntPtr handle) : MonoBehaviour(handle)
{
    public static DeathNoteUIController? Instance { get; private set; }
    private static DeathNoteModifier? _activeModifier;
    private static string _currentInput = "";

    private float _statusTimer;
    private bool _movableWasCached;
    private bool _cachedMovable;

    private TMPro.TextMeshPro _inputText = null!;
    private TMPro.TextMeshPro _statusText = null!;

    [HideFromIl2Cpp]
    public void Initialize(DeathNoteModifier modifier)
    {
        SetStaticState(this, modifier, "");
        _statusTimer = 0f;

        if (PlayerControl.LocalPlayer != null)
        {
            _movableWasCached = true;
            _cachedMovable = PlayerControl.LocalPlayer.moveable;
            PlayerControl.LocalPlayer.moveable = false;
        }

        CreateUI();
    }

    private static void SetStaticState(DeathNoteUIController? instance, DeathNoteModifier? modifier, string input)
    {
        Instance = instance;
        _activeModifier = modifier;
        _currentInput = input;
    }

    private void CreateUI()
    {
        var cam = Camera.main;
        if (cam != null)
        {
            transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, -50f);
        }

        var background = new GameObject("DeathNoteBG");
        background.transform.SetParent(transform);
        background.transform.localPosition = Vector3.zero;
        background.layer = LayerMask.NameToLayer("UI");

        var bgRenderer = background.AddComponent<SpriteRenderer>();
        bgRenderer.sprite = TouExtensionAssets.DeathNoteUISprite.LoadAsset();
        bgRenderer.sortingOrder = 100;

        if (cam != null)
        {
            var height = 2f * cam.orthographicSize;
            var width = height * cam.aspect;
            var sprBounds = bgRenderer.sprite.bounds.size;
            var scaleX = (width * 0.6f) / sprBounds.x;
            var scaleY = (height * 0.6f) / sprBounds.y;
            var scale = Mathf.Min(scaleX, scaleY);
            background.transform.localScale = new Vector3(scale, scale, 1f);
        }

        var statusObj = new GameObject("DeathNoteStatus");
        statusObj.transform.SetParent(background.transform);
        statusObj.transform.localPosition = new Vector3(0.44f, 0.5f, -0.7f);
        statusObj.layer = LayerMask.NameToLayer("UI");

        _statusText = statusObj.AddComponent<TMPro.TextMeshPro>();
        _statusText.fontSize = 1.5f;
        _statusText.alignment = TMPro.TextAlignmentOptions.Center;
        _statusText.color = Color.red;
        _statusText.sortingOrder = 101;
        _statusText.text = "";
        _statusText.rectTransform.sizeDelta = new Vector2(6f, 1f);

        var inputObj = new GameObject("DeathNoteInput");
        inputObj.transform.SetParent(background.transform);
        inputObj.transform.localPosition = new Vector3(0.44f, 0.2f, -0.7f);
        inputObj.layer = LayerMask.NameToLayer("UI");

        _inputText = inputObj.AddComponent<TMPro.TextMeshPro>();
        _inputText.fontSize = 1.5f;
        _inputText.alignment = TMPro.TextAlignmentOptions.Center;
        _inputText.color = new Color(0.15f, 0f, 0.15f);
        _inputText.sortingOrder = 101;
        _inputText.fontStyle = TMPro.FontStyles.Italic;
        _inputText.text = "_";
        _inputText.rectTransform.sizeDelta = new Vector2(6f, 1.5f);
    }

    private void Update()
    {
        if (_activeModifier == null)
        {
            Close();
            return;
        }

        if (PlayerControl.LocalPlayer != null)
        {
            PlayerControl.LocalPlayer.moveable = false;
        }

        if (_statusTimer > 0f)
        {
            _statusTimer -= Time.deltaTime;
            if (_statusTimer <= 0f && _statusText != null)
                _statusText.text = "";
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (string.IsNullOrWhiteSpace(_currentInput))
            {
                ShowStatus("Cannot submit empty name!");
                return;
            }

            var result = _activeModifier.OnNameSubmitted(_currentInput);
            if (result == DeathNoteSubmitResult.Success)
            {
                Close();
            }
            else
            {
                SetStaticState(Instance, _activeModifier, "");
                UpdateInputDisplay();
                ShowStatus(result == DeathNoteSubmitResult.SelfTarget
                    ? "You cannot curse yourself!"
                    : "Player not found!");
            }
            return;
        }

        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            if (_currentInput.Length > 0)
            {
                SetStaticState(Instance, _activeModifier, _currentInput[..^1]);
                UpdateInputDisplay();
            }
            return;
        }

        foreach (var c in Input.inputString)
        {
            if (c == '\b' || c == '\n' || c == '\r' || c == 27)
                continue;

            if (_currentInput.Length < 20)
            {
                SetStaticState(Instance, _activeModifier, _currentInput + c);
                UpdateInputDisplay();
            }
        }
    }

    private void UpdateInputDisplay()
    {
        if (_inputText != null)
        {
            _inputText.text = _currentInput.Length > 0 ? _currentInput + "_" : "_";
        }
    }

    private void ShowStatus(string message)
    {
        if (_statusText != null)
        {
            _statusText.text = message;
            _statusTimer = 3f;
        }
    }

    public void Close()
    {
        SetStaticState(null, null, _currentInput);

        if (PlayerControl.LocalPlayer != null)
        {
            PlayerControl.LocalPlayer.moveable = !_movableWasCached || _cachedMovable;
        }

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SetStaticState(null, null, _currentInput);
        }

        if (PlayerControl.LocalPlayer != null)
        {
            PlayerControl.LocalPlayer.moveable = !_movableWasCached || _cachedMovable;
        }
    }
}