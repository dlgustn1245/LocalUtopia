using UnityEngine;

namespace Qix
{
    // 그리드 상태를 텍스처로 시각화한다.
    //
    // 텍스처는 칸당 pixelsPerCell 픽셀로 만든다. 칸과 같은 해상도로 만들면 선(변)이 칸 하나를 통째로
    // 덮어 확보 영역의 경계가 실제보다 두껍게 보이기 때문이다.
    // 선은 두 칸 사이의 변 위치에 lineThickness 픽셀로 중심을 맞춰 그린다. 판정은 변으로 하고 표시만 픽셀로
    // 근사하는 것이라 게임 로직에는 영향이 없다.
    [RequireComponent(typeof(SpriteRenderer))]
    public class QixGridRenderer : MonoBehaviour
    {
        public SpriteRenderer backgroundRenderer;

        // 칸 하나가 차지하는 텍스처 픽셀 수. 클수록 선이 얇아 보이지만 텍스처 메모리는 제곱으로 늘어난다.
        // 80x140 칸 기준 4 → 320x560(약 0.7MB), 8 → 640x1120(약 2.9MB). 매 프레임 전체를 업로드하므로 4 를 넘기지 말 것.
        public int pixelsPerCell = 4;

        // 선 두께(픽셀). 변에 중심을 맞추므로 짝수면 양쪽 칸을 같은 폭으로 침범한다.
        public int lineThickness = 2;

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
        int textureWidth;
        int textureHeight;

        // 두 단계 dirty. 칸·경계가 바뀌면 전체를 다시 칠하고, 궤적만 늘었으면 궤적 변만 버퍼에 덧칠한다.
        // 궤적을 그리는 동안은 거의 매 프레임 갱신되므로 전체 칸(수만 개)을 매번 채우지 않는 것이 핵심이다.
        bool isStructureDirty;
        bool isTrailDirty;

        void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        void LateUpdate()
        {
            if (isStructureDirty)
            {
                Redraw();
            }
            else if (isTrailDirty)
            {
                RedrawTrailOnly();
            }

            isStructureDirty = false;
            isTrailDirty = false;
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

            pixelsPerCell = Mathf.Max(1, pixelsPerCell);
            lineThickness = Mathf.Clamp(lineThickness, 1, pixelsPerCell);
            textureWidth = grid.Columns * pixelsPerCell;
            textureHeight = grid.Rows * pixelsPerCell;

            texture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            pixelBuffer = new Color32[textureWidth * textureHeight];

            // pixelsPerUnit 을 칸당 픽셀 수로 두면 스프라이트 크기가 칸 수와 같아져 스케일 계산이 그대로다.
            spriteRenderer.sprite = Sprite.Create(
                texture,
                new Rect(0, 0, textureWidth, textureHeight),
                new Vector2(0.5f, 0.5f),
                pixelsPerCell);

            var fieldWorldSize = new Vector2(grid.Columns * grid.CellSize.x, grid.Rows * grid.CellSize.y);

            transform.localScale = new Vector3(grid.CellSize.x, grid.CellSize.y, 1f);
            transform.position = grid.Origin + fieldWorldSize * 0.5f;

            if (backgroundRenderer != null)
            {
                var spriteSize = backgroundRenderer.sprite.bounds.size;
                backgroundRenderer.transform.localScale = new Vector3(fieldWorldSize.x / spriteSize.x, fieldWorldSize.y / spriteSize.y, 1f);
                backgroundRenderer.transform.position = grid.Origin + fieldWorldSize * 0.5f;
            }

            isStructureDirty = false;
            isTrailDirty = false;
            Redraw();
        }

        // 칸이나 경계가 바뀌었을 때. 한 프레임에 여러 번 불려도 텍스처 업로드는 프레임당 한 번이면 충분하다.
        public void Refresh()
        {
            isStructureDirty = true;
        }

        // 궤적 꼭짓점이 하나 늘었을 때. 칸·경계는 그대로이므로 궤적 변만 덧칠한다.
        public void RefreshTrail()
        {
            isTrailDirty = true;
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

        // 버퍼에는 직전 프레임의 칸·경계·궤적이 그대로 남아 있으므로 궤적 변만 덧칠하면 된다.
        void RedrawTrailOnly()
        {
            if (grid == null)
            {
                return;
            }

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
                    var color = grid.GetState(new Vector2Int(x, y)) == CellState.Claimed ? claimed : empty;
                    PaintRect(x * pixelsPerCell, y * pixelsPerCell, (x + 1) * pixelsPerCell, (y + 1) * pixelsPerCell, color);
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

        // 변의 양 끝을 두께의 절반만큼 늘려 그린다. 꺾이는 모서리에서 두 선이 빈틈없이 만나게 하기 위함이다.
        void PaintHorizontalEdge(int x, int y, Color32 color)
        {
            int half = lineThickness / 2;
            int startY = LineStart(y * pixelsPerCell, textureHeight);
            PaintRect(
                x * pixelsPerCell - half, startY,
                (x + 1) * pixelsPerCell + half, startY + lineThickness,
                color);
        }

        void PaintVerticalEdge(int x, int y, Color32 color)
        {
            int half = lineThickness / 2;
            int startX = LineStart(x * pixelsPerCell, textureWidth);
            PaintRect(
                startX, y * pixelsPerCell - half,
                startX + lineThickness, (y + 1) * pixelsPerCell + half,
                color);
        }

        // 선을 변 위치에 중심을 맞추되, 바깥 테두리는 텍스처 밖으로 잘리지 않게 안쪽으로 밀어 넣는다.
        // 그래야 테두리도 내부 선과 같은 두께로 보인다.
        int LineStart(int center, int textureSize)
        {
            return Mathf.Clamp(center - lineThickness / 2, 0, textureSize - lineThickness);
        }

        // [x0, x1) x [y0, y1) 픽셀 사각형을 칠한다. 텍스처 범위를 벗어나는 부분은 잘라낸다.
        void PaintRect(int x0, int y0, int x1, int y1, Color32 color)
        {
            x0 = Mathf.Max(x0, 0);
            y0 = Mathf.Max(y0, 0);
            x1 = Mathf.Min(x1, textureWidth);
            y1 = Mathf.Min(y1, textureHeight);

            for (int y = y0; y < y1; y++)
            {
                int rowStart = y * textureWidth;
                for (int x = x0; x < x1; x++)
                {
                    pixelBuffer[rowStart + x] = color;
                }
            }
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
            isStructureDirty = false;
            isTrailDirty = false;
        }
    }
}
