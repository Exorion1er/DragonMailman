using FMODUnity;
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
        public StudioEventEmitter musicEmitter;

        private bool isPaused;
        private InputAction pauseAction;

        private void Awake()
        {
            pauseAction = inputAsset.FindActionMap("UI").FindAction("cancel");
        }

        private void Update()
        {
            if (pauseAction.WasPressedThisFrame()) SwitchPause();
        }

        private void OnEnable() => pauseAction.Enable();

        private void OnDisable() => pauseAction.Disable();

        private void SwitchPause()
        {
            if (isPaused)
                OnClickResume();
            else
                Pause();
        }

        public void Pause()
        {
            pauseMenuUI.SetActive(true);
            musicEmitter.SetParameter("PAUSE", 1);
            Time.timeScale = 0f;
            isPaused = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void OnClickResume()
        {
            pauseMenuUI.SetActive(false);
            musicEmitter.SetParameter("PAUSE", 0);
            Time.timeScale = 1f;
            isPaused = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
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
