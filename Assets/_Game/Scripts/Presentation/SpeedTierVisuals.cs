using Game.Core;
using Game.Gameplay;
using UnityEngine;

namespace Game.Presentation
{
    /// <summary>
    /// SPD-001 minimum readable feedback (F04 scope): core emission colour and a
    /// trail whose length grows with the tier. Reacts to tier-change events only
    /// and writes through a MaterialPropertyBlock, so no material instances are
    /// created per frame (B7, 기획서 14.5).
    /// Full VFX (dust, sparks, arcs, distortion) is P08.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public sealed class SpeedTierVisuals : MonoBehaviour
    {
        [SerializeField] private SpeedTierTracker tracker;
        [SerializeField] private TrailRenderer trail;

        [Header("Tier colours — TEMPORARY (기획서 10.4 팔레트)")]
        [SerializeField] private Color lowColor = new Color(0.85f, 0.85f, 0.9f);
        [SerializeField] private Color midColor = new Color(0.2f, 0.8f, 1f);
        [SerializeField] private Color highColor = new Color(0.4f, 1f, 1f);
        [SerializeField] private Color rumbleColor = new Color(1f, 1f, 0.9f);

        [Header("Trail duration per tier — TEMPORARY")]
        [SerializeField] private float lowTrailTime;
        [SerializeField] private float midTrailTime = 0.25f;
        [SerializeField] private float highTrailTime = 0.4f;
        [SerializeField] private float rumbleTrailTime = 0.6f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private Renderer _renderer;
        private MaterialPropertyBlock _block;

        public SpeedTierTracker Tracker
        {
            get => tracker;
            set => tracker = value;
        }

        public TrailRenderer Trail
        {
            get => trail;
            set => trail = value;
        }

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _block = new MaterialPropertyBlock();
            if (tracker == null)
            {
                tracker = GetComponentInParent<SpeedTierTracker>();
            }
        }

        private void OnEnable()
        {
            if (tracker != null)
            {
                tracker.TierChanged += Apply;
                Apply(tracker.CurrentTier);
            }
        }

        private void OnDisable()
        {
            if (tracker != null)
            {
                tracker.TierChanged -= Apply;
            }
        }

        /// <summary>Applies the visual state for a tier. Public so tests can drive it directly.</summary>
        public void Apply(SpeedTier tier)
        {
            Color color = ColorFor(tier);

            _renderer.GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, color);
            _block.SetColor(EmissionColorId, color * EmissionStrength(tier));
            _renderer.SetPropertyBlock(_block);

            if (trail == null)
            {
                return;
            }

            float time = TrailTimeFor(tier);
            trail.time = time;
            trail.emitting = time > 0f;
            trail.startColor = color;
            trail.endColor = new Color(color.r, color.g, color.b, 0f);
        }

        private Color ColorFor(SpeedTier tier)
        {
            switch (tier)
            {
                case SpeedTier.Mid: return midColor;
                case SpeedTier.High: return highColor;
                case SpeedTier.Rumble: return rumbleColor;
                default: return lowColor;
            }
        }

        private float TrailTimeFor(SpeedTier tier)
        {
            switch (tier)
            {
                case SpeedTier.Mid: return midTrailTime;
                case SpeedTier.High: return highTrailTime;
                case SpeedTier.Rumble: return rumbleTrailTime;
                default: return lowTrailTime;
            }
        }

        private static float EmissionStrength(SpeedTier tier)
        {
            switch (tier)
            {
                case SpeedTier.Mid: return 1.5f;
                case SpeedTier.High: return 3f;
                case SpeedTier.Rumble: return 6f;
                default: return 0.3f;
            }
        }
    }
}
