using System.Linq;
using ImmunWar.Battle;
using ImmunWar.Battle.State;
using ImmunWar.Combat.Abilities;
using ImmunWar.Core.Config;
using ImmunWar.StatusEffects;
using NUnit.Framework;
using UnityEngine;

namespace ImmunWar.Tests.EditMode
{
    public sealed class CB01SpecificationTests
    {
        private GameCatalog _catalog;

        [SetUp]
        public void SetUp()
        {
            _catalog = Resources.Load<GameCatalog>("ImmuneWar/GameCatalog");
            Assert.That(_catalog, Is.Not.Null, "GameCatalog must exist in Resources/ImmuneWar/GameCatalog");
        }

        [Test]
        public void Defenders_MatchCB01Table431()
        {
            var defs = _catalog.defenders.ToDictionary(d => d.Id);

            var mac = defs["def_macrophage"];
            Assert.That(mac.atpCost, Is.EqualTo(30));
            Assert.That(mac.maxHealth, Is.EqualTo(240));
            Assert.That(mac.attackDamage, Is.EqualTo(8));
            Assert.That(mac.attackInterval, Is.EqualTo(1.1f).Within(0.001f));
            Assert.That(mac.range, Is.EqualTo(1.2f).Within(0.001f));
            Assert.That(mac.blockCapacity, Is.EqualTo(3));
            Assert.That(mac.mobile, Is.True);
            Assert.That(mac.engageRadius, Is.EqualTo(2.2f).Within(0.001f));

            var tcell = defs["def_tcell"];
            Assert.That(tcell.atpCost, Is.EqualTo(30));
            Assert.That(tcell.maxHealth, Is.EqualTo(90));
            Assert.That(tcell.attackDamage, Is.EqualTo(14));
            Assert.That(tcell.attackInterval, Is.EqualTo(0.8f).Within(0.001f));
            Assert.That(tcell.range, Is.EqualTo(3.2f).Within(0.001f));
            Assert.That(tcell.blockCapacity, Is.EqualTo(0));

            var bcell = defs["def_bcell"];
            Assert.That(bcell.atpCost, Is.EqualTo(35));
            Assert.That(bcell.maxHealth, Is.EqualTo(95));
            Assert.That(bcell.attackDamage, Is.EqualTo(6));
            Assert.That(bcell.attackInterval, Is.EqualTo(1.1f).Within(0.001f));
            Assert.That(bcell.range, Is.EqualTo(3.0f).Within(0.001f));
            Assert.That(bcell.blockCapacity, Is.EqualTo(0));

            var nk = defs["def_nk"];
            Assert.That(nk.atpCost, Is.EqualTo(45));
            Assert.That(nk.maxHealth, Is.EqualTo(120));
            Assert.That(nk.attackDamage, Is.EqualTo(24));
            Assert.That(nk.attackInterval, Is.EqualTo(1.3f).Within(0.001f));
            Assert.That(nk.range, Is.EqualTo(2.5f).Within(0.001f));
            Assert.That(nk.blockCapacity, Is.EqualTo(1));
            Assert.That(nk.mobile, Is.True);
            Assert.That(nk.engageRadius, Is.EqualTo(2.8f).Within(0.001f));

            var platelet = defs["def_platelet"];
            Assert.That(platelet.atpCost, Is.EqualTo(35));
            Assert.That(platelet.maxHealth, Is.EqualTo(130));
            Assert.That(platelet.attackDamage, Is.EqualTo(4));
            Assert.That(platelet.attackInterval, Is.EqualTo(1.4f).Within(0.001f));
            Assert.That(platelet.range, Is.EqualTo(2.2f).Within(0.001f));
            Assert.That(platelet.blockCapacity, Is.EqualTo(0));

            var energy = defs["def_energy"];
            Assert.That(energy.atpCost, Is.EqualTo(40));
            Assert.That(energy.maxHealth, Is.EqualTo(80));
            Assert.That(energy.attackDamage, Is.EqualTo(0));
            Assert.That(energy.blockCapacity, Is.EqualTo(0));
        }

        [Test]
        public void Enemies_MatchCB01Tables36And432()
        {
            var enemies = _catalog.enemies.ToDictionary(e => e.Id);

            void Check(string id, float hp, float speed, int organDmg, int atp, float armor, float regen, float atkDmg, int atkTicks)
            {
                var e = enemies[id];
                Assert.That(e.maxHealth, Is.EqualTo(hp).Within(0.01f), id + " maxHealth");
                Assert.That(e.moveSpeed, Is.EqualTo(speed).Within(0.01f), id + " moveSpeed");
                Assert.That(e.organDamage, Is.EqualTo(organDmg), id + " organDamage");
                Assert.That(e.atpReward, Is.EqualTo(atp), id + " atpReward");
                Assert.That(e.armor, Is.EqualTo(armor).Within(0.01f), id + " armor");
                Assert.That(e.regenPerSecond, Is.EqualTo(regen).Within(0.01f), id + " regenPerSecond");
                Assert.That(e.attackDamage, Is.EqualTo(atkDmg).Within(0.01f), id + " attackDamage");
                Assert.That(e.attackIntervalTicks, Is.EqualTo(atkTicks), id + " attackIntervalTicks");
            }

            Check("ene_virus", 45f, 0.45f, 10, 3, 0f, 0f, 5f, 30);
            Check("ene_virus_swift", 28f, 0.95f, 8, 3, 0f, 0f, 1.8f, 12);
            Check("ene_virus_splitter", 85f, 0.50f, 10, 4, 0f, 0f, 4f, 40);
            Check("ene_bacteria", 90f, 0.58f, 10, 5, 0f, 0f, 7.5f, 28);
            Check("ene_bacteria_armored", 150f, 0.38f, 15, 7, 0.35f, 0f, 18f, 48);
            Check("ene_bacteria_toxic", 100f, 0.50f, 10, 6, 0f, 0f, 7.5f, 50);
            Assert.That(enemies["ene_bacteria_toxic"].attackRange, Is.EqualTo(2.4f).Within(0.01f));
            Check("ene_mutant", 120f, 0.70f, 15, 7, 0f, 0f, 11f, 26);
            Check("ene_mutant_regen", 100f, 0.55f, 15, 7, 0f, 6f, 8f, 32);
            Check("ene_super_pathogen", 1200f, 0.32f, 100, 30, 0.2f, 0f, 35f, 45);
        }

        [Test]
        public void MapsAndWaves_MatchCB01Sections52To54()
        {
            var maps = _catalog.maps.ToDictionary(m => m.Id);

            var lung = maps["map_lung"];
            Assert.That(lung.startingAtp, Is.EqualTo(150));
            Assert.That(lung.maximumVitality, Is.EqualTo(100));
            Assert.That(lung.waveSet.waves.Length, Is.EqualTo(3));
            Assert.That(lung.waveSet.waves.SelectMany(w => w.groups).Sum(g => g.count), Is.EqualTo(38));

            var brain = maps["map_brain"];
            Assert.That(brain.startingAtp, Is.EqualTo(180));
            Assert.That(brain.maximumVitality, Is.EqualTo(100));
            Assert.That(brain.waveSet.waves.Length, Is.EqualTo(4));
            Assert.That(brain.waveSet.waves.SelectMany(w => w.groups).Sum(g => g.count), Is.EqualTo(54));

            var stomach = maps["map_stomach"];
            Assert.That(stomach.startingAtp, Is.EqualTo(210));
            Assert.That(stomach.maximumVitality, Is.EqualTo(100));
            Assert.That(stomach.waveSet.waves.Length, Is.EqualTo(4));
            Assert.That(stomach.waveSet.waves.SelectMany(w => w.groups).Sum(g => g.count), Is.EqualTo(58));
            Assert.That(stomach.waveSet.waves[3].groups.Any(g => g.enemy && g.enemy.Id == "ene_super_pathogen" && g.count == 1), Is.True);
        }

        [Test]
        public void ActiveSkillsAndStatusSpecs_MatchCB01()
        {
            Assert.That(ActiveSkills.For(DefenderRole.Blocker).AtpCost, Is.EqualTo(20));
            Assert.That(ActiveSkills.For(DefenderRole.Blocker).CooldownTicks, Is.EqualTo(360));
            Assert.That(ActiveSkills.For(DefenderRole.Damage).AtpCost, Is.EqualTo(10));
            Assert.That(ActiveSkills.For(DefenderRole.Damage).CooldownTicks, Is.EqualTo(420));
            Assert.That(ActiveSkills.For(DefenderRole.Support).AtpCost, Is.EqualTo(25));
            Assert.That(ActiveSkills.For(DefenderRole.Support).CooldownTicks, Is.EqualTo(480));
            Assert.That(ActiveSkills.For(DefenderRole.Burst).AtpCost, Is.EqualTo(20));
            Assert.That(ActiveSkills.For(DefenderRole.Burst).CooldownTicks, Is.EqualTo(360));
            Assert.That(ActiveSkills.For(DefenderRole.Repair).AtpCost, Is.EqualTo(20));
            Assert.That(ActiveSkills.For(DefenderRole.Repair).CooldownTicks, Is.EqualTo(540));
            Assert.That(ActiveSkills.ClotShieldHealFraction, Is.EqualTo(0.10f).Within(0.001f));
            Assert.That(ActiveSkills.For(DefenderRole.Economy).AtpCost, Is.EqualTo(0));
            Assert.That(ActiveSkills.For(DefenderRole.Economy).CooldownTicks, Is.EqualTo(750));
            Assert.That(ActiveSkills.AtpSurgeAmount, Is.EqualTo(20));
            Assert.That(ActiveSkills.AtpSurgeInitialDelayTicks, Is.EqualTo(300));
            Assert.That(ActiveSkills.EnergyCellPassiveIntervalTicks, Is.EqualTo(120));
            Assert.That(ActiveSkills.PlateletRepairIntervalTicks, Is.EqualTo(150));
            Assert.That(CombatStatus.Exhausted.DurationTicks, Is.EqualTo(180));
        }

        [Test]
        public void EconomyAndVitalityCapsAndBudgets_EnforceCB01Rules()
        {
            var economy = new EconomyState(150);
            Assert.That(economy.WaveBudgetRemaining, Is.EqualTo(90));
            Assert.That(economy.CanSurge(20), Is.True);
            Assert.That(economy.TrySurge(20), Is.True);
            Assert.That(economy.Atp, Is.EqualTo(170));
            Assert.That(economy.WaveBudgetRemaining, Is.EqualTo(70));

            for (var i = 0; i < 14; i++) economy.GenerateWaveAtp(5);
            Assert.That(economy.WaveBudgetRemaining, Is.EqualTo(0));
            Assert.That(economy.GenerateWaveAtp(5), Is.EqualTo(0));
            Assert.That(economy.CanSurge(20), Is.False);

            economy.ResetWaveBudget();
            Assert.That(economy.WaveBudgetRemaining, Is.EqualTo(90));
            economy.Add(100);
            Assert.That(economy.Atp, Is.EqualTo(300));
            Assert.That(economy.CanSurge(20), Is.False, "Surge requires full 20 ATP room below 300 cap");

            var vitality = new VitalityState(100);
            vitality.Damage(30);
            Assert.That(vitality.Current, Is.EqualTo(70));
            for (var i = 0; i < 5; i++) vitality.RepairWave(3);
            Assert.That(vitality.Current, Is.EqualTo(82), "Wave repair is capped at +12 per wave");
            Assert.That(vitality.WaveRepairRemaining, Is.EqualTo(0));
            vitality.ResetWaveRepairBudget();
            Assert.That(vitality.RepairWave(3), Is.EqualTo(3));
        }

        [Test]
        public void BattleController_SupportsPauseDuringPreparingAndRunning()
        {
            var controller = BattleController.CreateForTests("map_lung", 150, 100);
            var state = controller.State;
            Assert.That(state.Phase, Is.EqualTo(BattlePhase.Preparing));

            controller.Pause();
            Assert.That(state.Phase, Is.EqualTo(BattlePhase.Paused));
            controller.Resume();
            Assert.That(state.Phase, Is.EqualTo(BattlePhase.Preparing));

            controller.StartBattle();
            Assert.That(state.Phase, Is.EqualTo(BattlePhase.Running));
            controller.Pause();
            Assert.That(state.Phase, Is.EqualTo(BattlePhase.Paused));
            controller.Resume();
            Assert.That(state.Phase, Is.EqualTo(BattlePhase.Running));

            controller.ForceDefeat();
            Assert.That(state.Phase, Is.EqualTo(BattlePhase.Defeat));
            Object.DestroyImmediate(controller.gameObject);
        }

        [Test]
        public void CanReceiveControl_RespectsShieldAndAdaptiveImmunity()
        {
            var system = new StatusEffectSystem();
            var effects = new System.Collections.Generic.List<StatusEffectState>();
            Assert.That(CombatStatus.CanReceiveControl(effects, StatusIds.Freeze, true), Is.True);
            CombatStatus.ApplyControl(system, effects, CombatStatus.Freeze, "b1", true);
            Assert.That(CombatStatus.CanReceiveControl(effects, StatusIds.Freeze, true), Is.False, "Adaptive mutant cannot receive second Freeze while adapted");
            Assert.That(CombatStatus.CanReceiveControl(effects, StatusIds.Freeze, false), Is.True, "Non-adaptive enemy can still receive Freeze (with resistance)");
        }
    }
}
