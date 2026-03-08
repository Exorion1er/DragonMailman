using UnityEngine;

namespace UI
{
    public class WebGLUIController : MonoBehaviour
    {
        public GameObject pauseQtd;
        public GameObject gameOverQtd;
        public GameObject pauseText;
        public GameObject pausePanel;

#if UNITY_WEBGL
        private void Start()
        {
            if (pauseQtd) pauseQtd.SetActive(false);
            if (gameOverQtd) gameOverQtd.SetActive(false);
            if (pauseText) pauseText.SetActive(true);
            if (pausePanel) pausePanel.SetActive(true);
        }
#endif
    }
}
