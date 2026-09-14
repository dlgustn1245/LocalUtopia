# LocalUtopia

Unity로 만든 세로형 모바일 게임. 선을 그려 영역을 확보하는 Qix 방식으로, 스테이지마다 숨겨진 캐릭터 그림을 드러내는 것이 목표다.

- 엔진: Unity 6000.6.0f1 (URP, 2D)
- 타겟 해상도: 1080 x 1920 (세로)
- 입력: 키보드(WASD / 방향키) + 화면 방향 버튼(터치)

## 실행

1. Unity Hub에서 6000.6.0f1로 프로젝트를 연다.
2. `Assets/01_Scenes/01_Title.unity`를 열고 재생한다. GameManager 와 SoundManager 가 타이틀 씬에만 있으므로 다른 씬을 단독 재생하면 동작하지 않는다.

## 씬 흐름

```
01_Title ─(스테이지 선택)─▶ 02_Loading ─(연출 후)─▶ 03_Qix ─┬─(클리어/실패)─▶ 01_Title
                                                          └─(보너스 클리어)─▶ 04_Ending ─▶ 05_Credit ─▶ 01_Title
```

| 씬 | 스크립트 | 역할 |
|---|---|---|
| 01_Title | `TitleScene`, `GameSetting` | 타이틀 → 안내 팝업 → 스테이지 목록의 3단계 화면. 설정 팝업에서 음소거, BGM/SFX 볼륨, 데이터 초기화. GameManager, SoundManager 가 여기 있다. |
| 02_Loading | `LoadingScene` | 선택한 스테이지의 적 애니메이션과 코멘트를 잠깐 보여준 뒤 게임 씬으로 넘어간다. 스테이지 BGM 이 여기서 시작된다. |
| 03_Qix | `QixScene` | 게임 본편. 이동, 궤적, 영역 확보, 적 스폰·접촉·함정, 타이머, 목숨, 결과 팝업. |
| 04_Ending | `EndingScene` | 버튼을 누를 때마다 다음 대사. 마지막 대사 뒤 크레딧으로. |
| 05_Credit | `CreditScene` | 크레딧 텍스트가 위로 흐르고 끝나면 바닥에서 다시 올라온다. 버튼으로 타이틀 복귀. |

씬 이름은 `SceneNames` 상수로만 참조한다. 씬 전환은 `SceneLoader.Load`가 담당하며 로딩 중 중복 요청을 막는다. 각 씬은 진입 시 `SceneFader`가 검은 화면에서 페이드인하고, 페이드 초반에는 raycast 를 막아 클릭이 새지 않게 한다.

## 씬별 동작

### 타이틀 (`TitleScene`)

상태는 `isTitleVisible` 하나로 관리하고, `SetTitleVisible`이 관련 오브젝트를 한꺼번에 켜고 끈다.

1. **타이틀 상태**: 로고, 시작 문구, 설정 버튼이 보인다. 화면 아무 곳이나 누르면 다음 상태로 간다. 단, 포인터가 UI 위에 있으면(`EventSystem.IsPointerOverGameObject`) 해당 버튼의 onClick 이 처리하므로 타이틀 진행으로 먹지 않는다. 이 때문에 배경·장식 Image 는 모두 `raycastTarget` 을 꺼야 한다. 켜져 있으면 어디를 눌러도 UI 위가 되어 진행이 막힌다.
2. **안내 팝업 상태**: 로고와 시작 문구를 숨기고 안내 팝업과 뒤로가기 버튼을 보인다. 팝업을 누르면 스테이지 목록.
3. **스테이지 목록 상태**: 버튼 목록이 로고 자리까지 세로로 놓인다. 클리어한 스테이지에는 마크, 보너스 버튼은 일반 스테이지를 모두 깼을 때만 활성. 버튼은 `GameManager.currStage` 를 정하고 로딩 씬으로 넘긴다.

스테이지 버튼, 클리어 마크, `GameManager.stages` 는 같은 인덱스로 정렬돼 있어야 한다. 길이가 다르면 예외 대신 에러 로그를 남기고 겹치는 범위만 묶는다.

### 로딩 (`LoadingScene`)

스테이지의 애니메이션 프레임을 일정 간격으로 순환하며 코멘트를 보여준 뒤 게임 씬을 로드한다. 스테이지 BGM 을 여기서 틀어 두면 같은 클립이라 게임 씬에서 끊기지 않고 이어진다.

### 크레딧 (`CreditScene`)

`RectMask2D` 뷰포트 안에서 텍스트를 위로 스크롤한다. 시작 시 텍스트의 preferredHeight 로 콘텐츠 높이를 맞추고 뷰포트 바닥 아래에 놓는다. 매 프레임 올리다가 텍스트 아래끝이 뷰포트 위를 완전히 지나면 다시 바닥으로 되돌려 무한 반복한다.

### 장식 (`Drifter`)

타이틀·엔딩·크레딧의 구름, 고양이, 엔딩의 적 행렬은 모두 `Drifter` 가 붙은 UI Image 다. 부모 가운데 앵커 기준으로 동작한다.

- **가로 이동**: 속도 벡터로 매 프레임 이동. 두 가지 끝 처리 중 하나를 고른다.
  - 되돌아오기(`bounceHorizontal`): 화면 끝에 닿으면 속도를 반전하고 좌우를 뒤집는다. 고양이용.
  - 되감기: 화면 밖으로 완전히 나가면 "부모 폭 + 공통 여유값(`wrapPadding`)" 만큼 반대편으로 옮긴다. 자기 폭만큼 옮기면 폭이 다른 여러 개가 같은 속도로 갈 때 한 바퀴마다 간격이 틀어져 겹친다. 여유값을 공통으로 두어 행렬 간격이 유지된다.
- **위아래 흔들림**: 기준 y 에 사인 파를 더한다. 진폭 0 이면 없음.
- **프레임 순환**: 프레임이 둘 이상이면 일정 간격으로 스프라이트를 바꾼다.

장식 배치 원칙:
- 캐릭터·구름은 원본 픽셀의 정수 배로 두어 픽셀아트가 뭉개지지 않게 한다. 배경(`3.bg`)과 아파트(`5.apart`)는 같은 폭으로 그려진 그림이라 둘 다 화면 폭에 맞춰 깐다.
- 엔딩 행렬은 모두 같은 속도로 두어 간격을 유지한다. 낙하하는 것은 없다.
- 러너 스프라이트는 한쪽 변이 직선인 모서리용 그림이라 화면 모서리에 붙인다.
- 로딩·플레이 씬은 꾸미지 않는다. 로딩은 이미 적 애니메이션이 움직이고, 플레이는 선을 읽는 화면이라 배경 움직임이 방해가 된다.

## 폴더 구조

```
Assets/
├─ 01_Scenes/                  씬 5개
├─ 02_Scripts/
│  ├─ Player/
│  │  ├─ Player.cs             입력 읽기와 좌표 이동만 담당. 어디로 갈 수 있는지는 판단하지 않는다. 피격 깜빡임, 무적 플래그, 감속 배율
│  │  └─ TouchDirectionButton.cs  화면 방향 버튼 하나. 누르는 동안 Player에 방향을 넘긴다.
│  ├─ Enemy/
│  │  ├─ QixEnemy.cs           적 부모. 칸 위치, 벽·궤적 판정이 붙은 이동(MoveBy), 프레임 애니
│  │  ├─ GlideEnemy.cs         랜덤 주기마다 빈 칸으로 직선 미끄러짐. 고양이·구름·발자국·경비
│  │  ├─ FallingEnemy.cs       위에서 아래로 낙하, 사라졌다 재등장. 돈
│  │  ├─ TrapperEnemy.cs       튕기며 직진, 주기마다 거미줄 요청. 거미
│  │  └─ EnemyData.cs          스테이지가 적을 어떻게 스폰할지: prefab, count, frames
│  ├─ Qix/
│  │  ├─ CellState.cs          칸 상태: Empty / Claimed
│  │  ├─ EdgeState.cs          변 상태: None / Boundary / Trail
│  │  ├─ QixGrid.cs            게임판 데이터. 칸 배열 + 변 배열, 좌표 변환
│  │  ├─ QixTrail.cs           그리는 중인 궤적의 꼭짓점 목록
│  │  ├─ QixCaptureService.cs  flood fill로 영역을 나누고 확보 처리
│  │  ├─ QixGridRenderer.cs    그리드를 Texture2D 한 장으로 그린다
│  │  ├─ QixScene.cs           게임 씬 컨트롤러
│  │  └─ StageData.cs          스테이지 설정 ScriptableObject
│  └─ Util/
│     ├─ Singleton.cs          MonoBehaviour 제네릭 싱글톤. 프로젝트 창 Create > Scripting > Singleton 으로 상속 스크립트 생성
│     ├─ SceneLoader.cs        SceneNames 상수 + 씬 로더
│     ├─ SceneFader.cs         씬 진입 페이드인
│     ├─ Drifter.cs            장식 UI 이동. 가로 흐름/되돌아오기, 위아래 흔들림, 프레임 순환
│     ├─ Manager/GameManager.cs   스테이지 목록, 현재 스테이지, 클리어 저장
│     ├─ Manager/SoundManager.cs  BGM/SFX 채널 각 1개, 채널 볼륨·음소거 저장
│     └─ SceneScripts/         Title / Loading / Ending / Credit 씬 스크립트, GameSetting(타이틀 설정 팝업 UI)
└─ 03_Resources/
   ├─ Font/                    NeoDunggeunmo, NotoSansKR SDF
   ├─ Component/               UI 이미지. 번호 순으로 관리한다.
   │  └─ Sound/                BGM(파일명에 BGM 포함), SFX
   ├─ Prefab/                  목숨 아이콘 등 런타임에 Instantiate 하는 프리팹
   │  └─ Enemy/                적 프리팹(그림별 1개) + SpiderWeb(거미줄)
   └─ SO/                      StageData 에셋 (Stage_*.asset)
```

## 핵심 로직 (Qix)

### 칸과 변을 분리한다

게임판은 두 격자를 겹쳐 들고 있다.

- 칸(cell): `Columns x Rows`. 확보 여부(`CellState`)만 갖는다. 채우기 판정과 렌더링에 쓴다.
- 변(edge): `(Columns+1) x (Rows+1)` 꼭짓점 사이의 선. 이동 가능 여부(`EdgeState`)를 결정한다.

플레이어는 칸이 아니라 꼭짓점 위에 서서 변을 따라 움직인다. 이동 가능성을 칸에서 유도하면 테두리 안쪽이 채워졌을 때 테두리를 못 다니게 되므로, 선은 칸과 별개로 기억한다.

### 이동

`QixScene.Update`가 꼭짓점 단위로 이동을 끊어 처리한다. 프레임마다 위치를 역산하면 빠른 이동에서 변을 건너뛰어 궤적에 구멍이 나기 때문이다.

| 상태 | 다음 변 | 결과 |
|---|---|---|
| 안전 | `Boundary` | 선을 따라 이동 |
| 안전 | 그릴 수 있는 변 | 궤적 시작 후 이동 |
| 그리는 중 | 그릴 수 있는 변 | 이동 |
| 그 외 | | 이동 불가 |

그릴 수 있는 변은 `None` 상태이고 양옆 칸이 모두 `Empty`인 변이다. 되돌아가기는 여기서 막힌다.

### 궤적 완성과 영역 확보

1. 도착한 꼭짓점이 이미 지나온 곳이면 자기 교차로 사망.
2. 도착한 꼭짓점에 `Boundary` 변이 닿아 있으면 궤적 완성. 궤적 변을 모두 `Boundary`로 승격한다.
3. `QixCaptureService.Capture`가 `Empty` 칸을 BFS로 영역 번호를 매긴다. 두 칸 사이에 `Boundary` 변이 있으면 벽이다.
4. 영역이 둘 이상이면 `keepRegion` 인 적이 있는 영역을 남기고 나머지를 `Claimed`로 바꾼다. 그런 적이 없으면 가장 넓은 영역을 남긴다.
5. 양옆이 모두 `Claimed`인 변은 `None`으로 정리한다. 확보 영역 내부를 걸어 다닐 수 없게 하기 위함이다.
6. 확보 비율이 `StageData.clearRatio` 이상이면 클리어.

### 영역 나누기 (`QixCaptureService`)

3~5단계의 실제 절차. 호출 전에 궤적이 `Boundary`로 승격돼 있어야 한다.

1. **영역 번호 매기기**: 모든 칸을 순서대로 훑으며, 아직 번호가 없는 `Empty` 칸을 만나면 새 영역 번호를 부여하고 거기서 BFS 를 시작한다. BFS 는 상하좌우 이웃 중 범위 안이고, 번호가 없고, `Empty` 이고, **두 칸 사이에 `Boundary` 변이 없는** 칸만 같은 영역으로 넣는다. 벽 판정을 칸이 아니라 변으로 하는 것이 핵심이다. 궤적은 칸을 차지하지 않고 칸 사이를 지나므로, 칸만 봐서는 새로 그린 선이 영역을 갈랐다는 사실을 알 수 없다.
2. **영역이 하나면 종료**: 궤적이 경계에 다시 닿기만 하고 영역을 가르지 못한 경우다. 확보할 것이 없으므로 0을 돌려준다.
3. **남길 영역 고르기**: `QixScene.CollectEnemyCells` 가 넘긴 칸(`keepRegion` 인 적의 `cell`)의 영역 번호를 모두 모은다. 적이 여러 영역에 흩어져 있으면 그 영역들이 전부 남는다. 하나도 없으면(낙하형만 있는 스테이지, 테스트) 가장 넓은 영역 하나만 남긴다.
4. **확보**: 남길 영역에 속하지 않은 칸을 모두 `Claimed`로 바꾸고 개수를 센다.
5. **선 정리**: 양옆이 모두 `Claimed`가 된 변을 `None`으로 되돌린다. 궤적은 항상 남긴 영역과 맞닿아 있으므로 여기서 지워지지 않는다.

메모리: 영역별 칸 목록을 만들지 않는다. 칸마다 영역 번호 하나(`int[,]`)와 영역별 크기 배열만 두고, 큐·집합·배열은 인스턴스 필드로 재사용한다. 그리드 크기가 같으면 `Array.Clear`만 하고 다시 쓴다. static 으로 두면 버퍼가 씬을 벗어나도 살아남으므로 인스턴스로 유지한다.

### 적

적은 플레이어와 달리 꼭짓점이 아니라 칸 위를 자유롭게 움직인다. 종류마다 행동만 다르고 나머지는 부모 `QixEnemy` 가 맡는다.

**이동과 판정 (`QixEnemy.MoveBy`)**: 이동량을 x/y 축으로 나눠 한 축씩 적용한다. 칸이 바뀌는 순간 두 칸 사이의 변(`QixGrid.GetEdgeBetweenCells`)을 본다.

| 변 | 결과 |
|---|---|
| `Boundary` | 그 축만 되돌리고 막힘(true) 반환. 바깥 테두리도 `Boundary` 라 격자 밖으로 못 나간다 |
| `Trail` | `onTrailHit` 콜백 → `QixScene.OnEnemyTouchedTrail` |
| `None` | 통과 |

축을 나누는 이유는 둘이다. 한 축이 막혀도 다른 축은 살아 벽을 따라 미끄러지고, 칸이 한 번에 한 축만 바뀌어 "두 칸은 인접하다" 는 전제가 지켜진다.

**한 조각은 반드시 한 칸(`CellSize`) 이하여야 한다.** 두 칸을 건너뛰면 사이의 변을 놓쳐 벽을 통과하고, `GetEdgeBetweenCells` 가 인접하지 않은 두 칸을 받아 배열 밖을 읽는다. 그래서 이동량을 **잘라내지 않고 한 칸 이하로 쪼개 여러 번 밟는다.** 잘라내면 프레임이 떨어질 때 남는 이동량이 통째로 버려져 적 속도가 프레임레이트를 타기 때문이다(30fps 에서 25% 느려짐). 조각 수 상한은 64이고, 여기 걸릴 만큼 큰 값은 조각 자체를 한 칸으로 잘라 불변식을 지킨다 — 상한에 닿으려면 한 프레임에 64칸(3.2 유닛)을 가야 하므로 정상 값에서는 걸리지 않는다.

`WorldToCell` 은 격자 밖 좌표도 그대로 돌려준다. 클램프하면 마지막 칸에서 테두리를 넘는 순간을 못 잡는다.

**종류**

| 클래스 | 행동 | `keepRegion` | 스테이지 |
|---|---|---|---|
| `GlideEnemy` | `minRest~maxRest` 쉬고 빈 칸 하나(`QixGrid.TryGetRandomEmptyCell`)를 골라 직선으로 간다. 도착하거나 막히면 다시 쉰다. | 켬 | Dani(발자국), JaeJae(고양이), Namu(구름), Bonus(경비 3인) |
| `FallingEnemy` | **언제나 맵 맨 윗줄의 임의 열**에서 떨어진다. 확보 여부를 보지 않는다. 벽에 막히거나 바닥으로 빠지면 렌더러를 끄고 `respawnDelay` 뒤 다시 맨 위에서 떨어진다. `SetActive(false)` 는 Update 가 멈춰 타이머를 못 세니 쓰지 않는다. | 끔 | Sonqo(돈) |
| `TrapperEnemy` | 랜덤 각도로 직진하다 막힌 축만 뒤집어 튕긴다. `trapInterval` 마다 `onPlaceTrap(cell)` 을 부른다. | 켬 | Leeguheok(거미) |

`keepRegion` 은 영역 확보 때 이 적이 선 영역을 남길지다. 낙하형은 필드를 계속 가로지르므로 확보 순간 어디 있는지가 운이라 끈다. 그러면 Sonqo 는 적 칸이 비어 "가장 넓은 영역만 남긴다" 분기로 떨어지는데 그게 맞는 동작이다.

**낙하형의 두 단계**: 확보된 칸에 있는 동안은 아직 "들어오는 중" 이라 `Descend` 가 변을 보지 않고 좌표만 내리며 칸을 직접 갱신한다. 미확보 칸에 닿으면 그때부터 `MoveBy` 로 넘어가 벽에 막히고 궤적도 밟는다. 이 단계가 필요한 이유는 둘이다. 확보·미확보 경계가 `Boundary` 라 보통 이동으로는 통과할 수 없고, 확보된 칸에서 바로 사라지게 하면 위쪽을 덮은 뒤로 등장 자체가 막힌다. 등장 위치를 "남은 영역의 천장" 으로 낮추는 방법도 있지만, 그러면 확보 모양에 따라 화면 한가운데서 튀어나와 떨어지는 것처럼 보이지 않는다.

**스폰**: `QixScene.SpawnEnemies` 가 `StageData.enemies`(`EnemyData[]`) 의 `prefab` 을 `count` 만큼 `Instantiate` 하고 (`EnemyData.prefab` 은 `GameObject` 가 아니라 `QixEnemy` 타입이다. 인스펙터가 적 스크립트 없는 프리팹을 아예 받지 않아 스폰 도중 터지는 대신 꽂는 자리에서 막힌다) `Init(grid, frames, 콜백)` → `Place(빈 칸)` 한다. 프리팹은 그림별로 하나씩(`Prefab/Enemy/`)이고 행동 수치(속도·주기)는 프리팹, 마릿수와 프레임은 `EnemyData` 에 둔다. 보너스처럼 인스턴스마다 그림이 다르면 `EnemyData` 를 여러 줄 쓴다. `Init` 은 생성자 대신이다. `Instantiate` 바로 다음 줄에서 부르면 `Awake` 는 이미 끝났고 첫 `Update` 는 아직이라 순서가 보장된다. 그래서 자식의 `Awake` 는 `grid` 를 쓰지 않는다.

**함정(거미줄)**: 거미는 "언제·어디" 만 알리고 생성·감속·제거는 `QixScene` 이 한 곳에서 한다(거미가 둘 이상이어도 사전이 하나). `traps` 사전의 키는 거미줄 스프라이트가 놓인 **중심 칸**이고, 감속 범위는 거기서 `trapCellRadius` 칸이다. 거미줄 그림은 0.27 유닛으로 칸(0.05)보다 다섯 배쯤 커서, 중심 칸만 느리게 하면 눈에 보이는 거미줄 대부분이 아무 효과가 없다. **거미줄 그림 크기나 `cellWorldSize` 를 바꾸면 `trapCellRadius` 도 같이 맞춘다.**

플레이어가 이동을 시작할 때 다음 변의 양옆 칸 중 하나가 덮여 있으면 `Player.speedMultiplier` 를 `trapSlowMultiplier` 로, 아니면 1 로 놓는다. 타이머 없이 그 변을 지나는 동안만 느리다. 중심 칸이 확보되면 `Capture` 직후와 클리어 때 `Destroy` 한다.

메모리: 적·거미줄은 QixManager 의 자식으로 `Instantiate` 하므로 씬이 내려가면 함께 사라진다. 거미줄은 확보 시 명시적으로 `Destroy`. 적 코드에는 코루틴이 없고 타이머는 모두 `Tick` 안의 float 다.

### 사망과 타이머

- 자기 교차 시 궤적 변을 `None`으로 되돌리고 궤적 시작점으로 복귀한다. 목숨이 0이 되면 실패 팝업.
- 적 접촉은 두 가지다. 적이 궤적 변을 밟는 것(`MoveBy` 안에서 판정)과, **선을 그리는 중에** 적 본체가 플레이어와 `hitRadius`(프리팹) 안으로 겹치는 것(`QixScene.CheckEnemyContact`, 프레임마다 거리 비교). 테두리 위에 서 있을 때는 적이 스쳐도 안전하다. 어느 쪽이든 같은 처리 뒤 `invincibleDuration` 동안 무적이다(`Player.isInvincible`). 무적 중 적 접촉은 무시되지만 자기 교차·시간 초과는 그대로 죽는다. `hitRadius` 는 스프라이트보다 작게 둔다. 그림 크기대로 잡으면 칸(0.05)에 비해 너무 커서 억울한 죽음이 잦다.
- 부활할 때 `Player.PlayerHitBlink(duration)` 로 피격 스프라이트를 깜빡인다. **깜빡임이 무적의 유일한 표시라 길이를 `HandlePlayerDeath` 가 정해 넘긴다** — 적 접촉이면 `invincibleDuration`, 무적이 없는 자기 교차면 짧은 `deathBlinkDuration`. Player 가 횟수를 직접 들고 있으면 무적 시간을 늘렸을 때 멀쩡히 보이면서 무적인 구간이 생긴다. 깜빡이는 도중에 그리기를 시작할 수 있으므로 끝나면 `baseSprite`(safe/draw 중 마지막 것)로 돌아간다. 깜빡이는 중에 또 죽으면 이전 코루틴을 끊고 새로 시작한다.
- 타이머는 코루틴으로 1초마다 줄어들고 0이 되면 즉시 실패.
- 클리어·실패 모두 BGM 을 멈추고 징글을 재생한 뒤 팝업을 켠다. 적은 `enabled = false` 로 멈춘다.

### 렌더링

`QixGridRenderer`가 칸당 `pixelsPerCell` 픽셀 크기의 `Texture2D` 한 장에 `Color32[]` 버퍼를 채워 `SetPixels32`로 올린다. 선은 변 위치에 `lineThickness` 픽셀로 중심을 맞춰 그린다. dirty는 두 단계다. 칸·경계가 바뀌면 `Refresh()`로 전체를 다시 칠하고, 궤적 꼭짓점만 늘었으면 `RefreshTrail()`로 궤적 변만 덧칠한다. 어느 쪽이든 업로드는 `LateUpdate`에서 프레임당 한 번이다. 텍스처와 스프라이트는 `OnDestroy`에서 `Destroy`한다. 매 프레임 텍스처 전체를 업로드하므로 `pixelsPerCell` 을 키우면 그만큼 대역폭이 늘어난다. 지금 값 이상으로 올리지 않는다.

## 공용 시스템

### Singleton

`Singleton<T>` 는 `Awake`에서 인스턴스를 등록하고 `DontDestroyOnLoad` 한다. 이미 인스턴스가 있으면 새로 들어온 오브젝트를 파괴한다. 단 `Destroy` 는 프레임 끝에 실행되므로, `Awake` 를 재정의하는 자식은 `base.Awake()` 뒤에 `Instance != this` 면 초기화를 건너뛰어야 한다(`SoundManager` 참고). 프로젝트 창 Create > Scripting > Singleton 메뉴로 상속 스크립트를 바로 만들 수 있다.

### GameManager

스테이지 목록의 단일 출처. 현재 스테이지 인덱스, 클리어 저장·조회, 데이터 초기화를 맡는다. 보너스 해금 여부(`AllCleared`)는 저장하지 않고 매번 계산한다.

### SoundManager

AudioSource 두 개를 갖는다. BGM 채널은 루프, SFX 채널은 `PlayOneShot`.

BGM 흐름:

| 시점 | 곡 |
|---|---|
| Title 시작 | 메뉴 |
| Loading 시작 | 스테이지 BGM. Qix 씬에서는 다시 부르지 않아 끊기지 않는다. |
| 클리어 / 실패 | 정지. 팝업 버튼을 누르기 전까지 정적. |
| Ending 시작 | 엔딩. Credit 까지 이어진다. |

`PlayBGM`은 같은 클립이 이미 재생 중이면 다시 시작하지 않는다. 씬 전환 직전에 `StopBGM`을 부르면 페이드 사이에 빈 구간만 생기므로 결과 팝업에서만 부른다.

SFX:

| 클립 | 재생 시점 |
|---|---|
| `selectSfx` | 타이틀 진행, 모든 UI 버튼 |
| `territorySfx` | 궤적 완성 |
| `completeSfx` | 스테이지 클리어 |
| `failSfx` | 목숨 소진 / 시간 초과 |

볼륨과 음소거: 채널 볼륨은 각 AudioSource 의 `volume`, 음소거는 `AudioListener.volume` 을 0/1 로 두어 분리한다. 음소거를 풀어도 채널 값이 남는다. 저장은 두 단계다. 볼륨 setter 는 PlayerPrefs 값만 쓰고 `Save()` 는 부르지 않는다. 슬라이더 드래그 중 매 프레임 불리기 때문이다. 디스크 동기화는 설정 팝업이 닫힐 때(`GameSetting.OnDisable`) 한 번 한다. 음소거는 한 번 눌릴 때 한 번이라 즉시 저장한다.

### 설정 팝업 (`GameSetting`)

음소거 토글, BGM/SFX 슬라이더, 데이터 초기화, 닫기 버튼.

- 팝업이 열릴 때 `SetIsOnWithoutNotify` / `SetValueWithoutNotify` 로 저장값을 UI 에 넣는다. 이벤트를 울리지 않아 "표시" 가 "사용자 입력" 으로 오해되지 않는다.
- 값 변경은 즉시 적용된다. 별도 저장·설정 초기화 버튼은 두지 않는다.
- SFX 슬라이더는 조절 중 들리는 게 없어서 손을 뗄 때(`PointerUp`) `selectSfx` 를 한 번 재생한다.
- 데이터 초기화는 확인 팝업(`Panel - ConfirmPanel`)을 거친다. 스테이지 클리어 기록만 지우고 타이틀을 다시 로드해 마크와 버튼을 갱신한다. 사운드 설정은 남긴다.

**확인 팝업**: 설정 팝업과 형제로 `Canvas - Main` 아래 마지막에 놓인다. 초기화 버튼을 누르면 **설정 팝업을 끄고** 확인 팝업을 켠다. 취소는 그 반대, 초기화는 지우고 씬을 다시 로드하므로 팝업을 되돌릴 필요가 없다.

설정 팝업을 켜 둔 채 위에 겹치지 않는 이유는 둘이다. 판 스프라이트(`56.credit_popup`)가 반투명이라 뒤의 슬라이더·라벨이 글자에 겹쳐 비치고, 폭이 같아(800) 확인 팝업이 설정 판의 일부처럼 보인다. 덮개도 둘 다 검정 78% 라 겹치면 95% 가 되어 뒤가 뭉개진다. 끄고 띄우면 어느 팝업이 떠도 뒤 타이틀이 같은 밝기다.

`GameSetting` 이 `Panel - SettingPanel` 본체에 붙어 있어 `settingPanel.SetActive(false)` 는 자기 자신을 끄는 것이고, 그때 `OnDisable` 의 `PlayerPrefs.Save()` 가 한 번 돈다. 취소 버튼은 확인 팝업 쪽에 있어 설정 팝업이 꺼져도 살아 있다. 다시 켤 때 `Start()` 는 재실행되지 않으므로 리스너가 중복 등록되지 않는다.

### 오디오 임포트 규칙

파일명에 `BGM`이 들어가면 배경음, 아니면 효과음으로 본다.

| 구분 | Load Type | 이유 |
|---|---|---|
| BGM (긴 곡) | Streaming, Load In Background | 통째로 메모리에 올릴 크기가 아니다. 씬 진입 시 읽기 히치를 피한다. |
| SFX (짧은 클립) | Decompress On Load, Preload | 압축을 풀어 두는 것이 가장 가볍고 재생 지연이 없다. Streaming 은 클립마다 버퍼를 잡아 오히려 손해. |

둘 다 Vorbis 압축에 Force To Mono. 빌드 크기 때문에 품질은 최대로 두지 않는다. 새 클립을 추가하면 같은 규칙으로 맞춘다.

## 스테이지 데이터

`StageData` ScriptableObject를 `GameManager.stages`에 인덱스 순서로 꽂는다. 타이틀의 `stageButtons`, `clearMarks`도 같은 인덱스로 정렬돼 있어야 한다.

| 필드 | 뜻 |
|---|---|
| `hiddenImage` | 확보 영역 아래 드러나는 그림 |
| `enemyAnims` | 로딩 씬에서 돌리는 애니메이션 프레임(Texture). 인게임 적 그림과 별개다. 보너스 로딩은 경비 4인 시트 한 장이지만 인게임은 개별 경비다 |
| `enemies` | `EnemyData[]`. 줄마다 `prefab`(Prefab/Enemy/), `count`, `frames`(인게임 애니 Sprite 배열, `frameInterval` 마다 순환). 보너스는 경비별로 세 줄 |
| `comment` | 로딩 씬 코멘트 |
| `timer` | 제한 시간(초) |
| `clearRatio` | 클리어에 필요한 확보 비율(%) |
| `deathCount` | 목숨 수 |
| `isBonusStage` | 보너스 스테이지 여부. 클리어 시 엔딩으로 간다. 해금 조건 계산에서는 제외된다. |
| `bgm` | 스테이지 BGM. 메뉴·엔딩 BGM 은 스테이지에 속하지 않으므로 `SoundManager` 필드에 둔다. |

발자국(`70~72.footprint_*`) 은 로딩과 같은 "양발 → 왼발 큼 → 양발 → 오른발 큼" 순환을 쓰기 위해 Sprite Single 모드로 두었다. Multiple 이면 PNG 전체가 Sprite 로 존재하지 않는다.

## 저장 데이터

PlayerPrefs 를 쓴다.

| 키 | 값 | 쓰는 곳 |
|---|---|---|
| `Stage{인덱스}` | 클리어 시 1 | `GameManager` |
| `bgmVolume`, `sfxVolume` | 0~1 | `SoundManager` |
| `mute` | 0 / 1 | `SoundManager` |

별도 해금 플래그는 없고, 보너스 해금은 `GameManager.AllCleared`가 매번 계산한다.

## 아직 없는 것

- 점수는 넣지 않기로 했다.

## 코드 규칙

`CLAUDE.md`를 따른다. 요약: `public` 필드 사용, 필드명에 언더바 접두사 금지, 네임스페이스는 폴더 미러링, 주석은 한국어로 WHY만. 런타임 생성 `UnityEngine.Object`는 `OnDestroy`에서 `Destroy`한다.
