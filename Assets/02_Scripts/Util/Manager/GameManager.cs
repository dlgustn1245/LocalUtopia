using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    public float clearRatio;
    public int deathCount;
    public bool isDead;

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

    public void Init(int stageCount)
    {
        clearRatio = 80f;
        deathCount = 3;
        isDead = false;
        currStage = -1;
        totalStage = stageCount;
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
}
