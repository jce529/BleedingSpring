/// <summary>적 AI의 상태 목록.</summary>
public enum EnemyState
{
    Idle,    // 대기 — 일정 시간 후 순찰 시작
    Patrol,  // 순찰 — 좌우로 왕복 이동
    Chase,   // 추격 — 감지 범위 내 플레이어를 따라감
    Attack,  // 공격 — 공격 범위 내 플레이어를 공격
    Hit,     // 피격 경직 — 짧은 시간 동작 중단
    Dead     // 사망 — EnemyStats가 처리(Die/Purify)
}
