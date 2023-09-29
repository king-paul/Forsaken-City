using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("Camera Pan")]
    [SerializeField] float minPanSpeed = 10;
    [SerializeField] float maxPanSpeed = 100;

    [Header("Panning Boundaries")]
    [SerializeField] float topBoundary;
    [SerializeField] float bottomBoundary;
    [SerializeField] float leftBoundary;
    [SerializeField] float rightBoundary;

    [Header("Camera Zoom")]
    [SerializeField] float zoomSpeed = 1;
    [SerializeField] float minCameraSize = 3.5f;
    [SerializeField] float maxCameraSize = 200;

    PlayerControls controls;

    private InputAction movement;
    private InputAction zoomIn;
    private InputAction zoomOut;
    private Vector2 moveDirection;

    private Camera camera;

    private float panSpeed;
    private float panSpeedStep;

    private float zoomPercent;

    // Start is called before the first frame update
    void Start()
    {
        controls = new PlayerControls();

        movement = controls.Camera.Move;
        movement.Enable();

        zoomIn = controls.Camera.ZoomIn;
        zoomIn.performed += ctx => ZoomIn();
        zoomIn.Enable();

        zoomOut = controls.Camera.ZoomOut;
        zoomOut.performed += ctx => ZoomOut();
        zoomOut.Enable();

        camera = GetComponent<Camera>();

        // calculate default pan speed
        zoomPercent = ((camera.orthographicSize - minCameraSize) * 100) / (maxCameraSize - minCameraSize);
        panSpeed = (maxPanSpeed / 100 * zoomPercent) + minPanSpeed;

        panSpeedStep = (maxPanSpeed - minPanSpeed) / (maxCameraSize - minCameraSize);

        //Debug.Log("Camera zoom: " + zoomPercent + "%");
        //Debug.Log("Pan Speed: " + panSpeed);
    }

    // Update is called once per frame
    void Update()
    {
        moveDirection = movement.ReadValue<Vector2>();

        PanCamera();
        zoomPercent = ((camera.orthographicSize - minCameraSize) * 100) / (maxCameraSize - minCameraSize);
    }

    private void PanCamera()
    {
        if (moveDirection.x > 0 && transform.position.x > rightBoundary)
            return;
        if (moveDirection.x < 0 && transform.position.x < leftBoundary)
            return;
        if (moveDirection.y > 0 && transform.position.y >= topBoundary)
            return;
        if (moveDirection.y < 0 && transform.position.y <= bottomBoundary)
            return;

        transform.Translate(moveDirection * panSpeed * Time.deltaTime);
    }

    private void ZoomIn()
    {
        if (camera.orthographicSize > minCameraSize)
        {
            camera.orthographicSize -= zoomSpeed;
            panSpeed -= panSpeedStep;
            //Debug.Log("PanSpeed: " + panSpeed);
            //Debug.Log("camera zoom: " + zoomPercent + "%");
        }
    }

    private void ZoomOut()
    {
        if (camera.orthographicSize < maxCameraSize)
        {
            camera.orthographicSize += zoomSpeed;
            panSpeed += panSpeedStep;
            //Debug.Log("PanSpeed: " + panSpeed);
            //Debug.Log("camera zoom: " + zoomPercent + "%");
        }
    }
    
}
