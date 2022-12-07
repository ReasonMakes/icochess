using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Generation generation;
    public PieceController pieceController;

    //private float moveSpeed = 0.06f;
    private float mouseSensitivity = 2f;
    [System.NonSerialized] public float yawDegrees = 0f;
    [System.NonSerialized] public float pitchDegrees = 0f;
    private float cameraDistanceToCenter = 2.5f;

    private void Start()
    {
        MoveCameraRotateAroundIcoCenter(); //necessary to prevent a sudden jump the first time the player tries to move the camera
    }

    private void Update()
    {
        MoveCameraRotateAroundIcoCenter();
        //MoveCameraFree();
    }

    private void MoveCameraRotateAroundIcoCenter()
    {
        //Rotate camera around world origin
        if (Input.GetKey(KeyCode.Mouse0))
        {
            yawDegrees += Input.GetAxisRaw("Mouse X") * mouseSensitivity;
            yawDegrees %= 360f;
            pitchDegrees = Mathf.Clamp(pitchDegrees + (Input.GetAxisRaw("Mouse Y") * mouseSensitivity), -89f, 89f);
        }

        Quaternion rotation = Quaternion.Euler(pitchDegrees, yawDegrees, 0);
        transform.position = rotation * Vector3.forward * cameraDistanceToCenter;

        //Always look at world origin
        Quaternion lookRotation = Quaternion.LookRotation(Vector3.zero - transform.position);
        if (lookRotation != Quaternion.identity)
        {
            transform.localRotation = lookRotation;
        }

        //Zoom
        cameraDistanceToCenter = Mathf.Max(1.5f, cameraDistanceToCenter + (Input.mouseScrollDelta.y * -0.1f));
    }

    //private void MoveCameraFree()
    //{
    //    //Cursor locking/visibility
    //    if (Input.GetKey(KeyCode.Escape))
    //    {
    //        Cursor.visible = true;
    //        Cursor.lockState = CursorLockMode.None;
    //    }
    //    if (Input.GetKey(KeyCode.Mouse0))
    //    {
    //        Cursor.visible = false;
    //        Cursor.lockState = CursorLockMode.Locked;
    //    }
    //    
    //    //Move
    //    Vector3 moveDirection = Vector3.zero;
    //    
    //    if (Input.GetKey(KeyCode.W))            { moveDirection += transform.forward; }
    //    if (Input.GetKey(KeyCode.A))            { moveDirection += -transform.right; }
    //    if (Input.GetKey(KeyCode.S))            { moveDirection += -transform.forward; }
    //    if (Input.GetKey(KeyCode.D))            { moveDirection += transform.right; }
    //    if (Input.GetKey(KeyCode.Space))        { moveDirection += transform.up; }
    //    if (Input.GetKey(KeyCode.LeftControl))  { moveDirection += -transform.up; }
    //
    //    float moveSpeed = 0.05f;
    //    transform.position += moveDirection.normalized * moveSpeed;
    //    
    //    //Look
    //    if (Cursor.lockState == CursorLockMode.Locked)
    //    {
    //        yawDegrees += Input.GetAxisRaw("Mouse X") * mouseSensitivity; yawDegrees %= 360f;
    //        pitchDegrees -= Input.GetAxisRaw("Mouse Y") * mouseSensitivity; pitchDegrees %= 360f;
    //        transform.localRotation = Quaternion.Euler(pitchDegrees, yawDegrees, 0f);
    //    }
    //}
}