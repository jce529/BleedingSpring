using UnityEngine;

/// <summary>
/// [I] 원거리 스킬에서 발사되는 투사체입니다.
/// PlayerCombat.Initialize()로 초기화합니다.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Projectile : MonoBehaviour
{
    private float       damage;
    private float       corruptionDamage;
    private bool        penetrating;   // 관통 여부 (2단계 수창)
    private LayerMask   enemyLayer;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;   // 투사체는 중력 무시
    }

    /// <param name="damage">충돌 시 데미지</param>
    /// <param name="speed">이동 속도</param>
    /// <param name="penetrating">true 이면 적을 관통</param>
    /// <param name="lifetime">소멸까지 시간(초)</param>
    /// <param name="enemyLayer">충돌 판정할 레이어</param>
    public void Initialize(float damage, float corruptionDamage, float speed, bool penetrating, float lifetime, LayerMask enemyLayer)
    {
        this.damage           = damage;
        this.corruptionDamage = corruptionDamage;
        this.penetrating      = penetrating;
        this.enemyLayer       = enemyLayer;

        rb.linearVelocity = transform.right * speed;
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 적 레이어인지 확인
        if ((enemyLayer.value & (1 << other.gameObject.layer)) == 0) return;

        other.GetComponent<IDamageable>()?.TakeDamage(damage, corruptionDamage);

        if (!penetrating)
            Destroy(gameObject);
    }
}
