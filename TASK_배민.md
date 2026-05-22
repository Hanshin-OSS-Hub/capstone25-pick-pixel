# 📋 작업 가이드 — 배민

## 담당 이슈

- **[CRITICAL #0]** Stage1 씬에 PlayerController 객체 2개 존재 — 중복 정리

---

## 📝 작업 내용

### 문제 상황

현재 `Assets/Scenes/Stage1.unity` 씬에 다음 두 객체가 동시 존재:

```
(A) 루트의 Player_1
    ✓ Tag="Player", Layer="Player"
    ✓ 자식: GroundSensor, WallSensor_R1/R2/L1/L2
    ✓ PlayerController 정상 동작

(B) Gameplay/Player
    ✗ Tag="Untagged", Layer="Default"
    ✗ 자식 1개뿐 (센서 없음)
    ✗ PlayerController.Awake()에서 NullReferenceException 발생
```

### 결과 (수정 후)

- `Gameplay/Player` 객체 **삭제**
- 비활성 상태의 `HeroKnight` 루트 객체도 **삭제**
- `Player_1`만 남겨서 모든 입력/애니메이션 통일

---

## 🛠️ 작업 단계

### 1단계: Stage1.unity 열기

1. Unity Editor에서 `Assets/Scenes/Stage1.unity` 열기
2. Hierarchy 패널에서 객체 확인:
   - `Player_1` (활성, 정상)
   - `Gameplay/Player` (활성, 문제)
   - `HeroKnight` (비활성, 중복)

### 2단계: 중복 객체 삭제

**Step 2-1: Gameplay/Player 삭제**
1. Hierarchy에서 `Gameplay` → `Player` 선택
2. Delete 키로 삭제
3. 콘솔에 NullReferenceException 없음 확인

**Step 2-2: HeroKnight 루트 삭제** (비활성 객체)
1. Hierarchy에서 `HeroKnight` 찾기 (회색으로 표시됨 = 비활성)
2. Delete 키로 삭제

### 3단계: CameraFollow 설정 확인

1. `Player_1` 객체 선택
2. Inspector에서 `CameraFollow` 컴포넌트 확인
3. **target** 필드 확인:
   - `Player_1` Transform이 할당되어 있어야 함
   - 아니면 Player_1을 드래그해서 할당

### 4단계: 플레이 테스트

1. Play 버튼 (▶) 클릭
2. Stage1 로드 완료 후:
   - **콘솔**: NullReferenceException 없어야 함
   - **플레이**: Player_1만 제어됨
   - **카메라**: Player_1을 정상적으로 추적
3. 플레이어가 정상 움직이는지 확인
4. Stop 버튼 (■) 클릭해서 종료

---

## ✅ 검수 체크리스트

- [ ] Stage1.unity에서 `Gameplay/Player` 삭제됨
- [ ] `HeroKnight` 루트 객체 삭제됨
- [ ] `Player_1`만 씬에 남음
- [ ] 콘솔에 NullReferenceException 없음
- [ ] 플레이어 이동/점프/대시 모두 정상
- [ ] 카메라가 플레이어를 제대로 추적
- [ ] Player.prefab과 일관성 있음

---

## 📤 커밋 및 푸시

```powershell
git add Assets/Scenes/Stage1.unity
git commit -m "CRITICAL #0: Stage1 중복 Player 객체 정리

- Gameplay/Player 객체 삭제
- HeroKnight 비활성 루트 객체 정리
- Player_1만 활성 상태로 유지
- NullReferenceException 제거"

git push origin 배민
```

**GitHub에서 PR 생성**: `배민` → `develop`

---

## 📚 참고 자료

- **ERRORS.txt** § [CRITICAL] 0 — 상세 설명
- **README.md** § 4-3 — PlayerController 설명
- **Assets/Scenes/Stage1.unity** — 직접 수정할 파일
