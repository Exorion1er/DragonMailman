using System.Collections;
using FMODUnity;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI
{
    public class MainMenuController : MonoBehaviour
    {
        public string mainScene = "Main";
        public GameObject controlsPanel;
        public RectTransform controlsLetter;
        public GameObject acceptButton;
        public GameObject quitButton;
        public GameObject stamp;
        public float timeBeforeSlideUp;
        public float animationDuration;
        public float timeBeforeStarting;
        public float yEndOffset;
        public EventReference letterOpenSfx;
        public EventReference letterSlideSfx;
        public EventReference stampSfx;

#if UNITY_WEBGL
        private void Start()
        {
            if (quitButton) quitButton.SetActive(false);
        }
#endif

        public void OnClickStart()
        {
            controlsPanel.SetActive(true);
            RuntimeManager.PlayOneShot(letterOpenSfx);
            StartCoroutine(AnimateLetter());
        }

        private IEnumerator AnimateLetter()
        {
            float elapsed = 0;
            Vector2 startPos = controlsLetter.anchoredPosition;
            Vector2 targetPos = new(startPos.x, yEndOffset);

            while (elapsed < timeBeforeSlideUp)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            RuntimeManager.PlayOneShot(letterSlideSfx);

            elapsed = 0;
            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / animationDuration;
                float smoothedT = Mathf.SmoothStep(0, 1, t);

                controlsLetter.anchoredPosition = Vector2.Lerp(startPos, targetPos, smoothedT);
                yield return null;
            }

            acceptButton.SetActive(true);
            controlsLetter.anchoredPosition = targetPos;
        }

        public void OnClickAccept()
        {
            acceptButton.SetActive(false);
            stamp.SetActive(true);
            RuntimeManager.PlayOneShot(stampSfx);
            StartCoroutine(AnimateAccept());
        }

        private IEnumerator AnimateAccept()
        {
            float elapsed = 0;
            while (elapsed < timeBeforeStarting)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

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
