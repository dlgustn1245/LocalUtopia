using System.Collections;
using UnityEngine;

public class LoadingScene : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(PlayLoading());
    }

    IEnumerator PlayLoading()
    {
        yield return new WaitForSeconds(3.0f);
        SceneLoader.Load(SceneNames.Qix);
    }
}
