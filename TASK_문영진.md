# 📋 작업 가이드 — 문영진

## 담당 이슈

- **[MEDIUM #4]** FindObjectOfType 구식 API 업그레이드
- **[MEDIUM #5]** MonsterAI 공격 후 쿨다운 개선 (& RangedAttack TODO)

---

## 📝 작업 내용

### 이슈 A: FindObjectOfType 구식 API

#### 문제 상황

- **파일**: `Assets/Scripts/SimplePortal.cs:42`
- **코드**:
  ```csharp
  mapManager = FindObjectOfType<MapManager>();
  ```
- **문제**: Unity 6 (2023.1+)에서 Deprecated API
- **결과**: 빌드 시 Warning 발생

#### 해결 방법

- `FindObjectOfType<T>()` → `Object.FindFirstObjectByType<T>()` 또는 `Object.FindAnyObjectByType<T>()`

---

### 이슈 B: MonsterAI 공격 쿨다운 문제

#### 문제 상황

- **파일**: `Assets/Scripts/Enemy/MonsterAI.cs` (라인 172~197)
- **문제**:
  - Attack() 진입 시 isAttacking 플래그로 중복 방지
  - EndAttack()에서 state = MonsterState.Chase로 복귀
  - 복귀 직후 Chase()에서 다시 attackRange 안이면 즉시 Attack으로 전이
  - attackDuration이 attackCooldown과 비슷하거나 크면 사실상 끊임없이 공격
  
- **추가**: RangedAttack() 라인 213은 TODO 상태로 투사체 생성 로직 비어있음

#### 해결 방법

1. AttackEndAttack() 후 짧은 후딜레이 추가 (Invoke 또는 cooldown timer)
2. attackCooldown을 attackDuration보다 충분히 크게 설정
3. RangedAttack() 투사체 생성 로직 구현 또는 TODO 주석 명확히

---

## 🛠️ 작업 단계

### 1단계: FindObjectOfType 업그레이드 (SimplePortal.cs)

**Step 1-1: 파일 열기**

1. VS Code 또는 Unity에서 `Assets/Scripts/SimplePortal.cs` 열기

**Step 1-2: 라인 42 찾기**

```csharp
// 변경 전
mapManager = FindObjectOfType<MapManager>();

// 변경 후 (둘 중 하나 선택)
mapManager = Object.FindFirstObjectByType<MapManager>();
// 또는
mapManager = Object.FindAnyObjectByType<MapManager>();
```

**선택 가이드:**
- `FindFirstObjectByType<T>()` — 가장 먼저 생성된 인스턴스 반환 (권장, 명확함)
- `FindAnyObjectByType<T>()` — 아무 인스턴스나 반환 (약간 더 빠름)

**Step 1-3: 저장**

- Ctrl+S (저장)
- Unity Editor가 자동 감지 (약 1초) → 컴파일

**Step 1-4: 콘솔 확인**

- Unity Console에 "FindObjectOfType" 관련 Warning 없음

---

### 2단계: MonsterAI 공격 쿨다운 개선

**Step 2-1: 파일 열기**

1. VS Code 또는 Unity에서 `Assets/Scripts/Enemy/MonsterAI.cs` 열기

**Step 2-2: 현재 코드 구조 이해 (라인 172~197)**

```csharp
// 현재 구조 (요약)
private float lastAttackTime = 0f;

void Attack()
{
    if (isAttacking) return;
    
    isAttacking = true;
    // ... 공격 애니메이션 시작
    // 아래 두 값이 가까우면 문제 발생:
    // - attackDuration: 공격 모션 길이 (예: 0.8초)
    // - attackCooldown: 공격 재사용 대기시간 (예: 0.5초)
}

void EndAttack()
{
    isAttacking = false;
    lastAttackTime = Time.time; // 여기서만 갱신
    state = MonsterState.Chase;
}

void Chase()
{
    // ... 플레이어 추적
    if (Vector3.Distance(...) <= attackRange)
    {
        if (Time.time - lastAttackTime >= attackCooldown)
        {
            Attack(); // 즉시 다시 공격 가능
        }
    }
}
```

**Step 2-3: 해결 방안 선택**

**방안 A: attackCooldown 값 증가 (가장 간단)**

1. Inspector에서 MonsterAI 컴포넌트 선택
2. `attackCooldown` 값 확인 (기본: 0.5초?)
3. `attackDuration` 값 확인 (기본: 0.8초?)
4. **attackCooldown >= attackDuration + 0.2초** 정도로 설정
   - 예: attackDuration = 0.8초라면 attackCooldown = 1.0초 이상
5. Play 테스트 → 몬스터가 공격 후 약간의 텀이 생김

**방안 B: 코드에서 후딜레이 추가 (더 정교함)**

```csharp
void EndAttack()
{
    isAttacking = false;
    lastAttackTime = Time.time;
    state = MonsterState.Chase;
    
    // 추가: 0.2초 후 재공격 가능하도록 설정
    // (attackCooldown 자체는 유지)
    // 방법: Invoke 사용
    // CancelInvoke("AllowNextAttack");
    // Invoke("AllowNextAttack", 0.2f);
}

void AllowNextAttack()
{
    // lastAttackTime을 현재 시간으로 리셋하면 
    // 다음 attackCooldown 체크가 작동함
    lastAttackTime = Time.time - attackCooldown; // 즉시 공격 가능하도록 조정
}
```

더 간단한 방법:
```csharp
void EndAttack()
{
    isAttacking = false;
    lastAttackTime = Time.time + 0.2f; // 0.2초 후딜레이 추가
    state = MonsterState.Chase;
}
```

**Step 2-4: 방안 선택 후 적용**

- **방안 A 선택 시**: Inspector에서 `attackCooldown` 값만 조정 (코드 수정 안 함)
- **방안 B 선택 시**: EndAttack() 라인 수정 후 저장

**Step 2-5: 플레이 테스트**

1. Stage1.unity Play 모드 진입
2. 몬스터와 전투:
   - 몬스터가 1회 공격
   - 약 0.5~1초 텀
   - 다시 공격
   - (반복)
3. 몬스터가 끊임없이 때리지 않음 확인

---

### 3단계: RangedAttack TODO 처리

**현재 상태**: 라인 213에서 TODO 주석만 있고 로직 비어있음

**Step 3-1: 코드 확인**

```csharp
void RangedAttack()
{
    // TODO: 투사체 프리팹 생성 로직
}
```

**Step 3-2: 두 가지 선택**

**옵션 A: TODO 주석 명확히 하기 (권장, 시간 제약 시)**

```csharp
void RangedAttack()
{
    // TODO: Projectile 프리팹 구현 필요
    // - 투사체 프리팹 생성 (Resources.Load 또는 Instantiate)
    // - 방향 설정 (플레이어 방향)
    // - 속도 설정
    // - 충돌 판정 (MonsterProjectile 스크립트 필요)
}
```

**옵션 B: 간단한 투사체 로직 구현 (선택 사항)**

- 시간이 충분하면:
1. `Assets/Prefabs/` 에 "Projectile.prefab" 또는 "MonsterProjectile.prefab" 생성
2. RangedAttack()에 Instantiate 로직 추가
3. 투사체 프리팹에 "MonsterProjectile" 스크립트 부착
4. 플레이어와 충돌 시 피격 처리

**현재는 방안 A (주석 명확히) 권장**

---

## ✅ 검수 체크리스트

### FindObjectOfType 업그레이드

- [ ] SimplePortal.cs 라인 42에서 FindObjectOfType → FindFirstObjectByType으로 변경됨
- [ ] Unity Editor 콘솔에 "FindObjectOfType" Warning 없음
- [ ] Portal 통과 시 MapManager 정상 작동 (다음 방 로드)

### MonsterAI 공격 쿨다운

- [ ] MonsterAI attackCooldown 값이 attackDuration보다 큼 (또는 코드 후딜레이 추가됨)
- [ ] Play 모드에서 몬스터가 공격 후 약간의 텀이 생김
- [ ] 몬스터가 끊임없이 때리지 않음 (시각적으로 확인)
- [ ] 플레이어가 여전히 몬스터 피격 가능

### RangedAttack TODO

- [ ] 라인 213 주석이 명확하게 작성됨 (또는 기본 로직 구현됨)

---

## 📤 커밋 및 푸시

```powershell
git add Assets/Scripts/SimplePortal.cs
git add Assets/Scripts/Enemy/MonsterAI.cs

git commit -m "MEDIUM #4,#5: FindObjectOfType 업그레이드 & MonsterAI 공격 쿨다운 개선

[#4] FindObjectOfType → FindFirstObjectByType 변경
- SimplePortal.cs:42 업데이트
- Unity 6 Deprecated API Warning 제거

[#5] MonsterAI 공격 쿨다운 개선
- attackCooldown 재설정 (attackDuration보다 충분히 큼)
- EndAttack 후 약간의 후딜레이 추가
- RangedAttack TODO 주석 명확히 작성

결과: 몬스터가 공격 후 약 0.5초 텀 생기면서 자연스러운 공격 패턴"

git push origin 문영진
```

**GitHub에서 PR 생성**: `문영진` → `develop`

---

## 📚 참고 자료

- **ERRORS.txt** § [MEDIUM] 3 — FindObjectOfType 설명
- **ERRORS.txt** § [MEDIUM] 4 — MonsterAI 공격 쿨다운 설명
- **README.md** § 4-5 — MonsterAI 상태머신 설명
- **Unity 공식 문서**: "Object.FindFirstObjectByType"
- **Assets/Scripts/Enemy/MonsterAI.cs** — 직접 수정할 파일
- **Assets/Scripts/SimplePortal.cs** — 직접 수정할 파일
