using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    // 스테이지 목록의 단일 출처. 타이틀의 버튼·클리어 마크 배열은 이 배열과 같은 인덱스로 정렬돼 있어야 한다.
    public StageData[] stages;

    public int currStage;

    public StageData CurrentStage => stages[currStage];

    // 보너스가 아닌 스테이지를 모두 클리어했는지. 보너스는 해금 대상이라 조건에서 제외한다.
    public bool AllCleared
    {
        get
        {
            for (int i = 0; i < stages.Length; i++)
            {
                if (!stages[i].isBonusStage && !IsCleared(i))
                {
                    return false;
                }
            }
            return stages.Length > 0;
        }
    }

    public void InitStage()
    {
        currStage = -1;

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
        for (int i = 0; i < stages.Length; i++)
        {
            PlayerPrefs.DeleteKey($"Stage{i}");
        }
        print("Key Deleted");
    }
}
