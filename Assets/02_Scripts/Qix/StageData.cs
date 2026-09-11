using UnityEngine;

[CreateAssetMenu(menuName = "Qix/Stage Data")]
public class StageData : ScriptableObject
{
    public Sprite hiddenImage;
    public GameObject[] enemy;
    public Texture[] enemyAnims;
    public string comment;
    public int enemyCount;
    public int timer;
    public float clearRatio;
    public int deathCount;
    public bool isBonusStage;
    public AudioClip bgm;
}
