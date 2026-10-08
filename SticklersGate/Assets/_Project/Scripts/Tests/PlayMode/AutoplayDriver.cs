using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using SticklersGate.Combat;
using SticklersGate.Data;

namespace SticklersGate.Tests.PlayMode
{
    /// <summary>
    /// Plays the player side of a running combat through the same
    /// CombatStateMachine API the UI uses, using <see cref="AutoPilot"/>
    /// decisions, and records a readable combat log. Rejected commands fall
    /// back to Defend so a bad decision can never stall the battle.
    /// </summary>
    public class AutoplayDriver : MonoBehaviour
    {
        /// <summary>Real-time seconds to let movement/attacks animate between commands (scaled by timeScale).</summary>
        private const float STEP_DELAY = 0.35f;

        public readonly StringBuilder Log = new StringBuilder();

        /// <summary>Set when something worth a screenshot just happened (consumed by the test).</summary>
        public string PendingCapture { get; set; }

        /// <summary>Player commands the state machine ignored (bad decision or bug).</summary>
        public int RejectedCommands { get; private set; }

        public int AreaCasts { get; private set; }
        public int PlayerTurns { get; private set; }

        private CombatStateMachine _sm;
        private bool _capturedFirstTurn;

        public void Attach(CombatStateMachine sm)
        {
            _sm = sm;
            _sm.OnPlayerActionNeeded += unit => StartCoroutine(PlayTurn(unit));
            _sm.OnMovementPhaseStarted += unit => StartCoroutine(EndTurnSoon());
            _sm.OnTurnStarted += unit => Write($"--- Turn {_sm.TurnCount}: {unit.Name} ({unit.CurrentHP}/{unit.MaxHP} HP) at {unit.Position}");
            _sm.OnUnitMoved += (unit, from, to) => Write($"  {unit.Name} moves {from} -> {to}");
            // Fires before damage is applied, so report the damage, not the resulting HP
            _sm.OnAttackResolved += (a, t, r) => Write(r.IsHit
                ? $"  {a.Name} hits {t.Name} for {r.TotalDamage}{(r.IsCriticalHit ? " (CRIT)" : "")} (was {t.CurrentHP}/{t.MaxHP})"
                : $"  {a.Name} misses {t.Name}");
            _sm.OnHealResolved += (h, t, r) => Write($"  {h.Name} heals {t.Name} for {r.TotalHealed} -> {t.CurrentHP}/{t.MaxHP}");
            _sm.OnUnitDowned += u => Write($"  ** {u.Name} is down");
            _sm.OnUnitDied += u => Write($"  ** {u.Name} dies");
            _sm.OnNarratorQuip += q => Write($"  [narrator] {q}");
            _sm.OnCombatResult += victory => Write(victory ? "=== VICTORY ===" : "=== DEFEAT ===");
        }

        private void Write(string line) => Log.AppendLine(line);

        private IEnumerator PlayTurn(CombatUnit unit)
        {
            PlayerTurns++;
            yield return null; // let the UI show its panels first

            if (!_capturedFirstTurn)
            {
                _capturedFirstTurn = true;
                PendingCapture = "first-player-turn";
                yield return null;
                yield return null;
            }

            var foes = new List<CombatUnit>(_sm.EnemyUnits);
            var friends = new List<CombatUnit>(_sm.PlayerUnits);

            // Move first (one move toward a good spot; any leftover movement is spent after acting)
            var move = AutoPilot.ChooseMove(unit, _sm.Grid, foes);
            if (move != unit.Position)
            {
                _sm.SubmitPlayerMovement(move);
                if (unit.Position != move)
                {
                    RejectedCommands++;
                    Write($"  !! move to {move} rejected (remaining movement {unit.RemainingMovement})");
                }
                yield return new WaitForSeconds(STEP_DELAY);
            }

            if (_sm.CurrentPhase != CombatPhase.PlayerChooseAction) yield break;

            var plan = AutoPilot.ChooseAction(unit, friends, foes);
            Write($"  plan: {plan.Reason}");

            if (plan.Defend)
            {
                _sm.SubmitDefend();
                yield break;
            }

            if (plan.Point.HasValue)
            {
                AreaCasts++;
                if (AreaCasts == 1) PendingCapture = $"area-{plan.Ability.Data.AbilityName}";
                _sm.SubmitPlayerAreaAction(plan.Ability, plan.Point.Value);
            }
            else
            {
                _sm.SubmitPlayerAction(plan.Ability, plan.Target);
            }

            yield return null;
            if (_sm.CurrentPhase == CombatPhase.PlayerChooseAction)
            {
                RejectedCommands++;
                Write($"  !! {plan.Ability.Data.AbilityName} rejected; defending instead");
                _sm.SubmitDefend();
            }
        }

        private IEnumerator EndTurnSoon()
        {
            yield return new WaitForSeconds(STEP_DELAY);
            if (_sm.CurrentPhase == CombatPhase.PlayerChooseMovement) _sm.EndTurn();
        }
    }
}
