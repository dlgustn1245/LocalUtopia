using UnityEngine;

namespace Qix
{
    // 확보(Claimed) / 미확보(Empty) / 궤적(Trail) 상태를 셀 단위로 들고 있는 게임판.
    // 테두리는 생성 시점에 Claimed 로 초기화한다 (Qix 규칙상 플레이어는 항상 확보된 영역에서 출발).
    public class QixGrid
    {
        public int Columns { get; }
        public int Rows { get; }
        public Vector2 Origin { get; }
        public Vector2 CellSize { get; }

        readonly CellState[,] cells;

        public QixGrid(int columns, int rows, Vector2 origin, Vector2 cellSize)
        {
            Columns = Mathf.Max(2, columns);
            Rows = Mathf.Max(2, rows);
            Origin = origin;
            CellSize = cellSize;
            cells = new CellState[Columns, Rows];

            InitializeBorder();
        }

        void InitializeBorder()
        {
            for (int x = 0; x < Columns; x++)
            {
                cells[x, 0] = CellState.Claimed;
                cells[x, Rows - 1] = CellState.Claimed;
            }

            for (int y = 0; y < Rows; y++)
            {
                cells[0, y] = CellState.Claimed;
                cells[Columns - 1, y] = CellState.Claimed;
            }
        }

        // 확보한 칸의 비율(0~1). 테두리가 처음부터 확보 상태이므로 시작값은 0 이 아니다.
        public float ClaimedRatio
        {
            get
            {
                int claimed = 0;
                foreach (var state in cells)
                {
                    if (state == CellState.Claimed)
                    {
                        claimed++;
                    }
                }

                return (float)claimed / (Columns * Rows);
            }
        }

        public bool IsInBounds(Vector2Int cell)
        {
            return cell.x >= 0 && cell.x < Columns && cell.y >= 0 && cell.y < Rows;
        }

        public CellState GetState(Vector2Int cell)
        {
            return cells[cell.x, cell.y];
        }

        public void SetState(Vector2Int cell, CellState state)
        {
            cells[cell.x, cell.y] = state;
        }

        // 셀 (x,y) 는 Origin + (x,y)*CellSize 부터 Origin + (x+1,y+1)*CellSize 까지의 사각형을 차지한다.
        // 렌더러가 텍스처 픽셀을 그리는 범위와 동일해야 하므로 이 규약을 바꾸지 말 것.
        public Vector2Int WorldToCell(Vector2 worldPosition)
        {
            var local = worldPosition - Origin;
            int x = Mathf.Clamp(Mathf.FloorToInt(local.x / CellSize.x), 0, Columns - 1);
            int y = Mathf.Clamp(Mathf.FloorToInt(local.y / CellSize.y), 0, Rows - 1);
            return new Vector2Int(x, y);
        }

        // 셀의 중심 월드 좌표를 반환한다.
        public Vector2 CellToWorld(Vector2Int cell)
        {
            return Origin + new Vector2((cell.x + 0.5f) * CellSize.x, (cell.y + 0.5f) * CellSize.y);
        }

    }
}
