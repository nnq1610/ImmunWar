using System.Collections;
using System.Collections.Generic;
using ImmunWar.Battle.State;
using ImmunWar.Combat;
using ImmunWar.Combat.Abilities;
using ImmunWar.Core.Config;
using ImmunWar.StatusEffects;
using UnityEngine;
using UnityEngine.UI;

namespace ImmunWar.UI
{
    /// <summary>Tap-to-cast defender skills (see <see cref="ActiveSkills"/>) and their animations.</summary>
    public sealed partial class PlayableBattleView
    {
        private readonly SkillCooldowns _skillCooldowns = new SkillCooldowns();

        /// <summary>Casts the skill of a placed defender. Returns false (with a status hint) when it cannot be cast.</summary>
        public bool ActivateSkill(string defenderId)
        {
            if (!_battle || _battle.State.Phase != BattlePhase.Running || _wave == null)
            {
                SetStatus("Skills can be used while a wave is in progress.");
                _audio?.Play(_art ? _art.uiError : null);
                return false;
            }
            if (!_defenders.TryGetValue(defenderId, out var visual)) return false;
            var spec = visual.Skill;
            var check = _skillCooldowns.Check(defenderId, spec, _clock, _battle.State.Economy.Atp, CombatStatus.IsDisabled(visual.State.Effects));
            if (check != SkillCheck.Ready)
            {
                SetStatus(check switch
                {
                    SkillCheck.CoolingDown => spec.Name + " is recharging (" + (_skillCooldowns.RemainingTicks(defenderId, _clock) / 30f).ToString("0.0") + "s).",
                    SkillCheck.NotEnoughAtp => spec.Name + " needs " + spec.AtpCost + " ATP.",
                    SkillCheck.Disabled => "This cell is frozen or paralyzed and cannot act.",
                    _ => "This cell has no skill."
                });
                _audio?.Play(_art ? _art.uiError : null);
                return false;
            }
            var cast = spec.Kind switch
            {
                ActiveSkillKind.Engulf => CastEngulf(visual),
                ActiveSkillKind.FeverRush => CastFeverRush(visual),
                ActiveSkillKind.AntibodyFreeze => CastAntibodyFreeze(visual),
                ActiveSkillKind.Execute => CastExecute(visual),
                ActiveSkillKind.ClotShield => CastClotShield(visual),
                ActiveSkillKind.AtpSurge => CastAtpSurge(visual),
                _ => false
            };
            if (!cast) _audio?.Play(_art ? _art.uiError : null);
            RefreshStats();
            return cast;
        }

        /// <summary>Pays for the skill and starts its cooldown once a cast has a valid target.</summary>
        private void Commit(DefenderVisual visual, Color color)
        {
            _battle.State.Economy.TrySpend(visual.Skill.AtpCost);
            _skillCooldowns.Start(visual.State.InstanceId, visual.Skill, _clock);
            visual.CastTime = Time.time;
            var at = RuntimeUi.Point(visual.Position);
            FloatText(at + new Vector2(0f, 110f), visual.Skill.Name + "!", color, 26);
            StartCoroutine(Ring(at, color, 40f, 190f, 0.35f));
            SetStatus(visual.Skill.Name + "!");
        }

        // ---------- Macrophage: Engulf ----------

        private bool CastEngulf(DefenderVisual visual)
        {
            EnemyVisual prey = null;
            foreach (var enemy in _enemies)
            {
                if (enemy.State.IsTerminal || enemy.Config.skill == EnemySkill.CytokineStorm) continue;
                if (Vector2.Distance(visual.Position, enemy.Follower.Position) > BlockRadius) continue;
                if (enemy.State.Health > enemy.Config.maxHealth * ActiveSkills.EngulfHealthFraction) continue;
                if (prey == null || enemy.State.Health < prey.State.Health) prey = enemy;
            }
            if (prey == null)
            {
                SetStatus("Engulf needs a weakened enemy (35% HP or less) right next to the Macrophage.");
                return false;
            }
            var green = new Color(0.35f, 1f, 0.75f);
            Commit(visual, green);
            var from = RuntimeUi.Point(visual.Position);
            var to = prey.BasePoint;
            var direction = (to - from).sqrMagnitude > 0.01f ? (to - from).normalized : Vector2.right;
            if (Mathf.Abs(direction.x) > 0.2f) visual.Facing = direction.x < 0f ? -1f : 1f;
            Kick(visual, direction * 30f);
            // Two pseudopods reach around the prey from either side.
            StartCoroutine(Lash(from, to, green));
            StartCoroutine(Lash(to, from, new Color(0.2f, 0.85f, 0.6f)));
            CombatStatus.Apply(_statusSystem, visual.State.Effects, CombatStatus.Digesting, visual.State.InstanceId);
            HealDefender(visual, visual.Config.maxHealth * 0.1f);
            DamageResolver.Apply(prey.State, prey.State.Health);
            var index = _enemies.IndexOf(prey);
            if (index >= 0) KillEnemy(index, visual);
            _audio?.Play(_art ? _art.heavyAttack : null, 0.9f, 0.1f);
            return true;
        }

        /// <summary>The engulfed enemy is dragged in, spinning and shrinking, then dissolves inside the macrophage.</summary>
        private IEnumerator Swallowed(Image image, DefenderVisual eater)
        {
            if (!image) yield break;
            var animator = image.GetComponent<Animator>();
            if (animator) animator.enabled = false;
            var bar = image.GetComponentInChildren<AnimatedHealthBar>();
            if (bar) bar.gameObject.SetActive(false);
            var rect = image.rectTransform;
            var start = rect.anchoredPosition;
            var startScale = rect.localScale;
            var color = image.color;
            const float duration = 0.4f;
            for (var elapsed = 0f; image && elapsed < duration; elapsed += Time.deltaTime)
            {
                var t = elapsed / duration;
                var eased = t * t;
                var mouth = eater.Image ? eater.Image.rectTransform.anchoredPosition : start;
                rect.anchoredPosition = Vector2.Lerp(start, mouth, eased);
                rect.localScale = startScale * (1f - 0.9f * eased);
                rect.localRotation = Quaternion.Euler(0f, 0f, 540f * eased);
                image.color = Color.Lerp(color, new Color(0.4f, 1f, 0.7f, 0.2f), t);
                yield return null;
            }
            var end = eater.Image ? eater.Image.rectTransform.anchoredPosition : start;
            if (image) Destroy(image.gameObject);
            eater.PopTime = Time.time;
            SpawnParticles(end, new Color(0.45f, 1f, 0.7f), 8, 45f);
            StartCoroutine(Ring(end, new Color(0.4f, 1f, 0.75f, 0.9f), 120f, 40f, 0.25f));
        }

        // ---------- T Cell: Fever Rush ----------

        private bool CastFeverRush(DefenderVisual visual)
        {
            Commit(visual, FireColor);
            CombatStatus.Apply(_statusSystem, visual.State.Effects, CombatStatus.Haste, visual.State.InstanceId);
            visual.State.CooldownTicks = 0;
            var at = RuntimeUi.Point(visual.Position);
            StartCoroutine(Ring(at, new Color(1f, 0.4f, 0.1f, 0.9f), 30f, 230f, 0.4f));
            StartCoroutine(HeatWaves(at));
            SpawnParticles(at, FireColor, 14, 90f);
            _audio?.Play(_art ? _art.heavyAttack : null, 0.7f, 0.2f);
            return true;
        }

        private IEnumerator HeatWaves(Vector2 at)
        {
            for (var i = 0; i < 3; i++)
            {
                StartCoroutine(Ring(at, new Color(1f, 0.6f, 0.2f, 0.7f), 60f, 160f, 0.3f));
                for (var k = 0; k < 4; k++)
                    Mote(at + new Vector2(Random.Range(-35f, 35f), -20f), new Vector2(Random.Range(-15f, 15f), Random.Range(110f, 160f)), new Color(1f, Random.Range(0.4f, 0.8f), 0.15f), Random.Range(14f, 22f), 0.5f);
                yield return new WaitForSeconds(0.12f);
            }
        }

        // ---------- B Cell: Antibody Freeze ----------

        private bool CastAntibodyFreeze(DefenderVisual visual)
        {
            var candidates = new List<EnemyState>();
            foreach (var enemy in _enemies)
                if (!enemy.State.IsTerminal && Vector2.Distance(visual.Position, enemy.Follower.Position) <= visual.Config.range + 0.5f)
                    candidates.Add(enemy.State);
            var chosen = TargetingSystem.Select(candidates);
            var target = chosen == null ? null : _enemies.Find(x => x.State == chosen);
            if (target == null)
            {
                SetStatus("Antibody Freeze needs an enemy within the B Cell's range.");
                return false;
            }
            Commit(visual, IceColor);
            var center = target.Follower.Position;
            var from = RuntimeUi.Point(visual.Position);
            var to = target.BasePoint;
            Kick(visual, -(to - from).normalized * 10f);
            StartCoroutine(FrostBomb(from, to));
            var source = visual.State.InstanceId;
            // Copy: a frozen splitter buds off a new enemy into the list.
            foreach (var enemy in new List<EnemyVisual>(_enemies))
            {
                if (enemy.State.IsTerminal || Vector2.Distance(enemy.Follower.Position, center) > ActiveSkills.AntibodyFreezeRadius) continue;
                ApplyEnemyControl(enemy, CombatStatus.Freeze, source);
                // The chill lingers as a slow after the ice melts.
                ApplyEnemyControl(enemy, CombatStatus.Slow, source, 150);
                CombatStatus.Apply(_statusSystem, enemy.State.Effects, CombatStatus.Mark, source, 150);
            }
            _audio?.Play(_art ? _art.place : null, 0.9f, 0.3f);
            return true;
        }

        /// <summary>An antibody frost orb arcs to the target and bursts into a ring of ice shards and snow.</summary>
        private IEnumerator FrostBomb(Vector2 from, Vector2 to)
        {
            yield return ArcOrb(from, to, new Color(0.75f, 0.95f, 1f), 34f, 60f, 0.18f, false);
            var radius = ActiveSkills.AntibodyFreezeRadius * 76f;
            StartCoroutine(Ring(to, new Color(0.7f, 0.95f, 1f, 0.95f), 40f, radius * 2f / 0.85f, 0.35f));
            StartCoroutine(Ring(to, new Color(1f, 1f, 1f, 0.8f), 20f, radius * 1.2f, 0.25f));
            var frost = RuntimeUi.Image(_arena, "FrostField", to, Vector2.one * radius * 2f, new Color(0.6f, 0.9f, 1f, 0.35f));
            frost.sprite = GlowSprite;
            StartCoroutine(FadeAndDestroy(frost, 0.7f, 0.6f, 1.05f));
            // Ice shards: thin crystals flung outwards.
            for (var i = 0; i < 10; i++)
            {
                var angle = i * Mathf.PI * 2f / 10f + Random.Range(-0.2f, 0.2f);
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                var shard = RuntimeUi.Image(_arena, "IceShard", to + direction * 20f, new Vector2(30f, 8f), new Color(0.85f, 0.97f, 1f, 0.95f));
                shard.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle * Mathf.Rad2Deg);
                StartCoroutine(Fling(shard, to + direction * 20f, direction * radius * 0.9f, 0.35f));
            }
            for (var i = 0; i < 12; i++)
                Mote(to + Random.insideUnitCircle * radius, new Vector2(Random.Range(-10f, 10f), -40f), Color.white, Random.Range(8f, 13f), 0.9f);
        }

        private static IEnumerator Fling(Image image, Vector2 from, Vector2 offset, float duration)
        {
            var color = image.color;
            for (var elapsed = 0f; image && elapsed < duration; elapsed += Time.deltaTime)
            {
                var t = elapsed / duration;
                image.rectTransform.anchoredPosition = from + offset * (1f - (1f - t) * (1f - t));
                color.a = 1f - t * t;
                image.color = color;
                yield return null;
            }
            if (image) Destroy(image.gameObject);
        }

        // ---------- NK Cell: Execute ----------

        private bool CastExecute(DefenderVisual visual)
        {
            EnemyVisual target = null;
            foreach (var enemy in _enemies)
            {
                if (enemy.State.IsTerminal || Vector2.Distance(visual.Position, enemy.Follower.Position) > visual.Config.range + 0.3f) continue;
                if (target == null || enemy.State.Health < target.State.Health) target = enemy;
            }
            if (target == null)
            {
                SetStatus("Execute needs an enemy within the NK Cell's reach.");
                return false;
            }
            var purple = new Color(0.8f, 0.4f, 1f);
            Commit(visual, purple);
            var from = RuntimeUi.Point(visual.Position);
            if (visual.Config.mobile)
            {
                // Blink next to the victim; it walks back home afterwards.
                var offset = visual.Position - target.Follower.Position;
                visual.Position = target.Follower.Position + (offset.sqrMagnitude > 0.0001f ? offset.normalized : Vector2.left) * 0.5f;
                visual.ChaseId = target.State.InstanceId;
            }
            var to = target.BasePoint;
            var direction = (to - from).sqrMagnitude > 0.01f ? (to - from).normalized : Vector2.right;
            if (Mathf.Abs(direction.x) > 0.2f) visual.Facing = direction.x < 0f ? -1f : 1f;
            StartCoroutine(Blink(visual, from, RuntimeUi.Point(visual.Position), purple));
            StartCoroutine(CrossSlash(to, purple));
            target.Knock += direction * 30f;
            target.HitTime = Time.time;
            var damage = EnemyDamage(target, visual.Config.attackDamage * ActiveSkills.ExecuteDamageMultiplier);
            FloatText(to + new Vector2(0f, 50f), Mathf.RoundToInt(damage).ToString(), new Color(1f, 0.6f, 1f), 36);
            if (HitEnemy(target, damage, true))
            {
                _skillCooldowns.Refund(visual.State.InstanceId, ActiveSkills.ExecuteKillRefund, _clock);
                FloatText(RuntimeUi.Point(visual.Position) + new Vector2(0f, 140f), "COOLDOWN -50%", new Color(0.9f, 0.7f, 1f), 20);
            }
            _audio?.Play(_art ? _art.heavyAttack : null, 1f, 0.1f);
            return true;
        }

        /// <summary>After-images along the NK Cell's blink path.</summary>
        private IEnumerator Blink(DefenderVisual visual, Vector2 from, Vector2 to, Color color)
        {
            StartCoroutine(FadeAndDestroy(LineImage(from, to, 26f, new Color(color.r, color.g, color.b, 0.35f)), 0.3f));
            if (!visual.Image || !visual.Image.sprite) yield break;
            for (var i = 0; i < 4; i++)
            {
                var ghost = RuntimeUi.Image(_arena, "BlinkGhost", Vector2.Lerp(from, to, i / 4f), visual.Image.rectTransform.sizeDelta, new Color(color.r, color.g, color.b, 0.55f));
                ghost.sprite = visual.Image.sprite;
                ghost.preserveAspect = true;
                ghost.rectTransform.localScale = new Vector3(visual.Facing, 1f, 1f);
                StartCoroutine(FadeAndDestroy(ghost, 0.3f));
            }
        }

        /// <summary>Two crossing slashes and a granzyme shockwave.</summary>
        private IEnumerator CrossSlash(Vector2 at, Color color)
        {
            for (var i = 0; i < 2; i++)
            {
                var slash = RuntimeUi.Image(_arena, "ExecuteSlash", at, new Vector2(150f, 9f), Color.Lerp(color, Color.white, 0.5f));
                slash.rectTransform.localRotation = Quaternion.Euler(0f, 0f, i == 0 ? 45f : -45f);
                StartCoroutine(FadeAndDestroy(slash, 0.3f, 0.3f, 1.2f));
                yield return new WaitForSeconds(0.06f);
            }
            StartCoroutine(Ring(at, new Color(color.r, color.g, color.b, 0.95f), 30f, 220f, 0.35f));
            SpawnParticles(at, color, 14, 100f);
        }

        // ---------- Platelet: Clot Shield ----------

        private bool CastClotShield(DefenderVisual visual)
        {
            var allies = new List<DefenderVisual>();
            foreach (var other in _defenders.Values)
                if (other.State.Health > 0f && Vector2.Distance(other.Position, visual.Position) <= ActiveSkills.ClotShieldRadius) allies.Add(other);
            Commit(visual, ClotColor);
            var from = RuntimeUi.Point(visual.Position);
            StartCoroutine(Ring(from, new Color(1f, 0.75f, 0.35f, 0.9f), 40f, ActiveSkills.ClotShieldRadius * 2f * 76f / 0.85f, 0.45f));
            foreach (var ally in allies)
            {
                var effects = ally.State.Effects;
                _statusSystem.Cleanse(effects, StatusIds.Freeze);
                _statusSystem.Cleanse(effects, StatusIds.Stun);
                _statusSystem.Cleanse(effects, StatusIds.Poison);
                CombatStatus.Apply(_statusSystem, effects, CombatStatus.Shield, visual.State.InstanceId);
                HealDefender(ally, ally.Config.maxHealth * ActiveSkills.ClotShieldHealFraction);
                // Fibrin threads stitch the shell onto each ally.
                if (ally != visual) StartCoroutine(FadeAndDestroy(LineImage(from, RuntimeUi.Point(ally.Position), 4f, new Color(1f, 0.8f, 0.4f, 0.9f)), 0.45f));
                SpawnParticles(RuntimeUi.Point(ally.Position), ClotColor, 6, 50f);
            }
            _audio?.Play(_art ? _art.heal : null, 0.8f, 0.3f);
            return true;
        }

        // ---------- Energy Cell: ATP Surge ----------

        private bool CastAtpSurge(DefenderVisual visual)
        {
            var gold = new Color(1f, 0.85f, 0.25f);
            Commit(visual, gold);
            _battle.State.Economy.Add(ActiveSkills.AtpSurgeAmount);
            CombatStatus.Apply(_statusSystem, visual.State.Effects, CombatStatus.Exhausted, visual.State.InstanceId);
            visual.SpinTime = Time.time;
            var from = RuntimeUi.Point(visual.Position);
            StartCoroutine(SurgeRings(from, gold));
            StartCoroutine(OrbStream(from, new Vector2(-600f, 510f), gold, 8, 0.6f));
            FloatText(from + new Vector2(0f, 60f), "+" + ActiveSkills.AtpSurgeAmount + " ATP", gold, 28);
            _audio?.Play(_art ? _art.atpGain : null, 1f, 0.2f);
            return true;
        }

        private IEnumerator SurgeRings(Vector2 at, Color color)
        {
            for (var i = 0; i < 3; i++)
            {
                StartCoroutine(Ring(at, new Color(color.r, color.g, color.b, 0.9f), 40f, 200f + i * 40f, 0.4f));
                SpawnParticles(at, color, 6, 70f);
                yield return new WaitForSeconds(0.1f);
            }
        }
    }
}
