using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CreditScene : MonoBehaviour
{
    public Button toTitle;

    public RectTransform content;
    public RectTransform viewport;
    public float scrollSpeed = 60f;

    float contentHeight;

    void Start()
    {
        toTitle.onClick.AddListener(() =>
        {
            SceneLoader.Load(SceneNames.Title);
        });

        contentHeight = content.GetComponent<TextMeshProUGUI>().preferredHeight;
        content.sizeDelta = new Vector2(content.sizeDelta.x, contentHeight);
        ResetToBottom();
    }

    void Update()
    {
        var pos = content.anchoredPosition;
        pos.y += scrollSpeed * Time.deltaTime;
        // 글 아래끝이 뷰포트 위를 완전히 지나면 바닥에서 다시 시작한다.
        if (pos.y > contentHeight)
        {
            pos.y = -viewport.rect.height;
        }
        content.anchoredPosition = pos;
    }

    void ResetToBottom()
    {
        content.anchoredPosition = new Vector2(0f, -viewport.rect.height);
    }
}
