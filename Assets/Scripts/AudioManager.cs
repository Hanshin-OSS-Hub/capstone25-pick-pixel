using UnityEngine;

/// <summary>
/// BGM을 관리하는 싱글톤 오디오 매니저
///
/// 사용법:
/// 1. 빈 오브젝트에 이 스크립트 붙이기
/// 2. Inspector에서 StageBGM 슬롯에 Gate_of_the_Hidden_Glade 클립 연결
/// 3. BossBGM은 나중에 연결 예정 (현재 비워 두기)
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("=== BGM 클립 ===")]
    [SerializeField] private AudioClip stageBGM;   // Gate_of_the_Hidden_Glade
    [SerializeField] private AudioClip bossBGM;    // 추후 추가 예정 — 비워 두기

    [Header("=== 오디오 소스 ===")]
    [SerializeField] private AudioSource bgmSource;

    [Header("=== 볼륨 ===")]
    [Range(0f, 1f)]
    [SerializeField] private float bgmVolume = 0.7f;

    // ─────────────────────────────────────────────
    private void Awake()
    {
        // 싱글톤 설정
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // bgmSource가 Inspector에서 연결되지 않은 경우 자동 추가
        if (bgmSource == null)
            bgmSource = gameObject.AddComponent<AudioSource>();

        bgmSource.loop = true;
        bgmSource.playOnAwake = false;
        bgmSource.volume = bgmVolume;
    }

    // ─────────────────────────────────────────────
    /// <summary>일반 스테이지 BGM 재생</summary>
    public void PlayStageBGM()
    {
        if (stageBGM == null)
        {
            Debug.LogWarning("[AudioManager] StageBGM 클립이 연결되지 않았습니다.");
            return;
        }

        if (bgmSource.clip == stageBGM && bgmSource.isPlaying)
            return; // 이미 재생 중이면 중복 재생 방지

        bgmSource.clip = stageBGM;
        bgmSource.Play();
        Debug.Log("[AudioManager] 스테이지 BGM 재생 시작");
    }

    /// <summary>보스방 BGM 재생 (클립이 없으면 무음)</summary>
    public void PlayBossBGM()
    {
        bgmSource.Stop();

        if (bossBGM == null)
        {
            // 보스 BGM 미설정 → 무음 유지
            Debug.Log("[AudioManager] 보스방 진입 — BGM 없음 (추후 추가 예정)");
            return;
        }

        bgmSource.clip = bossBGM;
        bgmSource.Play();
        Debug.Log("[AudioManager] 보스 BGM 재생 시작");
    }

    /// <summary>BGM 정지</summary>
    public void StopBGM()
    {
        bgmSource.Stop();
    }

    /// <summary>볼륨 실시간 변경</summary>
    public void SetVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        bgmSource.volume = bgmVolume;
    }
}
