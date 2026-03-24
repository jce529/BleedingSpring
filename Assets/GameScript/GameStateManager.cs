using System;
using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    public enum GameState
    {
        Playing,
        Paused,
        Inventory,
        Loading,
        GameClear,
        GameOver       // 체력 0 또는 오염도 초과 즉사 시 전환
    }

    // 싱글톤 패턴
    // 프로그램 실행 중 하나의 인스턴스만 생성하여 사용하며 그 인스턴스를 공유하는 디자인 패턴입니다.

    // 게임매니저 같은 클래스는 전역적으로 사용되어 메모리를 효율적으로 관리하기 위하여 하나의 인스턴스만 유지합니다.

    // 싱글톤 인스턴스
    public static GameStateManager Instance { get; private set; }

    // 현재 게임 상태를 외부에서 읽을 수 있게 함
    public GameState CurrentState { get; private set; } = GameState.Playing;

    // 상태 변경 시 이벤트를 발생시켜 다른 시스템이 반응하도록 함
    public event Action<GameState> OnGameStateChange;

    private void Awake()
    {
        // 싱글톤 초기화
        if (Instance == null)
        {
            Instance = this;
            // 씬이 바뀌어도 파괴되지 않고 유지 (옵션)
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 상태를 변경하는 메인 함수
    public void SetState(GameState newState)
    {
        if (CurrentState == newState) return;

        CurrentState = newState;

        switch (newState)
        {
            case GameState.Paused:
            case GameState.Inventory:
            case GameState.GameClear:
                Time.timeScale = 0f;
                break;

            case GameState.GameOver:
                // 즉사 처리: 시간 정지 후 게임 오버 UI 표시 신호 발송
                Time.timeScale = 0f;
                break;

            case GameState.Playing:
                Time.timeScale = 1f;
                break;

            // Loading은 timeScale 유지 (씬 전환 중 조작 필요할 수 있음)
        }

        // 구독자들에게 상태가 변경되었다고 알림
        OnGameStateChange?.Invoke(newState);
    }
}
