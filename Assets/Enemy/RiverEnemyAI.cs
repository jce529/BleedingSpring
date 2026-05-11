using System.Collections;
using UnityEngine;

/// <summary>
/// '강(River)' 타입 엘리트 적 AI.
/// 일반 적보다 강력하며, 2개 이상의 고유 공격 패턴을 가집니다.
/// 정화 시 특정 NPC 역할을 부여받습니다.
/// </summary>
public class RiverEnemyAI : EnemyAI
{
    [Header("[River] 패턴 발동 설정")]
    [Range(0f, 1f)]
    [SerializeField] private float specialPatternChance = 0.4f;
    [SerializeField] private float patternCooldown = 3f;

    [Header("[River] 패턴 1: 물의 파동 (범위)")]
    [SerializeField] private float waveRadius = 4f;
    [SerializeField] private float waveDamage = 25f;
    [SerializeField] private GameObject waveEffectPrefab;

    [Header("[River] 패턴 2: 수압 돌진 (이동)")]
    [SerializeField] private float dashForce = 15f;
    [SerializeField] private float dashDuration = 0.4f;
    [SerializeField] private float dashDamage = 35f;

    private float patternTimer;
    private bool isExecutingPattern;

    protected override void OnInitialize()
    {
        patternTimer = patternCooldown;
    }

    protected override void UpdateAttack()
    {
        if (isExecutingPattern) return;

        patternTimer += Time.deltaTime;

        // 패턴 쿨타임이 찼을 때 일정 확률로 특수 패턴 실행
        if (patternTimer >= patternCooldown && Random.value < specialPatternChance)
        {
            patternTimer = 0f;
            StartCoroutine(RandomPatternCoroutine());
            return;
        }

        // 특수 패턴이 아니면 기존 일반 공격 로직 수행
        base.UpdateAttack();
    }

    private IEnumerator RandomPatternCoroutine()
    {
        isExecutingPattern = true;
        
        // 0: 물의 파동, 1: 수압 돌진
        int patternIdx = Random.Range(0, 2);
        
        if (patternIdx == 0)
            yield return StartCoroutine(ExecuteWaveAttack());
        else
            yield return StartCoroutine(ExecuteDashAttack());

        isExecutingPattern = false;
    }

    public IEnumerator ExecuteWaveAttack()
    {
        Debug.Log($"[River] 패턴 1: 물의 파동 발동!");
        // 공격 전 멈춤
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = Vector2.zero;

        // 예비 동작 (잠시 대기)
        yield return new WaitForSeconds(0.5f);

        // 애니메이션 및 이펙트 처리
        if (waveEffectPrefab != null)
            Instantiate(waveEffectPrefab, transform.position, Quaternion.identity);

        // 범위 내 플레이어 타격
        var hits = Physics2D.OverlapCircleAll(transform.position, waveRadius, LayerMask.GetMask("Player"));
        foreach (var h in hits)
        {
            h.GetComponent<IDamageable>()?.TakeDamage(waveDamage, waveDamage * 0.5f);
        }
        yield return new WaitForSeconds(0.5f);
    }

    public IEnumerator ExecuteDashAttack()
    {
        Debug.Log($"[River] 패턴 2: 수압 돌진 발동!");
        if (PlayerController.Instance == null) yield break;

        float dir = PlayerController.Instance.transform.position.x > transform.position.x ? 1f : -1f;
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        
        // 돌진 전 예비 동작
        rb.linearVelocity = Vector2.zero;
        yield return new WaitForSeconds(0.4f);

        float elapsed = 0f;
        while (elapsed < dashDuration)
        {
            rb.linearVelocity = new Vector2(dir * dashForce, 0f);
            elapsed += Time.deltaTime;

            // 돌진 중 충돌 타격 (간단한 OverlapCircle)
            var hits = Physics2D.OverlapCircleAll(transform.position, 1f, LayerMask.GetMask("Player"));
            foreach (var h in hits)
            {
                h.GetComponent<IDamageable>()?.TakeDamage(dashDamage, dashDamage * 0.3f);
            }
            yield return null;
        }
        rb.linearVelocity = Vector2.zero;
        yield return new WaitForSeconds(0.3f);
    }
}
