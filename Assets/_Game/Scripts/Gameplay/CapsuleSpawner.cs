using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>
    /// WPN-005 / WPN-006: drops one capsule per drawn weapon on the session
    /// timeline. The draw takes slot-count weapons out of the whole pool, so once
    /// there are more weapon kinds than slots the *set* differs between runs.
    /// Capsules are never recycled — there are at most three per session.
    /// </summary>
    public sealed class CapsuleSpawner : MonoBehaviour
    {
        [SerializeField] private WeaponConfig config;
        [SerializeField] private WeaponSlots player;
        [SerializeField] private GameSession session;

        [Tooltip("WPN-005: 이 목록에서 슬롯 수만큼 무중복 추첨한다. 새 무기는 여기에 에셋만 추가하면 된다")]
        [SerializeField] private List<WeaponDefinition> pool = new List<WeaponDefinition>();

        private WeaponDefinition[] _draw;
        private int _spawned;
        private float _elapsed;
        private readonly List<WeaponCapsule> _live = new List<WeaponCapsule>();

        public WeaponConfig Config { get => config; set => config = value; }
        public WeaponSlots Player { get => player; set => player = value; }
        public GameSession Session { get => session; set => session = value; }
        public List<WeaponDefinition> Pool { get => pool; set => pool = value; }

        public int SpawnedCount => _spawned;
        public int LiveCount => _live.Count;
        public IReadOnlyList<WeaponCapsule> Live => _live;

        /// <summary>The weapons drawn for this run (WPN-005).</summary>
        public WeaponDefinition[] Draw => _draw;

        private void Awake()
        {
            BuildDraw();
        }

        /// <summary>A new draw every run. A scene reload (F07 restart) genuinely reshuffles.</summary>
        private void BuildDraw()
        {
            int slots = config != null ? config.maxWeapons : 3;
            int[] picked = WeaponDrawLogic.DrawIndices(pool.Count, slots, Random.Range(int.MinValue, int.MaxValue));

            _draw = new WeaponDefinition[picked.Length];
            for (int i = 0; i < picked.Length; i++)
            {
                _draw[i] = pool[picked[i]];
            }
        }

        private void Update()
        {
            if (config == null || player == null || _draw == null)
            {
                return;
            }

            if (session != null && !session.IsRunning)
            {
                return;
            }

            _elapsed += Time.deltaTime;

            float[] times = config.spawnTimes;
            while (_spawned < _draw.Length && _spawned < times.Length && _elapsed >= times[_spawned])
            {
                Spawn(_draw[_spawned]);
                _spawned++;
            }
        }

        /// <summary>Spawns one capsule on a ring around the player.</summary>
        public void Spawn(WeaponDefinition definition)
        {
            if (definition == null)
            {
                return;
            }

            float angle = Random.Range(0f, Mathf.PI * 2f);
            float distance = Random.Range(config.spawnDistanceMin, config.spawnDistanceMax);
            Vector3 origin = player.transform.position;
            var position = new Vector3(
                origin.x + Mathf.Cos(angle) * distance,
                origin.y,
                origin.z + Mathf.Sin(angle) * distance);

            var go = new GameObject("Capsule");
            go.transform.SetParent(transform, false);
            go.transform.position = position;

            var capsule = go.AddComponent<WeaponCapsule>();
            capsule.Initialise(definition, player, config);
            capsule.Claimed += OnClaimed;
            _live.Add(capsule);
        }

        private void OnClaimed(WeaponCapsule capsule)
        {
            capsule.Claimed -= OnClaimed;
            _live.Remove(capsule);
        }
    }
}
