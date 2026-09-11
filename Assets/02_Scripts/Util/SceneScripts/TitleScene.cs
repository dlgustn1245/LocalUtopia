using UnityEngine;
using UnityEngine.UI;

public class TitleScene : MonoBehaviour
{
    public GameObject stageButtonsObject;
    public Button[] stageButtons;
    public GameObject[] clearMarks;

    public Button popupButton, prevButton;
    public GameObject titleText;

    bool isTitleVisible = true;

    void Start()
    {
        GameManager.Instance.InitStage();

        BindButtonEvent();
        SetTitleVisible(true);
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
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
            });
            
            stageButtons[idx].gameObject.SetActive(!stages[idx].isBonusStage || allCleared);
            clearMarks[idx].SetActive(GameManager.Instance.IsCleared(idx));
        }
        
        popupButton.onClick.AddListener(() =>
        {
            stageButtonsObject.SetActive(true);
            popupButton.gameObject.SetActive(false);
        });
        prevButton.onClick.AddListener(() => SetTitleVisible(true));
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
        isTitleVisible = visible;
        titleText.SetActive(visible);
        popupButton.gameObject.SetActive(!visible);
        prevButton.gameObject.SetActive(!visible);
        stageButtonsObject.SetActive(false);
    }
}
