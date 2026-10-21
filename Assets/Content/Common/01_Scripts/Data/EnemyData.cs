using System;
using UnityEngine;

[Serializable]
public class EnemyData
{
    // GameObject 가 아니라 QixEnemy 로 둔다. 인스펙터가 적 스크립트 없는 프리팹을 아예 받지 않아
    // 스폰 도중 NullReferenceException 으로 터지는 대신 꽂는 자리에서 막힌다.
    public QixEnemy prefab;
    public int count;
    public Sprite[] frames;
}
