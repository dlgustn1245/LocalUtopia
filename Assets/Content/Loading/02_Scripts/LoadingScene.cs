using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadingScene : MonoBehaviour
{
    public RawImage enemyImage;
    public TextMeshProUGUI comment;

    readonly WaitForSeconds delay = new WaitForSeconds(0.5f);
    
    void Start()
    {
        StartCoroutine(PlayLoading());
        enemyImage.SetNativeSize();
        enemyImage.rectTransform.sizeDelta *= 3f;
    }

    IEnumerator PlayLoading()
    {
        var stage = GameManager.Instance.CurrentStage;
        comment.text = stage.comment;
        SoundManager.Instance.PlayBGM(stage.bgm);

        for (int i = 0; i < 6; i++)
        {
            enemyImage.texture = stage.enemyAnims[i % stage.enemyAnims.Length];
            yield return delay;
        }

        SceneLoader.Load(SceneNames.Qix);
    }
}
