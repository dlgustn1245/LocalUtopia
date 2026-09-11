using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EndingScene : MonoBehaviour
{
    public Button button;
    public TextMeshProUGUI text;
    public string[] lines;

    int index;

    void Start()
    {
        SoundManager.Instance.PlayBGM(SoundManager.Instance.endingBgm);
        text.text = lines[index];
        button.onClick.AddListener(() =>
        {
            SoundManager.Instance.PlaySFX(SoundManager.Instance.selectSfx);
            if (++index < lines.Length)
            {
                text.text = lines[index];
                return;
            }
            SceneLoader.Load(SceneNames.Credit);
        });
    }
}
