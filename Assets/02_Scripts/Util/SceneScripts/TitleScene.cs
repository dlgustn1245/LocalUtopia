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
        GameManager.Instance.InitStage(stageButtons.Length);
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
        for (int i = 0; i < stageButtons.Length; i++)
        {
            int idx = i;
            stageButtons[idx].onClick.AddListener(() =>
            {
                GameManager.Instance.currStage = idx;
                SceneLoader.Load(SceneNames.Loading);
            });

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
