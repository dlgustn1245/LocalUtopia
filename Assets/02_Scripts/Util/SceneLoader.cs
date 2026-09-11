using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneNames
{
    public const string Title = "01_Title";
    public const string Loading = "02_Loading";
    public const string Qix = "03_Qix";
    public const string Ending = "04_Ending";
    public const string Credit = "05_Credit";
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
        
        Debug.Log($"Scene: {sceneName} loaded");
        loadScene.completed += _ => isLoading = false;
    }
}
