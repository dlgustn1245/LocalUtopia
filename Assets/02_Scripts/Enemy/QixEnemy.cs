using System;
using Qix;
using UnityEngine;

public abstract class QixEnemy : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;
    public float moveSpeed = 2f;
    public float frameInterval = 0.25f;

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
        // 한 프레임에 두 칸 이상 건너뛰면 사이의 변을 놓쳐 벽을 통과한다. 축마다 한 칸으로 끊는다.
        delta.x = Mathf.Clamp(delta.x, -grid.CellSize.x, grid.CellSize.x);
        delta.y = Mathf.Clamp(delta.y, -grid.CellSize.y, grid.CellSize.y);

        bool blocked = TryStep(new Vector2(delta.x, 0f));
        blocked |= TryStep(new Vector2(0f, delta.y));

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
