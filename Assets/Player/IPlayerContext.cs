using UnityEngine;

/// <summary>
/// 스킬과 이동 컴포넌트가 플레이어 정보에 접근하기 위한 인터페이스.
/// 구체 클래스(PlayerController)에 직접 의존하지 않도록 분리합니다. (DIP)
/// </summary>
public interface IPlayerContext
{
    bool        FacingRight  { get; }
    Rigidbody2D Rigidbody    { get; }
    PlayerStats Stats        { get; }
    PlayerState CurrentState { get; }

    void ChangeState(PlayerState newState);

    /// <summary>대쉬 무적 프레임 제어에 사용됩니다.</summary>
    void SetInvincible(bool value);
}
