# 📋 작업 가이드 — 정인규

## 담당 이슈

- **[CRITICAL #2]** 한글 인코딩 손상 (Mojibake) — UTF-8 BOM으로 재저장

---

## 📝 작업 내용

### 문제 상황

4개의 스크립트 파일에서 한글 주석/문자열이 깨져 있음:

```
파일명                              | 문제 내용
-----------------------------------------
MonsterHit.cs:12                    | "몬스터 피격! 현재 HP: " → "���� ǰ�! ..."
CameraFollow.cs (다수 라인)         | 한글 주석 다수 깨짐
MainMenuController.cs (다수 라인)   | 한글 주석 다수 깨짐
MovingPlatform.cs:5                 | [Header("이동 발판")] → "[Header("�̵� ����")]"
```

### 원인

- 파일이 **EUC-KR / CP949**로 저장된 채 **UTF-8**로 재해석됨
- Visual Studio for Unity의 기본 ANSI 저장 또는 Notepad 한글 환경 영향

### 결과 (수정 후)

모든 파일을 **UTF-8 with BOM**으로 일관되게 저장 → 한글 가독성 복구

---

## 🛠️ 작업 단계

### 1단계: 수정할 파일 목록

```
1) Assets/Scripts/Enemy/MonsterHit.cs
2) Assets/Scripts/CameraFollow.cs
3) Assets/Scripts/MainMenuController.cs
4) Assets/Scripts/MovingPlatform.cs
```

### 2단계: VS Code에서 인코딩 변경

**Step 2-1: MonsterHit.cs 수정**

1. **VS Code에서 파일 열기**
   - File Explorer → `Assets/Scripts/Enemy/MonsterHit.cs` 더블클릭
   
2. **인코딩 변경**
   - 하단 우측 상태바에 "UTF-8" 또는 "GBK" 표시 확인
   - 클릭 → "UTF-8 with BOM" 선택
   - 파일이 다시 로드됨

3. **한글 텍스트 확인**
   - Line 12 주변: `"몬스터 피격! 현재 HP: "` 정상 표시되는지 확인
   - 깨진 글자가 보이면:
     - Ctrl+Z (Undo)로 되돌리고
     - 다시 "UTF-8 with BOM" 선택 (또는 "EUC-KR" → "UTF-8 with BOM")

4. **저장**
   - Ctrl+S 또는 File → Save
   - 파일이 UTF-8 BOM으로 저장됨

**Step 2-2: CameraFollow.cs 수정 (동일 방식)**

1. `Assets/Scripts/CameraFollow.cs` 열기
2. 하단 인코딩 표시 클릭 → "UTF-8 with BOM" 선택
3. 한글 주석 확인:
   - 기대: "데드존", "타일맵", "Clamp", "ROOM MODE" 등
4. Ctrl+S 저장

**Step 2-3: MainMenuController.cs 수정**

1. `Assets/Scripts/MainMenuController.cs` 열기
2. 하단 인코딩 표시 클릭 → "UTF-8 with BOM" 선택
3. 한글 주석 확인:
   - 기대: "패널 전환", "슬롯 선택 저장", 등
4. Ctrl+S 저장

**Step 2-4: MovingPlatform.cs 수정**

1. `Assets/Scripts/MovingPlatform.cs` 열기
2. 하단 인코딩 표시 클릭 → "UTF-8 with BOM" 선택
3. Line 5 주변 확인:
   - 기대: `[Header("이동 발판")]` 정상 표시
4. Ctrl+S 저장

---

### 3단계: Unity Editor에서 재확인

1. **Unity Editor 포커스**
   - VS Code에서 저장하면 Unity Editor가 자동 감지 (약 1~2초)
   - Console 창 확인 → 에러 없음

2. **Inspector에서 확인 (MovingPlatform)**
   - `Assets/Scenes/Stage1.unity` 또는 다른 씬에서 MovingPlatform 객체 선택
   - Inspector → Header 라벨이 "이동 발판"으로 정상 표시되는지 확인

3. **Debug.Log 확인 (MonsterHit)**
   - Play 모드 진입
   - 몬스터와 접촉 시 Console 출력 확인:
     - 기대: "몬스터 피격! 현재 HP: ..."
     - 현재: 깨진 글자면 Step 2를 다시 확인

---

## ✅ 검수 체크리스트

- [ ] MonsterHit.cs — "UTF-8 with BOM"으로 저장됨
- [ ] CameraFollow.cs — "UTF-8 with BOM"으로 저장됨
- [ ] MainMenuController.cs — "UTF-8 with BOM"으로 저장됨
- [ ] MovingPlatform.cs — "UTF-8 with BOM"으로 저장됨
- [ ] VS Code에서 모든 한글 텍스트 정상 표시됨
- [ ] Unity Editor Console에 인코딩 관련 에러 없음
- [ ] Inspector에서 Header 한글이 정상 표시됨
- [ ] Play 모드에서 Debug.Log 한글이 정상 표시됨

---

## 📤 커밋 및 푸시

```powershell
git add Assets/Scripts/Enemy/MonsterHit.cs
git add Assets/Scripts/CameraFollow.cs
git add Assets/Scripts/MainMenuController.cs
git add Assets/Scripts/MovingPlatform.cs

git commit -m "CRITICAL #2: 한글 인코딩 손상 복구 (UTF-8 BOM)

- MonsterHit.cs 한글 문자열 복구
- CameraFollow.cs 한글 주석 복구
- MainMenuController.cs 한글 주석 복구
- MovingPlatform.cs Header 한글 복구
- 모든 파일을 UTF-8 with BOM으로 저장"

git push origin 정인규
```

**GitHub에서 PR 생성**: `정인규` → `develop`

---

## 📚 참고 자료

- **ERRORS.txt** § [CRITICAL] 2 — 상세 설명
- **VS Code 공식 문서**: "Change file encoding" 검색
- **Git 한글 인코딩**: UTF-8 with BOM 권장
