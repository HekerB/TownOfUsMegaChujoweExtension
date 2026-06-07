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

        var behaviour = camera.GetComponent<InverterCameraBehaviour>()
            ?? camera.gameObject.AddComponent<InverterCameraBehaviour>();

        active = behaviour;
        behaviour.targetZ = 180f;
        behaviour.resetting = false;
    }

    public static void ResetCamera()
    {
        var camera = Camera.main;
        if (camera == null)
        {
            return;
        }

        active ??= camera.GetComponent<InverterCameraBehaviour>();
        if (active == null)
        {
            var euler = camera.transform.localEulerAngles;
            camera.transform.localEulerAngles = new Vector3(euler.x, euler.y, 0f);
            return;
        }

        active.targetZ = 0f;
        active.resetting = true;
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

        var camera = Camera.main;
        if (camera == null)
        {
            return;
        }

        var euler = camera.transform.localEulerAngles;
        camera.transform.localEulerAngles = new Vector3(euler.x, euler.y, 0f);
    }
}
