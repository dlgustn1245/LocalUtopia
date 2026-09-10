using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Qix
{
    // 플레이어를 꼭짓점 격자 위에서 선을 따라 이동시키고, 궤적이 완성되면 영역 확보를 처리한다.
    // 그리드는 지정한 플레이 영역(fieldSize / fieldCenter)을 기준으로 생성된다.
    //
    // 이동은 꼭짓점 단위로 끊어서 처리한다. 프레임마다 위치를 보고 역산하면 빠르게 움직일 때
    // 지나친 변을 놓쳐 궤적에 구멍이 생기기 때문이다.
    public class QixScene : MonoBehaviour
    {
        readonly List<Vector2Int> enemyCells = new();
        readonly QixCaptureService captureService = new();
        
        public Player player;
        public float cellWorldSize;
        public QixGridRenderer gridRenderer;

        public TextMeshProUGUI percentageText;
        public TextMeshProUGUI timerText;

        // 플레이 영역(월드 단위). 화면 전체가 아니라 상단 HUD, 하단 조작 UI 자리를 뺀 크기를 지정한다.
        // QixManager 를 선택하면 씬 뷰에 초록 사각형으로 표시되므로 보면서 조절하면 된다.
        public Vector2 fieldSize;
        public Vector2 fieldCenter;

        public Slider ratioSlider;

        public Button clearPopup;
        public Button failPopup;

        public GameObject deathCountIcon;
        public Transform deathCountParent;

        // GameManager 없이 이 씬만 단독 실행할 때 쓰는 스테이지. 타이틀을 거쳐 들어오면 무시된다.
        public StageData debugStage;

        QixGrid grid;
        QixTrail trail;

        StageData stage;
        int remainingLives;
        Vector2Int currentVertex;
        Vector2Int targetVertex;
        bool isMoving;
        bool canMove = true;

        Coroutine timerCoroutine;
        WaitForSeconds delay;
        int remainTime;

        void Awake()
        {
            BuildGrid();
            trail = new QixTrail();
        }

        void Start()
        {
            InitStage();
            gridRenderer.Bind(grid, trail);
            ratioSlider.value = 0f;
            percentageText.text = "0%";
            
            // 좌상단 모서리에서 시작한다. 아레나 테두리라 항상 이동 가능한 선 위다.
            SetPlayerFirstVertex();

            timerCoroutine = StartCoroutine(StartTimer());
            BindButtonEvent();
        }

        void BindButtonEvent()
        {
            clearPopup.onClick.AddListener(() =>
            {
                SceneLoader.Load(SceneNames.Title);
            });
            failPopup.onClick.AddListener(() =>
            {
                SceneLoader.Load(SceneNames.Title);
            });
        }

        void InitStage()
        {
            var gameManager = GameManager.Instance;
            stage = gameManager != null ? gameManager.stages[gameManager.currStage] : debugStage;
            remainingLives = stage.deathCount;
            gridRenderer.backgroundRenderer.sprite = stage.hiddenImage;

            for (int i = 0; i < stage.deathCount; i++)
            {
                Instantiate(deathCountIcon, deathCountParent);
            }
        }

        void SetPlayerFirstVertex()
        {
            MovePlayerToVertex(new Vector2Int(0, grid.Rows));
        }

        IEnumerator StartTimer()
        {
            delay = new WaitForSeconds(1.0f);

            for (remainTime = stage.timer; remainTime > 0; remainTime--)
            {
                timerText.text = $"TIME {remainTime}";
                yield return delay;
            }
            HandlePlayerDeath(true);
        }

        // 플레이어를 특정 꼭짓점으로 순간 이동시킨다.
        // 이동 상태 세 가지를 한꺼번에 맞추는 유일한 경로로 두어, 한쪽만 갱신되는 일이 없게 한다.
        void MovePlayerToVertex(Vector2Int vertex)
        {
            currentVertex = vertex;
            targetVertex = vertex;
            isMoving = false;
            player.MoveTo(grid.VertexToWorld(vertex));
        }

        // 씬 뷰에서 플레이 영역을 보면서 조절할 수 있게 그린다.
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(fieldCenter, fieldSize);
        }

        void Update()
        {
            if (isMoving)
            {
                if (!player.MoveTowards(grid.VertexToWorld(targetVertex)))
                {
                    return;
                }

                isMoving = false;
                ArriveAtVertex(targetVertex);
            }

            TryStartNextMove();
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

        void TryStartNextMove()
        {
            var direction = player.InputDirection;
            
            if (!canMove)
            {
                return;
            }
            
            if (direction == Vector2Int.zero)
            {
                return;
            }

            var next = currentVertex + direction;
            if (!grid.IsVertexInBounds(next))
            {
                return;
            }

            if (trail.IsDrawing)
            {
                // 그리는 중에는 미확보 영역만 가로지를 수 있다. 왔던 길로 되돌아가기도 여기서 막힌다.
                if (!grid.CanDrawEdge(currentVertex, next))
                {
                    return;
                }
            }
            else if (grid.GetEdge(currentVertex, next) == EdgeState.Boundary)
            {
                // 이미 놓인 선을 따라가는 이동. 그리기 버튼과 무관하다.
            }
            else if (grid.CanDrawEdge(currentVertex, next))
            {
                // 미확보 영역 쪽으로 향하면 곧바로 궤적이 시작된다.
                trail.Begin(currentVertex);
                player.SetDrawSprite();
            }
            else
            {
                return;
            }

            targetVertex = next;
            isMoving = true;
        }

        void ArriveAtVertex(Vector2Int vertex)
        {
            var previous = currentVertex;
            currentVertex = vertex;

            if (!trail.IsDrawing)
            {
                return;
            }

            // 이미 지나온 꼭짓점이면 궤적이 자기 자신과 교차한 것이다.
            if (!trail.TryAddPoint(vertex))
            {
                HandlePlayerDeath();
                return;
            }

            grid.SetEdge(previous, vertex, EdgeState.Trail);
            RefreshRenderer();

            if (grid.IsBoundaryVertex(vertex))
            {
                CompleteTrail();
            }
        }

        void CompleteTrail()
        {
            SetTrailEdges(EdgeState.Boundary);
            trail.Cancel();
            player.SetSafeSprite();

            // 궤적을 선으로 승격한 뒤에 호출해야 flood fill 이 새 선을 벽으로 인식한다.
            int capturedCells = captureService.Capture(grid, enemyCells);
            RefreshRenderer();

            if (capturedCells > 0)
            {
                ratioSlider.value = grid.ClaimedRatio;
                percentageText.text = $"{Mathf.FloorToInt(grid.ClaimedRatio * 100f)}%";
                
                if (grid.ClaimedRatio * 100f >= stage.clearRatio)
                {
                    print("Stage Clear");
                    canMove = false;
                    StageClear();
                }
            }
        }

        void StageClear()
        {
            SetPlayerFirstVertex();
            grid.ClaimAllArea();
            RefreshRenderer();
            StopCoroutine(timerCoroutine);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.StageClear();
            }


            ratioSlider.value = 1.0f;
            percentageText.text = "100%";
            clearPopup.gameObject.SetActive(true);
        }

        void HandlePlayerDeath(bool force = false)
        {
            SetTrailEdges(EdgeState.None);

            var respawnVertex = trail.Points.Count > 0 ? trail.Points[0] : currentVertex;
            trail.Cancel();
            player.SetSafeSprite();

            if (force || CheckPlayerDead())
            {
                StopCoroutine(timerCoroutine);
                canMove = false;
                failPopup.gameObject.SetActive(true);
                SetPlayerFirstVertex();
                RefreshRenderer();
                return;
            }

            MovePlayerToVertex(respawnVertex);
            RefreshRenderer();
        }

        bool CheckPlayerDead()
        {
            RemoveDeathCountIcon();

            return --remainingLives <= 0;
        }

        void RemoveDeathCountIcon()
        {
            int last = deathCountParent.childCount - 1;
            if (last >= 0)
            {
                Destroy(deathCountParent.GetChild(last).gameObject);
            }
        }

        void SetTrailEdges(EdgeState state)
        {
            // IReadOnlyList 를 foreach 로 돌면 열거자가 박싱되어 힙 할당이 생긴다. 인덱스로 접근할 것.
            var points = trail.Points;
            for (int i = 1; i < points.Count; i++)
            {
                grid.SetEdge(points[i - 1], points[i], state);
            }
        }

        void RefreshRenderer()
        {
            gridRenderer.Refresh();
        }
    }
}
