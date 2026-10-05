using System;
using System.Collections.Generic;
using ImmunWar.Battle.Movement;
using ImmunWar.Battle.State;
using ImmunWar.Combat.Abilities;
using ImmunWar.Core.Config;
using ImmunWar.StatusEffects;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ImmunWar.UI
{
    /// <summary>
    /// Placed defenders: their live position (mobile melee cells walk out to fight), blocking, and the per-frame
    /// presentation — walk cycle, recoil, status overlays and the skill charge ring.
    /// </summary>
    public sealed partial class PlayableBattleView
    {
        private sealed class DefenderVisual
        {
            public DefenderState State;
            public DefenderConfig Config;
            public Image Image;
            public Animator Animator;
            public AnimatedHealthBar HealthBar;
            public Color BaseColor = Color.white;
            // World units. Home is the placement point; Position moves for mobile cells.
            public Vector2 Home;
            public Vector2 Position;
            public string ChaseId;
            public int Held;
            public bool Walking;
            public ActiveSkillSpec Skill;
            // Presentation-only state, animated every frame in AnimateDefenders.
            public Vector2 Kick;
            public float Facing = 1f;
            public float Phase;
            public float HitTime = -10f;
            public float PopTime = -10f;
            public float CastTime = -10f;
            public float SpinTime = -10f;
            public float DustTime;
            public float BlockTextTime = -10f;
            public Image SkillRing;
            public Image SkillGlow;
            public readonly StatusOverlays Overlays = new StatusOverlays();
        }

        private readonly List<EngageCandidate> _engageCandidates = new List<EngageCandidate>();

        private void RegisterDefender(DefenderState state, DefenderConfig config, Image image, Animator animator, AnimatedHealthBar healthBar, Vector2 home)
        {
            var visual = new DefenderVisual
            {
                State = state, Config = config, Image = image, Animator = animator, HealthBar = healthBar, BaseColor = image.color,
                Home = home, Position = home, Phase = _defenders.Count * 1.37f, Skill = ActiveSkills.For(config.role)
            };
            // Skill charge ring under the cell: fills while recharging and glows once the skill can be cast.
            visual.SkillGlow = RuntimeUi.Image(image.transform, "SkillReady", new Vector2(0f, -40f), new Vector2(44f, 44f), new Color(0.5f, 1f, 1f, 0.6f));
            visual.SkillGlow.sprite = GlowSprite;
            visual.SkillRing = RuntimeUi.Image(image.transform, "SkillCharge", new Vector2(0f, -40f), new Vector2(28f, 28f), Color.white);
            visual.SkillRing.sprite = RingSprite;
            visual.SkillRing.type = Image.Type.Filled;
            visual.SkillRing.fillMethod = Image.FillMethod.Radial360;
            visual.SkillRing.fillOrigin = (int)Image.Origin360.Top;
            image.raycastTarget = true;
            var id = state.InstanceId;
            image.gameObject.AddComponent<DefenderClick>().Clicked += () => ActivateSkill(id);
            _defenders[id] = visual;
        }

        /// <summary>Pushes the sprite off its spot for a moment (attack lunge or recoil); it eases back every frame.</summary>
        private static void Kick(DefenderVisual visual, Vector2 offset)
        {
            visual.Kick += offset;
            visual.PopTime = Time.time;
        }

        // ================= Movement and blocking =================

        private void MoveMobileDefenders()
        {
            _engageCandidates.Clear();
            foreach (var enemy in _enemies)
                if (!enemy.State.IsTerminal) _engageCandidates.Add(new EngageCandidate(enemy.State.InstanceId, enemy.Follower.Position, enemy.State.BlockedById));
            foreach (var visual in _defenders.Values)
            {
                visual.Walking = false;
                if (!visual.Config.mobile || visual.State.Health <= 0f) continue;
                var effects = visual.State.Effects;
                // Ice, paralysis, a clot shell or a full stomach keep the cell where it stands.
                if (CombatStatus.IsDisabled(effects) || CombatStatus.Has(effects, StatusIds.Shield) || CombatStatus.Has(effects, StatusIds.Digesting)) continue;
                visual.ChaseId = MeleeEngagement.ChooseTarget(visual.State.InstanceId, visual.Home, visual.Config.engageRadius, visual.ChaseId, _engageCandidates);
                Vector2? target = null;
                if (visual.ChaseId != null)
                    foreach (var candidate in _engageCandidates)
                        if (candidate.Id == visual.ChaseId) { target = candidate.Position; break; }
                var goal = MeleeEngagement.Goal(visual.Home, visual.Position, target);
                var next = MeleeEngagement.Step(visual.Position, goal, visual.Config.moveSpeed * CombatStatus.MoveMultiplier(effects), 1f / 30f);
                var step = next - visual.Position;
                visual.Walking = step.sqrMagnitude > 1e-6f;
                if (Mathf.Abs(step.x) > 0.002f) visual.Facing = step.x < 0f ? -1f : 1f;
                visual.Position = next;
            }
        }

        /// <summary>Nearest living, able defender with room to hold one more enemy.</summary>
        private DefenderVisual FindBlocker(EnemyVisual enemy)
        {
            if (_clock < enemy.IgnoreBlockUntil) return null;
            DefenderVisual best = null;
            var bestDistance = BlockRadius;
            foreach (var visual in _defenders.Values)
            {
                if (visual.State.Health <= 0f || CombatStatus.IsDisabled(visual.State.Effects)) continue;
                if (!MeleeEngagement.HasCapacity(visual.Held, visual.Config.blockCapacity)) continue;
                var distance = Vector2.Distance(visual.Position, enemy.Follower.Position);
                if (distance > bestDistance) continue;
                best = visual;
                bestDistance = distance;
            }
            return best;
        }

        // ================= Presentation =================

        private void AnimateDefenders(float deltaTime)
        {
            var now = Time.time;
            var settle = 1f - Mathf.Exp(-14f * deltaTime);
            foreach (var visual in _defenders.Values)
            {
                if (!visual.Image) continue;
                var effects = visual.State.Effects;
                var still = CombatStatus.IsDisabled(effects) || CombatStatus.Has(effects, StatusIds.Shield);
                visual.Kick = Vector2.Lerp(visual.Kick, Vector2.zero, settle);
                var walking = visual.Walking && deltaTime > 0f;
                // Walk cycle: a springy hop with a side-to-side tilt; idle cells breathe gently.
                var stride = now * 11f + visual.Phase;
                var hop = walking ? Mathf.Abs(Mathf.Sin(stride)) * 9f : 0f;
                var bob = still ? 0f : Mathf.Sin(now * 2.4f + visual.Phase) * 2f;
                var shiver = CombatStatus.Has(effects, StatusIds.Freeze) && CombatStatus.Remaining(effects, StatusIds.Freeze) < 15
                    ? new Vector2(Mathf.Sin(now * 70f) * 3f, 0f) : Vector2.zero;
                var rect = visual.Image.rectTransform;
                rect.anchoredPosition = RuntimeUi.Point(visual.Position) + visual.Kick + new Vector2(0f, hop + bob) + shiver;

                var pop = Mathf.Clamp01(1f - (now - visual.PopTime) / 0.16f);
                var cast = Mathf.Clamp01(1f - (now - visual.CastTime) / 0.45f);
                var swell = 1f + 0.12f * Mathf.Sin(pop * Mathf.PI) + 0.3f * Mathf.Sin(cast * Mathf.PI);
                var squash = walking ? Mathf.Sin(stride * 2f) * 0.05f : 0f;
                var digest = CombatStatus.Has(effects, StatusIds.Digesting) ? Mathf.Sin(now * 9f) * 0.08f : 0f;
                rect.localScale = new Vector3(visual.Facing * swell * (1f + squash + digest), swell * (1f - squash - digest * 0.7f), 1f);
                var spin = Mathf.Clamp01((now - visual.SpinTime) / 0.6f);
                var spinAngle = spin < 1f ? 720f * (1f - (1f - spin) * (1f - spin)) : 0f;
                rect.localRotation = Quaternion.Euler(0f, 0f, (walking ? Mathf.Sin(stride) * 7f : 0f) + spinAngle);

                var hit = Mathf.Clamp01(1f - (now - visual.HitTime) / 0.2f);
                visual.Image.color = Color.Lerp(StatusTint(visual.BaseColor, effects, now, visual.Phase), new Color(1f, 0.3f, 0.3f, visual.BaseColor.a), hit);
                if (visual.Animator) visual.Animator.speed = AnimatorSpeed(effects);
                // Counter-mirror the health bar and skill ring so they never read backwards.
                if (visual.HealthBar) visual.HealthBar.transform.localScale = new Vector3(visual.Facing, 1f, 1f);
                if (walking && now - visual.DustTime > 0.16f)
                {
                    visual.DustTime = now;
                    Mote(rect.anchoredPosition + new Vector2(-visual.Facing * 18f, -30f), new Vector2(-visual.Facing * 30f, 20f), new Color(1f, 0.85f, 0.85f, 0.5f), 16f, 0.35f);
                }
                DrawStatus(visual.Overlays, visual.Image, 80f, effects, now, visual.Phase);
                DrawSkillRing(visual, now);
            }
        }

        private void DrawSkillRing(DefenderVisual visual, float now)
        {
            if (!visual.SkillRing) return;
            var id = visual.State.InstanceId;
            var progress = _skillCooldowns.Progress(id, _clock);
            var check = _skillCooldowns.Check(id, visual.Skill, _clock, _battle.State.Economy.Atp, CombatStatus.IsDisabled(visual.State.Effects));
            var ready = check == SkillCheck.Ready;
            var pulse = 0.5f + 0.5f * Mathf.Sin(now * 6f + visual.Phase);
            visual.SkillRing.fillAmount = progress;
            visual.SkillRing.color = ready ? Color.Lerp(new Color(0.4f, 1f, 1f), Color.white, pulse)
                : check == SkillCheck.NotEnoughAtp ? new Color(1f, 0.45f, 0.4f, 0.8f)
                : new Color(0.6f, 0.7f, 0.85f, 0.75f);
            visual.SkillRing.transform.localScale = new Vector3(visual.Facing, 1f, 1f);
            visual.SkillGlow.gameObject.SetActive(ready);
            if (ready) visual.SkillGlow.transform.localScale = Vector3.one * (0.85f + 0.3f * pulse);
        }

        /// <summary>Clicking a placed cell casts its skill.</summary>
        private sealed class DefenderClick : MonoBehaviour, IPointerClickHandler
        {
            public event Action Clicked;
            public void OnPointerClick(PointerEventData eventData) => Clicked?.Invoke();
        }
    }
}
