using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SticklersGate.Data;
using SticklersGate.Utils;

namespace SticklersGate.Combat
{
    /// <summary>
    /// Central combat controller. Manages turn order, phase transitions, and action resolution.
    /// Emits events for UI — never references UI code directly.
    /// </summary>
    public class CombatStateMachine : MonoBehaviour
    {
        // --- Events for UI to subscribe to ---
        public event Action OnCombatStarted;
        public event Action OnCombatEnded;
        public event Action<CombatUnit> OnTurnStarted;
        public event Action<CombatUnit> OnTurnEnded;
        public event Action<CombatUnit> OnPlayerActionNeeded;
        public event Action<CombatUnit, CombatUnit, AttackResult> OnAttackResolved;
        public event Action<CombatUnit, CombatUnit, HealResult> OnHealResolved;
        public event Action<CombatUnit> OnUnitDowned;
        public event Action<CombatUnit> OnUnitDied;
        public event Action<CombatUnit, bool> OnDeathSaveResult;
        public event Action<string> OnNarratorQuip;
        public event Action<List<CombatUnit>> OnInitiativeRolled;
        public event Action<bool> OnCombatResult; // true = victory, false = defeat
        public event Action<CombatPhase> OnPhaseChanged;

        // --- State ---
        private List<CombatUnit> _allUnits = new List<CombatUnit>();
        private List<CombatUnit> _turnOrder = new List<CombatUnit>();
        private List<CombatUnit> _playerUnits = new List<CombatUnit>();
        private List<CombatUnit> _enemyUnits = new List<CombatUnit>();
        private int _currentTurnIndex;
        private CombatPhase _currentPhase;
        private EncounterSO _encounter;
        private int _turnCount;

        // --- Public State ---
        public CombatPhase CurrentPhase => _currentPhase;
        public CombatUnit CurrentUnit => _currentTurnIndex < _turnOrder.Count ? _turnOrder[_currentTurnIndex] : null;
        public IReadOnlyList<CombatUnit> TurnOrder => _turnOrder;
        public IReadOnlyList<CombatUnit> PlayerUnits => _playerUnits;
        public IReadOnlyList<CombatUnit> EnemyUnits => _enemyUnits;
        public int TurnCount => _turnCount;

        /// <summary>
        /// Start a combat encounter with the given player party and encounter data.
        /// </summary>
        public void StartCombat(List<CombatUnit> playerUnits, EncounterSO encounter)
        {
            _encounter = encounter;
            _playerUnits = new List<CombatUnit>(playerUnits);
            _turnCount = 0;

            // Spawn enemies from encounter data
            _enemyUnits = new List<CombatUnit>();
            foreach (var spawn in encounter.Enemies)
            {
                var unit = CombatUnit.FromEnemy(spawn.enemyData, spawn.nameOverride);
                unit.Row = spawn.row;
                _enemyUnits.Add(unit);
            }

            _allUnits = new List<CombatUnit>();
            _allUnits.AddRange(_playerUnits);
            _allUnits.AddRange(_enemyUnits);

            DiceRoller.ClearHistory();
            SetPhase(CombatPhase.Setup);
            OnCombatStarted?.Invoke();

            // Show narrator intro
            if (encounter.IntroLines != null && encounter.IntroLines.Length > 0)
            {
                foreach (var line in encounter.IntroLines)
                {
                    OnNarratorQuip?.Invoke(line);
                }
            }

            StartCoroutine(CombatLoop());
        }

        private IEnumerator CombatLoop()
        {
            // Roll initiative
            SetPhase(CombatPhase.RollInitiative);
            RollInitiative();
            OnInitiativeRolled?.Invoke(_turnOrder);
            yield return new WaitForSeconds(1f);

            // Main combat loop
            while (true)
            {
                for (_currentTurnIndex = 0; _currentTurnIndex < _turnOrder.Count; _currentTurnIndex++)
                {
                    var unit = _turnOrder[_currentTurnIndex];

                    // Skip dead or fully incapacitated units
                    if (!unit.IsAlive) continue;

                    // Handle downed units (death saves, skip if stabilized)
                    if (!unit.IsConscious)
                    {
                        if (!unit.IsStabilized)
                        {
                            yield return HandleDeathSaves(unit);
                        }
                        continue;
                    }

                    // Turn start: always tick effects and cooldowns (even if stunned)
                    SetPhase(CombatPhase.TurnStart);
                    unit.TickTurnStart();

                    // Skip stunned units (after ticking so stun can expire)
                    if (unit.HasStatusEffect(StatusEffectType.Stunned))
                    {
                        OnNarratorQuip?.Invoke($"{unit.Name} is stunned and loses their turn!");
                        continue;
                    }

                    // Apply burning damage at turn start
                    if (unit.HasStatusEffect(StatusEffectType.Burning))
                    {
                        int burnDamage = DiceRoller.Roll(6);
                        unit.TakeDamage(burnDamage);
                        OnNarratorQuip?.Invoke($"{unit.Name} takes {burnDamage} fire damage from burning!");

                        if (!unit.IsConscious)
                        {
                            OnUnitDowned?.Invoke(unit);
                            continue;
                        }
                    }

                    OnTurnStarted?.Invoke(unit);
                    _turnCount++;

                    if (unit.IsPlayer)
                    {
                        // Wait for player input
                        SetPhase(CombatPhase.PlayerChooseAction);
                        OnPlayerActionNeeded?.Invoke(unit);

                        // Wait until phase advances past PlayerChooseTarget
                        while (_currentPhase == CombatPhase.PlayerChooseAction ||
                               _currentPhase == CombatPhase.PlayerChooseTarget)
                        {
                            yield return null;
                        }
                    }
                    else
                    {
                        // AI decision
                        yield return HandleEnemyTurn(unit);
                    }

                    OnTurnEnded?.Invoke(unit);

                    // Check end conditions
                    SetPhase(CombatPhase.CheckEndCondition);
                    var endResult = CheckEndCondition();
                    if (endResult.HasValue)
                    {
                        yield return EndCombat(endResult.Value);
                        yield break;
                    }
                }
            }
        }

        /// <summary>
        /// Called by UI when the player selects an action and target.
        /// </summary>
        public void SubmitPlayerAction(RuntimeAbility ability, CombatUnit target)
        {
            if (_currentPhase != CombatPhase.PlayerChooseAction &&
                _currentPhase != CombatPhase.PlayerChooseTarget)
                return;

            if (CurrentUnit == null || !CurrentUnit.IsPlayer)
                return;

            StartCoroutine(ResolvePlayerAction(ability, target));
        }

        /// <summary>
        /// Called by UI when the player chooses to defend. Grants bonus AC and ends the turn.
        /// </summary>
        public void SubmitDefend()
        {
            if (_currentPhase != CombatPhase.PlayerChooseAction &&
                _currentPhase != CombatPhase.PlayerChooseTarget)
                return;

            if (CurrentUnit == null || !CurrentUnit.IsPlayer)
                return;

            CurrentUnit.BonusAC = GameConstants.DEFEND_AC_BONUS;
            OnNarratorQuip?.Invoke($"{CurrentUnit.Name} takes a defensive stance. (+{GameConstants.DEFEND_AC_BONUS} AC)");
            SetPhase(CombatPhase.TurnEnd);
        }

        private IEnumerator ResolvePlayerAction(RuntimeAbility ability, CombatUnit target)
        {
            SetPhase(CombatPhase.ResolveAction);
            var unit = CurrentUnit;

            yield return ResolveAbility(unit, target, ability);

            ability.StartCooldown();

            SetPhase(CombatPhase.TurnEnd);
        }

        private IEnumerator HandleEnemyTurn(CombatUnit enemy)
        {
            SetPhase(CombatPhase.ResolveAction);

            var decision = EnemyAI.Decide(enemy, _playerUnits, _enemyUnits);

            if (decision.ChosenAbility == null || decision.Target == null)
            {
                OnNarratorQuip?.Invoke($"{enemy.Name} hesitates and does nothing. Pathetic.");
                yield return new WaitForSeconds(0.5f);
                SetPhase(CombatPhase.TurnEnd);
                yield break;
            }

            yield return ResolveAbility(enemy, decision.Target, decision.ChosenAbility);

            decision.ChosenAbility.StartCooldown();

            yield return new WaitForSeconds(0.5f);
            SetPhase(CombatPhase.TurnEnd);
        }

        private IEnumerator ResolveAbility(CombatUnit actor, CombatUnit target, RuntimeAbility runtimeAbility)
        {
            var ability = runtimeAbility.Data;

            // Gather all targets for multi-target abilities
            var targets = GatherTargets(actor, target, ability.TargetType);

            foreach (var currentTarget in targets)
            {
                if (!currentTarget.IsAlive || !currentTarget.IsConscious) continue;

                if (ability.DealsDamage)
                {
                    yield return ResolveDamage(actor, currentTarget, ability);
                }
                else if (ability.Heals)
                {
                    var result = DamageCalculator.ResolveHeal(actor, ability);
                    currentTarget.Heal(result.TotalHealed);
                    OnHealResolved?.Invoke(actor, currentTarget, result);
                }
                else if (ability.AppliesStatusEffect)
                {
                    currentTarget.ApplyStatusEffect(ability.AppliesEffect, ability.EffectDurationTurns);
                }

                if (targets.Count > 1)
                {
                    yield return new WaitForSeconds(0.15f);
                }
            }

            yield return new WaitForSeconds(0.3f);
        }

        private List<CombatUnit> GatherTargets(CombatUnit actor, CombatUnit singleTarget, AbilityTargetType targetType)
        {
            var targets = new List<CombatUnit>();

            switch (targetType)
            {
                case AbilityTargetType.AllEnemies:
                    var enemies = actor.IsPlayer ? _enemyUnits : _playerUnits;
                    foreach (var u in enemies)
                    {
                        if (u.IsAlive && u.IsConscious) targets.Add(u);
                    }
                    break;

                case AbilityTargetType.AllAllies:
                    var allies = actor.IsPlayer ? _playerUnits : _enemyUnits;
                    foreach (var u in allies)
                    {
                        if (u.IsAlive) targets.Add(u);
                    }
                    break;

                default:
                    targets.Add(singleTarget);
                    break;
            }

            return targets;
        }

        private IEnumerator ResolveDamage(CombatUnit actor, CombatUnit target, AbilitySO ability)
        {
            bool advantage = target.HasStatusEffect(StatusEffectType.Invisible) ? false :
                             actor.HasStatusEffect(StatusEffectType.Invisible);
            bool disadvantage = actor.HasStatusEffect(StatusEffectType.Frightened) ||
                                actor.HasStatusEffect(StatusEffectType.Poisoned);

            var result = DamageCalculator.ResolveAttack(actor, target, ability, advantage, disadvantage);
            OnAttackResolved?.Invoke(actor, target, result);

            if (result.IsHit)
            {
                target.TakeDamage(result.TotalDamage);

                if (result.AppliedEffect != StatusEffectType.None && result.EffectDuration > 0)
                {
                    if (ability.SavingThrowDC > 0)
                    {
                        var save = DamageCalculator.ResolveSavingThrow(target, ability.SavingThrowAbility, ability.SavingThrowDC);
                        if (!save.Success)
                        {
                            target.ApplyStatusEffect(result.AppliedEffect, result.EffectDuration);
                        }
                    }
                    else
                    {
                        target.ApplyStatusEffect(result.AppliedEffect, result.EffectDuration);
                    }
                }

                if (!target.IsConscious)
                {
                    OnUnitDowned?.Invoke(target);

                    if (!target.IsPlayer)
                    {
                        target.Die();
                        OnUnitDied?.Invoke(target);
                    }
                }

                if (result.IsCriticalHit && ability.CritQuips != null && ability.CritQuips.Length > 0)
                {
                    OnNarratorQuip?.Invoke(ability.CritQuips[UnityEngine.Random.Range(0, ability.CritQuips.Length)]);
                }
                else if (ability.HitQuips != null && ability.HitQuips.Length > 0)
                {
                    OnNarratorQuip?.Invoke(ability.HitQuips[UnityEngine.Random.Range(0, ability.HitQuips.Length)]);
                }
            }
            else
            {
                if (result.IsCriticalFail)
                {
                    OnNarratorQuip?.Invoke($"{actor.Name} rolls a natural 1! Stick Break!");
                }
                else if (ability.MissQuips != null && ability.MissQuips.Length > 0)
                {
                    OnNarratorQuip?.Invoke(ability.MissQuips[UnityEngine.Random.Range(0, ability.MissQuips.Length)]);
                }
            }

            yield return null;
        }

        private IEnumerator HandleDeathSaves(CombatUnit unit)
        {
            if (!unit.IsPlayer)
            {
                // Enemies just die when downed
                unit.Die();
                OnUnitDied?.Invoke(unit);
                yield break;
            }

            int roll = DiceRoller.RollD20();

            // Nat 20: revive with 1 HP
            if (DiceRoller.IsCriticalHit(roll))
            {
                unit.ReviveFromDeathSave();
                OnDeathSaveResult?.Invoke(unit, true);
                OnNarratorQuip?.Invoke($"{unit.Name} rolls a natural 20! They refuse to die!");
                yield break;
            }

            bool isCriticalFail = DiceRoller.IsCriticalFail(roll);
            bool success = !isCriticalFail && roll >= 10;

            // Apply primary death save result
            bool resolved = unit.RecordDeathSave(success);

            // Nat 1: apply second failure only if still alive
            if (isCriticalFail && unit.IsAlive)
            {
                resolved = unit.RecordDeathSave(false);
            }
            OnDeathSaveResult?.Invoke(unit, success);

            if (resolved && !unit.IsAlive)
            {
                OnUnitDied?.Invoke(unit);
                OnNarratorQuip?.Invoke($"{unit.Name} has fallen. Permanently.");
            }

            yield return new WaitForSeconds(0.5f);
        }

        private void RollInitiative()
        {
            _turnOrder.Clear();

            foreach (var unit in _allUnits)
            {
                int dexMod = unit.Stats.GetModifier(AbilityScore.Dexterity);
                unit.Initiative = DiceRoller.RollD20() + dexMod;
            }

            _turnOrder.AddRange(_allUnits);
            _turnOrder.Sort((a, b) =>
            {
                int compare = b.Initiative.CompareTo(a.Initiative);
                if (compare != 0) return compare;
                // Tie-break by DEX score
                return b.Stats.GetScore(AbilityScore.Dexterity).CompareTo(a.Stats.GetScore(AbilityScore.Dexterity));
            });
        }

        private bool? CheckEndCondition()
        {
            // Check if all players are dead
            bool allPlayersDead = true;
            foreach (var unit in _playerUnits)
            {
                if (unit.IsAlive) { allPlayersDead = false; break; }
            }
            if (allPlayersDead) return false;

            // Check victory condition
            switch (_encounter.VictoryCondition)
            {
                case VictoryCondition.DefeatAll:
                    bool allEnemiesDead = true;
                    foreach (var unit in _enemyUnits)
                    {
                        if (unit.IsAlive) { allEnemiesDead = false; break; }
                    }
                    if (allEnemiesDead) return true;
                    break;

                case VictoryCondition.SurviveTurns:
                    if (_turnCount >= _encounter.SurvivalTurns) return true;
                    break;

                case VictoryCondition.DefeatBoss:
                    bool bossAlive = false;
                    foreach (var unit in _enemyUnits)
                    {
                        if (unit.IsAlive && unit.EnemyData != null && unit.EnemyData.Tier == EnemyTier.Boss)
                        {
                            bossAlive = true;
                            break;
                        }
                    }
                    if (!bossAlive) return true;
                    break;
            }

            return null;
        }

        private IEnumerator EndCombat(bool victory)
        {
            if (victory)
            {
                SetPhase(CombatPhase.Victory);
                if (_encounter.VictoryLines != null && _encounter.VictoryLines.Length > 0)
                {
                    OnNarratorQuip?.Invoke(_encounter.VictoryLines[UnityEngine.Random.Range(0, _encounter.VictoryLines.Length)]);
                }
            }
            else
            {
                SetPhase(CombatPhase.Defeat);
                if (_encounter.DefeatLines != null && _encounter.DefeatLines.Length > 0)
                {
                    OnNarratorQuip?.Invoke(_encounter.DefeatLines[UnityEngine.Random.Range(0, _encounter.DefeatLines.Length)]);
                }
            }

            yield return new WaitForSeconds(1f);

            OnCombatResult?.Invoke(victory);
            OnCombatEnded?.Invoke();
        }

        private void SetPhase(CombatPhase phase)
        {
            _currentPhase = phase;
            OnPhaseChanged?.Invoke(phase);
        }
    }
}
