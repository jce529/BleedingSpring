using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// InputSystem_Actions 에셋을 래핑해 플레이어 입력 이벤트를 외부에 노출합니다.
/// PlayerController는 이 컴포넌트의 이벤트만 구독하면 됩니다. (DIP)
/// 게임 상태가 Playing이 아닐 때 입력을 차단합니다.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class InputHandler : MonoBehaviour
{
    // ─── 이벤트 ──────────────────────────────────────────────────────────────

    /// <summary>이동 벡터 (WASD / 화살표). canceled 시 Vector2.zero 전달.</summary>
    public event Action<Vector2> OnMove;

    public event Action OnJump;
    public event Action OnDash;
    public event Action OnBasicAttack;
    public event Action OnWideSlash;
    public event Action OnProjectile;

    /// <summary>O키: 수분 단계(0→1→2→3→0) 순환 요청.</summary>
    public event Action OnWaterTierSwitch;

    // ─── 내부 ────────────────────────────────────────────────────────────────

    private InputSystem_Actions inputActions;

    // ─── 초기화 ──────────────────────────────────────────────────────────────

    private void Awake()
    {
        inputActions = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();

        inputActions.Player.Move.performed         += OnMovePerformed;
        inputActions.Player.Move.canceled          += OnMoveCanceled;
        inputActions.Player.Jump.performed         += OnJumpPerformed;
        inputActions.Player.Dash.performed         += OnDashPerformed;
        inputActions.Player.BasicAttack.performed  += OnBasicAttackPerformed;
        inputActions.Player.WideSlash.performed    += OnWideSlashPerformed;
        inputActions.Player.Projectile.performed   += OnProjectilePerformed;
        inputActions.Player.WaterTierSwitch.performed += OnWaterTierSwitchPerformed;
    }

    private void OnDisable()
    {
        inputActions.Player.Move.performed         -= OnMovePerformed;
        inputActions.Player.Move.canceled          -= OnMoveCanceled;
        inputActions.Player.Jump.performed         -= OnJumpPerformed;
        inputActions.Player.Dash.performed         -= OnDashPerformed;
        inputActions.Player.BasicAttack.performed  -= OnBasicAttackPerformed;
        inputActions.Player.WideSlash.performed    -= OnWideSlashPerformed;
        inputActions.Player.Projectile.performed   -= OnProjectilePerformed;
        inputActions.Player.WaterTierSwitch.performed -= OnWaterTierSwitchPerformed;

        inputActions.Player.Disable();
    }

    private void OnDestroy()
    {
        inputActions?.Dispose();
    }

    // ─── 핸들러 ──────────────────────────────────────────────────────────────

    private void OnMovePerformed(InputAction.CallbackContext ctx)
    {
        if (!IsGamePlaying()) return;
        OnMove?.Invoke(ctx.ReadValue<Vector2>());
    }

    private void OnMoveCanceled(InputAction.CallbackContext ctx)
    {
        OnMove?.Invoke(Vector2.zero);
    }

    private void OnJumpPerformed(InputAction.CallbackContext ctx)
    {
        if (!IsGamePlaying()) return;
        OnJump?.Invoke();
    }

    private void OnDashPerformed(InputAction.CallbackContext ctx)
    {
        if (!IsGamePlaying()) return;
        OnDash?.Invoke();
    }

    private void OnBasicAttackPerformed(InputAction.CallbackContext ctx)
    {
        if (!IsGamePlaying()) return;
        OnBasicAttack?.Invoke();
    }

    private void OnWideSlashPerformed(InputAction.CallbackContext ctx)
    {
        if (!IsGamePlaying()) return;
        OnWideSlash?.Invoke();
    }

    private void OnProjectilePerformed(InputAction.CallbackContext ctx)
    {
        if (!IsGamePlaying()) return;
        OnProjectile?.Invoke();
    }

    private void OnWaterTierSwitchPerformed(InputAction.CallbackContext ctx)
    {
        if (!IsGamePlaying()) return;
        OnWaterTierSwitch?.Invoke();
    }

    // ─── 유틸리티 ────────────────────────────────────────────────────────────

    private static bool IsGamePlaying()
    {
        return GameStateManager.Instance == null ||
               GameStateManager.Instance.CurrentState == GameStateManager.GameState.Playing;
    }
}
