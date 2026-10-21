using UnityEngine;

// 비스듬히 직진하다 벽에 닿으면 그 축만 튕긴다. 일정 주기마다 지금 서 있는 칸에 함정을 요청한다.
// 함정 자체(거미줄 생성·감속·제거)는 QixScene 이 관리한다. 이 클래스는 언제 어디에 놓을지만 알린다. 거미가 쓴다.
public class TrapperEnemy : QixEnemy
{
    public float trapInterval = 3f;

    // QixScene 이 스폰 직후 연결한다. 인자는 함정을 놓을 칸.
    public System.Action<Vector2Int> onPlaceTrap;

    Vector2 direction;
    float trapTimer;

    void Awake()
    {
        // insideUnitCircle 은 영벡터가 나올 수 있어 각도로 뽑는다. 영벡터면 거미가 영원히 멈춘다.
        float angle = Random.Range(0f, Mathf.PI * 2f);
        direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        trapTimer = trapInterval;
    }

    protected override void Tick()
    {
        var step = direction * (moveSpeed * Time.deltaTime);

        // 축을 따로 넣어 어느 축이 막혔는지 알아내고 그 축만 뒤집는다. 한 번에 넣으면 막힌 축을 구분할 수 없다.
        if (MoveBy(new Vector2(step.x, 0f)))
        {
            direction.x = -direction.x;
        }

        if (MoveBy(new Vector2(0f, step.y)))
        {
            direction.y = -direction.y;
        }

        trapTimer -= Time.deltaTime;
        if (trapTimer <= 0f)
        {
            trapTimer = trapInterval;
            onPlaceTrap?.Invoke(cell);
        }
    }
}
