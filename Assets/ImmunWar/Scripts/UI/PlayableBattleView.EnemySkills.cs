using System.Collections;
using ImmunWar.Combat;
using ImmunWar.Core.Config;
using ImmunWar.StatusEffects;
using UnityEngine;
using UnityEngine.UI;

namespace ImmunWar.UI
{
    /// <summary>
    /// Each enemy type's signature skill (see <see cref="EnemySkill"/>) and its presentation.
    /// Timers run on the never-reset _clock; chance rolls use a fixed-seed generator.
    /// </summary>
    public sealed partial class PlayableBattleView
    {
        private const float DashChance = 0.3f;
        private const int DashCheckTicks = 45;
        private const float DashDistance = 1.8f;
        private const int StormChargeTicks = 45;
        private const int StormRechargeTicks = 450;
        private const int StormInterruptedTicks = 240;
        private const int StormFreezeTicks = 60;

        private readonly System.Random _skillRandom = new System.Random(7341);

        private void TickEnemySkills()
        {
            foreach (var enemy in _enemies)
                if (enemy.Config.skill == EnemySkill.CytokineStorm && !enemy.State.IsTerminal) TickStorm(enemy);
        }

        // ---------- Swift Virus: Dash ----------

        /// <summary>Now and then a blocked swift virus slips past its blocker along the route.</summary>
        private bool TryDash(EnemyVisual enemy, DefenderVisual blocker)
        {
            if (_clock < enemy.NextSkillTick) return false;
            enemy.NextSkillTick = _clock + DashCheckTicks;
            if (_skillRandom.NextDouble() >= DashChance) return false;
            var from = enemy.BasePoint;
            enemy.Follower.Warp(enemy.Follower.Distance + DashDistance);
            enemy.IgnoreBlockUntil = _clock + 20;
            StartCoroutine(DashStreak(enemy, from, RuntimeUi.Point(enemy.Follower.Position)));
            FloatText(RuntimeUi.Point(blocker.Position) + new Vector2(0f, 80f), "DODGED!", new Color(0.5f, 0.85f, 1f), 22);
            return true;
        }

        private IEnumerator DashStreak(EnemyVisual enemy, Vector2 from, Vector2 to)
        {
            var color = new Color(0.45f, 0.85f, 1f, 0.8f);
            StartCoroutine(FadeAndDestroy(LineImage(from, to, enemy.Size * 0.45f, new Color(color.r, color.g, color.b, 0.35f)), 0.3f));
            StartCoroutine(FadeAndDestroy(LineImage(from, to, 6f, Color.white), 0.2f));
            if (!enemy.Image || !enemy.Image.sprite) yield break;
            for (var i = 1; i <= 4; i++)
            {
                var ghost = RuntimeUi.Image(_arena, "DashGhost", Vector2.Lerp(from, to, i / 5f), enemy.Image.rectTransform.sizeDelta, color);
                ghost.sprite = enemy.Image.sprite;
                ghost.preserveAspect = true;
                ghost.rectTransform.localScale = enemy.Image.rectTransform.localScale;
                StartCoroutine(FadeAndDestroy(ghost, 0.3f));
            }
        }

        // ---------- Bacteria: Biofilm ----------

        private void TryBiofilm(EnemyVisual enemy)
        {
            if (enemy.BiofilmUsed || enemy.State.Health >= enemy.Config.maxHealth * EnemyTraits.BiofilmThreshold) return;
            enemy.BiofilmUsed = true;
            enemy.Shield = enemy.ShieldMax = enemy.Config.maxHealth * EnemyTraits.BiofilmShieldFraction;
            enemy.Biofilm = RuntimeUi.Image(enemy.Image.transform, "Biofilm", Vector2.zero, Vector2.one * (enemy.Size * 1.45f), new Color(0.4f, 1f, 0.8f, 0.4f));
            enemy.Biofilm.sprite = GlowSprite;
            var rim = RuntimeUi.Image(enemy.Biofilm.transform, "BiofilmRim", Vector2.zero, Vector2.one * (enemy.Size * 1.3f), new Color(0.55f, 1f, 0.85f, 0.9f));
            rim.sprite = RingSprite;
            StartCoroutine(Ring(enemy.BasePoint, new Color(0.5f, 1f, 0.85f, 0.9f), enemy.Size * 2.6f, enemy.Size * 1.3f, 0.3f));
            FloatText(enemy.BasePoint + new Vector2(0f, 90f), "BIOFILM", new Color(0.5f, 1f, 0.85f), 22);
        }

        private void DrawBiofilm(EnemyVisual enemy, float now)
        {
            if (!enemy.Biofilm) return;
            var left = enemy.ShieldMax > 0f ? enemy.Shield / enemy.ShieldMax : 0f;
            enemy.Biofilm.gameObject.SetActive(left > 0f);
            if (left <= 0f) return;
            var wobble = 1f + 0.05f * Mathf.Sin(now * 6f + enemy.Phase);
            enemy.Biofilm.rectTransform.localScale = new Vector3(wobble, 2f - wobble, 1f) * (0.75f + 0.25f * left);
            enemy.Biofilm.color = new Color(0.4f, 1f, 0.8f, 0.2f + 0.3f * left);
        }

        private void BiofilmPopped(EnemyVisual enemy)
        {
            SpawnParticles(enemy.BasePoint, new Color(0.5f, 1f, 0.85f), 10, enemy.Size * 0.9f);
            FloatText(enemy.BasePoint + new Vector2(0f, 90f), "POP!", new Color(0.5f, 1f, 0.85f), 22);
        }

        // ---------- Armored Bacteria: Spike Shell ----------

        private void ReflectSpikes(EnemyVisual enemy, DefenderVisual attacker, float damage)
        {
            DamageDefender(attacker, damage * EnemyTraits.SpikeReflectFraction, Color.clear, false);
            if (Time.time - enemy.SpikeTime < 0.5f) return;
            enemy.SpikeTime = Time.time;
            var center = enemy.BasePoint;
            var spikeColor = new Color(0.8f, 0.88f, 1f, 0.95f);
            for (var i = 0; i < 8; i++)
            {
                var angle = i * Mathf.PI / 4f;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                var spike = LineImage(center + direction * enemy.Size * 0.4f, center + direction * enemy.Size * 0.8f, 5f, spikeColor);
                StartCoroutine(FadeAndDestroy(spike, 0.22f, 0.6f, 1.2f));
            }
            StartCoroutine(Tracer(center, RuntimeUi.Point(attacker.Position), spikeColor));
        }

        // ---------- Toxic Bacteria: Paralyze ----------

        private void Paralyze(EnemyVisual enemy, DefenderVisual defender)
        {
            _statusSystem.Cleanse(defender.State.Effects, StatusIds.Poison);
            if (ApplyDefenderControl(defender, CombatStatus.Stun, enemy.State.InstanceId) != ControlOutcome.Applied) return;
            var at = RuntimeUi.Point(defender.Position);
            FloatText(at + new Vector2(0f, 95f), "PARALYZED!", ToxinColor, 24);
            StartCoroutine(Ring(at, new Color(0.75f, 1f, 0.3f, 0.9f), 40f, 150f, 0.3f));
            // Jagged toxin sparks around the cell.
            for (var i = 0; i < 3; i++)
            {
                var angle = Random.Range(0f, Mathf.PI * 2f);
                var a = at + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 20f;
                var b = a + new Vector2(Random.Range(-25f, 25f), Random.Range(-25f, 25f));
                var c = b + new Vector2(Random.Range(-25f, 25f), Random.Range(-25f, 25f));
                StartCoroutine(FadeAndDestroy(LineImage(a, b, 4f, ToxinColor), 0.3f));
                StartCoroutine(FadeAndDestroy(LineImage(b, c, 4f, ToxinColor), 0.3f));
            }
            _audio?.Play(_art ? _art.uiError : null, 0.5f, 0.2f);
        }

        // ---------- Mutant: Adapt ----------

        private void Adapted(EnemyVisual enemy)
        {
            if (Time.time - enemy.AdaptTextTime < 1f) return;
            enemy.AdaptTextTime = Time.time;
            FloatText(enemy.BasePoint + new Vector2(0f, 90f), "ADAPTED", new Color(0.85f, 0.5f, 1f), 20);
            StartCoroutine(Ring(enemy.BasePoint, new Color(0.85f, 0.5f, 1f, 0.9f), enemy.Size * 0.8f, enemy.Size * 1.6f, 0.25f));
        }

        // ---------- Regen Mutant: Revive ----------

        private IEnumerator Revive(EnemyVisual enemy)
        {
            var center = enemy.BasePoint;
            var green = new Color(0.45f, 1f, 0.55f);
            FloatText(center + new Vector2(0f, 100f), "REVIVE!", green, 26);
            for (var i = 0; i < 6; i++)
            {
                var angle = i * Mathf.PI / 3f;
                StartCoroutine(ArcOrb(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 110f, center, green, 18f, 30f, 0.35f, false));
            }
            if (!enemy.Image) yield break;
            // Collapse, then spring back to life.
            var rect = enemy.Image.rectTransform;
            for (var elapsed = 0f; rect && elapsed < 0.35f; elapsed += Time.deltaTime)
            {
                enemy.Knock = new Vector2(0f, -12f * Mathf.Sin(elapsed / 0.35f * Mathf.PI));
                yield return null;
            }
            StartCoroutine(Ring(enemy.BasePoint, green, enemy.Size * 0.5f, enemy.Size * 2.4f, 0.35f));
            SpawnParticles(enemy.BasePoint, green, 10, enemy.Size);
            _audio?.Play(_art ? _art.heal : null, 0.6f, 0.2f);
        }

        // ---------- Splitter Virus: Frost Split ----------

        /// <summary>The first freeze makes a splitter bud off a copy that escapes ahead of the ice.</summary>
        private void BudOff(EnemyVisual enemy)
        {
            enemy.Budded = true;
            var childConfig = enemy.Config.splitInto ? enemy.Config.splitInto : enemy.Config;
            if (!enemy.Route) return;
            var child = CreateEnemy(childConfig, enemy.Route);
            child.Budded = true;
            child.Follower.Warp(enemy.Follower.Distance + 0.5f);
            child.BasePoint = RuntimeUi.Point(child.Follower.Position);
            child.Knock = (child.BasePoint - enemy.BasePoint).normalized * 30f;
            _battle.State.Waves.ActiveEnemies++;
            FloatText(enemy.BasePoint + new Vector2(0f, 110f), "BUD!", new Color(1f, 0.65f, 0.3f), 24);
            SpawnParticles(enemy.BasePoint, new Color(1f, 0.65f, 0.3f), 8, enemy.Size * 0.7f);
        }

        // ---------- Super Pathogen: Cytokine Storm ----------

        private void TickStorm(EnemyVisual boss)
        {
            if (boss.StormChargeAt > 0)
            {
                // Freezing or stunning the boss mid-charge breaks the storm.
                if (CombatStatus.IsDisabled(boss.State.Effects))
                {
                    boss.StormChargeAt = 0;
                    boss.NextSkillTick = _clock + StormInterruptedTicks;
                    FloatText(boss.BasePoint + new Vector2(0f, 130f), "INTERRUPTED!", IceColor, 28);
                    return;
                }
                if (_clock < boss.StormChargeAt) return;
                boss.StormChargeAt = 0;
                boss.NextSkillTick = _clock + StormRechargeTicks;
                ReleaseStorm(boss);
                return;
            }
            if (boss.State.Health > boss.Config.maxHealth * (2f / 3f) || _clock < boss.NextSkillTick || _defenders.Count == 0) return;
            boss.StormChargeAt = _clock + StormChargeTicks;
            StartCoroutine(StormTelegraph(boss));
            FloatText(boss.BasePoint + new Vector2(0f, 130f), "CYTOKINE STORM!", new Color(1f, 0.35f, 0.3f), 30);
            SetStatus("Cytokine storm charging! Freeze the boss to interrupt it, or shield your cells with a Platelet.");
            _audio?.Play(_art ? _art.bossWarning : null, 0.9f);
        }

        /// <summary>Imploding red rings while the storm charges.</summary>
        private IEnumerator StormTelegraph(EnemyVisual boss)
        {
            while (boss.StormChargeAt > 0 && boss.Image && !boss.State.IsTerminal)
            {
                StartCoroutine(Ring(boss.BasePoint, new Color(1f, 0.3f, 0.25f, 0.85f), boss.Size * 3f, boss.Size * 0.9f, 0.3f));
                boss.HitTime = Time.time - 0.1f;
                yield return new WaitForSeconds(0.25f);
            }
        }

        private void ReleaseStorm(EnemyVisual boss)
        {
            var center = boss.BasePoint;
            StartCoroutine(Ring(center, new Color(1f, 0.85f, 0.9f, 0.9f), boss.Size, 2600f, 0.8f));
            StartCoroutine(Ring(center, new Color(0.6f, 0.9f, 1f, 0.8f), boss.Size, 2000f, 0.6f));
            StartCoroutine(ScreenFlash(new Color(0.75f, 0.9f, 1f, 0.35f), 0.45f));
            FloatText(center + new Vector2(0f, 150f), "FREEZE STORM!", IceColor, 32);
            foreach (var defender in _defenders.Values)
            {
                if (ApplyDefenderControl(defender, CombatStatus.Freeze, boss.State.InstanceId, StormFreezeTicks) != ControlOutcome.Applied) continue;
                var at = RuntimeUi.Point(defender.Position);
                StartCoroutine(ArcOrb(center, at, IceColor, 20f, 90f, 0.35f, false));
                SpawnParticles(at, Color.white, 6, 60f);
            }
            _audio?.Play(_art ? _art.organHit : null, 0.7f, 0.1f);
        }

        private IEnumerator ScreenFlash(Color color, float duration)
        {
            var flash = RuntimeUi.Image(_arena, "ScreenFlash", Vector2.zero, new Vector2(1920f, 1080f), color);
            yield return FadeAndDestroy(flash, duration);
        }
    }
}
