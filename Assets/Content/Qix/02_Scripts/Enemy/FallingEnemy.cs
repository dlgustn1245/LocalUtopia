using Qix;
using UnityEngine;

// 언제나 맵 맨 윗줄에서 아래로 떨어진다. 확보된 구역은 그냥 통과하고, 미확보 구역에 들어와야 벽에 막힌다.
// 막히거나 바닥까지 내려가면 사라졌다가 잠시 뒤 다시 맨 위에서 떨어진다.
// 필드를 계속 가로지르는 존재라 영역을 지키지 않는다. 프리팹에서 keepRegion 을 꺼 둔다. 돈이 쓴다.
public class FallingEnemy : QixEnemy
{
    public float respawnDelay = 1.5f;

    float respawnTimer;
    bool isFalling;

    void Awake()
    {
        // QixScene 이 놓아 준 자리는 쓰지 않고, 저마다 다른 시간에 위에서 등장한다.
        Vanish();
        respawnTimer = Random.Range(0f, respawnDelay);
    }

    protected override void Tick()
    {
        if (!isFalling)
        {
            respawnTimer -= Time.deltaTime;
            if (respawnTimer <= 0f)
            {
                TryRespawn();
            }

            return;
        }

        float step = moveSpeed * Time.deltaTime;

        // 확보된 칸을 지나는 동안은 아직 "들어오는 중" 이라 벽 판정 없이 내려오기만 한다.
        // 위쪽이 확보돼 있어도 항상 화면 맨 위에서 떨어져 들어오게 하려면 이 구간이 필요하다.
        // 확보·미확보 경계는 Boundary 라서 보통 이동으로는 통과할 수 없다.
        if (grid.GetState(cell) == CellState.Claimed)
        {
            Descend(step);
            return;
        }

        // 미확보 구역에 들어왔다. 여기서부터는 벽에 막히고 궤적도 밟는다.
        if (MoveBy(Vector2.down * step))
        {
            Vanish();
        }
    }

    // 확보 구역 통과용 이동. 변을 보지 않고 좌표만 내리므로 칸도 직접 갱신한다.
    void Descend(float step)
    {
        var position = (Vector2)transform.position;
        position.y -= step;
        transform.position = new Vector3(position.x, position.y, transform.position.z);

        var next = grid.WorldToCell(position);
        if (!grid.IsInBounds(next))
        {
            // 바닥까지 확보돼 있어 미확보 구역을 만나지 못했다.
            Vanish();
            return;
        }

        cell = next;
    }

    // SetActive(false) 로 숨기면 Update 가 멈춰 재등장 타이머를 스스로 셀 수 없다. 렌더러만 끈다.
    void Vanish()
    {
        isFalling = false;
        spriteRenderer.enabled = false;
        respawnTimer = respawnDelay;
    }

    // 확보 여부를 보지 않고 언제나 맵 맨 윗줄에서 떨어진다. 떨어지는 것은 항상 화면 위에서 들어와야 한다.
    // 그 자리가 확보된 칸이면 Descend 가 미확보 구역까지 통과시켜 준다.
    void TryRespawn()
    {
        Place(new Vector2Int(Random.Range(0, grid.Columns), grid.Rows - 1));
        spriteRenderer.enabled = true;
        isFalling = true;
    }
}
