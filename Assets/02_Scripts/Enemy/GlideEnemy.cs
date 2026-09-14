using UnityEngine;

// 랜덤 주기마다 빈 칸 하나를 골라 직선으로 미끄러진다. 벽에 막히거나 도착하면 다음 주기까지 쉰다.
// 고양이·구름·발자국·경비가 쓴다.
public class GlideEnemy : QixEnemy
{
    public float minRest = 0.5f;
    public float maxRest = 2f;

    Vector2 target;
    Vector2 direction;
    float restTimer;
    bool isGliding;

    void Awake()
    {
        // 여러 마리가 첫 프레임에 동시에 출발하지 않게 쉬는 상태로 시작한다.
        Rest();
    }

    protected override void Tick()
    {
        if (!isGliding)
        {
            restTimer -= Time.deltaTime;
            if (restTimer > 0f)
            {
                return;
            }

            if (!grid.TryGetRandomEmptyCell(out var targetCell))
            {
                Rest();
                return;
            }

            target = grid.CellToWorld(targetCell);
            direction = (target - (Vector2)transform.position).normalized;
            isGliding = true;
        }

        var remaining = target - (Vector2)transform.position;
        var step = direction * (moveSpeed * Time.deltaTime);

        // 목표를 지나치지 않게 마지막 한 걸음은 남은 거리만큼만 간다.
        bool arriving = step.sqrMagnitude >= remaining.sqrMagnitude;
        if (arriving)
        {
            step = remaining;
        }

        if (MoveBy(step) || arriving)
        {
            Rest();
        }
    }

    void Rest()
    {
        isGliding = false;
        restTimer = Random.Range(minRest, maxRest);
    }
}
