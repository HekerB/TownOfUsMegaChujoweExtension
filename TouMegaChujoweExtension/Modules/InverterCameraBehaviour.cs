using Reactor.Utilities.Attributes;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TouMegaChujoweExtension.Modules;

[RegisterInIl2Cpp]
public sealed class InverterCameraBehaviour(IntPtr cppPtr) : MonoBehaviour(cppPtr)
{
    private const float RotateSpeed = 8f;
    private static InverterCameraBehaviour? active;

    private float targetZ;
    private bool resetting;

    public static void Apply()
    {
        var camera = Camera.main;
        if (camera == null)
        {
            return;
        }

        var behaviour = camera.GetComponent<InverterCameraBehaviour>();
        if (behaviour == null)
        {
            behaviour = camera.gameObject.AddComponent<InverterCameraBehaviour>();
        }

        active = behaviour;
        behaviour.targetZ = 180f;
        behaviour.resetting = false;
    }

    public static void ResetCamera()
    {
        // The behaviour lives on the camera that was turned, which need not be Camera.main any more; resetting
        // whatever Camera.main is at that moment left the turned one upside down for the rest of the game.
        if (active == null)
        {
            active = null;
            var camera = Camera.main;
            if (camera != null)
            {
                active = camera.GetComponent<InverterCameraBehaviour>();
            }
        }

        if (active == null)
        {
            active = null;
            ForceUpright(Camera.main);
            return;
        }

        active.targetZ = 0f;
        active.resetting = true;
    }

    /// <summary>
    /// Puts a camera back upright at once, dropping any rotation still in progress.
    /// </summary>
    public static void ForceUpright(Camera? camera)
    {
        if (camera == null)
        {
            return;
        }

        var behaviour = camera.GetComponent<InverterCameraBehaviour>();
        if (behaviour != null)
        {
            Object.Destroy(behaviour);
        }

        var euler = camera.transform.localEulerAngles;
        camera.transform.localEulerAngles = new Vector3(euler.x, euler.y, 0f);
    }

    private void Update()
    {
        var euler = transform.localEulerAngles;
        var z = Mathf.LerpAngle(euler.z, targetZ, Time.deltaTime * RotateSpeed);
        transform.localEulerAngles = new Vector3(euler.x, euler.y, z);

        if (!resetting || Mathf.Abs(Mathf.DeltaAngle(z, 0f)) >= 0.5f)
        {
            return;
        }

        transform.localEulerAngles = new Vector3(euler.x, euler.y, 0f);
        active = null;
        Object.Destroy(this);
    }

    private void OnDestroy()
    {
        if (active == this)
        {
            active = null;
        }

        // Upright the camera this sat on, not whichever camera is main now.
        var euler = transform.localEulerAngles;
        transform.localEulerAngles = new Vector3(euler.x, euler.y, 0f);
    }
}
