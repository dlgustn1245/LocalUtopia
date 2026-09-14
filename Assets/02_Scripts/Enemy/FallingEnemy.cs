using Qix;
using UnityEngine;

// 위에서 아래로 떨어진다. 벽에 막히거나 자기 칸이 확보되면 사라지고, 잠시 뒤 맨 윗줄 임의 열에서 다시 떨어진다.
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

        if (grid.GetState(cell) == CellState.Claimed || MoveBy(Vector2.down * (moveSpeed * Time.deltaTime)))
        {
            Vanish();
        }
    }

    // SetActive(false) 로 숨기면 Update 가 멈춰 재등장 타이머를 스스로 셀 수 없다. 렌더러만 끈다.
    void Vanish()
    {
        isFalling = false;
        spriteRenderer.enabled = false;
        respawnTimer = respawnDelay;
    }

    // 맨 윗줄에서 빈 칸을 찾아 다시 떨어진다. 윗줄이 전부 확보됐으면 한 주기 더 기다린다.
    void TryRespawn()
    {
        int top = grid.Rows - 1;
        for (int i = 0; i < 16; i++)
        {
            var spawnCell = new Vector2Int(Random.Range(0, grid.Columns), top);
            if (grid.GetState(spawnCell) == CellState.Empty)
            {
                Place(spawnCell);
                spriteRenderer.enabled = true;
                isFalling = true;
                return;
            }
        }

        respawnTimer = respawnDelay;
    }
}
