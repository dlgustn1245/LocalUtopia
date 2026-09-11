using System;
using UnityEngine;
using UnityEngine.UI;

public class GameSetting : MonoBehaviour
{
    public Button resetButton;
    public Button closeButton;

    void Start()
    {
        BindButtonEvent();
    }

    void BindButtonEvent()
    {
        resetButton.onClick.AddListener(() =>
        {
            GameManager.Instance.DeleteData();
            SceneLoader.Load(SceneNames.Title);
        });
        closeButton.onClick.AddListener(() =>
        {
            gameObject.SetActive(false);
        });
    }
}
