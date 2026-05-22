# 🚀 capstone25-pick-pixel 브랜치 & 작업 세팅 가이드

**작성일**: 2026-05-22  
**목표**: 4명의 개발자가 병렬로 작업하고 충돌을 최소화하며 체계적으로 병합

---

## 📊 전체 브랜치 구조

```
GitHub Repository: capstone25-pick-pixel
│
├─ main (보호 브랜치 — 최종 배포용)
│  └─ develop (통합 브랜치 — PR 병합 지점)
│     │
│     ├─ 배민 (CRITICAL #0)
│     ├─ 정인규 (CRITICAL #2)
│     ├─ 임강연 (CRITICAL #1)
│     └─ 문영진 (MEDIUM #4,#5)
```

---

## 👥 개발자별 역할 & 담당 이슈

| 개발자 | 브랜치 | 담당 이슈 | 상태 |
|--------|--------|---------|------|
| **배민** | `배민` | [CRITICAL #0] Stage1 중복 Player 객체 정리 | 🔴 |
| **정인규** | `정인규` | [CRITICAL #2] 한글 인코딩 복구 (UTF-8 BOM) | 🔴 |
| **임강연** | `임강연` | [CRITICAL #1] 태그 등록 & 프리팹 적용 | 🔴 |
| **문영진** | `문영진` | [MEDIUM #4,#5] FindObjectOfType & MonsterAI | 🟡 |

**상태 범례**: 🔴 CRITICAL (우선도 높음) | 🟡 MEDIUM

---

## 🎯 시작하기 (각 개발자별)

### 1️⃣ 로컬에서 브랜치 받아오기

```powershell
# 최신 상태 동기화
git fetch origin

# 자신의 브랜치로 이동 (예: 배민)
git checkout 배민

# 또는 원격 브랜치에서 새로 생성
git checkout --track origin/배민
```

### 2️⃣ 작업 가이드 확인

각 브랜치의 **TASK_[이름].md** 파일 읽기:

- **배민**: `TASK_배민.md` 읽기 → Stage1 객체 정리
- **정인규**: `TASK_정인규.md` 읽기 → 한글 인코딩 수정
- **임강연**: `TASK_임강연.md` 읽기 → 태그 등록
- **문영진**: `TASK_문영진.md` 읽기 → API & AI 개선

### 3️⃣ 작업 진행

- 각자의 브랜치에서 **독립적으로** 담당 이슈 해결
- CRITICAL 이슈는 **1주일 내** 완료 권장
- MEDIUM 이슈는 **2주일 내** 완료 권장

### 4️⃣ 커밋 & 푸시

```powershell
# 작업 파일 스테이징
git add [수정한 파일들]

# 커밋 (이슈 번호 포함)
git commit -m "CRITICAL #0: 무언가를 수정했습니다

- 상세 내용 1
- 상세 내용 2"

# 푸시
git push origin 배민  # (자신의 브랜치명)
```

### 5️⃣ PR 생성 & 병합

1. **GitHub 웹사이트 접속**
2. 자신의 브랜치 → **"New Pull Request"** 클릭
3. 베이스: `develop`, 비교: `배민` (또는 자신의 브랜치) 확인
4. **PR 제목**: 커밋 메시지와 동일
5. **PR 설명**: 무엇을 수정했는지 간단히 작성
6. **"Create Pull Request"** 클릭
7. 팀원 1명 검토 후 **"Merge"** 버튼 클릭

---

## 🔄 협업 흐름 (요약)

### Phase 1: 개인 작업 (병렬)

```
개인 브랜치 (배민, 정인규, 임강연, 문영진)에서 각자 작업
           ↓
  git commit & git push
           ↓
     GitHub PR 생성
```

### Phase 2: 검토 & 병합 (순차)

```
PR #1 (배민) → Review → Merge to develop ✓
PR #2 (정인규) → Review → Merge to develop ✓
PR #3 (임강연) → Review → Merge to develop ✓
PR #4 (문영진) → Review → Merge to develop ✓
           ↓
   develop 브랜치 완성
```

### Phase 3: 최종 배포 (메인)

```
develop 상태 테스트 및 검증
           ↓
develop → main PR 생성
           ↓
최종 검토 후 Merge to main ✓
           ↓
     배포 완료! 🎉
```

---

## 📋 PR (Pull Request) 체크리스트

각자 PR을 생성할 때 다음을 확인하세요:

- [ ] **브랜치가 develop에서 분기함** (main에서 아님)
- [ ] **최신 develop과 동기화됨** (`git pull origin develop`)
- [ ] **충돌 없음** (GitHub에서 "This branch has no conflicts" 표시)
- [ ] **코드 리뷰 요청** (팀원 1명 이상)
- [ ] **테스트 완료** (Play 모드에서 정상 작동 확인)
- [ ] **커밋 메시지가 명확함** (이슈 번호 + 설명)
- [ ] **TASK_[이름].md의 체크리스트 모두 완료**

---

## ⚠️ 주의사항

### DO ✅

- **develop에 자주 동기화하기** (`git pull origin develop`)
- **작은 커밋 여러 개** 만들기 (한 번에 큰 커밋 ❌)
- **팀원과 겹치는 파일 피하기** (협력 필요 시 미리 얘기)
- **테스트 후 푸시** (코드 컴파일 가능 확인)

### DON'T ❌

- main 브랜치에 직접 커밋 금지
- develop을 무시하고 main으로 PR 생성 금지
- 다른 사람의 브랜치 건들기 금지
- 테스트 없이 푸시하기 금지
- 큰 파일(바이너리) 커밋하기 금지

---

## 🔗 문제 발생 시 연락처

| 문제 | 담당 |
|------|------|
| 브랜치 충돌 | 해당 담당자 + 정인규 (Merge 담당) |
| Unity 컴파일 에러 | 해당 담당자 + 정인규 |
| PR 검토 요청 | 다른 팀원 3명 중 1명 |
| 전체 브랜치 구조 질문 | 정인규 |

---

## 📚 참고 파일

- **README.md** § 11 — 브랜치 전략 & 분담표
- **ERRORS.txt** — 전체 이슈 상세 설명
- **TASK_[이름].md** — 각자의 담당 작업 가이드
- **Git 기본 명령어**:
  ```
  git checkout [브랜치명]     # 브랜치 이동
  git pull origin [브랜치명]  # 최신 동기화
  git add .                   # 스테이징
  git commit -m "메시지"      # 커밋
  git push origin [브랜치명]  # 푸시
  ```

---

## ✅ 최종 확인 항목

- [ ] 4개의 개인 브랜치 생성됨 (배민, 정인규, 임강연, 문영진)
- [ ] develop 브랜치 생성됨
- [ ] 각 개인 브랜치에 TASK_[이름].md 파일 있음
- [ ] README.md에 브랜치 구조 & 분담표 추가됨
- [ ] 모든 브랜치가 origin에 푸시됨
- [ ] GitHub에서 모든 브랜치가 보임

---

**이제 시작할 준비가 되었습니다!** 🚀

각자 자신의 브랜치에서 담당 이슈를 진행하시면 됩니다.
문제 발생 시 README.md 또는 이 문서를 참고해주세요.

Happy Coding! 💻
