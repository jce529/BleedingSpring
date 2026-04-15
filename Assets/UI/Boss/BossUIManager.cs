using System.Collections;
using UnityEngine;

/// <summary>
/// 보스 UI(HUD)의 전체 생명주기를 관리하는 싱글톤 클래스.
/// 보스 방 진입 시 UI 표시(Fade In) 및 보스 사망 시 UI 제거(Fade Out)를 총괄합니다.
/// </summary>
public class BossUIManager : MonoBehaviour
{
    public static BossUIManager Instance { get; private set; }

    [Header("UI 구성 요소")]
    [SerializeField] private CanvasGroup bossUICanvasGroup;
    [SerializeField] private BossHUDBar bossHUDBar;
    
    [Header("설정")]
    [SerializeField] private float fadeDuration = 0.5f;

    private Coroutine _fadeCoroutine;
    private BossStats _activeBoss;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Persistent singleton is not strictly required but common for UI managers
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 초기 상태: 숨김
        if (bossUICanvasGroup != null)
        {
            bossUICanvasGroup.alpha = 0f;
            bossUICanvasGroup.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 보스 HUD를 화면에 표시하고 해당 보스 데이터를 바인딩합니다.
    /// </summary>
    /// <param name="boss">표시할 보스의 스탯 데이터</param>
    public void ShowBossUI(BossStats boss)
    {
        if (boss == null) return;
        
        _activeBoss = boss;
        
        // 보스 사망 이벤트 구독 (자동 숨김용)
        _activeBoss.OnDeath += HandleBossDeath;

        if (bossHUDBar != null)
        {
            bossHUDBar.Bind(_activeBoss);
        }

        StartFade(1f, true);
    }

    /// <summary>
    /// 보스 HUD를 화면에서 서서히 제거합니다.
    /// </summary>
    public void HideBossUI()
    {
        if (_activeBoss != null)
        {
            _activeBoss.OnDeath -= HandleBossDeath;
            _activeBoss = null;
        }

        // bossHUDBar.Unbind()는 FadeOut 완료 시점 혹은 보스 사망 시 호출됨
        StartFade(0f, false);
    }

    private void HandleBossDeath()
    {
        // 보스가 죽으면 자동으로 UI를 숨깁니다.
        // BossHUDBar 내부적으로도 HandleBossDeath가 있지만, 매니저가 흐름을 제어합니다.
        HideBossUI();
    }

    private void StartFade(float targetAlpha, bool show)
    {
        if (_fadeCoroutine != null)
            StopCoroutine(_fadeCoroutine);

        _fadeCoroutine = StartCoroutine(FadeRoutine(targetAlpha, show));
    }

    private IEnumerator FadeRoutine(float targetAlpha, bool show)
    {
        if (bossUICanvasGroup == null) yield break;

        if (show)
        {
            bossUICanvasGroup.gameObject.SetActive(true);
        }

        float startAlpha = bossUICanvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            bossUICanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
            yield return null;
        }

        bossUICanvasGroup.alpha = targetAlpha;

        if (!show && targetAlpha <= 0f)
        {
            bossUICanvasGroup.gameObject.SetActive(false);
            if (bossHUDBar != null) bossHUDBar.Unbind();
        }
    }
}
