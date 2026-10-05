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
    /// <summary>
    /// Combat statuses in the playable battle: ticking, damage over time, the shared damage path for enemies and
    /// defenders, and the overlays that show each status (ice block, flames, stun stars, clot shell...).
    /// </summary>
    public sealed partial class PlayableBattleView
    {
        private readonly StatusEffectSystem _statusSystem = new StatusEffectSystem();

        private static readonly Color IceColor = new Color(0.6f, 0.9f, 1f);
        private static readonly Color FireColor = new Color(1f, 0.5f, 0.12f);
        private static readonly Color ToxinColor = new Color(0.7f, 1f, 0.25f);
        private static readonly Color ClotColor = new Color(1f, 0.72f, 0.3f);

        /// <summary>Lazily created child images that show a unit's statuses.</summary>
        private sealed class StatusOverlays
        {
            public Image Ice;
            public Image IceRim;
            public Image Flame;
            public Image Net;
            public Image Mark;
            public Image Shell;
            public Image ShellRim;
            public Image Aura;
            public Image[] Stars;
            public bool WasFrozen;
            public bool WasShielded;
            public float FrozenAt;
            public float ShieldedAt;
            public float EmberTime;
            public float BubbleTime;
            public float SnoozeTime;
        }

        // ================= Ticking =================

        private void TickStatuses()
        {
            for (var i = _enemies.Count - 1; i >= 0; i--)
            {
                if (i >= _enemies.Count) continue;
                var enemy = _enemies[i];
                var effects = enemy.State.Effects;
                if (effects.Count == 0) continue;
                _statusSystem.Tick(effects);
                var pulse = CombatStatus.PulseDamage(effects);
                if (pulse <= 0f) continue;
                FloatText(enemy.BasePoint + new Vector2(Random.Range(-20f, 20f), 50f), Mathf.Max(1, Mathf.RoundToInt(pulse)).ToString(), FireColor, 20);
                HitEnemy(enemy, pulse);
            }
            foreach (var visual in _defenders.Values)
            {
                var effects = visual.State.Effects;
                if (effects.Count == 0) continue;
                _statusSystem.Tick(effects);
                var pulse = CombatStatus.PulseDamage(effects);
                // Fever Rush burns the T Cell's own reserves.
                if (CombatStatus.Has(effects, StatusIds.Haste) && _clock % 30 == 0)
                    pulse += visual.Config.maxHealth * ActiveSkills.FeverRushHealthLossPerSecond;
                if (pulse <= 0f) continue;
                DamageDefender(visual, pulse, Color.clear, false);
            }
        }

        // ================= Damage paths =================

        /// <summary>
        /// Applies already-mitigated damage to an enemy through its biofilm shield and revival; returns true if it died.
        /// </summary>
        private bool HitEnemy(EnemyVisual enemy, float damage, bool executed = false)
        {
            if (enemy.State.IsTerminal || damage <= 0f) return false;
            var hadShield = enemy.Shield > 0f;
            damage = EnemyTraits.Absorb(ref enemy.Shield, damage);
            if (hadShield && enemy.Shield <= 0f) BiofilmPopped(enemy);
            enemy.HitTime = Time.time;
            if (damage <= 0f) return false;
            var result = DamageResolver.Apply(enemy.State, damage);
            if (!result.Defeated)
            {
                if (enemy.Config.skill == EnemySkill.Biofilm) TryBiofilm(enemy);
                return false;
            }
            if (enemy.Config.skill == EnemySkill.Revive && !enemy.Revived && EnemyTraits.TryRevive(enemy.State, enemy.Config.maxHealth, executed))
            {
                enemy.Revived = true;
                StartCoroutine(Revive(enemy));
                return false;
            }
            var index = _enemies.IndexOf(enemy);
            if (index >= 0) KillEnemy(index);
            return true;
        }

        private void DamageDefender(DefenderVisual defender, float damage, Color effect, bool showEffect = true)
        {
            var multiplier = CombatStatus.DamageTakenMultiplier(defender.State.Effects);
            if (multiplier <= 0f)
            {
                // Encased in a clot: the blow glances off the shell.
                if (Time.time - defender.BlockTextTime > 0.6f)
                {
                    defender.BlockTextTime = Time.time;
                    FloatText(RuntimeUi.Point(defender.Position) + new Vector2(0f, 70f), "BLOCKED", ClotColor, 18);
                    SpawnParticles(RuntimeUi.Point(defender.Position), ClotColor, 4, 40f);
                }
                return;
            }
            defender.State.ReceiveDamage(damage * multiplier);
            if (showEffect) ShowEffect(_art ? _art.hit : null, defender.Position, effect, 55f);
            if (defender.HealthBar) defender.HealthBar.SetValue(defender.State.Health, defender.Config.maxHealth);
            defender.HitTime = Time.time;
        }

        private void HealDefender(DefenderVisual defender, float amount)
        {
            defender.State.Health = Mathf.Min(defender.Config.maxHealth, defender.State.Health + amount);
            if (defender.HealthBar) defender.HealthBar.SetValue(defender.State.Health, defender.Config.maxHealth);
            FloatText(RuntimeUi.Point(defender.Position) + new Vector2(0f, 80f), "+" + Mathf.RoundToInt(amount), new Color(0.45f, 1f, 0.55f), 22);
        }

        // ================= Applying statuses =================

        /// <summary>Freeze, stun or slow an enemy; adaptive mutants shrug off a control they just suffered.</summary>
        private bool ApplyEnemyControl(EnemyVisual enemy, StatusSpec spec, string sourceId, int durationTicks = 0)
        {
            var effects = enemy.State.Effects;
            var wasFrozen = CombatStatus.Has(effects, StatusIds.Freeze);
            var outcome = CombatStatus.ApplyControl(_statusSystem, effects, spec, sourceId, enemy.Config.skill == EnemySkill.Adapt, durationTicks);
            if (outcome == ControlOutcome.Adapted)
            {
                Adapted(enemy);
                return false;
            }
            if (outcome != ControlOutcome.Applied) return false;
            if (spec.Id == StatusIds.Freeze)
            {
                if (!wasFrozen) FloatText(enemy.BasePoint + new Vector2(0f, 85f), "FROZEN", IceColor, 22);
                if (enemy.Config.skill == EnemySkill.FrostSplit && !enemy.Budded) BudOff(enemy);
            }
            return true;
        }

        private ControlOutcome ApplyDefenderControl(DefenderVisual defender, StatusSpec spec, string sourceId, int durationTicks = 0)
        {
            var outcome = CombatStatus.ApplyControl(_statusSystem, defender.State.Effects, spec, sourceId, false, durationTicks);
            if (outcome == ControlOutcome.Shielded)
                FloatText(RuntimeUi.Point(defender.Position) + new Vector2(0f, 85f), "SHIELDED", ClotColor, 20);
            else if (spec.Id == StatusIds.Freeze || spec.Id == StatusIds.Stun)
                defender.ChaseId = null; // it lets go of whatever it was holding
            return outcome;
        }

        /// <summary>T Cell fever shots set enemies alight; fire thaws a frozen enemy.</summary>
        private void Ignite(EnemyVisual enemy, string sourceId)
        {
            var effects = enemy.State.Effects;
            var wasBurning = CombatStatus.Has(effects, StatusIds.Burn);
            if (CombatStatus.ApplyBurn(_statusSystem, effects, sourceId))
            {
                FloatText(enemy.BasePoint + new Vector2(0f, 95f), "THAWED", new Color(0.85f, 0.9f, 1f), 18);
                for (var i = 0; i < 5; i++)
                    Mote(enemy.BasePoint + new Vector2(Random.Range(-20f, 20f), 10f), new Vector2(Random.Range(-15f, 15f), 70f), new Color(0.9f, 0.95f, 1f, 0.6f), 26f, 0.6f);
            }
            if (!wasBurning) FloatText(enemy.BasePoint + new Vector2(0f, 75f), "BURN", FireColor, 20);
        }

        /// <summary>A burning enemy that dies passes its fire to the nearest neighbour.</summary>
        private void SpreadFire(EnemyVisual dead)
        {
            if (!CombatStatus.Has(dead.State.Effects, StatusIds.Burn)) return;
            EnemyVisual nearest = null;
            var best = 1.4f;
            foreach (var enemy in _enemies)
            {
                if (enemy == dead || enemy.State.IsTerminal) continue;
                var distance = Vector2.Distance(enemy.Follower.Position, dead.Follower.Position);
                if (distance > best) continue;
                best = distance;
                nearest = enemy;
            }
            if (nearest == null) return;
            CombatStatus.ApplyBurn(_statusSystem, nearest.State.Effects, dead.State.InstanceId);
            StartCoroutine(ArcOrb(dead.BasePoint, nearest.BasePoint, FireColor, 24f, 50f, 0.3f, false));
            FloatText(nearest.BasePoint + new Vector2(0f, 75f), "SPREAD!", FireColor, 20);
        }

        // ================= Presentation =================

        private static Color StatusTint(Color baseColor, IList<StatusEffectState> effects, float now, float phase)
        {
            if (effects.Count == 0) return baseColor;
            var color = baseColor;
            if (CombatStatus.Has(effects, StatusIds.Freeze)) color = Color.Lerp(color, new Color(0.55f, 0.85f, 1f, color.a), 0.65f);
            else if (CombatStatus.Has(effects, StatusIds.Burn)) color = Color.Lerp(color, new Color(1f, 0.55f, 0.2f, color.a), 0.25f + 0.15f * Mathf.Sin(now * 20f + phase));
            if (CombatStatus.Has(effects, StatusIds.Poison)) color = Color.Lerp(color, new Color(0.6f, 1f, 0.3f, color.a), 0.3f);
            if (CombatStatus.Has(effects, StatusIds.Stun)) color = Color.Lerp(color, new Color(1f, 1f, 0.55f, color.a), 0.3f);
            if (CombatStatus.Has(effects, StatusIds.Haste)) color = Color.Lerp(color, new Color(1f, 0.55f, 0.35f, color.a), 0.2f + 0.1f * Mathf.Sin(now * 12f));
            if (CombatStatus.Has(effects, StatusIds.Shield)) color = Color.Lerp(color, new Color(1f, 0.8f, 0.45f, color.a), 0.35f);
            if (CombatStatus.Has(effects, StatusIds.Exhausted)) color = Color.Lerp(color, new Color(0.55f, 0.55f, 0.6f, color.a), 0.55f);
            return color;
        }

        private static float AnimatorSpeed(IList<StatusEffectState> effects)
        {
            if (CombatStatus.IsDisabled(effects) || CombatStatus.Has(effects, StatusIds.Shield)) return 0f;
            if (CombatStatus.Has(effects, StatusIds.Slow)) return 0.55f;
            return CombatStatus.Has(effects, StatusIds.Haste) ? 1.6f : 1f;
        }

        private void DrawStatus(StatusOverlays o, Image host, float size, IList<StatusEffectState> effects, float now, float phase)
        {
            var parent = host.transform;
            var at = host.rectTransform.anchoredPosition;

            // Ice block: crystallizes in, shimmers, and shatters into shards when it melts.
            var frozen = CombatStatus.Has(effects, StatusIds.Freeze);
            if (frozen && !o.WasFrozen) o.FrozenAt = now;
            if (!frozen && o.WasFrozen) { SpawnParticles(at, IceColor, 9, size * 0.8f); SpawnParticles(at, Color.white, 4, size * 0.5f); }
            o.WasFrozen = frozen;
            if (frozen && !o.Ice)
            {
                o.Ice = Overlay(parent, "IceBlock", size * 1.35f, new Color(0.6f, 0.9f, 1f, 0.5f), GlowSprite);
                o.IceRim = Overlay(parent, "IceRim", size * 1.2f, new Color(0.85f, 0.97f, 1f, 0.95f), RingSprite);
            }
            if (o.Ice)
            {
                o.Ice.gameObject.SetActive(frozen);
                o.IceRim.gameObject.SetActive(frozen);
                if (frozen)
                {
                    var grow = Mathf.Clamp01((now - o.FrozenAt) / 0.18f);
                    var scale = Mathf.Lerp(1.6f, 1f, grow);
                    o.Ice.rectTransform.localScale = Vector3.one * scale;
                    o.IceRim.rectTransform.localScale = Vector3.one * scale;
                    o.IceRim.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f + Mathf.Sin(now * 2f + phase) * 6f);
                    o.Ice.color = new Color(0.6f, 0.9f, 1f, 0.42f + 0.1f * Mathf.Sin(now * 5f + phase));
                    if (now - o.BubbleTime > 0.35f)
                    {
                        o.BubbleTime = now;
                        Mote(at + new Vector2(Random.Range(-size * 0.35f, size * 0.35f), size * 0.3f), new Vector2(0f, -25f), Color.white, 10f, 0.5f);
                    }
                }
            }

            // Flames: a flickering glow that grows with burn stacks, shedding embers.
            var burn = CombatStatus.Stacks(effects, StatusIds.Burn);
            if (burn > 0 && !o.Flame) o.Flame = Overlay(parent, "Flame", size, new Color(1f, 0.5f, 0.1f, 0.55f), GlowSprite);
            if (o.Flame)
            {
                o.Flame.gameObject.SetActive(burn > 0);
                if (burn > 0)
                {
                    var flicker = Mathf.PerlinNoise(now * 9f, phase);
                    o.Flame.rectTransform.anchoredPosition = new Vector2(0f, -size * 0.1f);
                    o.Flame.rectTransform.localScale = new Vector3(0.8f + 0.15f * burn, (0.9f + 0.15f * burn) * (0.9f + 0.3f * flicker), 1f);
                    o.Flame.color = new Color(1f, 0.35f + 0.3f * flicker, 0.08f, 0.45f + 0.2f * flicker);
                    if (now - o.EmberTime > 0.12f / burn)
                    {
                        o.EmberTime = now;
                        var ember = Color.Lerp(new Color(1f, 0.85f, 0.3f), new Color(1f, 0.3f, 0.1f), Random.value);
                        Mote(at + new Vector2(Random.Range(-size * 0.3f, size * 0.3f), -size * 0.1f), new Vector2(Random.Range(-12f, 12f), Random.Range(70f, 110f)), ember, Random.Range(10f, 16f), 0.55f);
                    }
                }
            }

            // Slow: a sticky clot net spinning around the unit.
            var slowed = CombatStatus.Has(effects, StatusIds.Slow) && !frozen;
            if (slowed && !o.Net) o.Net = Overlay(parent, "ClotNet", size * 1.1f, new Color(1f, 0.7f, 0.3f, 0.75f), RingSprite);
            if (o.Net)
            {
                o.Net.gameObject.SetActive(slowed);
                o.Net.rectTransform.localRotation = Quaternion.Euler(0f, 0f, now * 40f);
            }

            // Antibody mark: a pulsing pink tag above the head.
            var marked = CombatStatus.Has(effects, StatusIds.Mark);
            if (marked && !o.Mark)
            {
                o.Mark = Overlay(parent, "AntibodyMark", 26f, new Color(1f, 0.45f, 0.9f, 0.95f), RingSprite);
                o.Mark.rectTransform.anchoredPosition = new Vector2(0f, size * 0.5f + 32f);
            }
            if (o.Mark)
            {
                o.Mark.gameObject.SetActive(marked);
                o.Mark.rectTransform.localScale = Vector3.one * (0.85f + 0.2f * Mathf.Sin(now * 9f));
            }

            // Stun: three stars circling over the head.
            var stunned = CombatStatus.Has(effects, StatusIds.Stun);
            if (stunned && o.Stars == null)
            {
                o.Stars = new Image[3];
                for (var i = 0; i < 3; i++) o.Stars[i] = Overlay(parent, "StunStar", 14f, new Color(1f, 0.95f, 0.4f), GlowSprite);
            }
            if (o.Stars != null)
                for (var i = 0; i < o.Stars.Length; i++)
                {
                    o.Stars[i].gameObject.SetActive(stunned);
                    if (!stunned) continue;
                    var angle = now * 6f + i * 2.094f;
                    o.Stars[i].rectTransform.anchoredPosition = new Vector2(Mathf.Cos(angle) * size * 0.32f, size * 0.5f + 6f + Mathf.Sin(angle) * 7f);
                    o.Stars[i].rectTransform.localScale = Vector3.one * (0.8f + 0.4f * (0.5f + 0.5f * Mathf.Sin(angle)));
                }

            // Poison: toxic bubbles fizzing off the unit.
            if (CombatStatus.Has(effects, StatusIds.Poison) && now - o.BubbleTime > 0.25f)
            {
                o.BubbleTime = now;
                Mote(at + new Vector2(Random.Range(-size * 0.3f, size * 0.3f), 0f), new Vector2(0f, 60f), ToxinColor, Random.Range(9f, 14f), 0.6f);
            }

            // Clot shell: an amber shell snaps shut around the cell and cracks apart when it expires.
            var shielded = CombatStatus.Has(effects, StatusIds.Shield);
            if (shielded && !o.WasShielded) o.ShieldedAt = now;
            if (!shielded && o.WasShielded) SpawnParticles(at, ClotColor, 10, size * 0.9f);
            o.WasShielded = shielded;
            if (shielded && !o.Shell)
            {
                o.Shell = Overlay(parent, "ClotShell", size * 1.4f, new Color(1f, 0.72f, 0.3f, 0.32f), GlowSprite);
                o.ShellRim = Overlay(parent, "ClotShellRim", size * 1.28f, new Color(1f, 0.8f, 0.4f, 0.95f), RingSprite);
            }
            if (o.Shell)
            {
                o.Shell.gameObject.SetActive(shielded);
                o.ShellRim.gameObject.SetActive(shielded);
                if (shielded)
                {
                    var snap = Mathf.Clamp01((now - o.ShieldedAt) / 0.22f);
                    var scale = Mathf.Lerp(1.7f, 1f, 1f - (1f - snap) * (1f - snap)) * (1f + 0.03f * Mathf.Sin(now * 7f));
                    o.Shell.rectTransform.localScale = Vector3.one * scale;
                    o.ShellRim.rectTransform.localScale = Vector3.one * scale;
                    o.ShellRim.rectTransform.localRotation = Quaternion.Euler(0f, 0f, now * 25f);
                }
            }

            // Fever Rush: a hot pulsing aura.
            var hasted = CombatStatus.Has(effects, StatusIds.Haste);
            if (hasted && !o.Aura) o.Aura = Overlay(parent, "FeverAura", size * 1.6f, new Color(1f, 0.45f, 0.15f, 0.35f), GlowSprite);
            if (o.Aura)
            {
                o.Aura.gameObject.SetActive(hasted);
                if (hasted) o.Aura.rectTransform.localScale = Vector3.one * (0.9f + 0.15f * Mathf.Sin(now * 14f));
            }

            // Digesting: bubbles rise out of the full macrophage.
            if (CombatStatus.Has(effects, StatusIds.Digesting) && now - o.BubbleTime > 0.2f)
            {
                o.BubbleTime = now;
                Mote(at + new Vector2(Random.Range(-size * 0.25f, size * 0.25f), size * 0.2f), new Vector2(Random.Range(-10f, 10f), 55f), new Color(0.5f, 1f, 0.7f, 0.8f), Random.Range(8f, 14f), 0.6f);
            }

            // Exhausted Energy Cell dozes off.
            if (CombatStatus.Has(effects, StatusIds.Exhausted) && now - o.SnoozeTime > 1.4f)
            {
                o.SnoozeTime = now;
                FloatText(at + new Vector2(28f, size * 0.55f), "z Z", new Color(0.8f, 0.85f, 1f), 18);
            }
        }

        private static Image Overlay(Transform parent, string name, float size, Color color, Sprite sprite)
        {
            var image = RuntimeUi.Image(parent, name, Vector2.zero, Vector2.one * size, color);
            image.sprite = sprite;
            return image;
        }

        /// <summary>Single soft particle drifting with a constant velocity (pixels per second) while it fades.</summary>
        private void Mote(Vector2 from, Vector2 velocity, Color color, float size, float duration)
        {
            var dot = RuntimeUi.Image(_arena, "Mote", from, Vector2.one * size, color);
            dot.sprite = GlowSprite;
            StartCoroutine(Drift(dot, from, velocity, duration));
        }

        private static IEnumerator Drift(Image dot, Vector2 from, Vector2 velocity, float duration)
        {
            var color = dot.color;
            var alpha = color.a;
            for (var elapsed = 0f; dot && elapsed < duration; elapsed += Time.deltaTime)
            {
                var t = elapsed / duration;
                dot.rectTransform.anchoredPosition = from + velocity * elapsed;
                dot.rectTransform.localScale = Vector3.one * (1f - 0.5f * t);
                color.a = alpha * (1f - t);
                dot.color = color;
                yield return null;
            }
            if (dot) Destroy(dot.gameObject);
        }
    }
}
