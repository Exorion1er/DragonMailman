using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField]
        private string mainScene = "Main";

        public void OnClickStart()
        {
            SceneManager.LoadScene(mainScene);
        }

        public void OnClickQuit()
        {
            Application.Quit();

#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#endif
        }
    }
}