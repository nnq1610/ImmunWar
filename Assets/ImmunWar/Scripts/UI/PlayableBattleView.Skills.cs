using System.Collections;
using System.Collections.Generic;
using ImmunWar.Battle.State;
using ImmunWar.Combat;
using ImmunWar.Core.Config;
using ImmunWar.StatusEffects;
using UnityEngine;
using UnityEngine.UI;

namespace ImmunWar.UI
{
    /// <summary>
    /// Per-type enemy attacks and per-cell defender basic attacks, each with its own mechanic and presentation.
    /// </summary>
    public sealed partial class PlayableBattleView
    {
        private const float NkSplashRadius = 1.2f;

        private readonly Dictionary<string, int> _attackCounts = new Dictionary<string, int>();

        private struct Strike
        {
            public float Damage;
            public bool Crit;
            public bool Burst;
            public bool VsMutant;
            public bool Ignite;
        }

        // ================= Enemy attacks =================

        private void EnemyAttack(EnemyVisual enemy, DefenderVisual defender)
        {
            var config = enemy.Config;
            var damage = Mathf.Max(2f, config.organDamage * 0.5f * config.biteMultiplier);
            var from = enemy.BasePoint;
            var to = RuntimeUi.Point(defender.Position);
            var direction = (to - from).sqrMagnitude > 0.01f ? (to - from).normalized : Vector2.right;
            var tint = EnemyTint(VisualBaseId(config)) * config.tint;
            var interval = 30;
            var single = true;
            switch (config.attackStyle)
            {
                case EnemyAttackStyle.Nibble:
                    interval = 12;
                    damage *= 0.45f;
                    enemy.Knock += direction * 10f;
                    SpawnParticles(to, tint, 3, 26f);
                    break;
                case EnemyAttackStyle.Slam:
                    interval = 40;
                    single = false;
                    enemy.Knock += Vector2.up * 28f;
                    StartCoroutine(Ring(from, new Color(1f, 0.55f, 0.15f, 0.9f), 80f, 260f, 0.35f));
                    foreach (var other in DefendersNear(enemy.Follower.Position, 1.5f))
                        DamageDefender(other, damage * 0.8f, new Color(1f, 0.6f, 0.2f, 0.8f));
                    FloatText(from + new Vector2(0f, 70f), "SLAM!", new Color(1f, 0.6f, 0.2f), 24);
                    break;
                case EnemyAttackStyle.Whip:
                    interval = 28;
                    StartCoroutine(Lash(from, to, new Color(0.75f, 0.3f, 0.95f)));
                    break;
                case EnemyAttackStyle.Ram:
                    interval = 48;
                    damage *= 1.7f;
                    StartCoroutine(RamCharge(enemy, direction));
                    Kick(defender, direction * 18f);
                    SpawnParticles(to, new Color(1f, 0.85f, 0.5f), 8, 50f);
                    FloatText(to + new Vector2(0f, 70f), "BAM!", new Color(0.8f, 0.88f, 1f), 26);
                    break;
                case EnemyAttackStyle.AcidSpit:
                    SpitAt(enemy, defender, damage);
                    return;
                case EnemyAttackStyle.Claw:
                    interval = 26;
                    damage *= 1.1f;
                    enemy.Knock += direction * 12f;
                    StartCoroutine(ClawSlash(to, direction));
                    break;
                case EnemyAttackStyle.Drain:
                    interval = 32;
                    var heal = damage * 0.8f;
                    enemy.State.Health = Mathf.Min(config.maxHealth, enemy.State.Health + heal);
                    enemy.Knock += direction * 10f;
                    StartCoroutine(OrbStream(to, from, new Color(0.4f, 1f, 0.5f), 4, 0.3f));
                    FloatText(from + new Vector2(0f, 55f), "+" + Mathf.RoundToInt(heal), new Color(0.45f, 1f, 0.5f), 22);
                    break;
                case EnemyAttackStyle.SporeBurst:
                    interval = 45;
                    single = false;
                    StartCoroutine(Ring(from, new Color(0.7f, 0.85f, 0.3f, 0.85f), 120f, 2.2f * 2f * 76f / 0.85f, 0.6f));
                    SpawnParticles(from, new Color(0.75f, 0.9f, 0.35f), 18, 150f);
                    foreach (var other in DefendersNear(enemy.Follower.Position, 2.2f))
                        DamageDefender(other, damage, new Color(0.75f, 0.9f, 0.3f, 0.8f));
                    FloatText(from + new Vector2(0f, 100f), "SPORE BURST!", new Color(0.8f, 0.95f, 0.35f), 28);
                    break;
                default:
                    enemy.Knock += direction * 16f;
                    break;
            }
            if (single) DamageDefender(defender, damage, new Color(1f, 0.46f, 0.51f, 0.72f));
            enemy.NextContactTick = _tick + interval;
        }

        /// <summary>Ranged enemies spit at the nearest defender in range while they keep walking.</summary>
        private void TryRangedAttack(EnemyVisual enemy)
        {
            DefenderVisual nearest = null;
            var best = enemy.Config.attackRange;
            foreach (var defender in _defenders.Values)
            {
                if (defender.State.Health <= 0f) continue;
                var distance = Vector2.Distance(defender.Position, enemy.Follower.Position);
                if (distance > best) continue;
                best = distance;
                nearest = defender;
            }
            if (nearest != null) SpitAt(enemy, nearest, Mathf.Max(2f, enemy.Config.organDamage * 0.5f * enemy.Config.biteMultiplier));
        }

        /// <summary>Acid glob: light hit plus a poison stack. Paralyzing toxins lock up a cell soaked with enough stacks.</summary>
        private void SpitAt(EnemyVisual enemy, DefenderVisual defender, float damage)
        {
            enemy.NextContactTick = _tick + 50;
            DamageDefender(defender, damage * 0.6f, Color.clear, false);
            enemy.Knock += Vector2.up * 8f;
            StartCoroutine(ArcOrb(enemy.BasePoint, RuntimeUi.Point(defender.Position), new Color(0.75f, 1f, 0.2f), 26f, 70f, 0.35f, true));
            var effects = defender.State.Effects;
            if (CombatStatus.Has(effects, StatusIds.Shield)) return;
            CombatStatus.Apply(_statusSystem, effects, CombatStatus.Poison, enemy.State.InstanceId);
            if (enemy.Config.skill == EnemySkill.Paralyze && CombatStatus.Stacks(effects, StatusIds.Poison) >= CombatStatus.ParalyzePoisonStacks)
                Paralyze(enemy, defender);
        }

        private List<DefenderVisual> DefendersNear(Vector2 world, float radius)
        {
            var hits = new List<DefenderVisual>();
            foreach (var defender in _defenders.Values)
                if (defender.State.Health > 0f && Vector2.Distance(defender.Position, world) <= radius)
                    hits.Add(defender);
            return hits;
        }

        // ================= Defender basic attacks =================

        private void DefenderAct(DefenderVisual visual)
        {
            var defender = visual.State;
            var config = visual.Config;
            CombatSystem.TickCooldown(defender);
            var disabled = CombatStatus.IsDisabled(defender.Effects);
            if (config.role == DefenderRole.Economy && _tick % 90 == 0 && !disabled && !CombatStatus.Has(defender.Effects, StatusIds.Exhausted))
                GenerateAtp(visual.Position);
            if (config.role == DefenderRole.Repair && _tick % 120 == 0 && !disabled && _battle.State.Vitality.Current < _battle.State.Vitality.Maximum)
                RepairOrgan(visual.Position);
            if (config.attackDamage <= 0 || defender.CooldownTicks > 0 || !CombatStatus.CanAttack(defender.Effects)) return;
            var target = PickTarget(visual);
            if (target == null) return;
            // Pick the target first so its armor, marks and the cell's skill can shape the damage dealt.
            var strike = PrepareStrike(defender, config, target);
            defender.CooldownTicks = Mathf.Max(1, Mathf.CeilToInt(config.attackInterval * 30f * CombatStatus.CooldownMultiplier(defender.Effects)));
            if (visual.Animator) visual.Animator.Play("Attack", 0, 0f);
            PlaySkill(visual, target, strike);
            var melee = Vector2.Distance(visual.Position, target.Follower.Position) <= BlockRadius;
            HitEnemy(target, strike.Damage);
            if (melee && target.Config.skill == EnemySkill.SpikeShell) ReflectSpikes(target, visual, strike.Damage);
        }

        /// <summary>Mobile cells keep hitting the enemy they walked out to; others use the shared targeting rule.</summary>
        private EnemyVisual PickTarget(DefenderVisual visual)
        {
            var candidates = new List<EnemyState>();
            foreach (var enemy in _enemies)
            {
                if (enemy.State.IsTerminal || Vector2.Distance(visual.Position, enemy.Follower.Position) > visual.Config.range) continue;
                if (enemy.State.InstanceId == visual.ChaseId) return enemy;
                candidates.Add(enemy.State);
            }
            var chosen = TargetingSystem.Select(candidates);
            return chosen == null ? null : _enemies.Find(x => x.State == chosen);
        }

        private Strike PrepareStrike(DefenderState defender, DefenderConfig config, EnemyVisual target)
        {
            var count = _attackCounts.TryGetValue(defender.InstanceId, out var previous) ? previous + 1 : 1;
            _attackCounts[defender.InstanceId] = count;
            var strike = new Strike { Damage = config.attackDamage };
            if (config.role == DefenderRole.Damage && count % 4 == 0) { strike.Crit = true; strike.Damage *= 2f; }
            if (config.role == DefenderRole.Damage && CombatStatus.Has(defender.Effects, StatusIds.Haste)) strike.Ignite = true;
            if (config.role == DefenderRole.Burst)
            {
                if (count % 3 == 0) { strike.Burst = true; strike.Damage *= 1.5f; }
                if (target.Config.Id.StartsWith("ene_mutant", System.StringComparison.Ordinal)) { strike.VsMutant = true; strike.Damage *= 2f; }
            }
            strike.Damage = EnemyDamage(target, strike.Damage);
            return strike;
        }

        /// <summary>Damage after the target's armor and any antibody mark.</summary>
        private static float EnemyDamage(EnemyVisual target, float damage) =>
            damage * (1f - target.Config.armor) * CombatStatus.DamageTakenMultiplier(target.State.Effects);

        private void PlaySkill(DefenderVisual visual, EnemyVisual target, Strike strike)
        {
            var config = visual.Config;
            var from = RuntimeUi.Point(visual.Position);
            var to = target.BasePoint;
            var direction = (to - from).sqrMagnitude > 0.01f ? (to - from).normalized : Vector2.right;
            if (Mathf.Abs(direction.x) > 0.2f) visual.Facing = direction.x < 0f ? -1f : 1f;
            target.HitTime = Time.time;
            var numberColor = new Color(1f, 0.95f, 0.6f);
            switch (config.role)
            {
                case DefenderRole.Blocker:
                    // Macrophage — Engulf: lunges, jaws snap shut on the target and drag it in.
                    Kick(visual, direction * 24f);
                    StartCoroutine(Ring(to, new Color(0.3f, 1f, 0.8f, 0.9f), target.Size * 1.8f, target.Size * 0.4f, 0.18f));
                    target.Knock -= direction * 14f;
                    SpawnParticles(to, new Color(0.4f, 1f, 0.8f), 5, 30f);
                    _audio?.Play(_art ? _art.heavyAttack : null, 0.5f, 0.08f);
                    break;
                case DefenderRole.Damage:
                    // T Cell — Precision shot: thin piercing beam; every 4th shot is a golden critical.
                    // During Fever Rush the beam runs hot and sets the target on fire.
                    Kick(visual, -direction * 7f);
                    var beamColor = strike.Ignite ? new Color(1f, 0.45f, 0.1f) : strike.Crit ? new Color(1f, 0.85f, 0.25f) : new Color(0.55f, 0.85f, 1f);
                    StartCoroutine(Beam(from, to, beamColor, strike.Crit || strike.Ignite ? 9f : 4f));
                    StartCoroutine(Ring(from, new Color(0.6f, 0.9f, 1f, 0.8f), 30f, 70f, 0.15f));
                    target.Knock += direction * (strike.Crit ? 18f : 8f);
                    if (strike.Crit)
                    {
                        numberColor = new Color(1f, 0.8f, 0.2f);
                        FloatText(to + new Vector2(0f, 75f), "CRIT!", numberColor, 28);
                        StartCoroutine(Ring(to, new Color(1f, 0.85f, 0.25f, 0.9f), 30f, 140f, 0.25f));
                    }
                    if (strike.Ignite) Ignite(target, visual.State.InstanceId);
                    _audio?.Play(_art ? _art.attack : null, strike.Crit ? 0.7f : 0.4f, 0.07f);
                    break;
                case DefenderRole.Support:
                    // B Cell — Antibody volley: three antibodies mark the target, which then takes extra damage.
                    Kick(visual, -direction * 5f);
                    StartCoroutine(AntibodyVolley(from, to));
                    if (!CombatStatus.Has(target.State.Effects, StatusIds.Mark)) FloatText(to + new Vector2(0f, 75f), "MARKED", new Color(1f, 0.5f, 0.9f), 20);
                    CombatStatus.Apply(_statusSystem, target.State.Effects, CombatStatus.Mark, visual.State.InstanceId);
                    _audio?.Play(_art ? _art.place : null, 0.35f, 0.12f);
                    break;
                case DefenderRole.Burst:
                    // NK Cell — Granzyme: quick strikes; every 3rd releases a burst that splashes nearby enemies.
                    Kick(visual, strike.Burst ? direction * 14f : -direction * 6f);
                    StartCoroutine(Tracer(from, to, new Color(0.7f, 0.35f, 1f)));
                    if (strike.Burst)
                    {
                        StartCoroutine(Ring(to, new Color(0.75f, 0.35f, 1f, 0.95f), 40f, NkSplashRadius * 2f * 76f / 0.85f, 0.35f));
                        SpawnParticles(to, new Color(0.8f, 0.45f, 1f), 12, 90f);
                        FloatText(to + new Vector2(0f, 80f), "BURST!", new Color(0.85f, 0.5f, 1f), 26);
                        Splash(target, strike.Damage * 0.5f, NkSplashRadius);
                    }
                    if (strike.VsMutant) FloatText(to + new Vector2(30f, 100f), "x2 MUTANT", new Color(1f, 0.6f, 1f), 20);
                    _audio?.Play(_art ? _art.heavyAttack : null, strike.Burst ? 0.8f : 0.45f, 0.07f);
                    break;
                case DefenderRole.Repair:
                    // Platelet — Clot shot: a sticky glob that slows the target.
                    Kick(visual, -direction * 5f);
                    StartCoroutine(ArcOrb(from, to, new Color(1f, 0.7f, 0.3f), 20f, 40f, 0.25f, false));
                    var wasSlowed = CombatStatus.Has(target.State.Effects, StatusIds.Slow);
                    if (ApplyEnemyControl(target, CombatStatus.Slow, visual.State.InstanceId) && !wasSlowed)
                        FloatText(to + new Vector2(0f, 75f), "SLOWED", new Color(1f, 0.75f, 0.35f), 20);
                    _audio?.Play(_art ? _art.attack : null, 0.3f, 0.1f);
                    break;
                default:
                    StartCoroutine(Tracer(from, to, DefenderColor(config.role)));
                    break;
            }
            ShowEffect(_art ? _art.hit : null, target.Follower.Position, Color.Lerp(DefenderColor(config.role), Color.white, 0.4f), 58f);
            if (target.Config.armor > 0f) numberColor = new Color(0.7f, 0.82f, 1f);
            FloatText(to + new Vector2(UnityEngine.Random.Range(-16f, 16f), 42f), Mathf.Max(1, Mathf.RoundToInt(strike.Damage)).ToString(), numberColor, strike.Crit ? 32 : 24);
        }

        private void Splash(EnemyVisual source, float damage, float radius)
        {
            var center = source.Follower.Position;
            for (var i = _enemies.Count - 1; i >= 0; i--)
            {
                if (i >= _enemies.Count) continue;
                var enemy = _enemies[i];
                if (enemy == source || enemy.State.IsTerminal || Vector2.Distance(enemy.Follower.Position, center) > radius) continue;
                enemy.Knock += (enemy.BasePoint - source.BasePoint).normalized * 16f;
                HitEnemy(enemy, EnemyDamage(enemy, damage));
            }
        }

        /// <summary>Energy Cell: golden pulse and an ATP orb that flies to the ATP counter.</summary>
        private void GenerateAtp(Vector2 position)
        {
            _battle.State.Economy.Add(5);
            var from = RuntimeUi.Point(position);
            StartCoroutine(Ring(from, new Color(1f, 0.85f, 0.25f, 0.9f), 50f, 150f, 0.4f));
            StartCoroutine(ArcOrb(from, new Vector2(-600f, 510f), new Color(1f, 0.88f, 0.3f), 22f, 120f, 0.6f, false));
            FloatText(from + new Vector2(0f, 50f), "+5", new Color(1f, 0.88f, 0.25f), 22);
            _audio?.Play(_art ? _art.atpGain : null, 0.35f, 0.4f);
        }

        /// <summary>Platelet: a stream of healing particles flows into the organ.</summary>
        private void RepairOrgan(Vector2 position)
        {
            _battle.State.Vitality.Repair(3);
            StartCoroutine(OrbStream(RuntimeUi.Point(position), Vector2.zero, new Color(0.45f, 1f, 0.55f), 5, 0.5f));
            StartCoroutine(Ring(Vector2.zero, new Color(0.45f, 1f, 0.55f, 0.8f), 200f, 320f, 0.5f));
            FloatText(new Vector2(0f, 140f), "+3", new Color(0.45f, 1f, 0.55f), 26);
            _audio?.Play(_art ? _art.heal : null, 0.4f, 1f);
        }

        // ================= Effect primitives =================

        private Image LineImage(Vector2 from, Vector2 to, float width, Color color)
        {
            var delta = to - from;
            var line = RuntimeUi.Image(_arena, "FxLine", (from + to) * 0.5f, new Vector2(delta.magnitude, width), color);
            line.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            return line;
        }

        private IEnumerator FadeAndDestroy(Image image, float duration, float scaleFrom = 1f, float scaleTo = 1f)
        {
            if (!image) yield break;
            var color = image.color;
            var alpha = color.a;
            for (var elapsed = 0f; image && elapsed < duration; elapsed += Time.deltaTime)
            {
                var t = elapsed / duration;
                image.rectTransform.localScale = Vector3.one * Mathf.Lerp(scaleFrom, scaleTo, t);
                color.a = alpha * (1f - t);
                image.color = color;
                yield return null;
            }
            if (image) Destroy(image.gameObject);
        }

        private IEnumerator Ring(Vector2 center, Color color, float startSize, float endSize, float duration)
        {
            var ring = RuntimeUi.Image(_arena, "FxRing", center, Vector2.one * startSize, color);
            ring.sprite = RingSprite;
            var alpha = color.a;
            for (var elapsed = 0f; ring && elapsed < duration; elapsed += Time.deltaTime)
            {
                var t = elapsed / duration;
                ring.rectTransform.sizeDelta = Vector2.one * Mathf.Lerp(startSize, endSize, 1f - (1f - t) * (1f - t));
                color.a = alpha * (1f - t);
                ring.color = color;
                yield return null;
            }
            if (ring) Destroy(ring.gameObject);
        }

        private IEnumerator Beam(Vector2 from, Vector2 to, Color color, float width)
        {
            var core = LineImage(from, to, width, Color.white);
            var glow = LineImage(from, to, width * 3f, new Color(color.r, color.g, color.b, 0.45f));
            core.transform.SetAsLastSibling();
            StartCoroutine(FadeAndDestroy(glow, 0.16f));
            yield return FadeAndDestroy(core, 0.12f);
        }

        private IEnumerator Lash(Vector2 from, Vector2 to, Color color)
        {
            // A bent flagellum: two segments through an offset midpoint, snapping out then fading.
            var normal = new Vector2(-(to - from).y, (to - from).x).normalized;
            var mid = (from + to) * 0.5f + normal * 26f;
            var a = LineImage(from, mid, 7f, color);
            var b = LineImage(mid, to, 7f, color);
            StartCoroutine(FadeAndDestroy(a, 0.22f));
            yield return FadeAndDestroy(b, 0.22f);
        }

        private IEnumerator ClawSlash(Vector2 at, Vector2 direction)
        {
            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 60f;
            var perpendicular = new Vector2(-direction.y, direction.x);
            for (var i = -1; i <= 1; i++)
            {
                var slash = RuntimeUi.Image(_arena, "ClawSlash", at + perpendicular * (i * 14f), new Vector2(78f, 6f), new Color(1f, 0.92f, 0.95f, 0.95f));
                slash.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
                StartCoroutine(FadeAndDestroy(slash, 0.22f, 0.4f, 1.15f));
                yield return new WaitForSeconds(0.03f);
            }
        }

        private IEnumerator RamCharge(EnemyVisual enemy, Vector2 direction)
        {
            enemy.Knock -= direction * 20f;
            yield return new WaitForSeconds(0.1f);
            enemy.Knock += direction * 48f;
        }

        private IEnumerator ArcOrb(Vector2 from, Vector2 to, Color color, float size, float height, float duration, bool splash)
        {
            var orb = RuntimeUi.Image(_arena, "FxOrb", from, Vector2.one * size, color);
            orb.sprite = GlowSprite;
            for (var elapsed = 0f; orb && elapsed < duration; elapsed += Time.deltaTime)
            {
                var t = elapsed / duration;
                orb.rectTransform.anchoredPosition = Vector2.Lerp(from, to, t) + Vector2.up * (height * 4f * t * (1f - t));
                yield return null;
            }
            if (orb) Destroy(orb.gameObject);
            if (!splash) yield break;
            SpawnParticles(to, color, 7, 40f);
            var puddle = RuntimeUi.Image(_arena, "AcidSplash", to, new Vector2(70f, 70f), new Color(color.r, color.g, color.b, 0.6f));
            puddle.sprite = GlowSprite;
            StartCoroutine(FadeAndDestroy(puddle, 0.5f, 0.6f, 1.3f));
        }

        private IEnumerator AntibodyVolley(Vector2 from, Vector2 to)
        {
            var normal = new Vector2(-(to - from).y, (to - from).x).normalized;
            for (var i = -1; i <= 1; i++)
            {
                StartCoroutine(ArcOrb(from, to + normal * (i * 10f), new Color(1f, 0.5f, 0.9f), 14f, i * 22f, 0.2f, false));
                yield return new WaitForSeconds(0.05f);
            }
        }

        private IEnumerator OrbStream(Vector2 from, Vector2 to, Color color, int count, float duration)
        {
            for (var i = 0; i < count; i++)
            {
                StartCoroutine(ArcOrb(from, to, color, 16f, UnityEngine.Random.Range(-30f, 30f), duration, false));
                yield return new WaitForSeconds(0.06f);
            }
        }
    }
}
