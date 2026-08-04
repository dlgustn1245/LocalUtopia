using System.Collections.Generic;
using UnityEngine;

namespace Qix
{
    // 플레이어 위치를 그리드 좌표로 추적하며 궤적 생성 -> 확보 판정을 담당하는 컨트롤러.
    // 그리드는 지정한 플레이 영역(fieldSize / fieldCenter)을 기준으로 생성된다.
    public class QixController : MonoBehaviour
    {
        public Player player;
        public float cellWorldSize;
        public QixGridRenderer gridRenderer;

        // 플레이 영역(월드 단위). 화면 전체가 아니라 상단 HUD, 하단 조작 UI 자리를 뺀 크기를 지정한다.
        // QixManager 를 선택하면 씬 뷰에 초록 사각형으로 표시되므로 보면서 조절하면 된다.
        public Vector2 fieldSize;
        public Vector2 fieldCenter;

        readonly List<Vector2Int> enemyCells = new();
        readonly QixCaptureService captureService = new();

        QixGrid grid;
        QixTrail trail;
        Vector2Int lastCell;
        Vector2Int lastClaimedCell;

        void Awake()
        {
            BuildGrid();
            trail = new QixTrail();
        }

        // 씬 뷰에서 플레이 영역을 보면서 조절할 수 있게 그린다.
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(fieldCenter, fieldSize);
        }

        void Start()
        {
            if (gridRenderer != null)
            {
                gridRenderer.Bind(grid);
            }

            // 가장자리 셀 중심을 경계로 주면 플레이어가 플레이 영역을 벗어나지 않는다.
            player.SetBounds(
                grid.CellToWorld(Vector2Int.zero),
                grid.CellToWorld(new Vector2Int(grid.Columns - 1, grid.Rows - 1)));

            SnapPlayerToClaimedCell();
        }

        void Update()
        {
            var currentCell = grid.WorldToCell(player.transform.position);
            if (currentCell == lastCell)
            {
                return;
            }

            StepTo(currentCell);
        }

        // 플레이어는 확보된 영역에서 출발해야 한다.
        // 미확보 칸에서 시작하면 시작하자마자 궤적이 그려진다.
        void SnapPlayerToClaimedCell()
        {
            var startCell = grid.WorldToCell(player.transform.position);
            if (grid.GetState(startCell) != CellState.Claimed)
            {
                startCell = grid.FindNearestCell(startCell, CellState.Claimed);
            }

            lastClaimedCell = startCell;
            MovePlayerToCell(startCell);
        }

        void MovePlayerToCell(Vector2Int cell)
        {
            player.MoveTo(grid.CellToWorld(cell));
            lastCell = cell;
        }

        // 프레임 사이에 두 칸 이상 이동하면 궤적에 구멍이 생겨 영역 분할에 실패한다.
        // 지나친 칸을 빠짐없이 순서대로 방문시킨다.
        void StepTo(Vector2Int target)
        {
            while (lastCell != target)
            {
                var delta = target - lastCell;
                var step = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y)
                    ? new Vector2Int((int)Mathf.Sign(delta.x), 0)
                    : new Vector2Int(0, (int)Mathf.Sign(delta.y));

                lastCell += step;

                // 사망으로 플레이어가 되돌려졌다면 target 은 이미 무효한 목적지다.
                if (!HandleCellEnter(lastCell))
                {
                    return;
                }
            }
        }

        public void SetEnemyCells(IReadOnlyList<Vector2Int> cells)
        {
            enemyCells.Clear();
            for (int i = 0; i < cells.Count; i++)
            {
                enemyCells.Add(cells[i]);
            }
        }

        void BuildGrid()
        {
            var origin = fieldCenter - fieldSize * 0.5f;

            // 칸 수를 반올림한 뒤 실제 칸 크기를 다시 계산해, 그리드가 플레이 영역에 정확히 들어맞게 한다.
            int columns = Mathf.Max(2, Mathf.RoundToInt(fieldSize.x / cellWorldSize));
            int rows = Mathf.Max(2, Mathf.RoundToInt(fieldSize.y / cellWorldSize));
            var cellSize = new Vector2(fieldSize.x / columns, fieldSize.y / rows);

            grid = new QixGrid(columns, rows, origin, cellSize);
        }

        // 이어서 다음 칸을 진행해도 되면 true, 사망으로 플레이어가 되돌려졌으면 false 를 반환한다.
        bool HandleCellEnter(Vector2Int cell)
        {
            var state = grid.GetState(cell);

            if (state == CellState.Claimed)
            {
                // 사망 시 복귀 지점. 확보 영역을 벗어나기 직전 칸을 기억한다.
                lastClaimedCell = cell;
                CompleteTrailIfAny();
                return true;
            }

            if (!trail.IsDrawing)
            {
                trail.Begin(cell);
                grid.SetState(cell, CellState.Trail);
                RefreshRenderer();
                return true;
            }

            if (!trail.TryAddPoint(cell))
            {
                HandlePlayerDeath();
                return false;
            }

            grid.SetState(cell, CellState.Trail);
            RefreshRenderer();
            return true;
        }

        void CompleteTrailIfAny()
        {
            if (!trail.IsDrawing)
            {
                return;
            }

            // 칸 수와 무관하게 Capture 를 호출해야 한다. 건너뛰면 Trail 상태인 칸이 그대로 남아
            // 영원히 미확보로 취급되며 이후 flood fill 을 막는다.
            int capturedCells = captureService.Capture(grid, trail.Points, enemyCells);
            trail.Cancel();
            RefreshRenderer();

            #if UNITY_EDITOR
            if (capturedCells > 0)
            {
                print($"확보 영역 {grid.ClaimedRatio * 100f:F1}% (이번 확보 {capturedCells}칸)");
            }
            #endif
        }

        void HandlePlayerDeath()
        {
            // IReadOnlyList 를 foreach 로 돌면 열거자가 박싱되어 힙 할당이 생긴다. 인덱스로 접근할 것.
            var points = trail.Points;
            for (int i = 0; i < points.Count; i++)
            {
                grid.SetState(points[i], CellState.Empty);
            }

            trail.Cancel();

            // 미확보 칸에 그대로 두면 다음 프레임에 곧바로 새 궤적이 시작된다.
            MovePlayerToCell(lastClaimedCell);
            RefreshRenderer();
        }

        void RefreshRenderer()
        {
            if (gridRenderer != null)
            {
                gridRenderer.Refresh();
            }
        }
    }
}
