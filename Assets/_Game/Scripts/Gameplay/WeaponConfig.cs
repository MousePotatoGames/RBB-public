using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>F08 TEMPORARY tuning for capsules and surface attachment (WPN-001 계열).</summary>
    [CreateAssetMenu(menuName = "RumbleBall/Weapon Config", fileName = "WeaponConfig")]
    public sealed class WeaponConfig : ScriptableObject
    {
        [Header("WPN-003 — slots")]
        [Tooltip("동시에 장착 가능한 무기 수. WPN-005 추첨 수이기도 하다")]
        [Range(1, 3)] public int maxWeapons = 3;

        [Header("WPN-001a — attachment (TEMPORARY)")]
        [Tooltip("무기 사이 최소 각도. 접촉 지점 그대로 붙이되 이 각도보다 가까우면 밀어낸다")]
        [Range(0f, 109f)] public float minSeparationDegrees = 50f;

        [Tooltip("공 중심에서 무기까지의 거리. 공 반지름(0.5)보다 살짝 바깥")]
        [Min(0.1f)] public float attachRadius = 0.55f;

        [Min(0.05f)] public float weaponScale = 0.28f;

        [Header("WPN-005 / WPN-006 — capsules (TEMPORARY)")]
        [Tooltip("FIRST_PLAYABLE 세션 타임라인. 90초 세션이므로 기획서의 0:15/1:00/2:00 대신 이 값을 쓴다")]
        public float[] spawnTimes = { 10f, 35f, 60f };

        [Min(1f)] public float spawnDistanceMin = 12f;
        [Min(1f)] public float spawnDistanceMax = 20f;

        [Tooltip("캡슐 접촉 판정 반경")]
        [Min(0.1f)] public float pickupRadius = 1.1f;

        [Header("WPN-006 — beacon (TEMPORARY)")]
        [Min(1f)] public float beaconHeight = 8f;
        [Min(0.01f)] public float beaconWidth = 0.18f;
    }
}
