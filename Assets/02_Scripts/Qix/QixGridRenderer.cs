using UnityEngine;

namespace Qix
{
    // 그리드 상태를 텍스처로 시각화한다. 알고리즘 확인용 샘플 렌더러.
    //
    // 선(변)은 두께가 없어 칸 텍스처로 그대로 표현할 수 없으므로, 변마다 한쪽 칸을 칠해 나타낸다.
    // 판정은 변으로 하고 표시만 칸 단위로 근사하는 것이라 게임 로직에는 영향이 없다.
    // 확보 영역 선과 궤적이 같은 규칙으로 1칸씩 그려지므로 두께가 항상 같다.
    [RequireComponent(typeof(SpriteRenderer))]
    public class QixGridRenderer : MonoBehaviour
    {
        public SpriteRenderer backgroundRenderer;
        
        public Color emptyColor;
        public Color claimedColor;
        public Color lineColor;
        public Color trailColor;

        SpriteRenderer spriteRenderer;
        Texture2D texture;
        QixGrid grid;
        QixTrail trail;

        // RGBA32 텍스처와 형식이 같아 SetPixels32 가 변환 없이 복사한다.
        // Color(float x4, 16바이트) 대신 쓰면 버퍼 크기도 1/4 이다.
        Color32[] pixelBuffer;
        bool isDirty;

        void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        void LateUpdate()
        {
            if (!isDirty)
            {
                return;
            }

            isDirty = false;
            Redraw();
        }

        void OnDestroy()
        {
            ReleaseTexture();
        }

        public void Bind(QixGrid targetGrid, QixTrail targetTrail)
        {
            ReleaseTexture();

            grid = targetGrid;
            trail = targetTrail;
            texture = new Texture2D(grid.Columns, grid.Rows, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            pixelBuffer = new Color32[grid.Columns * grid.Rows];

            spriteRenderer.sprite = Sprite.Create(
                texture,
                new Rect(0, 0, grid.Columns, grid.Rows),
                new Vector2(0.5f, 0.5f),
                1f);

            var fieldWorldSize = new Vector2(grid.Columns * grid.CellSize.x, grid.Rows * grid.CellSize.y);

            transform.localScale = new Vector3(grid.CellSize.x, grid.CellSize.y, 1f);
            transform.position = grid.Origin + fieldWorldSize * 0.5f;

            if (backgroundRenderer != null)
            {
                var spriteSize = backgroundRenderer.sprite.bounds.size;
                backgroundRenderer.transform.localScale = new Vector3(fieldWorldSize.x / spriteSize.x, fieldWorldSize.y / spriteSize.y, 1f);
                backgroundRenderer.transform.position = grid.Origin + fieldWorldSize * 0.5f;
            }
            
            isDirty = false;
            Redraw();
        }

        // 한 프레임에 여러 칸이 바뀌어도 텍스처 업로드는 프레임당 한 번이면 충분하다.
        // 궤적을 그리는 동안 프레임마다 여러 번 호출되므로 여기서 곧바로 다시 그리지 않는다.
        public void Refresh()
        {
            isDirty = true;
        }

        void Redraw()
        {
            if (grid == null)
            {
                return;
            }

            FillCells();
            PaintBoundaryLines();

            // 그리는 중인 궤적이 가장 잘 보여야 하므로 마지막에 덮어쓴다.
            PaintTrail();

            texture.SetPixels32(pixelBuffer);
            texture.Apply(false);
        }

        void FillCells()
        {
            Color32 empty = emptyColor;
            Color32 claimed = claimedColor;

            for (int y = 0; y < grid.Rows; y++)
            {
                for (int x = 0; x < grid.Columns; x++)
                {
                    pixelBuffer[y * grid.Columns + x] =
                        grid.GetState(new Vector2Int(x, y)) == CellState.Claimed ? claimed : empty;
                }
            }
        }

        void PaintBoundaryLines()
        {
            Color32 line = lineColor;

            for (int x = 0; x < grid.Columns; x++)
            {
                for (int y = 0; y <= grid.Rows; y++)
                {
                    if (grid.GetHorizontalEdge(x, y) == EdgeState.Boundary)
                    {
                        PaintHorizontalEdge(x, y, line);
                    }
                }
            }

            for (int x = 0; x <= grid.Columns; x++)
            {
                for (int y = 0; y < grid.Rows; y++)
                {
                    if (grid.GetVerticalEdge(x, y) == EdgeState.Boundary)
                    {
                        PaintVerticalEdge(x, y, line);
                    }
                }
            }
        }

        // 궤적은 꼭짓점 목록을 그대로 쓴다. 전체 격자를 훑는 것보다 훨씬 싸다.
        void PaintTrail()
        {
            if (trail == null)
            {
                return;
            }

            Color32 color = trailColor;
            var points = trail.Points;

            for (int i = 1; i < points.Count; i++)
            {
                var from = points[i - 1];
                var to = points[i];

                if (from.y == to.y)
                {
                    PaintHorizontalEdge(Mathf.Min(from.x, to.x), from.y, color);
                }
                else
                {
                    PaintVerticalEdge(from.x, Mathf.Min(from.y, to.y), color);
                }
            }
        }

        // 선은 칸과 칸 사이에 놓이므로 한쪽 칸만 칠해 두께를 1칸으로 맞춘다.
        // 위/오른쪽 칸을 기본으로 삼고 격자 밖이면 반대쪽으로 넘긴다.
        // 확보 영역 선과 궤적이 같은 규칙을 쓰므로 궤적이 선으로 승격돼도 위치가 튀지 않는다.
        //
        // 변이 끝나는 꼭짓점 쪽 칸까지 칠하는 이유:
        // 가로 변은 위쪽 칸을, 세로 변은 오른쪽 칸을 잡기 때문에 "왼쪽 가로 + 아래 세로"로 꺾이는
        // 모서리에서만 두 칸이 대각선으로 어긋난다. 끝 칸을 함께 칠하면 네 방향 모두 공통 칸이 생긴다.
        // 같은 줄에 칠하므로 직선 구간의 두께는 그대로 1칸이다.
        void PaintHorizontalEdge(int x, int y, Color32 color)
        {
            int row = y < grid.Rows ? y : y - 1;
            PaintCell(x, row, color);
            PaintCell(x + 1, row, color);
        }

        void PaintVerticalEdge(int x, int y, Color32 color)
        {
            int column = x < grid.Columns ? x : x - 1;
            PaintCell(column, y, color);
            PaintCell(column, y + 1, color);
        }

        void PaintCell(int x, int y, Color32 color)
        {
            if (x < 0 || x >= grid.Columns || y < 0 || y >= grid.Rows)
            {
                return;
            }

            pixelBuffer[y * grid.Columns + x] = color;
        }

        // 런타임 생성 Texture2D / Sprite 는 GC 로 네이티브 메모리가 회수되지 않아 직접 파괴한다.
        void ReleaseTexture()
        {
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                Destroy(spriteRenderer.sprite);
                spriteRenderer.sprite = null;
            }

            if (texture != null)
            {
                Destroy(texture);
                texture = null;
            }

            pixelBuffer = null;
            trail = null;
            isDirty = false;
        }
    }
}
