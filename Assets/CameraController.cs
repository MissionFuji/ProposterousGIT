using UnityEngine;

public class CameraController : MonoBehaviour
{

    public float mouseSensitivity = 10;
    public Transform target;
    public float dstFromTarget = 2;
    public Vector2 pitchMinMax = new Vector2(-40, 85);
    public float rotationSmoothTime = .12f;

    private bool playerReady = false;
    private bool lockCursor;
    private Vector3 rotationSmoothVelocity;
    private Vector3 currentRotation;

    float yaw;
    float pitch;

    public void ReadyCamera(Transform tar) {
        target = tar;
        playerReady = true;
    }

    void Start() {
        if (lockCursor) {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void LateUpdate() {
        if (playerReady) {
            yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
            pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
            pitch = Mathf.Clamp(pitch, pitchMinMax.x, pitchMinMax.y);

            currentRotation = Vector3.SmoothDamp(currentRotation, new Vector3(pitch, yaw), ref rotationSmoothVelocity, rotationSmoothTime);
            transform.eulerAngles = currentRotation;

            transform.position = (target.position + new Vector3(0f, 8f, 0f)) - transform.forward * dstFromTarget;
        }
    }


}
