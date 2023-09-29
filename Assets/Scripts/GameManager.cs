using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class GameManager : MonoBehaviour
{
    public static PlayerControls controls;

    [Header("GUI and Text")]
    public GameObject gameWinDialog;
    public GameObject gameOverDialog;
    public GameObject pauseMenu;
    public GameObject alertMessage;
    public float alertTime = 1.0f;
    public Image blackScreen;
    public float fadeToBlackSpeed = 1;

    [Header("Texutres")]
    public Image[] smogAreas;

    [Header("Music and Sound Effects")]
    public AudioClip[] levelTracks;
    public AudioClip gameOverSound;

    private AudioSource musicSource;
    private AudioSource soundSource;

    private InputAction pauseToggle;
    private InputHandler playerInput;
    private PlayerController playerController;

    private float colorAlpha = 0;

    public bool GameRunning { get; private set; } = true;

    public bool FadeToBlack { get; set; } = false;

    public bool GameWon { get; set; } = false;

    public static GameManager Instance { get; private set; } // singleton

    private void Awake()
    {        
        // If there is an instance, and it is not this one, delete it.
        if (Instance != null && Instance != this)
        {
            Destroy(Instance);
        }

        Instance = this;

        if (controls == null)
            controls = new PlayerControls();

        if (pauseToggle == null)
        {
            pauseToggle = controls.UI.Cancel;
            pauseToggle.Enable();
            pauseToggle.performed += TogglePause;
        }
    }

    void Start()
    {
        musicSource = GetComponents<AudioSource>()[0];
        soundSource = GetComponents<AudioSource>()[1]; // sound effects source

        playerInput = FindObjectOfType<InputHandler>();
        playerController = FindObjectOfType<PlayerController>();

        if(gameOverDialog)
            gameOverDialog.SetActive(false);

        if(alertMessage)
            alertMessage.SetActive(false);

        Time.timeScale = 1;

        // enable collisions between player and enemies
        Physics2D.IgnoreLayerCollision(6, 7, false);
        // ignore collisions between enemies
        Physics2D.IgnoreLayerCollision(7, 7, true);
    }

    private void Update()
    {
        if(FadeToBlack)
        {
            colorAlpha += (fadeToBlackSpeed / 255);
            blackScreen.color = new Color(0, 0, 0, colorAlpha);

            if (colorAlpha >= 1)
            {
                FadeToBlack = false;
                EndGame(GameWon);
            }
        }
    }

    private void TogglePause(InputAction.CallbackContext context)
    {
        if (GameRunning)
        {
            pauseMenu.SetActive(true);
            Time.timeScale = 0;
            playerInput.enabled = false;
            Cursor.lockState = CursorLockMode.Confined;

            GameRunning = false;
        }
        else
        {
            ResumeGame();
        }
        
    }

    public void StartEndSequence(bool gameWon)
    {
        DisableEnemies();
        FadeToBlack = true;
        GameWon = gameWon;
    }

    public void ResumeGame()
    {
        pauseMenu.SetActive(false);
        Time.timeScale = 1;
        playerInput.enabled = true;        

        playerInput.SwitchItem((int)playerController.ItemSelected);

        Cursor.lockState = CursorLockMode.Locked;

        GameRunning = true;
    }

    public void EndGame(bool won)
    {
        if(won)
            gameWinDialog.SetActive(true);
        else
            gameOverDialog.SetActive(true);

        GetComponents<AudioSource>()[0].Stop(); // stop music playing
        Time.timeScale = 0;

        GameRunning = false;

        Cursor.lockState = CursorLockMode.Confined;
    }

    public void RestartGame()
    {
        // unsubscribe event from Escape and disable it
        pauseToggle.performed -= TogglePause;
        pauseToggle.Disable();

        // reload the scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void DisableEnemies()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        foreach (GameObject enemy in enemies)
        {
            enemy.SetActive(false);
        }
    }

    public IEnumerator ShowAlert()
    {
        alertMessage.SetActive(true);
        yield return new WaitForSeconds(alertTime);
        alertMessage.SetActive(false);
    }

    public void GoToMainMenu()
    {
        SceneManager.LoadScene(0); // restart first scene
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void PlaySound(AudioClip clip, float volumeScale = 1)
    {
        if (clip != null)
            soundSource.PlayOneShot(clip, volumeScale);
        else
            Debug.LogWarning("Sound effect has not been assigned to PlayerSound.");
    }

}

[System.Serializable]
public class SoundEffect
{
    public AudioClip audioClip;
    [Range(0, 1)]
    public float volume = 1;
}
