using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Qix/Stage Data")]
public class StageData : ScriptableObject
{
    public Sprite hiddenImage;
    public Texture[] enemyAnims;
    public string comment;
    public int timer;
    public float clearRatio;
    public int deathCount;
    public bool isBonusStage;
    public AudioClip bgm;
    public EnemyData[] enemies;
}
