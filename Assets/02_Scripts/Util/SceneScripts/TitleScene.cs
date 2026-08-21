using UnityEngine;
using UnityEngine.UI;

public class TitleScene : MonoBehaviour
{
    public GameObject stageButtonsObject;
    public Button[] stageButtons;
    public GameObject[] clearMarks;

    bool isFirstTouch = true;

    void Start()
    {
        GameManager.Instance.Init(stageButtons.Length);
        BindButtonEvent();
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
            stageButtons[idx].onClick.AddListener(delegate
            {
                GameManager.Instance.currStage = idx;
                SceneLoader.Load(SceneNames.Loading);
            });

            clearMarks[idx].SetActive(GameManager.Instance.IsCleared(idx));
        }
    }
    
    void OnClickTitle()
    {
        if (!isFirstTouch)
        {
            return;
        }
        isFirstTouch = false;
        
        stageButtonsObject.SetActive(true);
    }
}
