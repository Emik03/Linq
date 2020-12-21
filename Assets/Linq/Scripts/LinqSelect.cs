using KModkit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Rnd = UnityEngine.Random;

namespace Linq
{
    internal class LinqSelect
    {
        internal LinqSelect(LinqScript linq)
        {
            _linq = linq;
            _linq.ModuleId = ++LinqScript.moduleIdCounter;

            List<LinqFunctions> allFunctions = Enum.GetValues(typeof(LinqFunctions)).Cast<LinqFunctions>().ToList();

            if (Application.isEditor)
            {
                isInverted = _linq.ModuleId / allFunctions.Count() % 2 == 1;
                functions = Enumerable.Repeat(allFunctions[_linq.ModuleId % allFunctions.Count()], MaxStage).ToArray();
            }

            else
            {
                isInverted = Rnd.Range(0, 1f) > 0.5f;
                functions = allFunctions.Shuffle().Take(MaxStage).ToArray();
            }

            _linq.StartCoroutine(WaitForSerialNumber());
        }

        internal bool isAnimating, isInverted;
        internal bool[] buttonStates = new bool[6], initialButtonStates = new bool[6];
        internal const int MaxStage = 6; 
        internal int currentStage;
        internal object parameter;
        internal readonly LinqFunctions[] functions;

        private static readonly int[] _invertedIndexes = { 0, 3, 1, 4, 2, 5 };
        private readonly LinqScript _linq;

        internal KMSelectable.OnInteractHandler ButtonPress(int i)
        {
            return delegate ()
            {
                _linq.Buttons[i].AddInteractionPunch();
                _linq.Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, _linq.Buttons[i].transform);
                _linq.Audio.PlaySoundAtTransform((!isInverted ? i + 1 : _invertedIndexes[i] + 1).ToString(), _linq.Buttons[i].transform);

                if (isAnimating)
                    return false;
                if (!_linq.IsSolved)
                    if (!isInverted)
                        Function.InvertBoolean(ref buttonStates[i]);
                    else
                        Function.InvertBoolean(ref buttonStates[_invertedIndexes[i]]);
                else
                    for (int j = 0; j < _linq.Buttons.Length; j++)
                        if (i != j)
                            if (!isInverted)
                                Function.InvertBoolean(ref buttonStates[j]);
                            else
                                Function.InvertBoolean(ref buttonStates[_invertedIndexes[j]]);

                UpdateButtons();
                return false;
            };
        }

        internal Action OnHighlight(KMHighlightable highlightable)
        {
            return delegate ()
            {
                highlightable.GetComponent<MeshRenderer>().enabled = true;
            };
        }

        internal Action OnHighlightEnded(KMHighlightable highlightable)
        {
            return delegate ()
            {
                highlightable.GetComponent<MeshRenderer>().enabled = false;
            };
        }

        internal KMSelectable.OnInteractHandler TextPress()
        {
            return delegate ()
            {
                if (_linq.IsSolved)
                    return false;

                bool[] answer = LinqValidate.Run(_linq.Info.GetSerialNumber(), initialButtonStates, functions[currentStage], parameter);
                
                _linq.TextSelectable.AddInteractionPunch(2);
                _linq.Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.TypewriterKey, _linq.TextSelectable.transform);
                _linq.Audio.PlaySoundAtTransform(answer.SequenceEqual(buttonStates) ? "8" : "7", _linq.TextSelectable.transform);

                if (answer.SequenceEqual(buttonStates))
                {
                    currentStage++;

                    if (currentStage == MaxStage)
                        _linq.StartCoroutine(Solve());

                    else
                        Generate();
                }

                else
                {
                    Debug.LogFormat("[Linq #{0}]: Strike! Expected {1} but received {2}.", _linq.ModuleId, answer.Select(b => b ? "O" : "-").Join(""), buttonStates.Select(b => b ? "O" : "-").Join(""));
                    Array.Copy(initialButtonStates, buttonStates, 6);
                    UpdateButtons();
                    _linq.Module.HandleStrike();
                }

                return false;
            };
        }

        private IEnumerator WaitForSerialNumber()
        {
            yield return new WaitWhile(() => _linq.Info.GetSerialNumber() == null);
            Generate();
        }

        private void Generate()
        {
            do initialButtonStates = Function.RandomBools(6);
            while (initialButtonStates.Distinct().Count() == 1);
            Array.Copy(initialButtonStates, buttonStates, 6);
            UpdateButtons();
            
            parameter = null;
            switch (functions[currentStage])
            {
                case LinqFunctions.Skip:
                case LinqFunctions.SkipLast:
                case LinqFunctions.Take:
                case LinqFunctions.TakeLast:
                case LinqFunctions.ElementAt:
                    parameter = Rnd.Range(0, buttonStates.Where(b => b).Count());
                    break;

                case LinqFunctions.Except:
                case LinqFunctions.Intersect:
                case LinqFunctions.Concat:
                case LinqFunctions.Append:
                case LinqFunctions.Prepend:
                    parameter = _linq.Info.GetSerialNumber().Take(Rnd.Range(1, 5)).ToArray().Shuffle().Join("");
                    break;
            }

            _linq.Text.text = functions[currentStage].ToString() + "\n(" + parameter + ")";

            bool[] answer = LinqValidate.Run(_linq.Info.GetSerialNumber(), initialButtonStates, functions[currentStage], parameter);
            Debug.LogFormat("[Linq #{0}]: Entering stage {1}. Calling function {2} on {3} returns {4}.", _linq.ModuleId, currentStage + 1, functions[currentStage] + "(" + parameter + ")", initialButtonStates.Select(b => b ? "O" : "-").Join(""), answer.Select(b => b ? "O" : "-").Join(""));
        }

        internal IEnumerator Solve()
        {
            isAnimating = true;

            Debug.LogFormat("[Linq #{0}]: Solved!", _linq.ModuleId);
            _linq.Text.text = "using\nSolve;";
            _linq.IsSolved = true;
            _linq.Module.HandlePass();

            buttonStates = Enumerable.Repeat(true, 6).ToArray();
            bool inverted = false;

            for (int k = 0; k < 2; k++)
            {
                for (int j = 0; j < 2; j++)
                {
                    for (int i = inverted ? _linq.Buttons.Length - 1 : 0; inverted ? i >= 0 : i < _linq.Buttons.Length; i += inverted ? -1 : 1)
                    {
                        _linq.Audio.PlaySoundAtTransform((i + 1).ToString(), _linq.Buttons[i].transform);
                        Function.InvertBoolean(ref buttonStates[i]);
                        UpdateButtons();

                        yield return new WaitForSecondsRealtime(0.1f);
                    }

                    _linq.Audio.PlaySoundAtTransform("8", _linq.TextSelectable.transform);
                    Function.InvertBooleanArray(buttonStates);
                    UpdateButtons();

                    yield return new WaitForSecondsRealtime(0.5f);
                    Function.InvertBooleanArray(buttonStates);
                    UpdateButtons();
                }

                Function.InvertBoolean(ref inverted);
            }

            int[] melody = { 6, 5, 3, 4, 2, 1 };
            buttonStates = !isInverted ? new[] { false, true, true, false, false, true } : new[] { false, true, false, true, false, true };
            for (int i = 0; i < _linq.Buttons.Length; i++)
            {
                _linq.Audio.PlaySoundAtTransform(melody[i].ToString(), _linq.Buttons[melody[i] - 1].transform);

                Function.InvertBooleanArray(buttonStates);
                if (i == _linq.Buttons.Length - 1)
                    buttonStates = Enumerable.Repeat(true, 6).ToArray();

                UpdateButtons();
                yield return new WaitForSecondsRealtime(0.2f);
            }

            buttonStates = new bool[6];
            UpdateButtons();

            isAnimating = false;
        }

        private void UpdateButtons()
        {
            if (!isInverted)
                for (int i = 0; i < _linq.ButtonRenderers.Length; i++)
                    _linq.ButtonRenderers[i].material.color = buttonStates[i] ? new Color32(208, 224, 240, 255) : new Color32(26, 28, 30, 255);
            else
                for (int i = 0; i < _linq.ButtonRenderers.Length; i++)
                    _linq.ButtonRenderers[i].material.color = buttonStates[_invertedIndexes[i]] ? new Color32(208, 224, 240, 255) : new Color32(26, 28, 30, 255);
        }
    }
}
