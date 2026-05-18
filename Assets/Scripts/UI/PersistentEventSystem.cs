using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// PauseMenu 프리팩의 EventSystem 자식에 붙이는 헬퍼.
/// 현재 씬에 다른 EventSystem 이 이미 있으면 자신을 모두 끌고,
/// 없으면 자신을 켜서 하나의 EventSystem 만 활성화되도록 보장한다.
/// </summary>
[RequireComponent(typeof(EventSystem))]
public class PersistentEventSystem : MonoBehaviour
{
    private EventSystem _self;

    void Awake()
    {
        _self = GetComponent<EventSystem>();
        Reconcile();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Reconcile();
    }

    void Reconcile()
    {
        bool otherActive = false;
        var all = FindObjectsOfType<EventSystem>(includeInactive: false);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != _self) { otherActive = true; break; }
        }

        bool shouldBeActive = !otherActive;
        if (_self.gameObject.activeSelf != shouldBeActive)
            _self.gameObject.SetActive(shouldBeActive);
    }
}
