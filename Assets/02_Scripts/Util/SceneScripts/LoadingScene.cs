using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadingScene : MonoBehaviour
{
    public Image enemyImage;
    public TextMeshProUGUI comment;

    readonly WaitForSeconds delay = new WaitForSeconds(0.5f);
    
    void Start()
    {
        StartCoroutine(PlayLoading());
    }

    IEnumerator PlayLoading()
    {
        var stage = GameManager.Instance.stages[GameManager.Instance.currStage];
        comment.text = stage.comment;

        for (int i = 0; i < 3; i++)
        {
            enemyImage.sprite = stage.enemyAnims[0];
            yield return delay;
            enemyImage.sprite = stage.enemyAnims[1];
            yield return delay;
        }

        SceneLoader.Load(SceneNames.Qix);
    }
}
