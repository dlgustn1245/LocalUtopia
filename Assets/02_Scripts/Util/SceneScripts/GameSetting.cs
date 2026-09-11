using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GameSetting : MonoBehaviour
{
    public Button resetButton;
    public Button closeButton;

    public Toggle muteToggle;
    public Slider bgmVolume, sfxVolume;

    void Start()
    {
        InitSetting();
        BindButtonEvent();
    }

    void InitSetting()
    {
        muteToggle.SetIsOnWithoutNotify(SoundManager.Instance.isMuted);
        bgmVolume.SetValueWithoutNotify(SoundManager.Instance.bgmVolume);
        sfxVolume.SetValueWithoutNotify(SoundManager.Instance.sfxVolume);
    }

    void BindButtonEvent()
    {
        resetButton.onClick.AddListener(() =>
        {
            SoundManager.Instance.PlaySFX(SoundManager.Instance.selectSfx);
            GameManager.Instance.DeleteData();
            SceneLoader.Load(SceneNames.Title);
        });
        closeButton.onClick.AddListener(() =>
        {
            gameObject.SetActive(false);
        });
        
        muteToggle.onValueChanged.AddListener(flag =>
        {
            SoundManager.Instance.SetMute(flag);
        });
        
        bgmVolume.onValueChanged.AddListener(volume =>
        {
            SoundManager.Instance.SetBgmVolume(volume);
        });
        
        sfxVolume.onValueChanged.AddListener(volume =>
        {
            SoundManager.Instance.SetSfxVolume(volume);
        });
    }
    
    void OnDisable()
    {
        print("Setting Saved");
        PlayerPrefs.Save();
    }
}
