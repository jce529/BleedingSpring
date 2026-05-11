using System.Collections;

/// <summary>
/// 보스의 각 페이즈별 행동을 정의하는 인터페이스.
/// </summary>
public interface IBossPhase
{
    /// <summary>해당 페이즈가 시작될 때 호출됩니다.</summary>
    void OnPhaseEnter();

    /// <summary>해당 페이즈의 AI 업데이트 로직.</summary>
    void OnPhaseUpdate();

    /// <summary>해당 페이즈가 종료될 때 호출됩니다.</summary>
    void OnPhaseExit();
}
