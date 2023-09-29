using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
#if UNITY_EDITOR
    [SerializeField] SceneAsset newGameScene;
#endif

    [SerializeField] GameObject[] screens;
    
    private Button[] buttons;
    private int currentScreen = 0;

    private SceneManager sceneManager;

    private void Awake()
    {     
        // Check the number of monitors connected
        if (Display.displays.Length > 1)
        {
            // Set display to monitor 0 (first monitor connect to the system)
            Display.displays[0].Activate();
        }       
    }

    // Start is called before the first frame update
    void Start()
    {
        // Starts with only the first screen visible
        for (int i = 1; i < screens.Length; i++)
        {
            screens[i].SetActive(false);
        }

#if UNITY_EDITOR

        if (screens.Length > 0)
        {
            //Gets the buttons from the first screen
            buttons = screens[0].GetComponentsInChildren<Button>();
            for (int i = 0; i < buttons.Length; i++)
            {
                print(buttons[i].name);
            }


            //Starts with the first Button selected (For controller support)
            buttons[0].Select();
        }
#endif

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void NewGame()
    {
        //print("New Game Start!");
#if UNITY_EDITOR
        SceneManager.LoadScene(newGameScene.name);
#else
        SceneManager.LoadScene(1);
#endif

    }

    public void Continue()
    {
        Debug.Log("Continue Game");
    }

    public void Settings()
    {
        //print("Settings Open");
        SetActiveScreen(1);
    }
    public void SaveSettings()
    {
        //print("Save and Close!");
        SetActiveScreen(0);
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        //print("Exit Game!");     
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    // works with editor only
#if UNITY_EDITOR
    public void LoadSence(SceneAsset scene)
    {
        SceneManager.LoadScene(scene.name);
    }
#endif

    // Private Functions --------------------------------------------------
    private void SetActiveScreen(int id)
    {
        //Toggles active menu screen
        screens[currentScreen].SetActive(false);
        currentScreen = id;
        screens[currentScreen].SetActive(true);
        //Sets the new buttons
        if (screens[currentScreen].GetComponentsInChildren<Button>().Length > 0)
        {
            //print("Buttons Get!");
            buttons = screens[currentScreen].GetComponentsInChildren<Button>();
            //Starts with the first Button selected (For controller support)
            buttons[0].Select();
        }
        
    }

}
