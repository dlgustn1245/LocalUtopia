using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    public StageData[] stages;
    
    public int deathCount;
    public int currStage;
    
    int totalStage;

    public bool AllCleared
    {
        get
        {
            for (int i = 0; i < totalStage; i++)
            {
                if (!IsCleared(i))
                {
                    return false;
                }
            }
            return totalStage > 0;
        }
    }

    public void InitStage(int stageCount)
    {
        currStage = -1;
        totalStage = stageCount;

        //DeleteData();
    }

    public void StageClear()
    {
        PlayerPrefs.SetInt($"Stage{currStage}", 1);
        PlayerPrefs.Save();
    }

    public bool IsCleared(int stage)
    {
        return PlayerPrefs.GetInt($"Stage{stage}") == 1;
    }

    void DeleteData()
    {
        for (int i = 0; i < 5; i++)
        {
            PlayerPrefs.DeleteKey($"Stage{i}");
        }
        print("Key Deleted");
    }
}
