using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneNames
{
    public const string Title = "01_Title";
    public const string Loading = "02_Loading";
    public const string Qix = "03_Qix";
}

public static class SceneLoader
{
    static bool isLoading;

    public static void Load(string sceneName)
    {
        if (isLoading)
        {
            return;
        }
        isLoading = true;
        var loadScene = SceneManager.LoadSceneAsync(sceneName);
        if (loadScene == null)
        {
            Debug.Log($"Scene: {sceneName} is null");
            isLoading = false;
            return;
        }
        
        loadScene.completed += _ => isLoading = false;
    }
}
