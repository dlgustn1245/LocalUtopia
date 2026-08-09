using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleScene : MonoBehaviour
{
    public string qixSceneName;

    bool isLoading;
    
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (!isLoading)
            {
                OnClickTitle();
            }
        }
    }

    void OnClickTitle()
    {
        if (!isLoading)
        {
            isLoading = true;
        }
        SceneManager.LoadSceneAsync(qixSceneName);
    }
}
