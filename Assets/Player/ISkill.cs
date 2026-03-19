/// <summary>
/// 모든 플레이어 스킬이 구현해야 하는 인터페이스. (ISP — 스킬만을 위한 작은 계약)
/// PlayerController는 이 인터페이스를 통해 스킬을 호출합니다. (DIP)
/// </summary>
public interface ISkill
{
    bool IsOnCooldown { get; }
    int  Stage        { get; }

    /// <summary>PlayerController.Start()에서 호출합니다.</summary>
    void Initialize(IPlayerContext context);

    /// <summary>스킬 단계를 0~3 범위로 설정합니다.</summary>
    void SetStage(int stage);

    /// <summary>
    /// 스킬 사용을 시도합니다.
    /// 쿨다운 중이거나 선행 조건 미충족 시 아무 일도 일어나지 않습니다.
    /// </summary>
    void TryUse();
}
