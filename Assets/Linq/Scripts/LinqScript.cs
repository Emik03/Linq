using KModkit;
using Linq;
using System;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class LinqScript : ModuleScript
{
    #region Fields
    public override KMAudio KMAudio { get { return Audio; } }
    public KMAudio Audio;

    public override KMBombInfo KMBombInfo { get { return Info; } }
    public KMBombInfo Info;

    public override KMBombModule KMBombModule { get { return Module; } }
    public KMBombModule Module;

    public override KMSelectable[][] KMSelectables { get { return new KMSelectable[][] { Buttons, new KMSelectable[] { TextSelectable } }; } }
    public KMSelectable[] Buttons;

    public KMHighlightable TextHighlightable, ModuleHighlightable;
    public KMHighlightable[] ButtonHighlightables;
    public KMSelectable TextSelectable, ModuleSelectable;
    public Material HighlightMaterial;
    public Renderer[] ButtonRenderers;
    public TextMesh Text;

    public override Func<KMSelectable.OnInteractHandler>[] HandlePress { get { return new Func<KMSelectable.OnInteractHandler>[] { _select.TextPress }; } }
    public override Func<int, KMSelectable.OnInteractHandler>[] HandlePresses { get { return new Func<int, KMSelectable.OnInteractHandler>[] { _select.ButtonPress }; } }

    public override bool IsSolved { get; set; }
    public override int ModuleId { get; set; }
    public override string HelpMessage { get { return TwitchHelpMessage; } }
    private const string TwitchHelpMessage = @"!{0} highlight [Hovers over all buttons] | !{0} submit 126 [Presses positions 1, 2, 6 and then hits submit]";

    internal static int moduleIdCounter;

    private bool _isRunningTwitchCommand;
    private LinqSelect _select;
    #endregion

    #region Methods
    public override void Activate()
    {
        _select = new LinqSelect(this);

        ModuleSelectable.OnHighlight += _select.OnHighlight(ModuleHighlightable);
        ModuleSelectable.OnHighlightEnded += _select.OnHighlightEnded(ModuleHighlightable);

        TextSelectable.OnHighlight += _select.OnHighlight(TextHighlightable);
        TextSelectable.OnHighlightEnded += _select.OnHighlightEnded(TextHighlightable);

        for (int i = 0; i < Buttons.Length; i++)
        {
            Buttons[i].OnHighlight += _select.OnHighlight(ButtonHighlightables[i]);
            Buttons[i].OnHighlightEnded += _select.OnHighlightEnded(ButtonHighlightables[i]);
        }
    }

    public override IEnumerator ProcessTwitchCommand(string command)
    {
        string[] split = command.Split();

        if (Regex.IsMatch(split[0], @"^\s*highlight\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            _isRunningTwitchCommand = true;

            StartCoroutine(TwitchHighlight());

            while (_isRunningTwitchCommand)
                yield return true;
        }

        else if (split.Any(s => !(s.ToLowerInvariant() == "submit" || s.All(c => "123456".Contains(c.ToString())))))
            yield return "sendtochaterror Invalid command!";

        else
        {
            yield return null;
            _isRunningTwitchCommand = true;

            StartCoroutine(TwitchSelect(split));

            while (_isRunningTwitchCommand)
                yield return true;
        }

    }

    private IEnumerator TwitchHighlight()
    {
        yield return null;

        for (int i = 0; i < Buttons.Length; i++)
        {
            Buttons[i].OnHighlight();
            yield return new WaitForSecondsRealtime(1);
            Buttons[i].OnHighlightEnded();
        }

        _isRunningTwitchCommand = false;
    }

    private IEnumerator TwitchSelect(string[] split)
    {
        yield return null;
        bool isSubmit = false;

        for (int i = 0; i < split.Length; i++)
        {
            if (split[i].ToLowerInvariant() == "submit")
            {
                isSubmit = true;
                continue;
            }

            for (int j = 0; j < split[i].Length; j++)
            {
                Buttons[!_select.isInverted ? (int)char.GetNumericValue(split[i][j]) - 1 : new[] { 0, 2, 4, 1, 3, 5 }[(int)char.GetNumericValue(split[i][j]) - 1]].OnInteract();
                yield return new WaitForSecondsRealtime(0.2f);
            }
        }

        if (isSubmit)
            TextSelectable.OnInteract();

        _isRunningTwitchCommand = false;
    }

    public override IEnumerator TwitchHandleForcedSolve()
    {
        yield return null;
        for (int i = _select.currentStage; i < LinqSelect.MaxStage; i++)
        {
            _isRunningTwitchCommand = true;

            bool[] answer = LinqValidate.Run(Info.GetSerialNumber(), _select.initialButtonStates, _select.functions[i], _select.parameter);
            string answerIndexes = string.Empty;

            for (int j = 0; j < answer.Length; j++)
                if (answer[j] != _select.buttonStates[j])
                    answerIndexes += (j + 1).ToString();

            StartCoroutine(TwitchSelect(new string[] { "submit", answerIndexes }));
            yield return new WaitForSecondsRealtime(0.2f);

            while (_isRunningTwitchCommand)
                yield return true;
        }
    }
    #endregion
}
