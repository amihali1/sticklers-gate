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
        public event Action<CombatUnit, ItemSO> OnItemUsed;
        public event Action<CombatUnit> OnMovementPhaseStarted;
        public event Action<CombatUnit, GridPosition, GridPosition> OnUnitMoved; // unit, from, to

        // --- State ---
        private List<CombatUnit> _allUnits = new List<CombatUnit>();
        private List<CombatUnit> _turnOrder = new List<CombatUnit>();
        private List<CombatUnit> _playerUnits = new List<CombatUnit>();
        private List<CombatUnit> _enemyUnits = new List<CombatUnit>();
        private int _currentTurnIndex;
        private CombatPhase _currentPhase;
        private EncounterSO _encounter;
        private int _turnCount;
        private Inventory _inventory;
        private CombatGrid _grid;

        // --- Public State ---
        public CombatPhase CurrentPhase => _currentPhase;
        public CombatGrid Grid => _grid;
        public CombatUnit CurrentUnit => _currentTurnIndex < _turnOrder.Count ? _turnOrder[_currentTurnIndex] : null;
        public IReadOnlyList<CombatUnit> TurnOrder => _turnOrder;
        public IReadOnlyList<CombatUnit> PlayerUnits => _playerUnits;
        public IReadOnlyList<CombatUnit> EnemyUnits => _enemyUnits;
        public int TurnCount => _turnCount;
        public Inventory Inventory => _inventory;

        /// <summary>
        /// Set the party inventory for use during combat.
        /// </summary>
        public void SetInventory(Inventory inventory)
        {
            _inventory = inventory;
        }

        /// <summary>
        /// Start a combat encounter with the given player party and encounter data.
        /// The grid comes from <paramref name="layout"/> if given, otherwise from
        /// the encounter's own layout, otherwise the default grid.
        /// </summary>
        public void StartCombat(List<CombatUnit> playerUnits, EncounterSO encounter, GridLayoutSO layout = null)
        {
            _encounter = encounter;
            _playerUnits = new List<CombatUnit>(playerUnits);
            _turnCount = 0;

            // Spawn enemies from encounter data
            _enemyUnits = new List<CombatUnit>();
            foreach (var spawn in encounter.Enemies)
            {
                var unit = CombatUnit.FromEnemy(spawn.enemyData, spawn.nameOverride);
                _enemyUnits.Add(unit);
            }

            _allUnits = new List<CombatUnit>();
            _allUnits.AddRange(_playerUnits);
            _allUnits.AddRange(_enemyUnits);

            // Build the combat grid and place every unit on a tile.
            SetupGrid(layout != null ? layout : encounter.GridLayout);

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
                        // Movement phase: player picks a reachable tile or skips.
                        SetPhase(CombatPhase.PlayerChooseMovement);
                        OnMovementPhaseStarted?.Invoke(unit);
                        while (_currentPhase == CombatPhase.PlayerChooseMovement)
                        {
                            yield return null;
                        }

                        // SubmitPlayerMovement/SkipMovement advances into the action
                        // phase and fires OnPlayerActionNeeded. Wait for the action.
                        while (_currentPhase == CombatPhase.PlayerChooseAction ||
                               _currentPhase == CombatPhase.PlayerChooseTarget)
                        {
                            yield return null;
                        }
                    }
                    else
                    {
                        // AI decision (movement, then action)
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
        /// Called by UI when the player moves the active unit to a tile. Ignored
        /// if the tile is not reachable within the unit's movement this turn.
        /// Advances to the action phase on success.
        /// </summary>
        public void SubmitPlayerMovement(GridPosition destination)
        {
            if (_currentPhase != CombatPhase.PlayerChooseMovement) return;
            if (CurrentUnit == null || !CurrentUnit.IsPlayer) return;

            if (destination != CurrentUnit.Position)
            {
                if (!IsReachable(CurrentUnit, destination)) return;
                MoveUnitTo(CurrentUnit, destination);
            }

            BeginPlayerActionPhase();
        }

        /// <summary>
        /// Called by UI when the player declines to move. Advances to the action
        /// phase without moving.
        /// </summary>
        public void SkipMovement()
        {
            if (_currentPhase != CombatPhase.PlayerChooseMovement) return;
            if (CurrentUnit == null || !CurrentUnit.IsPlayer) return;

            BeginPlayerActionPhase();
        }

        private void BeginPlayerActionPhase()
        {
            SetPhase(CombatPhase.PlayerChooseAction);
            OnPlayerActionNeeded?.Invoke(CurrentUnit);
        }

        /// <summary>True if the tile is reachable by the unit within its movement this turn.</summary>
        private bool IsReachable(CombatUnit unit, GridPosition destination)
        {
            if (_grid == null) return false;
            var reachable = Pathfinder.GetReachablePositions(_grid, unit.Position, unit.MovementSpeed);
            return reachable.Contains(destination);
        }

        /// <summary>Moves a unit on the grid, updates its position, and fires OnUnitMoved.</summary>
        private void MoveUnitTo(CombatUnit unit, GridPosition destination)
        {
            GridPosition from = unit.Position;
            _grid.MoveOccupant(unit, destination);
            unit.Position = destination;
            OnUnitMoved?.Invoke(unit, from, destination);
        }

        /// <summary>
        /// Whether the actor can currently reach the target with the ability. AoE
        /// and self abilities always qualify.
        /// </summary>
        private static bool InRange(CombatUnit actor, RuntimeAbility ability, CombatUnit target)
        {
            // IsInRange short-circuits true for AoE/self (target may be null there).
            return DamageCalculator.IsInRange(actor, target, ability.Data);
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

            // Enforce ability range for single-target actions; keep the player's
            // turn so they can pick a reachable target (or move next time).
            if (!InRange(CurrentUnit, ability, target))
            {
                if (target != null)
                    OnNarratorQuip?.Invoke($"{target.Name} is out of range for {ability.Data.AbilityName}.");
                return;
            }

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

        /// <summary>
        /// Called by UI when the player uses a consumable item on a target.
        /// </summary>
        public void SubmitUseItem(ItemSO item, CombatUnit target)
        {
            if (_currentPhase != CombatPhase.PlayerChooseAction &&
                _currentPhase != CombatPhase.PlayerChooseTarget)
                return;

            if (CurrentUnit == null || !CurrentUnit.IsPlayer)
                return;

            if (_inventory == null || !_inventory.Has(item))
                return;

            StartCoroutine(ResolveItem(CurrentUnit, target, item));
        }

        private IEnumerator ResolveItem(CombatUnit user, CombatUnit target, ItemSO item)
        {
            SetPhase(CombatPhase.ResolveAction);

            _inventory.Remove(item);
            OnItemUsed?.Invoke(user, item);

            var targets = GatherTargets(user, target, item.TargetType);

            foreach (var t in targets)
            {
                if (!t.IsAlive) continue;

                if (item.Heals)
                {
                    t.Heal(item.HealAmount);
                    OnHealResolved?.Invoke(user, t,
                        new HealResult { TotalHealed = item.HealAmount });
                    OnNarratorQuip?.Invoke($"{user.Name} uses {item.ItemName} on {t.Name}. +{item.HealAmount} HP!");
                }

                if (item.DealsDamage && t.IsConscious)
                {
                    t.TakeDamage(item.DamageAmount);
                    OnNarratorQuip?.Invoke($"{user.Name} throws {item.ItemName} at {t.Name}! {item.DamageAmount} damage!");

                    if (!t.IsConscious)
                    {
                        OnUnitDowned?.Invoke(t);
                        if (!t.IsPlayer)
                        {
                            t.Die();
                            OnUnitDied?.Invoke(t);
                        }
                    }
                }

                if (item.RemovesStatus)
                {
                    t.RemoveStatusEffect(item.RemovesEffect);
                    OnNarratorQuip?.Invoke($"{user.Name} uses {item.ItemName}. {item.RemovesEffect} removed from {t.Name}!");
                }

                if (item.AppliesEffect != StatusEffectType.None && item.EffectDuration > 0)
                {
                    t.ApplyStatusEffect(item.AppliesEffect, item.EffectDuration);
                }

                if (targets.Count > 1)
                {
                    yield return new WaitForSeconds(0.15f);
                }
            }

            // Quips
            if (item.UseQuips != null && item.UseQuips.Length > 0)
            {
                OnNarratorQuip?.Invoke(item.UseQuips[UnityEngine.Random.Range(0, item.UseQuips.Length)]);
            }

            yield return new WaitForSeconds(0.3f);
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
            // Movement phase: reposition toward the player party.
            GridPosition destination = EnemyAI.DecideMovement(enemy, _grid, _playerUnits);
            if (destination != enemy.Position && _grid.IsWalkable(destination))
            {
                MoveUnitTo(enemy, destination);
                yield return new WaitForSeconds(0.3f);
            }

            SetPhase(CombatPhase.ResolveAction);

            var decision = EnemyAI.Decide(enemy, _playerUnits, _enemyUnits);

            if (decision.ChosenAbility == null || decision.Target == null)
            {
                OnNarratorQuip?.Invoke($"{enemy.Name} hesitates and does nothing. Pathetic.");
                yield return new WaitForSeconds(0.5f);
                SetPhase(CombatPhase.TurnEnd);
                yield break;
            }

            // Range guard: if the chosen single-target ability can't reach even
            // after moving, forfeit the attack this turn.
            if (!InRange(enemy, decision.ChosenAbility, decision.Target))
            {
                OnNarratorQuip?.Invoke($"{enemy.Name} can't reach {decision.Target.Name} and holds position.");
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
                    bool applyEffect = true;
                    if (ability.SavingThrowDC > 0)
                    {
                        var save = DamageCalculator.ResolveSavingThrow(
                            currentTarget, ability.SavingThrowAbility, ability.SavingThrowDC);
                        if (save.Success)
                        {
                            applyEffect = false;
                            OnNarratorQuip?.Invoke($"{currentTarget.Name} resists the effect!");
                        }
                    }

                    if (applyEffect)
                    {
                        currentTarget.ApplyStatusEffect(ability.AppliesEffect, ability.EffectDurationTurns);
                    }
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

        /// <summary>
        /// Builds the combat grid (from the layout, or a default one) and places
        /// every unit on a starting tile. Players spawn on the left column, enemies
        /// on the right, unless the layout supplies explicit spawn points.
        /// </summary>
        private void SetupGrid(GridLayoutSO layout)
        {
            _grid = layout != null
                ? CombatGrid.FromLayout(layout)
                : new CombatGrid(GameConstants.DEFAULT_GRID_WIDTH, GameConstants.DEFAULT_GRID_HEIGHT);

            var playerSpawns = layout != null && layout.PlayerSpawns.Length > 0 ? layout.PlayerSpawns : null;
            var enemySpawns = layout != null && layout.EnemySpawns.Length > 0 ? layout.EnemySpawns : null;

            PlaceSide(_playerUnits, playerSpawns, defaultColumn: 1);
            PlaceSide(_enemyUnits, enemySpawns, defaultColumn: _grid.Width - 2);
        }

        /// <summary>
        /// Places one side's units on the grid. Uses explicit spawn points when
        /// available, otherwise stacks units down a default column. Falls back to
        /// the nearest free tile if a preferred tile is blocked or occupied.
        /// </summary>
        private void PlaceSide(List<CombatUnit> units, GridPosition[] spawns, int defaultColumn)
        {
            int column = Mathf.Clamp(defaultColumn, 0, _grid.Width - 1);

            for (int i = 0; i < units.Count; i++)
            {
                GridPosition preferred = spawns != null && i < spawns.Length
                    ? spawns[i]
                    : new GridPosition(column, Mathf.Min(i, _grid.Height - 1));

                GridPosition tile = FindFreeTile(preferred);
                units[i].Position = tile;
                _grid.PlaceOccupant(units[i], tile);
            }
        }

        /// <summary>
        /// Returns the preferred tile if walkable, otherwise the first walkable
        /// tile found scanning the grid. Assumes the grid has capacity for all units.
        /// </summary>
        private GridPosition FindFreeTile(GridPosition preferred)
        {
            if (_grid.IsWalkable(preferred))
                return preferred;

            for (int x = 0; x < _grid.Width; x++)
            {
                for (int y = 0; y < _grid.Height; y++)
                {
                    var candidate = new GridPosition(x, y);
                    if (_grid.IsWalkable(candidate))
                        return candidate;
                }
            }

            // Should never happen for correctly sized grids; surface loudly if it does.
            throw new InvalidOperationException("No free tile available on the combat grid for unit placement.");
        }
    }
}
