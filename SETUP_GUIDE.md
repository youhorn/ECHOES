# ECHOES — Unity 수동 세팅 가이드

> 21개 스크립트(`Assets/Scripts/`)를 실제로 동작시키기 위한 에디터 조립 가이드.
> Unity 2022.3 LTS / 2D / 레거시 Input 기준. 위에서부터 순서대로 따라가면 됩니다.

---

## 0. 진행 전략 (중요)

`echoes_assets_v2`의 스프라이트 시트는 **라벨 텍스트가 박힌 레퍼런스 이미지**라 그대로 슬라이스하면
글자까지 잘립니다. 그래서 두 단계로 진행합니다.

1. **Phase A — 플레이스홀더로 전 시스템 검증** (네모 스프라이트로 먼저 굴린다)
2. **Phase B — 실제 아트 교체** (깨끗한 아틀라스를 만들어 스프라이트만 갈아끼운다)

먼저 A로 게임 로직을 전부 확인하고, 그다음 B로 비주얼을 입히세요.

---

## 1. 프로젝트 기본 설정

### 1-1. 레이어 / 태그
**Edit ▸ Project Settings ▸ Tags and Layers**
- Layers(User Layer)에 추가: `Ground`, `Player`, `Enemy`
- Tag: `Player` 는 기본 존재 (없으면 추가)

### 1-2. 해상도 / 픽셀 기준 (보고서 2장)
- 기본 해상도: **480 × 270**
- PPU: **16**, 타일: 16×16, 캐릭터 32×32
- Game 뷰 좌상단 해상도 드롭다운 ▸ `+` ▸ Fixed Resolution `480 x 270` 추가

### 1-3. Pixel Perfect Camera (선택, 권장)
- Main Camera ▸ Add Component ▸ **Pixel Perfect Camera**
  - Assets Pixels Per Unit: 16
  - Reference Resolution: 480 × 270
  - Camera ▸ Projection: **Orthographic**, Size 약 **8.4375**

### 1-4. 물리 (선택)
- Project Settings ▸ Physics 2D ▸ Gravity Y: -30 정도 (픽셀 점프 느낌이 빠릿)

---

## 2. 플레이스홀더 스프라이트 만들기 (Phase A)

별도 이미지 없이 Unity 기본 도형으로 충분합니다.
- Hierarchy ▸ `+` ▸ 2D Object ▸ **Sprites ▸ Square** 로 네모 스프라이트 생성 가능
- 색 구분: 플레이어=보라, 적/보스=빨강, 바닥=회색, 아이템=노랑

---

## 3. 씬 구성 & Build Settings

만들 씬: `Title`, `Stage1`, `Stage2`, `Stage3`, `Stage4`, `Stage5`
- `Assets/Scenes/`에 6개 씬 생성 (SampleScene을 복제/리네임해도 됨)
- **File ▸ Build Settings ▸ Add Open Scenes** 로 6개 모두 등록
  - 순서: Title(0), Stage1(1) … Stage5(6) — 인덱스 자체는 이름 로드라 중요치 않지만 등록은 필수

---

## 4. GameManager (모든 씬 공통의 뿌리)

`Title` 씬에서 한 번만 만들면 DontDestroyOnLoad로 계속 따라다닙니다.

1. 빈 GameObject `GameManager` 생성 ▸ `GameManager.cs` 추가
2. 인스펙터:
   - **Title Scene**: `Title`
   - **Stage Scenes** (size 5): `Stage1`,`Stage2`,`Stage3`,`Stage4`,`Stage5`

> 기억 문장 5개와 "거짓=④"는 코드에 내장돼 있어 별도 입력 불필요.

---

## 5. 플레이어 프리팹

1. 빈 GameObject `Player` 생성 ▸ **Tag = `Player`**
2. 자식으로 `Sprite`(SpriteRenderer + 플레이어 네모) 추가, 그 자식에 **Animator**
3. `Player`에 컴포넌트 추가:
   - **Rigidbody2D** — Gravity Scale 3, Freeze Rotation Z ✔ (스크립트가 자동 설정도 함)
   - **BoxCollider2D** (또는 CapsuleCollider2D) — 발끝에 맞게 크기 조정
   - **PlayerController** / **PlayerHealth** / **PlayerAttack**
4. 인스펙터 핵심:
   - PlayerController ▸ **Ground Layer** = `Ground`
   - PlayerAttack ▸ **Target Layers** = `Enemy`
   - (선택) PlayerHealth ▸ **Respawn Point** = 체크포인트 Transform
5. `Assets/Prefabs/`로 드래그해 프리팹화

### Animator Controller (Echo)
`Echo.controller` 생성 후 파라미터:
| 이름 | 타입 |
|---|---|
| Speed | Float |
| IsGrounded | Bool |
| VerticalVelocity | Float |
| Jump | Trigger |
| Attack | Trigger |
| Hurt | Trigger |
| Death | Trigger |
- 상태: Idle ↔ Run (Speed>0.1), Jump, Attack, Hurt, Death
- Phase A에선 Animator 없이도 이동/점프/전투는 동작 (애니만 안 보일 뿐)

---

## 6. 바닥 / 스테이지 지형

1. 바닥용 GameObject(넓은 회색 네모) 생성 ▸ **Layer = `Ground`**
2. **BoxCollider2D** 추가 (Is Trigger 끄기)
3. 여러 발판을 깔아 테스트 코스 구성
   - 실제론 Tilemap 권장: 2D Object ▸ Tilemap, 타일 팔레트로 그리고 TilemapCollider2D + CompositeCollider2D, Layer=`Ground`

---

## 7. HUD (각 스테이지 씬)

1. **UI ▸ Canvas** 생성 (Render Mode: Screen Space - Overlay, Canvas Scaler: Scale With Screen Size, 480×270)
2. Canvas 자식으로:
   - **HP 바**: Image (Image Type = **Filled**, Horizontal) → `hpFill`
   - **HP 텍스트**: TextMeshPro - Text → `hpText`
   - **열쇠 텍스트**: TMP → `keyText`
   - **기억 텍스트**: TMP → `memoryText`
   - (선택) 스테이지 번호/이름 TMP → `stageNumberText`, `stageNameText`
3. Canvas(또는 빈 오브젝트)에 **HUDController.cs** 추가 ▸ 위 레퍼런스 연결
4. ▶ 재생 시 플레이어 HP/열쇠/기억이 자동 반영

> ⚠️ TMP 첫 사용 시 "Import TMP Essentials" 팝업 → 임포트.
> 한글은 **Noto Sans CJK KR**로 TMP Font Asset 생성(Window ▸ TextMeshPro ▸ Font Asset Creator) 후 적용.

---

## 8. 기억 인벤토리 패널 (MemoryUI)

1. Canvas 자식 `MemoryPanel`(처음엔 비활성) ▸ 5개 행(각 행에 TMP 텍스트 + 체크 아이콘)
2. 빈 오브젝트에 **MemoryUI.cs**:
   - **Panel** = MemoryPanel
   - **Entry Texts** (size 5) = 각 행 TMP
   - **Entry Checks** (size 5) = 각 행 체크 GameObject
   - **Counter Text** = "n / 5" TMP
   - Toggle Key = Tab
3. 인게임에서 **Tab**으로 열고 닫힘 (열려 있는 동안 플레이어 입력 잠김)

---

## 9. NPC + 대화 시스템 (Stage 1, 2)

### 9-1. DialogueSystem (씬당 1개, Canvas 안 권장)
1. `DialogueBox` 패널(비활성): 화자 TMP, 본문 TMP, ▼ 인디케이터
2. `ChoicePanel`(비활성): **Button** 2~4개 (각 버튼 자식에 TMP 라벨)
3. 빈 오브젝트에 **DialogueSystem.cs**:
   - Dialogue Panel / Speaker Text / Body Text / Continue Indicator
   - Choice Panel / Choice Buttons(size 2~4)
   - Char Interval = 0.03

### 9-2. NPC (Stage 1 — 단순 대화)
1. NPC GameObject + SpriteRenderer + **Collider2D (Is Trigger ✔)** (대화 감지 범위)
2. **NPCInteraction.cs**:
   - Speaker Name: `기억지기`
   - **Lines**: 대사 여러 줄 입력 (보고서 예시 사용 가능)
   - Choices: **비움** (단순 대화)
   - **Reward Memory Id**: `1`, **Reward Keys**: `1`
   - Interact Indicator: [E] 표시 오브젝트(선택)
3. 결과: 대화 끝까지 진행 → 기억①+열쇠 지급 → 잠긴 문 통과

### 9-3. NPC (Stage 2 — 선택지 분기)
- 위와 같되 **Choices** 채움: 각 선택지 text / **Is Correct**(정답에 ✔) / Response Text
- Reward Memory Id: `2`. 정답을 골라야만 보상 지급, 오답은 재시도

---

## 10. 문 / 열쇠 (스테이지 출구)

1. `Door` GameObject + SpriteRenderer + **Collider2D (Is Trigger ✔)**
2. **DoorController.cs**:
   - Locked Sprite / Open Sprite (obj_door_locked / obj_door_open)
   - **Go To Next Stage** ✔ (다음 스테이지로 자동 전환)
   - Transition Delay 0.8
3. (선택) 열쇠를 필드에서 줍게 하려면: 노란 네모 + Collider2D(Trigger) + **KeyPickup.cs**
4. 플레이어가 열쇠 보유 후 문 앞에서 **E** → 열림 → 다음 씬

---

## 11. 퍼즐 (Stage 3)

1. `PuzzleManager` 빈 오브젝트 ▸ **PuzzleManager.cs**:
   - **Correct Order** (예: `2,4,1,3`) — 조각 id의 정답 순서
   - **Slot Renderers** (size = 조각 수): 슬롯 SpriteRenderer들
   - Empty/Filled Slot Sprite (obj_puzzle_slot)
   - Reward Memory Id `3`, Reward Keys `1`
   - **Reward Object**: 정답 시 켤 출구 문/열쇠 (처음엔 비활성으로 둠)
2. 조각마다 GameObject + Collider2D(Trigger) + **PuzzlePiece.cs**:
   - Piece Id (1,2,3,4…)
   - **Manager** = 위 PuzzleManager 드래그
   - Active Highlight / Interact Indicator(선택)
3. 플레이어가 조각 앞에서 **E** → 정답 순서로 누르면 보상 + 출구 활성화

---

## 12. 보스 (Stage 4)

### 12-1. 투사체 프리팹
1. 빨간 작은 네모 + **Collider2D (Is Trigger ✔)** + **BossProjectile.cs**
   - Wall Layers = `Ground`
   - (선택) Hit Effect
2. 프리팹화 (`Assets/Prefabs/BossProjectile`)

### 12-2. 보스
1. `Boss` GameObject + 자식 Sprite(빨간 큰 네모)+Animator + **Collider2D** + **Rigidbody2D**
2. **BossController.cs**:
   - Max HP 100, Phase2 Threshold 0.5
   - **Left X / Right X**: 보스 이동 가능 X 범위(아레나 폭)
   - **Projectile Prefab** = 위 투사체 프리팹
   - **Fire Point** = 보스 앞 빈 Transform
   - Phase2 Projectile Count 3, Spread Angle 20
   - Reward Memory Ids = `4, 5`
   - **Go To Next Stage** ✔ (처치 후 Stage5로)
3. 보스가 플레이어 공격(Z/J)을 받도록 보스 **Layer = `Enemy`** (PlayerAttack의 Target Layers와 일치)

### Animator (Boss) 파라미터
`Idle/Dash/Shoot/Teleport/Death`(Trigger), `Phase2`(Bool)

> (선택) 보스 HP바: `BossController.OnBossHealthChanged`에 별도 UI를 구독시키면 됨.

---

## 13. 엔딩 (Stage 5)

1. Canvas에 엔딩 UI: 5개 문장 행(TMP), 각 행 강조 오브젝트(선택), 참/거짓 라벨(선택)
2. `TrueEndingPanel`, `BadEndingPanel`(둘 다 비활성)
3. 빈 오브젝트에 **EndingController.cs**:
   - Entry Labels (size 5), Entry Highlights(선택), Entry Verdicts(선택)
   - True/Bad Ending Panel 연결
   - Allow Retry ✔
4. **↑↓**로 문장 선택, **Z** 확인 → ④(거짓) 제거 시 진실 엔딩

> 5개 문장은 GameManager에 저장된 내용을 자동으로 불러옵니다(앞 스테이지에서 모두 획득한 상태 가정).
> 테스트 시엔 GameManager에서 미리 CollectMemory가 호출되도록 앞 스테이지를 거치거나, 임시로 모두 수집해 두세요.

---

## 14. 타이틀 (Title)

1. Canvas + 로고 Image + **Button** 2개 (게임 시작 / 종료)
2. 빈 오브젝트에 **TitleMenu.cs** (First Stage Scene = `Stage1`)
3. 버튼 OnClick 연결:
   - 게임 시작 → `TitleMenu.StartGame`
   - 종료 → `TitleMenu.QuitGame`
4. `GameManager`도 이 씬에 있어야 함(4장) → StartGame이 진행 초기화 + Stage1 로드

---

## 15. 전체 플로우 점검 (최소 동작 체크리스트)

- [ ] Title ▸ 게임 시작 → Stage1 로드
- [ ] A/D 이동, Space 점프, 바닥 착지 정상
- [ ] NPC 대화(E) → 기억①+열쇠 → 문(E) → Stage2
- [ ] Stage2 선택지 정답 → 기억②+열쇠 → Stage3
- [ ] Stage3 퍼즐 정답 → 기억③+열쇠 → Stage4
- [ ] Stage4 보스: Z/J로 타격, HP 50%서 페이즈2, 처치 → 기억④⑤ → Stage5
- [ ] Stage5 엔딩: ④(거짓) 제거 → 진실 엔딩
- [ ] Tab으로 기억창 열람, 피격 시 HP 감소·무적 깜빡임

---

## Phase B — 실제 아트 교체

전 시스템이 동작하면 비주얼을 입힙니다.
1. `echoes_assets_v2`의 PNG를 `Assets/Art/`로 복사
2. **깨끗한 아틀라스 준비**: 레퍼런스 시트는 라벨이 있으므로,
   - 각 캐릭터/이펙트를 **라벨 없는 균일 그리드 시트**로 다시 추출하거나
   - 시트에서 캐릭터 프레임 영역만 잘라 새 PNG로 저장
3. 임포트 설정(공통): Texture Type **Sprite (2D and UI)**, **PPU 16**, Filter **Point**, Compression **None**, Sprite Mode **Multiple** ▸ Sprite Editor에서 Slice(Grid By Cell Size 32×32 / 48×48 / 16×16)
4. Animator 각 상태의 클립을 잘린 스프라이트로 구성
5. 프리팹/UI의 SpriteRenderer·Image의 Sprite만 교체 (스크립트 수정 불필요)

---

## 자주 막히는 부분

| 증상 | 원인/해결 |
|---|---|
| 점프 안 됨 / 공중에서 무한점프 | PlayerController **Ground Layer** 미지정, 또는 바닥 Layer가 `Ground`가 아님 |
| 공격해도 보스 안 맞음 | 보스 Layer ≠ PlayerAttack **Target Layers**(`Enemy`) |
| 대화/문/NPC 반응 없음 | 플레이어 **Tag=`Player`** 누락, 또는 콜라이더 **Is Trigger** 안 켬 |
| 씬 전환 안 됨 | 씬을 **Build Settings에 등록** 안 함, GameManager Stage Scenes 이름 오타 |
| HUD가 0으로 고정 | HUDController 레퍼런스 미연결, 또는 GameManager가 씬에 없음 |
| 한글 □(두부) | TMP 폰트가 한글 미지원 → Noto Sans CJK로 Font Asset 생성·적용 |
