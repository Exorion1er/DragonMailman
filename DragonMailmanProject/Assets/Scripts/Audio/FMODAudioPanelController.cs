using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using UnityEngine.UI;

namespace Audio
{
    public class FMODAudioPanelController : MonoBehaviour
    {
        [Header("Master Settings")]
        public string masterVcaPath = "vca:/Master";
        public Slider masterSlider;
        public string masterSaveKey = "MasterVolume";

        [Header("Music Settings")]
        public string musicVcaPath = "vca:/Music";
        public Slider musicSlider;
        public string musicSaveKey = "MusicVolume";

        [Header("SFX Settings")]
        public string sfxVcaPath = "vca:/SFX";
        public Slider sfxSlider;
        public string sfxSaveKey = "SFXVolume";

        private VCA masterVca;
        private VCA musicVca;
        private VCA sfxVca;

        private void Start()
        {
            masterVca = RuntimeManager.GetVCA(masterVcaPath);
            musicVca = RuntimeManager.GetVCA(musicVcaPath);
            sfxVca = RuntimeManager.GetVCA(sfxVcaPath);

            float savedMaster = PlayerPrefs.GetFloat(masterSaveKey, 1.0f);
            float savedMusic = PlayerPrefs.GetFloat(musicSaveKey, 1.0f);
            float savedSfx = PlayerPrefs.GetFloat(sfxSaveKey, 1.0f);

            masterVca.setVolume(savedMaster);
            musicVca.setVolume(savedMusic);
            sfxVca.setVolume(savedSfx);

            masterSlider.value = savedMaster;
            musicSlider.value = savedMusic;
            sfxSlider.value = savedSfx;
        }

        public void SetMasterVolume(float volume)
        {
            masterVca.setVolume(volume);
            PlayerPrefs.SetFloat(masterSaveKey, volume);
        }

        public void SetMusicVolume(float volume)
        {
            musicVca.setVolume(volume);
            PlayerPrefs.SetFloat(musicSaveKey, volume);
        }

        public void SetSfxVolume(float volume)
        {
            sfxVca.setVolume(volume);
            PlayerPrefs.SetFloat(sfxSaveKey, volume);
        }
    }
}
