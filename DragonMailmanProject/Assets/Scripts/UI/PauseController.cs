using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace UI
{
    public class PauseMenuController : MonoBehaviour
    {
        public GameObject pauseMenuUI;
        public InputActionAsset inputAsset;

        private bool isPaused;
        private InputAction pauseAction;

        private void Awake()
        {
            pauseAction = inputAsset.FindActionMap("UI").FindAction("cancel");
        }

        private void OnEnable()
        {
            pauseAction.Enable();
            pauseAction.performed += _ => DeterminePauseState();
        }

        private void OnDisable()
        {
            pauseAction.Disable();
            pauseAction.performed -= _ => DeterminePauseState();
        }

        private void DeterminePauseState()
        {
            if (isPaused)
                Resume();
            else
                Pause();
        }

        public void Resume()
        {
            pauseMenuUI.SetActive(false);
            Time.timeScale = 1f;
            isPaused = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        public void Pause()
        {
            pauseMenuUI.SetActive(true);
            Time.timeScale = 0f;
            isPaused = true;
            Cursor.lockState = CursorLockMode.None;
        }

        public void OnClickQuitToMainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("Main Menu");
        }

        public void OnClickQuitToDesktop()
        {
            Application.Quit();

#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#endif
        }
    }
}