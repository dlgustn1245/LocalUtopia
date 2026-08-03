using UnityEngine;

namespace Qix
{
    // 그리드 상태를 텍스처로 시각화한다. 알고리즘 확인용 샘플 렌더러.
    [RequireComponent(typeof(SpriteRenderer))]
    public class QixGridRenderer : MonoBehaviour
    {
        public Color emptyColor = new(0f, 0f, 0f, 0f);
        public Color claimedColor = new(0.2f, 0.6f, 1f, 0.6f);
        public Color trailColor = Color.yellow;

        SpriteRenderer spriteRenderer;
        Texture2D texture;
        QixGrid grid;

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

        public void Bind(QixGrid targetGrid)
        {
            ReleaseTexture();

            grid = targetGrid;
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

            transform.localScale = new Vector3(grid.CellSize.x, grid.CellSize.y, 1f);
            transform.position = grid.Origin + new Vector2(grid.Columns * grid.CellSize.x, grid.Rows * grid.CellSize.y) * 0.5f;

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

            Color32 empty = emptyColor;
            Color32 claimed = claimedColor;
            Color32 trail = trailColor;

            for (int y = 0; y < grid.Rows; y++)
            {
                for (int x = 0; x < grid.Columns; x++)
                {
                    var cell = new Vector2Int(x, y);
                    pixelBuffer[y * grid.Columns + x] = grid.GetState(cell) switch
                    {
                        CellState.Claimed => claimed,
                        CellState.Trail => trail,
                        _ => empty
                    };
                }
            }

            texture.SetPixels32(pixelBuffer);
            texture.Apply(false);
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
            isDirty = false;
        }
    }
}
