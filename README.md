# capstone25-pick-pixel

2D 픽셀 아트 사이드 스크롤 **로그라이트(Roguelite)** 액션 게임 — 한신대학교 캡스톤 2025 프로젝트.

플레이어는 매 런마다 무작위로 구성된 방을 차례로 통과하며 다양한 몬스터를 처치하고
보스 방까지 도달하는 것을 목표로 한다.

---

## 1. 개발 환경

| 항목 | 버전 / 값 |
| --- | --- |
| Unity Editor | **6000.3.11f1** (Unity 6) |
| 렌더 파이프라인 | Universal Render Pipeline (URP) 17.3.0 |
| 입력 시스템 | Unity Input System 1.19.0 (현재 코드는 구식 `Input.GetKey` 사용) |
| 2D 패키지 | 2D Animation, Aseprite Importer, PSD Importer, SpriteShape, Tilemap Extras |
| 외부 도구 | MCP for Unity (`com.coplaydev.unity-mcp`) — AI 에디터 자동화 |
| TextMeshPro | 내장 (UI 텍스트 전반) |

### 사용 외부 에셋
- **Hero Knight - Pixel Art** by Sven Thole — 플레이어 캐릭터 스프라이트 & 애니메이션
- **Dark Forest** by Szadi Art — 배경 / 타일맵 (`Assets/Dark_Forest`)
- **Monster Image** — 몬스터 스프라이트 (Oni / Tiger / Zombie)

---

## 2. 폴더 구조 (요약)

```
Assets/
├─ Animations/           # 플레이어/몬스터 애니메이션 클립 & Animator Controller
│  ├─ Player/            # Player.controller, Idle/Run/Jump/Dash 등
│  └─ Monster/           # Oni, Tiger, Zombie 별 클립
├─ Dark_Forest/          # 배경 타일맵 & 데모 씬
├─ Hero Knight - Pixel Art/  # 외부 에셋 (스프라이트·애니·데모 스크립트)
├─ Images/               # UI/플레이어/몬스터 이미지
├─ Palette/              # 타일맵 팔레트
├─ Physics/              # 2D Physics Material
├─ Prefabs/
│  ├─ Player.prefab
│  ├─ Monster_Oni / Tiger / Zombie.prefab
│  ├─ portal effect/     # 포탈 파티클
│  └─ Rooms/
│     ├─ Fixed/  Room_Entrance, Boss, Checkpoint, Elite, Exit (※ 현재 비어 있음)
│     └─ Random/ Combat(A/B/C), Vertical(A/B)              (※ 현재 비어 있음)
├─ Resources/            # BillingMode.json (Unity IAP)
├─ Scenes/
│  ├─ MainMenu.unity     # 시작 메뉴
│  ├─ Lobby.unity        # 로비/허브
│  ├─ Stage1.unity       # 메인 스테이지 (MapManager 사용)
│  └─ SampleScene.unity  # 초기 샘플 씬
├─ Scripts/              # 본 프로젝트 코드 (아래 § 5 참조)
├─ Settings/             # URP 렌더러/볼륨 프로파일
└─ TextMesh Pro/         # TMP 기본 에셋
```

---

## 3. 씬 흐름

```
MainMenu  ──[Start 버튼 + 슬롯 선택]──▶  Stage1
   │                                       │
   ├──[Settings]── 설정 패널                │ (MapManager가 방 자동 순환)
   └──[Quit]──── 게임 종료                  ▼
                                       Stage Clear / 사망 → 새 런
Lobby      : 허브용 (현재 별도 진입 경로 정해지지 않음)
SampleScene: 초기 테스트용 씬
```

---

## 4. 핵심 시스템

### 4‑1. 맵 매니저 (`MapManager.cs`)
- **씬에 미리 배치된** 방(GameObject)들을 활성/비활성으로 전환하는 방식.
- 시작 방 → 전투 방 2~3개(랜덤·연속 중복 방지) → 출구 방 순으로 런 구성.
- `[ContextMenu]` 로 `Generate Run Order`, `New Run`, `Print Current Run` 디버그 가능.

### 4‑2. 포탈 (`SimplePortal.cs`)
- `PortalDirection.Right` → `MapManager.GoToNextRoom()` 호출 후 다음 방의 `Portal_Left` 위치로 워프.
- `PortalDirection.Left` → 이전 방의 `Portal_Right` 위치로 워프.
- 카메라 동시 이동 옵션(`moveCamera`) 제공.
- 각 방 프리팹에는 `Portals/Portal_Left`, `Portals/Portal_Right` 하위 트랜스폼이 있어야 함.

### 4‑3. 플레이어 (`PlayerController.cs`)
HeroKnight Animator(`AnimState`, `AirSpeedY`, `Grounded`, `WallSlide` 등)를 그대로 사용.
- **이동** : A/D 또는 좌우 화살표 (`Horizontal` 축)
- **점프 / 더블 점프** : Space (`extraJumps = 1` → 더블 점프 1회)
- **점프 컷** : 상승 중 Space 떼면 수직속도 × `jumpCutMultiplier`
- **대시 / 롤** : Left Shift (`dashDuration`, `dashCooldown`)
- **공격 콤보** : 좌클릭 (1→2→3 콤보, `attackComboWindow` 내)
- **블록** : 우클릭 (누르고 있는 동안 `IdleBlock`)
- **벽 슬라이드** : 좌·우 `WallSensor_*` 두 개가 모두 감지될 때
- **플랫폼 하강** : S + Space (`OneWayPlatform` 태그 충돌체에 한해 0.4초 무시)
- **테스트 키** : E = 사망 모션, Q = 피격 모션

### 4‑4. 카메라 (`CameraFollow.cs`)
- Tilemap `cellBounds` 를 월드 좌표로 환산하여 카메라 위치를 Clamp.
- 데드존(`deadZoneSize`)을 두어 작은 움직임에 흔들리지 않도록 함.
- `useRoomMode = true` 시 룸 단위로 스냅(메트로배니아 스타일).

### 4‑5. 몬스터 AI (`MonsterAI.cs` + `MonsterHit.cs`)
- 상태머신 : **Patrol → Chase → Attack**
- 타입 플래그
  - `isFlying`     : 중력 0 + Y축 자유 이동
  - `isRanged`     : 원거리 공격 (Projectile은 TODO 상태)
  - `isDashMelee`  : 돌진 후 근접 타격
- `groundCheck` Raycast 로 낭떠러지 회피, `flipCooldown` 으로 좌우 진동 방지
- `hitCollider` GameObject 를 공격 모션 동안만 활성화 → `MonsterHit.cs` 가 HP 차감

### 4‑6. 메인 메뉴 (`MainMenuController.cs`)
- 패널 전환식 메뉴 (Main / Save Select / Settings)
- 슬롯 선택 시 `PlayerPrefs`에 `SelectedSaveSlot` 저장 후 `Stage1` 로드
- Editor / Build 양쪽에서 안전한 Quit 처리

### 4‑7. 기타
- `MovingPlatform.cs` — 두 좌표 사이 왕복 이동 발판
- `GroundCheck.cs`  — OverlapCircle 기반 별도 지면 검사 컴포넌트(현재 메인 컨트롤러는 Sensor_HeroKnight 사용)

---

## 5. 스크립트 목록

| 경로 | 역할 |
| --- | --- |
| `Assets/Scripts/Player/PlayerController.cs` | 메인 플레이어 컨트롤러 |
| `Assets/Scripts/Player/GroundCheck.cs` | 보조 지면 체크 |
| `Assets/Scripts/Enemy/MonsterAI.cs` | 몬스터 상태머신 |
| `Assets/Scripts/Enemy/MonsterHit.cs` | 몬스터 HP / 피격 |
| `Assets/Scripts/MapManager.cs` | 방 순서 생성·전환 |
| `Assets/Scripts/SimplePortal.cs` | 방 간 포탈 |
| `Assets/Scripts/CameraFollow.cs` | 카메라 추적·클램프 |
| `Assets/Scripts/MovingPlatform.cs` | 이동 발판 |
| `Assets/Scripts/MainMenuController.cs` | 메인 메뉴 UI 컨트롤러 |
| `Assets/Hero Knight - Pixel Art/Demo/HeroKnight.cs` | (외부) 원본 데모 컨트롤러 — 미사용/중복 |
| `Assets/Hero Knight - Pixel Art/Demo/Sensor_HeroKnight.cs` | 트리거 카운터 (Ground/WallSensor에서 사용) |
| `Assets/Hero Knight - Pixel Art/Demo/DestroyEvent_HeroKnight.cs` | 애니메이션 이벤트용 파티클 자동 제거 |

---

## 6. 조작 키

| 입력 | 동작 |
| --- | --- |
| `A` / `D`, ← / → | 좌우 이동 |
| `Space` | 점프 / 더블 점프 |
| `S` + `Space` | 한쪽 통행(OneWayPlatform) 하강 |
| `Left Shift` | 대시 / 롤 |
| 좌클릭 | 공격 (1→2→3 콤보) |
| 우클릭 (홀드) | 블록 |
| `Q` | (테스트) 피격 모션 |
| `E` | (테스트) 사망 모션 |

---

## 7. 빌드 / 실행

1. Unity Hub 에서 **Unity 6000.3.11f1** 설치 후 본 프로젝트 폴더 열기
2. 첫 진입 시 `Library` 가 없으면 임포트가 자동 실행됨 (수 분 소요)
3. `Assets/Scenes/MainMenu.unity` 를 열고 ▶ 재생
4. (선택) MCP for Unity 사용 시 — VS Code / Claude Code 등의 MCP 클라이언트 연결

---

## 8. 알려진 이슈

알려진 컴파일/런타임/리소스 이슈 목록은 별도 파일 **[ERRORS.txt](./ERRORS.txt)** 를 참고하세요.
핵심 이슈만 요약하면 다음과 같습니다.

- 일부 스크립트에 **태그 미등록**으로 인한 런타임 동작 실패 가능 (`PlayerAttack`, `Player`, `OneWayPlatform`).
- 여러 스크립트의 한글 주석/문자열이 **인코딩 깨짐(mojibake)** 상태.
- Unity 6 기준 **Obsolete API** (`FindObjectOfType`) 사용.
- `Assets/Prefabs/Rooms/...` 하위 다수 폴더가 **빈 상태**.

---

## 9. TODO (향후 개선 방향)

- [ ] 누락 태그(`PlayerAttack`, `Player`, `OneWayPlatform`) 등록 및 프리팹에 적용
- [ ] 깨진 한글 주석/디버그 메시지를 UTF‑8(BOM) 로 통일 저장
- [ ] `MonsterAI.RangedAttack()` — 투사체 프리팹 구현
- [ ] 빈 방 프리팹 채우기(또는 사용하지 않는 폴더 정리)
- [ ] 새 Input System Actions(`InputSystem_Actions.inputactions`) 로 마이그레이션
- [ ] HP/UI HUD, 사망/리트라이 흐름, 사운드 연출, 보스 패턴
- [ ] Hero Knight 원본 `HeroKnight.cs` 와 `PlayerController.cs` 중복 정리

---

## 10. 라이선스 / 크레딧

- **Hero Knight - Pixel Art** — © Sven Thole (Unity Asset Store)
- **Dark Forest** — © Szadi Art (xpx@onet.eu)
- 본 프로젝트 코드 자체는 학내 캡스톤 결과물로, 외부 에셋의 라이선스는 각 에셋 제공자의 정책을 따른다.

---

## 11. 브랜치 전략 / 개발 분담 (2026-05-22)

### 브랜치 구조

```
main (메인 배포 브랜치 — PR 병합만 가능)
  ↑
develop (통합 브랜치 — 모든 개인 브랜치 작업 병합)
  ↑
  ├─ 배민 (개인 작업 브랜치)
  ├─ 정인규 (개인 작업 브랜치)
  ├─ 임강연 (개인 작업 브랜치)
  └─ 문영진 (개인 작업 브랜치)
```

### 작업 분담

| 담당자 | 브랜치 | 담당 이슈 | 수정 파일 |
|--------|--------|---------|---------|
| **배민** | `배민` | [CRITICAL #0] Stage1 중복 Player 객체 정리 | `Assets/Scenes/Stage1.unity`<br>`Assets/Prefabs/Player.prefab` |
| **정인규** | `정인규` | [CRITICAL #2] 한글 인코딩 손상 복구<br>(UTF-8 with BOM 재저장) | `MonsterHit.cs`<br>`CameraFollow.cs`<br>`MainMenuController.cs`<br>`MovingPlatform.cs` |
| **임강연** | `임강연` | [CRITICAL #1] 누락 태그 등록 & 프리팹 적용 | `ProjectSettings/TagManager.asset`<br>`Assets/Prefabs/Player.prefab`<br>`Assets/Prefabs/Monster_*.prefab` |
| **문영진** | `문영진` | [MEDIUM #4] FindObjectOfType 업그레이드<br>[MEDIUM #5] MonsterAI 공격 쿨다운 개선 | `SimplePortal.cs`<br>`Assets/Scripts/Enemy/MonsterAI.cs` |

### 작업 흐름

1. **각자 개인 브랜치에서 작업**
   ```powershell
   git checkout 배민  # 또는 자신의 브랜치명
   git pull origin 배민
   # ... 파일 수정 ...
   git add .
   git commit -m "CRITICAL #0: Stage1 중복 Player 객체 정리"
   git push origin 배민
   ```

2. **GitHub에서 PR 생성**
   - 개인 브랜치 → `develop` 브랜치로 PR 생성
   - 팀원 1명 이상의 검토 후 병합

3. **충돌 처리**
   - PR 생성 시 자동으로 충돌 표시됨
   - 병합 전 충돌 해결 (GitHub UI 또는 로컬)
   - develop은 항상 컴파일 가능한 상태 유지

4. **최종 메인 병합**
   - 모든 CRITICAL 이슈 해결 후
   - develop → main PR 생성 & 병합

### 주의사항

- **각 브랜치는 담당 파일만 수정** — 충돌 최소화
- **develop에 자주 동기화** — `git pull origin develop` 로 최신 상태 유지
- **커밋 메시지는 이슈 번호 포함** — 예: "CRITICAL #0: 중복 객체 정리"
- **프리팹/씬 파일 수정 시 주의** — 병합 전 충돌 여부 확인
