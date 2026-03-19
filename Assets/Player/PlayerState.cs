/// <summary>
/// 플레이어 상태 열거형.
/// PlayerController, IPlayerContext, SkillBase 등 여러 파일에서 공유합니다.
/// </summary>
public enum PlayerState
{
    Idle,
    Moving,
    Jumping,
    Falling,
    Dashing,
    Attacking,
    Dead
}
