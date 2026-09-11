using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TitleScene : MonoBehaviour
{
    public GameObject stageButtonsObject;
    public Button[] stageButtons;
    public GameObject[] clearMarks;

    public Button popupButton, prevButton;
    public Button settingButton;
    public GameObject settingPopup;
    public GameObject titleText;
    public GameObject logo;

    bool isTitleVisible = true;

    void Start()
    {
        GameManager.Instance.InitStage();

        BindButtonEvent();
        SetTitleVisible(true);
        SoundManager.Instance.PlayBGM(SoundManager.Instance.menuBgm);
    }

    void Update()
    {
        // 설정 버튼처럼 UI 위를 누른 클릭은 버튼 onClick 이 처리하므로 타이틀 진행으로 먹지 않는다.
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            SoundManager.Instance.PlaySFX(SoundManager.Instance.selectSfx);
            OnClickTitle();
        }
    }

    void BindButtonEvent()
    {
        var stages = GameManager.Instance.stages;
        bool allCleared = GameManager.Instance.AllCleared;

        // 세 배열은 같은 인덱스로 정렬돼 있어야 한다. 길이가 다르면 예외 대신 로그를 남기고 겹치는 범위만 묶는다.
        if (stageButtons.Length != stages.Length || clearMarks.Length != stages.Length)
        {
            Debug.LogError($"스테이지 배열 길이 불일치: stages={stages.Length}, buttons={stageButtons.Length}, marks={clearMarks.Length}");
        }

        int count = Mathf.Min(stageButtons.Length, stages.Length, clearMarks.Length);
        for (int i = 0; i < count; i++)
        {
            int idx = i;
            stageButtons[idx].onClick.AddListener(() =>
            {
                GameManager.Instance.currStage = idx;
                SceneLoader.Load(SceneNames.Loading);
                SoundManager.Instance.PlaySFX(SoundManager.Instance.selectSfx);
            });
            
            stageButtons[idx].gameObject.SetActive(!stages[idx].isBonusStage || allCleared);
            clearMarks[idx].SetActive(GameManager.Instance.IsCleared(idx));
        }
        
        popupButton.onClick.AddListener(() =>
        {
            SoundManager.Instance.PlaySFX(SoundManager.Instance.selectSfx);
            stageButtonsObject.SetActive(true);
            popupButton.gameObject.SetActive(false);
        });
        prevButton.onClick.AddListener(() =>
        {
            SoundManager.Instance.PlaySFX(SoundManager.Instance.selectSfx);
            SetTitleVisible(true);
        });
        settingButton.onClick.AddListener(() =>
        {
            SoundManager.Instance.PlaySFX(SoundManager.Instance.selectSfx);
            settingPopup.SetActive(true);
        });
    }

    void OnClickTitle()
    {
        if (!isTitleVisible)
        {
            return;
        }

        SetTitleVisible(false);
    }

    void SetTitleVisible(bool visible)
    {
        settingButton.gameObject.SetActive(visible);
        isTitleVisible = visible;
        titleText.SetActive(visible);
        // 스테이지 목록은 로고 자리까지 쓰므로 타이틀 텍스트와 함께 숨긴다.
        logo.SetActive(visible);
        popupButton.gameObject.SetActive(!visible);
        prevButton.gameObject.SetActive(!visible);
        stageButtonsObject.SetActive(false);
    }
}
