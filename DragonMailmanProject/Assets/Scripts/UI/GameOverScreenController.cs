using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI
{
    public class GameOverScreenController : MonoBehaviour
    {
        public GameObject gameOverPanel;

        private void Start()
        {
            gameOverPanel.SetActive(false);
        }

        public void ShowGameOverScreen()
        {
            gameOverPanel.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void OnClickRestart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
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
