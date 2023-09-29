using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class ScrollText : MonoBehaviour
{
    public float scrollSpeed = 25;
    public float heldScrollSpeed = 100;
    public float maxYPosition = 2600;

    public UnityEvent onScrollComplete;

    private RectTransform rect;

    InputAction escape;
    PlayerControls controls;

    // Start is called before the first frame update
    void Start()
    {
        controls = new PlayerControls();
        rect = GetComponent<RectTransform>();

        escape = controls.UI.Cancel;
        escape.performed += context => onScrollComplete.Invoke();
        escape.Enable();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.anyKey)
        {
            rect.position = new Vector2(rect.position.x, rect.position.y + heldScrollSpeed * Time.deltaTime);
        }
        else
        {
            rect.position = new Vector2(rect.position.x, rect.position.y + scrollSpeed * Time.deltaTime);
        }

        if(rect.position.y > maxYPosition)
        {
            onScrollComplete.Invoke();
        }

    }
}
