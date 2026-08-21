using UnityEngine;

[CreateAssetMenu(menuName = "Qix/Stage Data")]
public class StageData : ScriptableObject
{
    public Sprite hiddenImage;
    public GameObject enemy;
    public int enemyCount;
    public float clearRatio;
}
