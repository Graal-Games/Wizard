using UnityEngine;

public interface IMovementEffects
{
    void EnterCastMovementSlow(float newMoveSpeedValue, float animationMultiplier);
    void ExitCastMovementSlow();
    void ApplyMovementSlow(float slowAmount, float duration);
    void ApplyKnockback(Vector3 force);
    void StopMovement();
    void ResumeMovement();
    float GetCurrentMoveSpeed();
    bool IsSlowed { get; }
} 