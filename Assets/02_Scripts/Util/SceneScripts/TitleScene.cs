using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleScene : MonoBehaviour
{
    public GameObject stageButtonsObject;
    public Button[] stageButtons;
    public GameObject[] clearMarks;
    public string qixSceneName;

    bool isLoading;
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
                if (isLoading)
                {
                    return;
                }
                SceneManager.LoadSceneAsync(qixSceneName);
                isLoading = true;
            });
            if (GameManager.Instance.IsCleared(i))
            {
                print($"Stage {i} is Cleared");
                clearMarks[i].SetActive(true);
            }
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
