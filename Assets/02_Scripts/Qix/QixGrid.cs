using System;
using UnityEngine;

namespace Qix
{
    // 게임판. 두 가지 격자를 겹쳐서 들고 있다.
    // - 칸(cell): Columns x Rows. 확보 여부를 담으며 채우기 판정과 렌더링에 쓴다.
    // - 변(edge): 꼭짓점 격자 (Columns+1) x (Rows+1) 위의 선. 플레이어의 이동 경로다.
    //
    // 플레이어는 칸이 아니라 꼭짓점 위에 서고 변을 따라 움직인다. 확보한 영역의 내부는 지날 수 없다.
    public class QixGrid
    {
        public int Columns { get; }
        public int Rows { get; }
        public Vector2 Origin { get; }
        public Vector2 CellSize { get; }

        readonly CellState[,] cells;

        // horizontalEdges[x, y]: 꼭짓점 (x,y) - (x+1,y) 를 잇는 변
        // verticalEdges[x, y]:   꼭짓점 (x,y) - (x,y+1) 를 잇는 변
        readonly EdgeState[,] horizontalEdges;
        readonly EdgeState[,] verticalEdges;

        public QixGrid(int columns, int rows, Vector2 origin, Vector2 cellSize)
        {
            Columns = Mathf.Max(2, columns);
            Rows = Mathf.Max(2, rows);
            Origin = origin;
            CellSize = cellSize;

            cells = new CellState[Columns, Rows];
            horizontalEdges = new EdgeState[Columns, Rows + 1];
            verticalEdges = new EdgeState[Columns + 1, Rows];

            InitializeBorderEdges();
        }

        // 아레나 바깥 테두리는 처음부터 이동 가능한 선이다. 칸은 전부 미확보 상태로 시작한다.
        void InitializeBorderEdges()
        {
            for (int x = 0; x < Columns; x++)
            {
                horizontalEdges[x, 0] = EdgeState.Boundary;
                horizontalEdges[x, Rows] = EdgeState.Boundary;
            }

            for (int y = 0; y < Rows; y++)
            {
                verticalEdges[0, y] = EdgeState.Boundary;
                verticalEdges[Columns, y] = EdgeState.Boundary;
            }
        }

        // 확보한 칸의 비율(0~1).
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

        // ---- 칸 ----

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

        public void ClaimAllArea()
        {
            for (int i = 0; i < Columns; i++)
            {
                for (int j = 0; j < Rows; j++)
                {
                    cells[i, j] = CellState.Claimed;
                }
            }
            Array.Clear(horizontalEdges, 0, horizontalEdges.Length);
            Array.Clear(verticalEdges, 0, verticalEdges.Length);
            
            InitializeBorderEdges();
        }

        // 칸 (x,y) 는 Origin + (x,y)*CellSize 부터 Origin + (x+1,y+1)*CellSize 까지의 사각형을 차지한다.
        // 렌더러가 텍스처 픽셀을 그리는 범위와 동일해야 하므로 이 규약을 바꾸지 말 것.
        public Vector2Int WorldToCell(Vector2 worldPosition)
        {
            var local = worldPosition - Origin;
            int x = Mathf.Clamp(Mathf.FloorToInt(local.x / CellSize.x), 0, Columns - 1);
            int y = Mathf.Clamp(Mathf.FloorToInt(local.y / CellSize.y), 0, Rows - 1);
            return new Vector2Int(x, y);
        }

        // 칸의 중심 월드 좌표를 반환한다.
        public Vector2 CellToWorld(Vector2Int cell)
        {
            return Origin + new Vector2((cell.x + 0.5f) * CellSize.x, (cell.y + 0.5f) * CellSize.y);
        }

        // ---- 꼭짓점 ----

        public bool IsVertexInBounds(Vector2Int vertex)
        {
            return vertex.x >= 0 && vertex.x <= Columns && vertex.y >= 0 && vertex.y <= Rows;
        }

        // 꼭짓점은 칸의 모서리에 놓이므로 중심 보정이 없다.
        public Vector2 VertexToWorld(Vector2Int vertex)
        {
            return Origin + new Vector2(vertex.x * CellSize.x, vertex.y * CellSize.y);
        }

        // ---- 변 ----

        // from 과 to 는 상하좌우로 인접한 꼭짓점이어야 한다. 격자를 벗어나면 None 을 반환한다.
        public EdgeState GetEdge(Vector2Int from, Vector2Int to)
        {
            if (from.y == to.y)
            {
                int x = Mathf.Min(from.x, to.x);
                if (x < 0 || x >= Columns || from.y < 0 || from.y > Rows)
                {
                    return EdgeState.None;
                }

                return horizontalEdges[x, from.y];
            }

            int y = Mathf.Min(from.y, to.y);
            if (from.x < 0 || from.x > Columns || y < 0 || y >= Rows)
            {
                return EdgeState.None;
            }

            return verticalEdges[from.x, y];
        }

        public void SetEdge(Vector2Int from, Vector2Int to, EdgeState state)
        {
            if (from.y == to.y)
            {
                horizontalEdges[Mathf.Min(from.x, to.x), from.y] = state;
                return;
            }

            verticalEdges[from.x, Mathf.Min(from.y, to.y)] = state;
        }

        // 꼭짓점에 이동 가능한 선이 하나라도 닿아 있는지. 궤적이 기존 선에 도달했는지 판정한다.
        public bool IsBoundaryVertex(Vector2Int vertex)
        {
            return GetEdge(vertex, vertex + Vector2Int.left) == EdgeState.Boundary
                || GetEdge(vertex, vertex + Vector2Int.right) == EdgeState.Boundary
                || GetEdge(vertex, vertex + Vector2Int.down) == EdgeState.Boundary
                || GetEdge(vertex, vertex + Vector2Int.up) == EdgeState.Boundary;
        }

        // 궤적을 그리며 지날 수 있는 변인지.
        // 이미 선이 놓였거나(되돌아가기, 기존 경로) 확보된 영역에 접하면 그릴 수 없다.
        public bool CanDrawEdge(Vector2Int from, Vector2Int to)
        {
            if (GetEdge(from, to) != EdgeState.None)
            {
                return false;
            }

            if (from.y == to.y)
            {
                int x = Mathf.Min(from.x, to.x);
                return IsCellEmpty(x, from.y - 1) && IsCellEmpty(x, from.y);
            }

            int y = Mathf.Min(from.y, to.y);
            return IsCellEmpty(from.x - 1, y) && IsCellEmpty(from.x, y);
        }

        // 인접한 두 칸 사이를 선이 막고 있는지. flood fill 의 벽 판정에 쓴다.
        public bool IsBoundaryBetweenCells(Vector2Int a, Vector2Int b)
        {
            if (a.y == b.y)
            {
                return verticalEdges[Mathf.Max(a.x, b.x), a.y] == EdgeState.Boundary;
            }

            return horizontalEdges[a.x, Mathf.Max(a.y, b.y)] == EdgeState.Boundary;
        }

        // 양옆 칸이 모두 확보된 변을 지운다. 두 미확보 영역을 갈랐던 옛 궤적이 양쪽 다 확보된 뒤에도 남아
        // 확보 영역 내부를 걸어 다닐 수 있게 되는 것을 막는다. 확보 영역은 둘레만 다닐 수 있어야 한다.
        // 바깥 테두리는 한쪽이 격자 밖이라 여기서 지워지지 않는다.
        public void ClearEdgesInsideClaimed()
        {
            for (int x = 0; x < Columns; x++)
            {
                for (int y = 1; y < Rows; y++)
                {
                    if (cells[x, y - 1] == CellState.Claimed && cells[x, y] == CellState.Claimed)
                    {
                        horizontalEdges[x, y] = EdgeState.None;
                    }
                }
            }

            for (int x = 1; x < Columns; x++)
            {
                for (int y = 0; y < Rows; y++)
                {
                    if (cells[x - 1, y] == CellState.Claimed && cells[x, y] == CellState.Claimed)
                    {
                        verticalEdges[x, y] = EdgeState.None;
                    }
                }
            }
        }

        // 렌더러가 변을 직접 훑을 수 있게 열어 둔다. 인덱스 범위는 각 배열의 크기와 같다.
        public EdgeState GetHorizontalEdge(int x, int y)
        {
            return horizontalEdges[x, y];
        }

        public EdgeState GetVerticalEdge(int x, int y)
        {
            return verticalEdges[x, y];
        }

        bool IsCellEmpty(int x, int y)
        {
            return x >= 0 && x < Columns && y >= 0 && y < Rows && cells[x, y] == CellState.Empty;
        }
    }
}
