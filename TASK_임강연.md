# 📋 작업 가이드 — 임강연

## 담당 이슈

- **[CRITICAL #1]** 누락된 태그 등록 & 프리팹 적용

---

## 📝 작업 내용

### 문제 상황

두 개의 태그가 프로젝트에 미등록되어 있어서 런타임 기능 실패:

```
(1) "PlayerAttack" 미등록
    └─ MonsterHit.cs:9 에서 CompareTag("PlayerAttack") 항상 false
    └─ 플레이어 공격이 몬스터와 충돌하지 않음
    
(2) "OneWayPlatform" 미등록
    └─ PlayerController.cs:284 에서 CompareTag("OneWayPlatform") 항상 false
    └─ S+Space 한쪽 통행 플랫폼 하강 기능 무력화
```

### 결과 (수정 후)

- 프로젝트 설정에 2개 태그 등록
- 플레이어 공격 콜라이더에 "PlayerAttack" 태그 부착
- 한쪽 통행 발판에 "OneWayPlatform" 태그 부착

---

## 🛠️ 작업 단계

### 1단계: 태그 등록

**Step 1-1: Edit → Project Settings 열기**

1. Unity Editor 상단 메뉴 → `Edit` → `Project Settings`
2. 왼쪽 패널에서 `Tags and Layers` 클릭
3. `Tags` 섹션 확인:
   - 현재 태그: Untagged, Respawn, Finish, EditorOnly, MainCamera, Player, GameController, Ground, Monster

**Step 1-2: PlayerAttack 태그 추가**

1. `Tags` 섹션에서 마지막 항목 아래 "+" 버튼 클릭
2. "Element 9" (또는 비어있는 슬롯)에 이름 입력:
   ```
   PlayerAttack
   ```
3. Enter 키로 추가 완료

**Step 1-3: OneWayPlatform 태그 추가**

1. 다시 "+" 버튼 클릭 (또는 다음 빈 슬롯)
2. "Element 10"에 이름 입력:
   ```
   OneWayPlatform
   ```
3. Enter 키로 추가 완료

**Step 1-4: 확인**

Project Settings 창 닫기 (변경사항 자동 저장) → TagManager.asset 파일이 수정됨

---

### 2단계: 플레이어 공격 콜라이더에 "PlayerAttack" 태그 부착

#### 목표: 플레이어 공격 히트박스 객체를 찾아 "PlayerAttack" 태그 부착

**Step 2-1: 플레이어 프리팹 또는 씬에서 찾기**

**방법 A: 프리팹에서 설정 (권장)**

1. Project 창 → `Assets/Prefabs/` 폴더
2. `Player.prefab` 더블클릭 (Prefab 편집 모드 진입)
3. Hierarchy에서 자식 객체 탐색:
   - `Player` → 자식 객체들 확인
   - 공격 콜라이더로 보이는 객체 찾기 (예: "AttackBox", "HitCollider", 또는 비슷한 이름)
   
   **혹은 PlayerController.cs 코드 확인:**
   ```csharp
   // PlayerController.cs에서 공격 콜라이더 변수명 찾기
   // 예: [SerializeField] private GameObject attackCollider;
   ```

4. 찾은 객체 선택 → Inspector 우측 상단에 **Tag** 드롭다운 있음
5. Tag 드롭다운 클릭 → `PlayerAttack` 선택
6. Apply 버튼 클릭 (Prefab 저장)
7. Prefab 편집 모드 종료 (상단 "Player" 우측 뒤로가기 버튼 또는 좌측 상단 "Assets")

**방법 B: 씬에서 설정 (임시 테스트용)**

1. `Assets/Scenes/Stage1.unity` 열기
2. Hierarchy에서 Player_1 → 자식 객체 탐색
3. 공격 콜라이더 객체 선택
4. Inspector → Tag 드롭다운 → `PlayerAttack` 선택
5. 저장 (Ctrl+S)

---

### 3단계: 한쪽 통행 플랫폼에 "OneWayPlatform" 태그 부착

#### 목표: 한쪽 통행 플랫폼(OneWayPlatform) 프리팹 또는 씬 객체에 태그 부착

**Step 3-1: 한쪽 통행 플랫폼 찾기**

**Option A: 씬에서 직접 찾기**

1. `Assets/Scenes/Stage1.unity` 열기
2. Hierarchy 검색 (상단 검색창):
   - "OneWay" 또는 "platform" 검색
   - 또는 수동으로 Hierarchy 스크롤해서 찾기
   - 일반적으로 "Stage1_..." 방 내부에 발판 객체들이 있음

3. 한쪽 통행 발판 객체 선택 (예: "Platform_OneWay" 또는 비슷한 이름)
4. Inspector → Tag → `OneWayPlatform` 선택
5. 모든 한쪽 통행 발판에 같은 태그 부착 (여러 개 있을 수 있음)

**Option B: 프리팹에서 설정 (향후 확장용)**

1. Project 창 → `Assets/Prefabs/Rooms/` 폴더 탐색
2. 한쪽 통행 플랫폼 프리팹 찾기 (있으면)
3. 더블클릭해서 Prefab 편집 모드
4. Tag → `OneWayPlatform` 선택
5. Apply 버튼

---

### 4단계: 플레이 테스트

1. **Stage1.unity에서 Play 버튼 클릭**

2. **플레이어 공격 테스트**
   - 좌클릭으로 몬스터 공격
   - 몬스터가 피격 애니메이션 실행
   - Console에 "몬스터 피격!" (또는 한글) 메시지 출력

3. **한쪽 통행 플랫폼 테스트**
   - 한쪽 통행 발판 위에 서기
   - S + Space 입력 → 아래로 하강
   - 발판을 통과해서 아래로 떨어져야 함
   - (발판이 없으면 스테이지 설계상 테스트 불가능할 수 있음)

4. **콘솔 에러 확인**
   - "PlayerAttack" 또는 "OneWayPlatform" 관련 경고 없음

5. Play 종료 (Stop 버튼)

---

## ✅ 검수 체크리스트

- [ ] Project Settings → Tags and Layers에 "PlayerAttack" 등록됨
- [ ] Project Settings → Tags and Layers에 "OneWayPlatform" 등록됨
- [ ] Player.prefab의 공격 콜라이더에 "PlayerAttack" 태그 부착됨
- [ ] Stage1의 한쪽 통행 발판(들)에 "OneWayPlatform" 태그 부착됨
- [ ] Play 모드에서 플레이어 공격이 몬스터와 충돌함
- [ ] Play 모드에서 S+Space로 한쪽 통행 발판 하강 가능
- [ ] Console에 태그 관련 경고 없음

---

## 📤 커밋 및 푸시

```powershell
# ProjectSettings 변경사항 스테이징 (태그 등록)
git add ProjectSettings/TagManager.asset

# Player.prefab 또는 Stage1.unity 변경사항 스테이징
git add Assets/Prefabs/Player.prefab
git add Assets/Scenes/Stage1.unity

# 커밋
git commit -m "CRITICAL #1: 누락 태그 등록 및 프리팹 적용

- ProjectSettings: PlayerAttack, OneWayPlatform 태그 등록
- Player.prefab: 공격 콜라이더에 PlayerAttack 태그 부착
- Stage1.unity: 한쪽 통행 발판에 OneWayPlatform 태그 부착
- 플레이어 공격·플랫폼 하강 기능 복구"

git push origin 임강연
```

**GitHub에서 PR 생성**: `임강연` → `develop`

---

## 📚 참고 자료

- **ERRORS.txt** § [CRITICAL] 1 — 상세 설명
- **README.md** § 4-3 — PlayerController (S+Space 플랫폼 하강)
- **README.md** § 4-5 — MonsterAI (공격 판정)
- **Assets/Scripts/Enemy/MonsterHit.cs:9** — CompareTag("PlayerAttack") 코드
- **Assets/Scripts/Player/PlayerController.cs:284** — CompareTag("OneWayPlatform") 코드
