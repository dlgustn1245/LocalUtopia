using System;
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
        readonly List<QixEnemy> enemies = new();
        readonly QixCaptureService captureService = new();

        // 거미줄. 키는 스프라이트가 놓인 중심 칸. 거미(TrapperEnemy)는 놓을 칸만 알려 주고 생성·감속·제거는 여기서 한다.
        readonly Dictionary<Vector2Int, GameObject> traps = new();
        readonly List<Vector2Int> trapRemoveBuffer = new();

        public Player player;
        public GameObject trapPrefab;
        public float trapSlowMultiplier = 0.25f;

        // 거미줄 하나가 덮는 칸 수(중심에서 상하좌우로). 스프라이트가 칸보다 훨씬 크므로 중심 칸만 느리게 하면
        // 눈에 보이는 거미줄 대부분이 아무 효과가 없다. 거미줄 그림 크기나 cellWorldSize 를 바꾸면 같이 맞춘다.
        public int trapCellRadius = 2;

        // 거미줄을 밟은 뒤 느려져 있는 시간. readonly 로 두면 직렬화되지 않아 인스펙터에서 못 만진다.
        public float trapSlowDelay = 1.5f;

        public float invincibleDuration = 2f;

        // 무적이 없는 사망(자기 교차) 연출 길이. 적 접촉 때는 무적 시간만큼 깜빡인다.
        public float deathBlinkDuration = 0.6f;
        public float cellWorldSize;
        public QixGridRenderer gridRenderer;

        public TextMeshProUGUI percentageText;
        public TextMeshProUGUI timerText;

        // 플레이 영역(월드 단위). 화면 전체가 아니라 상단 HUD, 하단 조작 UI 자리를 뺀 크기를 지정한다.
        // QixManager 를 선택하면 씬 뷰에 초록 사각형으로 표시되므로 보면서 조절하면 된다.
        public Vector2 fieldSize;
        public Vector2 fieldCenter;

        public Slider ratioSlider;

        public Button bonusClearPopup;
        public Button clearPopup;
        public Button failPopup;

        public GameObject deathCountIcon;
        public Transform deathCountParent;

        QixGrid grid;
        QixTrail trail;

        StageData stage;
        int remainingLives;
        Vector2Int currentVertex;
        Vector2Int targetVertex;
        bool isMoving;
        bool canMove = true;

        Coroutine timerCoroutine;
        Coroutine blinkCoroutine;
        WaitForSeconds delay;
        int remainTime;

        Coroutine trapSlowCoroutine;

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
                SoundManager.Instance.PlaySFX(SoundManager.Instance.selectSfx);
                SceneLoader.Load(SceneNames.Title);
            });
            failPopup.onClick.AddListener(() =>
            {
                SoundManager.Instance.PlaySFX(SoundManager.Instance.selectSfx);
                SceneLoader.Load(SceneNames.Title);
            });
            bonusClearPopup.onClick.AddListener(() =>
            {
                SoundManager.Instance.PlaySFX(SoundManager.Instance.selectSfx);
                SceneLoader.Load(SceneNames.Ending);
            });
        }

        void InitStage()
        {
            stage = GameManager.Instance.CurrentStage;
            remainingLives = stage.deathCount;
            gridRenderer.backgroundRenderer.sprite = stage.hiddenImage;

            for (int i = 0; i < stage.deathCount; i++)
            {
                Instantiate(deathCountIcon, deathCountParent);
            }

            SpawnEnemies();
        }

        void SpawnEnemies()
        {
            var spawns = stage.enemies;
            if (spawns == null)
            {
                return;
            }

            for (int i = 0; i < spawns.Length; i++)
            {
                var spawn = spawns[i];
                if (spawn.prefab == null)
                {
                    Debug.LogError($"{stage.name}: enemies[{i}].prefab 이 비어 있다.", stage);
                    continue;
                }

                for (int j = 0; j < spawn.count; j++)
                {
                    var enemy = Instantiate(spawn.prefab, transform);
                    enemy.Init(grid, spawn.frames, OnEnemyHit);

                    // 시작 시점엔 모든 칸이 Empty 라 실패하지 않는다. 실패해도 (0,0) 이라 안전하다.
                    grid.TryGetRandomEmptyCell(out var cell);
                    enemy.Place(cell);

                    if (enemy is TrapperEnemy trapper)
                    {
                        trapper.onPlaceTrap = PlaceTrap;
                    }

                    enemies.Add(enemy);
                }
            }
        }

        // 적이 궤적을 밟았거나 본체에 닿았다. 무적이면 무시하고, 살아남았으면 잠시 무적을 준다.
        void OnEnemyHit()
        {
            if (player.isInvincible)
            {
                return;
            }

            HandlePlayerDeath(false, invincibleDuration);
        }

        // 적 본체와의 접촉. 선을 그리는 중에만 판정한다. 테두리 위는 안전지대다.
        void CheckEnemyContact()
        {
            if (!canMove || !trail.IsDrawing || player.isInvincible)
            {
                return;
            }

            Vector2 playerPosition = player.transform.position;
            for (int i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (!enemy.spriteRenderer.enabled)
                {
                    continue; // 사라진 낙하형
                }

                float radius = enemy.hitRadius;
                if (((Vector2)enemy.transform.position - playerPosition).sqrMagnitude < radius * radius)
                {
                    OnEnemyHit();
                    return;
                }
            }
        }

        // StartCoroutine 은 첫 yield 까지 동기 실행되므로, 같은 프레임에 다른 적이 또 닿아도 여기서 막힌다.
        IEnumerator RunInvincible(float duration)
        {
            player.isInvincible = true;
            yield return new WaitForSeconds(duration);
            player.isInvincible = false;
        }

        void PlaceTrap(Vector2Int cell)
        {
            if (grid.GetState(cell) != CellState.Empty || TryGetTrapOnEdge(cell, out _))
            {
                return;
            }

            var trap = Instantiate(trapPrefab, transform);
            var world = grid.CellToWorld(cell);
            trap.transform.position = new Vector3(world.x, world.y, trap.transform.position.z);
            traps.Add(cell, trap);
        }

        // 확보된 칸 위의 거미줄을 지운다. 열거 중에 Remove 할 수 없어 키를 모아 두고 지운다.
        void RemoveClaimedTraps()
        {
            if (traps.Count == 0)
            {
                return;
            }

            trapRemoveBuffer.Clear();
            foreach (var pair in traps)
            {
                if (grid.GetState(pair.Key) == CellState.Claimed)
                {
                    Destroy(pair.Value);
                    trapRemoveBuffer.Add(pair.Key);
                }
            }

            for (int i = 0; i < trapRemoveBuffer.Count; i++)
            {
                traps.Remove(trapRemoveBuffer[i]);
            }
        }

        // 변의 양옆 칸 중 하나를 덮고 있는 거미줄의 중심 칸을 돌려준다. 밟은 거미줄을 없애야 해서 참/거짓만으로는 부족하다.
        bool TryGetTrapCell(Vector2Int from, Vector2Int to, out Vector2Int trapCenter)
        {
            if (traps.Count == 0)
            {
                trapCenter = default;
                return false;
            }

            if (from.y == to.y)
            {
                int x = Mathf.Min(from.x, to.x);
                return TryGetTrapOnEdge(new Vector2Int(x, from.y - 1), out trapCenter) || 
                       TryGetTrapOnEdge(new Vector2Int(x, from.y), out trapCenter);
            }

            int y = Mathf.Min(from.y, to.y);
            return TryGetTrapOnEdge(new Vector2Int(from.x - 1, y), out trapCenter) || 
                   TryGetTrapOnEdge(new Vector2Int(from.x, y), out trapCenter);
        }

        // 거미줄 스프라이트는 칸보다 훨씬 커서 여러 칸을 덮는다. 중심 칸에서 trapCellRadius 안이면 덮인 것으로 본다.
        // 거미줄 수가 많아야 수십 개고 플레이어가 한 변으로 출발할 때만 부르므로 전수 비교로 충분하다.
        //
        // 첫 일치에서 멈추지 않고 가장 가까운 중심을 고른다. PlaceTrap 은 중심이 반지름 안에 겹치는 것만 막으므로
        // 중심이 3~4칸 떨어진 거미줄은 덮는 범위가 겹칠 수 있는데, 그때 먼저 놓인 쪽을 돌려주면
        // 플레이어 발밑 거미줄은 남고 멀리 있는 것이 사라진다.
        bool TryGetTrapOnEdge(Vector2Int cell, out Vector2Int trapCenter)
        {
            int nearest = int.MaxValue;
            trapCenter = default;

            foreach (var center in traps.Keys)
            {
                // 덮는 범위가 정사각형이라 체비쇼프 거리로 안팎을 판정한다.
                int distance = Mathf.Max(Mathf.Abs(cell.x - center.x), Mathf.Abs(cell.y - center.y));
                if (distance <= trapCellRadius && distance < nearest)
                {
                    nearest = distance;
                    trapCenter = center;
                }
            }

            return nearest != int.MaxValue;
        }

        // 영역을 지키는 적의 칸만 모은다. 낙하형은 지나가는 중이라 세지 않는다.
        void CollectEnemyCells()
        {
            enemyCells.Clear();
            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i].keepRegion)
                {
                    enemyCells.Add(enemies[i].cell);
                }
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
            // 이동 블록보다 먼저 본다. 아래에 두면 변을 건너는 중인 프레임은 early return 에 걸려 판정을 건너뛴다.
            // 거미줄로 느려졌을 때 한 변에 여러 프레임이 걸리므로, 하필 가장 피하기 어려울 때 판정이 드물어진다.
            CheckEnemyContact();

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

            if (TryGetTrapCell(currentVertex, next, out var trapCell))
            {
                StepOnTrap(trapCell);
            }
            targetVertex = next;
            isMoving = true;
        }

        void StepOnTrap(Vector2Int center)
        {
            Destroy(traps[center]);
            traps.Remove(center);

            if (trapSlowCoroutine != null)
            {
                StopCoroutine(trapSlowCoroutine);
            }

            trapSlowCoroutine = StartCoroutine(RunTrapSlow());
        }

        IEnumerator RunTrapSlow()
        {
            player.speedMultiplier = trapSlowMultiplier;
            yield return new WaitForSeconds(trapSlowDelay);
            player.speedMultiplier = 1.0f;
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
            gridRenderer.RefreshTrail();

            if (grid.IsBoundaryVertex(vertex))
            {
                CompleteTrail();
            }
        }

        void CompleteTrail()
        {
            SoundManager.Instance.PlaySFX(SoundManager.Instance.territorySfx);
            SetTrailEdges(EdgeState.Boundary);
            trail.Cancel();
            player.SetSafeSprite();

            // 궤적을 선으로 승격한 뒤에 호출해야 flood fill 이 새 선을 벽으로 인식한다.
            CollectEnemyCells();
            int capturedCells = captureService.Capture(grid, enemyCells);
            RemoveClaimedTraps();
            RefreshRenderer();

            // 적 영역을 남기는 모드에서는 궤적 양쪽이 모두 확보되어 현재 꼭짓점의 선이 전부 지워질 수 있다.
            // 그대로 두면 어느 방향으로도 못 움직이므로 테두리로 되돌린다.
            // ponytail: 가장 가까운 선 위 꼭짓점을 찾는 대신 시작 모서리로 보낸다. 적 구현 후 체감이 나쁘면 개선.
            if (!grid.IsBoundaryVertex(currentVertex))
            {
                SetPlayerFirstVertex();
            }

            if (capturedCells > 0)
            {
                float claimedRatio = grid.ClaimedRatio;
                ratioSlider.value = claimedRatio;
                percentageText.text = $"{Mathf.FloorToInt(claimedRatio * 100f)}%";

                if (claimedRatio * 100f >= stage.clearRatio)
                {
                    print("Stage Clear");
                    canMove = false;
                    StageClear();
                }
            }
        }

        void StageClear()
        {
            SoundManager.Instance.StopBGM();
            SoundManager.Instance.PlaySFX(SoundManager.Instance.completeSfx);
            SetPlayerFirstVertex();
            grid.ClaimAllArea();
            RemoveClaimedTraps();
            RefreshRenderer();
            StopCoroutine(timerCoroutine);
            GameManager.Instance.StageClear();
            
            foreach (var enemy in enemies)
            {
                enemy.enabled = false;
            }

            ratioSlider.value = 1.0f;
            percentageText.text = "100%";
            if (stage.isBonusStage)
            {
                bonusClearPopup.gameObject.SetActive(true);
            }
            else
            {
                clearPopup.gameObject.SetActive(true);
            }
        }

        // 살아남아 부활했으면 true, 목숨을 다 써서 실패 팝업이 떴으면 false.
        // invincibleAfter 가 0보다 크면 그 시간 동안 무적이 되고 깜빡임도 같은 길이로 맞춘다.
        bool HandlePlayerDeath(bool force = false, float invincibleAfter = 0f)
        {
            SetTrailEdges(EdgeState.None);

            var respawnVertex = trail.Points.Count > 0 ? trail.Points[0] : currentVertex;
            trail.Cancel();
            player.SetSafeSprite();

            // 거미줄 감속은 죽으면 푼다. 밟은 거미줄은 이미 사라졌고 부활 지점은 거기서 멀어서,
            // 그대로 두면 화면에 원인이 없는 채로 느리게 움직인다.
            if (trapSlowCoroutine != null)
            {
                StopCoroutine(trapSlowCoroutine);
                trapSlowCoroutine = null;
            }
            player.speedMultiplier = 1f;

            if (force || CheckPlayerDead())
            {
                SoundManager.Instance.StopBGM();
                SoundManager.Instance.PlaySFX(SoundManager.Instance.failSfx);
                StopCoroutine(timerCoroutine);
                canMove = false;
                failPopup.gameObject.SetActive(true);
                SetPlayerFirstVertex();
                RefreshRenderer();
                foreach (var enemy in enemies)
                {
                    enemy.enabled = false;
                }
                return false;
            }

            MovePlayerToVertex(respawnVertex);
            RefreshRenderer();

            // 깜빡이는 도중에 또 죽으면 두 코루틴이 렌더러를 번갈아 끄고 켜서 엉킨다. 이전 것을 끊고 새로 시작한다.
            if (blinkCoroutine != null)
            {
                StopCoroutine(blinkCoroutine);
            }
            blinkCoroutine = StartCoroutine(player.PlayerHitBlink(
                invincibleAfter > 0f ? invincibleAfter : deathBlinkDuration));

            if (invincibleAfter > 0f)
            {
                StartCoroutine(RunInvincible(invincibleAfter));
            }

            return true;
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
