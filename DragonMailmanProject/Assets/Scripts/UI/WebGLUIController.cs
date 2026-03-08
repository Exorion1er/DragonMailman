using UnityEngine;

namespace UI
{
    public class WebGLUIController : MonoBehaviour
    {
        public GameObject pauseQtd;
        public GameObject gameOverQtd;
        public GameObject pauseText;
        public PauseMenuController pauseMenu;

#if UNITY_WEBGL
        private void Start()
        {
            if (pauseQtd) pauseQtd.SetActive(false);
            if (gameOverQtd) gameOverQtd.SetActive(false);
            if (pauseText) pauseText.SetActive(true);
            pauseMenu.Pause();
        }
#endif
    }
}
