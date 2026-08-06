using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>
    /// F13 TEMPORARY tuning for experience, orbs and level-ups.
    /// XP-002's curve lives here rather than in code so the playtest can move it
    /// without a recompile — the 90초 세션에 몇 번 오르는가 is the open question.
    /// </summary>
    [CreateAssetMenu(menuName = "RumbleBall/Progress Config", fileName = "ProgressConfig")]
    public sealed class ProgressConfig : ScriptableObject
    {
        [Header("XP-002 — 곡선 (TEMPORARY)")]
        [Tooltip("드론 1기 처치로 얻는 경험치")]
        [Min(0f)] public float droneExperience = 1f;

        [Tooltip("누적 요구량. XP-002의 Lv2=15, Lv3=35, Lv4=65, Lv5=105")]
        public float[] levelThresholds = { 15f, 35f, 65f, 105f };

        [Header("XP-001 — 오브 (TEMPORARY)")]
        [Tooltip("이 거리 안에 들어오면 플레이어에게 끌려온다")]
        [Min(0f)] public float magnetRadius = 2.5f;

        [Tooltip("끌려오는 속도. 캐논 투사체(14)보다 느리게")]
        [Min(0f)] public float orbSpeed = 9f;

        [Tooltip("이 거리 안이면 흡수된다")]
        [Min(0.01f)] public float absorbDistance = 0.6f;

        [Tooltip("동시에 살아있을 수 있는 오브 수 (기획서 15장 풀링)")]
        [Min(1)] public int orbLiveCap = 64;

        // LVL-001의 "0.2초 이내 재개"에는 노브를 두지 않습니다 — 선택 즉시 재개하므로
        // 지연이 0이고, 재개를 느리게 만드는 설정은 규칙을 위반하는 방향뿐입니다.

        [Header("Greybox (P07 replaces this)")]
        public PrimitiveType orbShape = PrimitiveType.Sphere;
        [Min(0.01f)] public float orbScale = 0.22f;
    }
}
