using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>
    /// F07 TEMPORARY session tuning (LOSE-001). F13's 90-second victory timer
    /// (FP-001) belongs here too — the seat is reserved, not filled.
    /// </summary>
    [CreateAssetMenu(menuName = "RumbleBall/Session Config", fileName = "SessionConfig")]
    public sealed class SessionConfig : ScriptableObject
    {
        [Header("LOSE-001 — result screen (TEMPORARY)")]
        [Tooltip("사망 후 결과 화면이 뜨기까지의 지연. 게임 시간이 멈춘 뒤이므로 실시간으로 잰다")]
        [Min(0f)] public float resultDelaySeconds = 0.6f;
    }
}
