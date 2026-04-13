using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Linq;
using Grants.Engine;
using Grants.Fighters.Grants;
using Grants.Models.Cards;
using Grants.Models.Fighter;
using Grants.Models.Match;
using Grants.UI;
using MatchType = Grants.Models.Match.MatchType;

namespace Grants.Screens;

public partial class FightScreen
{
    // ---- Pre-round self-choice (generic persona protocol) ----

    private PersonaChoiceRequest? _preRoundChoice = null;
    private int _preRoundChoiceIndex = 0;

    private void ExecuteRound()
    {
        var round = ResolutionEngine.ResolveFirstHalf(_match);
        _firstHalfLogCount = round.FirstHalfLogCount;
        _needsSecondHalf = _match.Phase == MatchPhase.RoundMidpoint;
        int firstHalfEnd = _needsSecondHalf ? _firstHalfLogCount : round.Log.Count;
        _resolutionSteps = BuildResolutionSteps(round.Log.Take(firstHalfEnd).ToList());
        _stepIndex = 0;
        _resolutionFullyDisplayed = false;
        _roundLog = round.Log.ToList();
    }

    private void AdvanceResolutionStep()
    {
        _stepIndex++;
        if (_stepIndex >= _resolutionSteps.Count)
        {
            if (_needsSecondHalf)
            {
                ResolutionEngine.ResolveSecondHalf(_match);
                _roundLog = _match.CurrentRoundState!.Log.ToList();
                var secondHalfLines = _roundLog.Skip(_firstHalfLogCount).ToList();
                var secondHalfSteps = BuildResolutionSteps(secondHalfLines);
                _resolutionSteps.AddRange(secondHalfSteps);
                _needsSecondHalf = false;
                if (_stepIndex >= _resolutionSteps.Count)
                    _resolutionFullyDisplayed = true;
            }
            else
            {
                _resolutionFullyDisplayed = true;
            }
        }
    }

    private static List<List<string>> BuildResolutionSteps(List<string> lines)
    {
        var steps = new List<List<string>>();
        var current = new List<string>();
        foreach (var line in lines)
        {
            bool startsNew = line.Contains(" moves to ") || line.StartsWith("Simultaneous:")
                          || line.StartsWith("---");
            if (startsNew && current.Count > 0)
            {
                steps.Add(current);
                current = new List<string>();
            }
            current.Add(line);
        }
        if (current.Count > 0)
            steps.Add(current);
        return steps;
    }

    private void StartNewRound()
    {
        _preRoundLog.Clear();
        // Clear round-scoped stat modifiers left over from the previous round
        _match.FighterA.RoundPowerModifier   = 0;
        _match.FighterA.RoundDefenseModifier = 0;
        _match.FighterA.RoundSpeedModifier   = 0;
        _match.FighterB.RoundPowerModifier   = 0;
        _match.FighterB.RoundDefenseModifier = 0;
        _match.FighterB.RoundSpeedModifier   = 0;
        _match.Stage.ApplyPreRoundEffects(_match.FighterA, _match, _match.StageState, _preRoundLog);
        _match.Stage.ApplyPreRoundEffects(_match.FighterB, _match, _match.StageState, _preRoundLog);
        AdvanceStageChoices(skipA: false);
    }

    private void AdvanceStageChoices(bool skipA)
    {
        if (!skipA && _match.Stage.RequiresRoundStartChoice(_match.FighterA, _match, _match.StageState))
        {
            if (_match.FighterAIsHuman)
            {
                _match.Phase = MatchPhase.StageChoiceA;
                return;
            }
            bool aiA = _match.Stage.ResolveAiChoice(_match.FighterA, _match, _match.StageState);
            _match.Stage.OnFighterChoice(_match.FighterA, aiA, _match, _match.StageState);
        }

        if (_match.Stage.RequiresRoundStartChoice(_match.FighterB, _match, _match.StageState))
        {
            if (_match.FighterBIsHuman)
            {
                _match.Phase = MatchPhase.StageChoiceB;
                return;
            }
            bool aiB = _match.Stage.ResolveAiChoice(_match.FighterB, _match, _match.StageState);
            _match.Stage.OnFighterChoice(_match.FighterB, aiB, _match, _match.StageState);
        }

        AdvancePersonaChoices();
    }

    private void AdvancePersonaChoices()
    {
        // FighterB's persona may offer FighterA a choice
        if (_match.FighterB.Definition.Persona.RequiresOpponentRoundStartChoice(
                _match.FighterB, _match.FighterA, _match, _match.FighterB.PersonaState))
        {
            if (_match.FighterAIsHuman)
            {
                _match.Phase = MatchPhase.PersonaChoiceA;
                return;
            }
            bool aiA = _match.FighterB.Definition.Persona.ResolveAiOpponentChoice(
                _match.FighterB, _match.FighterA, _match, _match.FighterB.PersonaState);
            _match.FighterB.Definition.Persona.OnOpponentChoice(
                _match.FighterB, _match.FighterA, aiA, _match, _match.FighterB.PersonaState);
            if (aiA) _preRoundLog.Add($"[{_match.FighterB.DisplayName}] AI opponent spends a Curse token (-1 Power/-1 Speed).");
        }

        // FighterA's persona may offer FighterB a choice
        if (_match.FighterA.Definition.Persona.RequiresOpponentRoundStartChoice(
                _match.FighterA, _match.FighterB, _match, _match.FighterA.PersonaState))
        {
            if (_match.FighterBIsHuman)
            {
                _match.Phase = MatchPhase.PersonaChoiceB;
                return;
            }
            bool aiB = _match.FighterA.Definition.Persona.ResolveAiOpponentChoice(
                _match.FighterA, _match.FighterB, _match, _match.FighterA.PersonaState);
            _match.FighterA.Definition.Persona.OnOpponentChoice(
                _match.FighterA, _match.FighterB, aiB, _match, _match.FighterA.PersonaState);
            if (aiB) _preRoundLog.Add($"[{_match.FighterA.DisplayName}] AI opponent spends a Curse token (-1 Power/-1 Speed).");
        }

        AdvancePersonaSelfChoiceA();
    }

    private void AdvancePersonaSelfChoiceA()
    {
        // FighterA's persona may offer FighterA a self-choice
        if (_match.FighterA.Definition.Persona.RequiresSelfRoundStartChoice(
                _match.FighterA, _match.FighterB, _match, _match.FighterA.PersonaState))
        {
            if (_match.FighterAIsHuman)
            {
                _match.Phase = MatchPhase.PersonaSelfChoiceA;
                return;
            }
            bool aiSelf = _match.FighterA.Definition.Persona.ResolveAiSelfChoice(
                _match.FighterA, _match.FighterB, _match, _match.FighterA.PersonaState);
            _match.FighterA.Definition.Persona.OnSelfChoice(
                _match.FighterA, _match.FighterB, aiSelf, _match, _match.FighterA.PersonaState);
            if (aiSelf) _preRoundLog.Add($"[{_match.FighterA.DisplayName}] spends from pool (+2 Pwr/+2 Spd).");
        }

        AdvancePersonaSelfChoiceB();
    }

    private void AdvancePersonaSelfChoiceB()
    {
        // FighterB's persona may offer FighterB a self-choice
        if (_match.FighterB.Definition.Persona.RequiresSelfRoundStartChoice(
                _match.FighterB, _match.FighterA, _match, _match.FighterB.PersonaState))
        {
            if (_match.FighterBIsHuman)
            {
                _match.Phase = MatchPhase.PersonaSelfChoiceB;
                return;
            }
            bool aiSelf = _match.FighterB.Definition.Persona.ResolveAiSelfChoice(
                _match.FighterB, _match.FighterA, _match, _match.FighterB.PersonaState);
            _match.FighterB.Definition.Persona.OnSelfChoice(
                _match.FighterB, _match.FighterA, aiSelf, _match, _match.FighterB.PersonaState);
            if (aiSelf) _preRoundLog.Add($"[{_match.FighterB.DisplayName}] spends from pool (+2 Pwr/+2 Spd).");
        }

        AdvancePreRoundSelfChoiceA();
    }

    private void AdvancePreRoundSelfChoiceA()
    {
        var fa = _match.FighterA;
        var req = fa.Definition.Persona.GetPreRoundSelfChoice(fa, _match.FighterB, _match, fa.PersonaState);
        if (req != null)
        {
            _preRoundChoice = req;
            _preRoundChoiceIndex = 0;
            if (_match.FighterAIsHuman)
            {
                _match.Phase = MatchPhase.PreRoundSelfChoiceA;
                return;
            }
            string? chosen = fa.Definition.Persona.ResolveAiPreRoundSelfChoice(fa, _match.FighterB, _match, fa.PersonaState);
            fa.Definition.Persona.OnPreRoundSelfChoiceSelected(fa, chosen, _match, fa.PersonaState);
            if (chosen != null)
            {
                var opt = req.Options.FirstOrDefault(o => o.Id == chosen);
                if (opt != null) _preRoundLog.Add($"[{fa.DisplayName}] {opt.Label}");
            }
        }
        AdvancePreRoundSelfChoiceB();
    }

    private void AdvancePreRoundSelfChoiceB()
    {
        var fb = _match.FighterB;
        var req = fb.Definition.Persona.GetPreRoundSelfChoice(fb, _match.FighterA, _match, fb.PersonaState);
        if (req != null)
        {
            _preRoundChoice = req;
            _preRoundChoiceIndex = 0;
            if (_match.FighterBIsHuman)
            {
                _match.Phase = MatchPhase.PreRoundSelfChoiceB;
                return;
            }
            string? chosen = fb.Definition.Persona.ResolveAiPreRoundSelfChoice(fb, _match.FighterA, _match, fb.PersonaState);
            fb.Definition.Persona.OnPreRoundSelfChoiceSelected(fb, chosen, _match, fb.PersonaState);
            if (chosen != null)
            {
                var opt = req.Options.FirstOrDefault(o => o.Id == chosen);
                if (opt != null) _preRoundLog.Add($"[{fb.DisplayName}] {opt.Label}");
            }
        }
        EnterCardSelection();
    }

    private void EnterCardSelection()
    {
        _match.Phase = MatchPhase.CardSelection;
        _resolutionSteps.Clear();
        _stepIndex = 0;
        _resolutionFullyDisplayed = false;
        _needsSecondHalf = false;
        _roundLog.Clear();
        LoadAvailablePairs();
    }

    /// <summary>
    /// After FighterB responds to FighterA's persona choice prompt, check whether
    /// FighterA's persona also needs a choice from FighterB and advance accordingly.
    /// </summary>
    private void AdvanceAfterFighterBPersonaChoice()
    {
        if (_match.FighterA.Definition.Persona.RequiresOpponentRoundStartChoice(
                _match.FighterA, _match.FighterB, _match, _match.FighterA.PersonaState))
        {
            if (_match.FighterBIsHuman) { _match.Phase = MatchPhase.PersonaChoiceB; }
            else
            {
                bool aiB = _match.FighterA.Definition.Persona.ResolveAiOpponentChoice(
                    _match.FighterA, _match.FighterB, _match, _match.FighterA.PersonaState);
                _match.FighterA.Definition.Persona.OnOpponentChoice(
                    _match.FighterA, _match.FighterB, aiB, _match, _match.FighterA.PersonaState);
                EnterCardSelection();
            }
        }
        else EnterCardSelection();
    }

    private void Resign()
    {
        _resignPending = false;
        _match.Winner = _match.FighterB;
        _match.Loser = _match.FighterA;
        _match.Phase = MatchPhase.MatchOver;
    }

    private void HandleMatchEnd()
    {
        bool won = !_match.IsDraw && _match.Winner == _match.FighterA;
        SwitchTo(ScreenType.PostMatch, (_match, won));
    }

    private void ReplayMatch()
    {
        OnEnter(_replayData);
    }
}
