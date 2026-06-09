using System;
using UnityEngine;
using Il2CppInterop.Runtime.Attributes;
using Reactor.Utilities.Attributes;

namespace TouMegaChujoweExtension.Modules;

[RegisterInIl2Cpp]
public class InverterCameraBehaviour : MonoBehaviour
{
    public static InverterCameraBehaviour? Instance { get; private set; }

    private float _currentAngle = 0f;
    private float _targetAngle = 0f;
    private float _rotationDirection = 1f; // 1 for clockwise (+180), -1 for counterclockwise (-180)
    private float _rotationSpeed = 240f; // degrees per second, so 180 degrees takes 0.75 seconds

    public InverterCameraBehaviour(IntPtr ptr) : base(ptr) { }

    private void Awake()
    {
        Instance = this;
    }

    [HideFromIl2Cpp]
    public void SetDisoriented(bool disoriented)
    {
        if (disoriented)
        {
            // Choose a random direction to rotate: left (-1) or right (+1)
            _rotationDirection = UnityEngine.Random.value > 0.5f ? 1f : -1f;
            _targetAngle = 180f * _rotationDirection;
        }
        else
        {
            _targetAngle = 0f;
        }
    }

    private void Update()
    {
        var cam = Camera.main;
        if (cam == null) return;

        // Smoothly rotate currentAngle towards targetAngle
        if (Mathf.Abs(_currentAngle - _targetAngle) > 0.01f)
        {
            _currentAngle = Mathf.MoveTowards(_currentAngle, _targetAngle, _rotationSpeed * Time.deltaTime);
            
            var euler = cam.transform.localEulerAngles;
            euler.z = _currentAngle;
            cam.transform.localEulerAngles = euler;
        }
        else
        {
            _currentAngle = _targetAngle;
            var euler = cam.transform.localEulerAngles;
            euler.z = _currentAngle;
            cam.transform.localEulerAngles = euler;

            // If we are back to 0, and not disoriented, we destroy this component to clean up
            if (_targetAngle == 0f && Instance == this)
            {
                Destroy(this);
            }
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        // Ensure camera Z rotation is fully restored back to 0 when destroyed
        var cam = Camera.main;
        if (cam != null)
        {
            var euler = cam.transform.localEulerAngles;
            euler.z = 0f;
            cam.transform.localEulerAngles = euler;
        }
    }
}
