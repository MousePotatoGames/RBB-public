using System;
using System.Collections.Generic;
using Game.Core;
using UnityEngine;
using UnityEngine.Pool;

namespace Game.Gameplay
{
    /// <summary>
    /// XP-001 (B1, B2, B6): the only place orbs are created, dropped or recalled.
    ///
    /// Drops hang off <see cref="PlayerDamageDealer.Killed"/>, which already carries
    /// the death position and already fires exactly once per kill — so XP-001's
    /// "적 사망 후 경험치 중복 지급 금지" holds by construction rather than by a
    /// guard nobody would remember to keep.
    /// </summary>
    public sealed class ExperienceOrbPool : MonoBehaviour
    {
        [SerializeField] private ProgressConfig config;

        [Tooltip("XP-001: 처치 이벤트의 출처. 사망 위치를 그대로 쓴다")]
        [SerializeField] private PlayerDamageDealer killSource;

        [Tooltip("오브가 끌려갈 대상")]
        [SerializeField] private Transform player;

        [Tooltip("세션이 끝나면 남은 오브를 회수한다 (B6)")]
        [SerializeField] private GameSession session;

        [Tooltip("PAS-003 (B23): 흡수 반경 배율의 출처. 없으면 배율 1")]
        [SerializeField] private PlayerProgress progress;

        private ObjectPool<ExperienceOrb> _pool;
        private readonly List<ExperienceOrb> _live = new List<ExperienceOrb>();
        private Mesh _orbMesh;
        private Material _orbMaterial;

        /// <summary>Collected(value) — one absorbed orb (B4).</summary>
        public event Action<float> Collected;

        public ProgressConfig Config { get => config; set => config = value; }
        public PlayerDamageDealer KillSource { get => killSource; set => killSource = value; }
        public Transform Player { get => player; set => player = value; }
        public GameSession Session { get => session; set => session = value; }
        public PlayerProgress Progress { get => progress; set => progress = value; }

        /// <summary>
        /// PAS-003 (B23): the magnet radius every live orb reads. Asked of the pool
        /// rather than of each orb so a hundred orbs do not each hold a reference to
        /// the player's progress.
        /// </summary>
        public float EffectiveMagnetRadius
        {
            get
            {
                float baseRadius = config != null ? config.magnetRadius : 0f;
                return baseRadius * (progress != null ? progress.Effects.MagnetMultiplier : 1f);
            }
        }

        public int LiveCount => _live.Count;

        /// <summary>Orbs ever dropped — B2 asserts this matches the kill count.</summary>
        public int DropCount { get; private set; }

        public int CollectCount { get; private set; }

        /// <summary>Objects ever instantiated. Pooling means this stays far below DropCount.</summary>
        public int TotalCreated { get; private set; }

        public ExperienceOrb LiveAt(int index) =>
            index >= 0 && index < _live.Count ? _live[index] : null;

        private void OnEnable()
        {
            if (killSource != null)
            {
                killSource.Killed += OnEnemyKilled;
            }

            if (session != null)
            {
                session.Ended += OnSessionEnded;
            }
        }

        private void OnDisable()
        {
            if (killSource != null)
            {
                killSource.Killed -= OnEnemyKilled;
            }

            if (session != null)
            {
                session.Ended -= OnSessionEnded;
            }
        }

        private void OnSessionEnded(SessionState _) => RecallAll();

        private void OnEnemyKilled(Vector3 position) =>
            Drop(position, config != null ? config.droneExperience : 1f); // B1

        /// <summary>B1: drops one orb. Returns false when the live cap is reached.</summary>
        public bool Drop(Vector3 position, float value)
        {
            int cap = config != null ? config.orbLiveCap : 64;
            if (_live.Count >= cap)
            {
                return false;
            }

            EnsurePool();
            ExperienceOrb orb = _pool.Get();
            _live.Add(orb);
            orb.Launch(position, value, player, config, this);
            DropCount++;
            return true;
        }

        /// <summary>B4: called by an orb that reached the player.</summary>
        public void Collect(ExperienceOrb orb)
        {
            if (orb == null || !_live.Remove(orb))
            {
                return; // already gone — never release into the pool twice
            }

            CollectCount++;
            float value = orb.Value;
            _pool?.Release(orb);
            Collected?.Invoke(value);
        }

        /// <summary>B6: takes the field back. Uncollected orbs award nothing.</summary>
        public void RecallAll()
        {
            for (int i = _live.Count - 1; i >= 0; i--)
            {
                _live[i].Deactivate();
                _pool?.Release(_live[i]);
            }

            _live.Clear();
        }

        private void EnsurePool()
        {
            if (_pool != null)
            {
                return;
            }

            _pool = new ObjectPool<ExperienceOrb>(
                createFunc: Create,
                actionOnGet: o => o.gameObject.SetActive(true),
                actionOnRelease: o =>
                {
                    o.Deactivate();
                    o.gameObject.SetActive(false);
                },
                actionOnDestroy: o => Destroy(o.gameObject),
                collectionCheck: true,
                defaultCapacity: 16,
                maxSize: 64);
        }

        /// <summary>
        /// B5: an orb is a mesh and nothing else — no collider is ever created, so
        /// none has to be destroyed.
        ///
        /// The other pools in this project strip the collider off a
        /// <c>CreatePrimitive</c> with <c>DestroyImmediate</c>, which is fine where
        /// they are built (FixedUpdate). Orbs are not: they are created from the
        /// kill event, which fires inside a physics contact callback, and Unity
        /// refuses immediate destruction there. Taking the mesh once and building
        /// bare objects afterwards sidesteps the whole question.
        /// </summary>
        private void EnsureTemplate()
        {
            if (_orbMesh != null)
            {
                return;
            }

            GameObject primitive = GameObject.CreatePrimitive(config != null ? config.orbShape : PrimitiveType.Sphere);
            _orbMesh = primitive.GetComponent<MeshFilter>().sharedMesh;
            _orbMaterial = primitive.GetComponent<MeshRenderer>().sharedMaterial;
            Destroy(primitive);
        }

        private ExperienceOrb Create()
        {
            TotalCreated++;
            EnsureTemplate();

            var go = new GameObject("ExperienceOrb", typeof(MeshFilter), typeof(MeshRenderer));
            go.GetComponent<MeshFilter>().sharedMesh = _orbMesh;
            go.GetComponent<MeshRenderer>().sharedMaterial = _orbMaterial;

            go.transform.SetParent(transform, false);
            go.transform.localScale = Vector3.one * (config != null ? config.orbScale : 0.22f);
            go.SetActive(false);

            return go.AddComponent<ExperienceOrb>();
        }
    }
}
