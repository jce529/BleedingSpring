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
        GameClear
    }
    //싱글톤 패턴
    //디자인 패턴 중 하나로써 하나의 객체만 생성하여 사용하여 그 객체만을 사용하는 디자인 패턴입니다.

    //기능만을 가진 클래스를 여러군데에서 만드는 것이 메모리적인 손해이기 때문에 하나만 사용

    // 싱글톤 인스턴스
    public static GameStateManager Instance { get; private set; }

    // 현재 게임 상태를 외부에서 읽을 수 있게 함
    public GameState CurrentState { get; private set; } = GameState.Playing;

    // 상태 변경 시 이벤트를 발생시켜 다른 시스템이 구독하도록 함
    public event Action<GameState> OnGameStateChange;

    private void Awake()
    {
        // 싱글톤 초기화
        if (Instance == null)
        {
            Instance = this;
            // 씬이 바뀌어도 파괴되지 않게 설정 (옵션)
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 상태를 변경하는 핵심 함수
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

            case GameState.Playing:
                Time.timeScale = 1f;
                break;
        }

        // 2. 구독자들에게 상태가 변경되었음을 알림
        OnGameStateChange?.Invoke(newState);
    }
}