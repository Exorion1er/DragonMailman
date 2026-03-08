using UnityEngine;

namespace UI
{
    public class WebGLUIController : MonoBehaviour
    {
        public GameObject pauseQtd;
        public GameObject gameOverQtd;

#if UNITY_WEBGL
        private void Start()
        {
            if (pauseQtd) pauseQtd.SetActive(false);
            if (gameOverQtd) gameOverQtd.SetActive(false);
        }
#endif
    }
}
