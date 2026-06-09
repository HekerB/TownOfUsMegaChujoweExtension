using System;
using Reactor.Utilities.Attributes;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Joker;

[RegisterInIl2Cpp]
public sealed class JokerPiPBorderAnimator : MonoBehaviour
{
    public JokerPiPBorderAnimator(IntPtr ptr) : base(ptr) { }

    public Sprite[] Frames = Array.Empty<Sprite>();
    public float FramesPerSecond = 8f;

    private SpriteRenderer? _renderer;
    private float _timer;
    private int _currentFrame;

    private void Start()
    {
        _renderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (_renderer == null || Frames.Length == 0) return;

        _timer += Time.deltaTime;
        var frameDuration = 1f / FramesPerSecond;

        if (_timer >= frameDuration)
        {
            _timer -= frameDuration;
            _currentFrame = (_currentFrame + 1) % Frames.Length;
            _renderer.sprite = Frames[_currentFrame];
        }
    }
}