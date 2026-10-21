using System;
using Qix;
using UnityEngine;

public abstract class QixEnemy : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;
    public float moveSpeed = 2f;
    public float frameInterval = 0.25f;

    // 본체 접촉 판정 반지름. 스프라이트보다 작게 둔다. 그림 크기대로 잡으면 테두리 근처를 지나기만 해도 죽는다.
    public float hitRadius = 0.15f;

    public bool keepRegion = true;

    public Vector2Int cell;

    protected QixGrid grid;

    Action onTrailHit;
    Sprite[] frames;
    float frameTimer;
    int frameIndex;

    public void Init(QixGrid grid, Sprite[] frames, Action onTrailHit)
    {
        this.grid = grid;
        this.frames = frames;
        this.onTrailHit = onTrailHit;

        if (frames != null && frames.Length > 0)
        {
            spriteRenderer.sprite = frames[0];
        }
    }
    
    public void Place(Vector2Int cell)
    {
        this.cell = cell;
        var world = grid.CellToWorld(cell);
        transform.position = new Vector3(world.x, world.y, transform.position.z);
    }

    void Update()
    {
        Tick();
        Animate();
    }

    protected abstract void Tick();
    
    // 이동량을 x/y 축으로 나눠 적용한다. 한 축이 막혀도 다른 축은 살아 벽을 따라 미끄러진다.
    // 축을 나누면 칸도 한 번에 한 축만 바뀌어서, GetEdgeBetweenCells 의 "두 칸은 인접하다" 전제가 지켜진다.
    // 한 축이라도 벽에 막혔으면 true.
    protected bool MoveBy(Vector2 delta)
    {
        // 한 번에 두 칸 이상 건너뛰면 사이의 변을 놓쳐 벽을 통과한다. 그렇다고 잘라내면 프레임이 떨어질 때
        // 이동량이 통째로 버려져 적이 느려지므로, 한 칸 이하로 쪼개서 여러 번 밟는다.
        // 상한은 말도 안 되는 moveSpeed 가 한 프레임을 통째로 잡아먹지 않게 하는 안전장치다.
        int steps = Mathf.Clamp(Mathf.Max(
            Mathf.CeilToInt(Mathf.Abs(delta.x) / grid.CellSize.x),
            Mathf.CeilToInt(Mathf.Abs(delta.y) / grid.CellSize.y)), 1, 64);

        // 상한에 걸렸을 때 남는 이동량은 버린다. 한 조각이 한 칸을 넘으면 GetEdgeBetweenCells 의
        // "두 칸은 인접하다" 전제가 깨져 배열 밖을 읽는다.
        var stepDelta = delta / steps;
        stepDelta.x = Mathf.Clamp(stepDelta.x, -grid.CellSize.x, grid.CellSize.x);
        stepDelta.y = Mathf.Clamp(stepDelta.y, -grid.CellSize.y, grid.CellSize.y);

        bool blocked = false;

        for (int i = 0; i < steps; i++)
        {
            blocked |= TryStep(new Vector2(stepDelta.x, 0f));
            blocked |= TryStep(new Vector2(0f, stepDelta.y));
        }

        return blocked;
    }

    bool TryStep(Vector2 delta)
    {
        if (delta == Vector2.zero)
        {
            return false;
        }

        var next = (Vector2)transform.position + delta;
        var nextCell = grid.WorldToCell(next);

        if (nextCell != cell)
        {
            // 격자 밖으로 나가는 경우도 여기서 걸린다. 바깥 테두리가 Boundary 라서 막힘으로 잡힌다.
            var edge = grid.GetEdgeBetweenCells(cell, nextCell);
            if (edge == EdgeState.Boundary)
            {
                return true;
            }

            if (edge == EdgeState.Trail)
            {
                onTrailHit?.Invoke();
            }

            cell = nextCell;
        }

        transform.position = new Vector3(next.x, next.y, transform.position.z);
        return false;
    }

    void Animate()
    {
        if (frames == null || frames.Length < 2)
        {
            return;
        }

        frameTimer += Time.deltaTime;
        if (frameTimer < frameInterval)
        {
            return;
        }

        frameTimer -= frameInterval;
        frameIndex = (frameIndex + 1) % frames.Length;
        spriteRenderer.sprite = frames[frameIndex];
    }
}
