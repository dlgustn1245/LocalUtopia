# LocalUtopia

Unity로 만든 세로형 모바일 게임. 선을 그려 영역을 확보하는 Qix 방식으로, 스테이지마다 숨겨진 캐릭터 그림을 드러내는 것이 목표다.

- 엔진: Unity 6000.0.74f1 (URP, 2D)
- 타겟 해상도: 1080 x 1920 (세로)
- 입력: 키보드(WASD / 방향키) + 화면 방향 버튼(터치)

## 실행

1. Unity Hub에서 6000.0.74f1로 프로젝트를 연다.
2. `Assets/01_Scenes/01_Title.unity`를 열고 재생한다.
3. `03_Qix` 씬을 단독 재생할 때는 `QixScene` 컴포넌트의 `debugStage`에 StageData를 하나 꽂아 둔다. 타이틀을 거치면 무시된다.

## 씬 흐름

```
01_Title ─(스테이지 선택)─▶ 02_Loading ─(3초)─▶ 03_Qix ─┬─(클리어/실패)─▶ 01_Title
                                                       └─(보너스 클리어)─▶ 04_Ending ─▶ 05_Credit ─▶ 01_Title
```

| 씬 | 스크립트 | 하는 일 |
|---|---|---|
| 01_Title | `TitleScene` | 스테이지 버튼 목록. 클리어한 스테이지에 마크 표시. 일반 스테이지를 모두 깨면 보너스 버튼이 나타난다. |
| 02_Loading | `LoadingScene` | 선택한 스테이지의 적 애니메이션과 코멘트를 0.5초 간격으로 6프레임 보여준 뒤 게임 씬으로 넘어간다. |
| 03_Qix | `QixScene` | 게임 본편. 이동, 궤적, 영역 확보, 타이머, 목숨, 클리어/실패 팝업. |
| 04_Ending | `EndingScene` | 버튼을 누를 때마다 다음 대사를 보여주고, 마지막 대사 뒤 크레딧으로 간다. |
| 05_Credit | `CreditScene` | `RectMask2D` 뷰포트 안에서 크레딧 텍스트가 위로 흐르고, 끝나면 바닥에서 다시 올라온다. |

씬 이름은 `SceneNames` 상수로만 참조한다. 씬 전환은 `SceneLoader.Load`가 담당하며 로딩 중 중복 요청을 막는다.

## 폴더 구조

```
Assets/
├─ 01_Scenes/                  씬 5개
├─ 02_Scripts/
│  ├─ Player/
│  │  ├─ Player.cs             입력 읽기와 좌표 이동만 담당. 어디로 갈 수 있는지는 판단하지 않는다.
│  │  └─ TouchDirectionButton.cs  화면 방향 버튼 하나. 누르는 동안 Player에 방향을 넘긴다.
│  ├─ Enemy/
│  │  └─ CatEnemy.cs           미구현 (빈 클래스)
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
│     ├─ Singleton.cs          MonoBehaviour 제네릭 싱글톤
│     ├─ SceneLoader.cs        SceneNames 상수 + 씬 로더
│     ├─ SceneFader.cs         씬 진입 페이드인
│     ├─ Manager/GameManager.cs   스테이지 목록, 현재 스테이지, 클리어 저장
│     └─ SceneScripts/         Title / Loading / Ending / Credit 씬 스크립트
└─ 03_Resources/
   ├─ Font/                    NeoDunggeunmo, NotoSansKR SDF
   └─ Component/               UI 이미지. 번호 순으로 관리한다.
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
4. 영역이 둘 이상이면 적이 있는 영역을 남기고 나머지를 `Claimed`로 바꾼다. 적이 없으면 가장 넓은 영역을 남긴다.
5. 양옆이 모두 `Claimed`인 변은 `None`으로 정리한다. 확보 영역 내부를 걸어 다닐 수 없게 하기 위함이다.
6. 확보 비율이 `StageData.clearRatio` 이상이면 클리어.

작업 버퍼는 인스턴스 필드로 재사용하므로 확보마다 새 할당이 없다.

### 사망과 타이머

- 자기 교차 시 궤적 변을 `None`으로 되돌리고 궤적 시작점으로 복귀한다. 목숨이 0이 되면 실패 팝업.
- 타이머는 코루틴으로 1초마다 줄어들고 0이 되면 즉시 실패.

### 렌더링

`QixGridRenderer`가 칸당 `pixelsPerCell` 픽셀 크기의 `Texture2D` 한 장에 `Color32[]` 버퍼를 채워 `SetPixels32`로 올린다. 선은 변 위치에 `lineThickness` 픽셀로 중심을 맞춰 그린다. `Refresh()`는 dirty 플래그만 세우고 `LateUpdate`에서 프레임당 한 번만 업로드한다. 텍스처와 스프라이트는 `OnDestroy`에서 `Destroy`한다.

## 스테이지 데이터

`StageData` ScriptableObject를 `GameManager.stages`에 인덱스 순서로 꽂는다. 타이틀의 `stageButtons`, `clearMarks`도 같은 인덱스로 정렬돼 있어야 한다.

| 필드 | 뜻 |
|---|---|
| `hiddenImage` | 확보 영역 아래 드러나는 그림 |
| `enemyAnims` | 로딩 씬에서 돌리는 애니메이션 프레임 |
| `comment` | 로딩 씬 코멘트 |
| `timer` | 제한 시간(초) |
| `clearRatio` | 클리어에 필요한 확보 비율(%) |
| `deathCount` | 목숨 수 |
| `isBonusStage` | 보너스 스테이지 여부. 클리어 시 엔딩으로 간다. 해금 조건 계산에서는 제외된다. |

`enemy`, `enemyCount`는 아직 사용하지 않는다.

## 저장 데이터

PlayerPrefs `Stage{인덱스}` 키에 클리어 여부를 1로 저장한다. 별도 해금 플래그는 없고, 보너스 해금은 `GameManager.AllCleared`가 매번 계산한다.

## 아직 없는 것

- 적. `CatEnemy`는 빈 클래스이고 `QixScene.SetEnemyCells`를 호출하는 곳이 없다. 현재 사망 조건은 자기 교차와 타이머만이다.
- 사운드, 점수, 설정 화면.

## 코드 규칙

`CLAUDE.md`를 따른다. 요약: `public` 필드 사용, 필드명에 언더바 접두사 금지, 네임스페이스는 폴더 미러링, 주석은 한국어로 WHY만. 런타임 생성 `UnityEngine.Object`는 `OnDestroy`에서 `Destroy`한다.
