using UnityEngine;

[CreateAssetMenu(menuName = "Qix/Stage Data")]
public class StageData : ScriptableObject
{
    public Sprite hiddenImage;
    public GameObject enemy;
    public Sprite[] enemyAnims;
    public string comment;
    public int enemyCount;
    public float clearRatio;
}
