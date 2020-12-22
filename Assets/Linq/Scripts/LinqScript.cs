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

    public override Func<KMSelectable.OnInteractHandler>[] HandlePress { get { return new Func<KMSelectable.OnInteractHandler>[] { select.TextPress }; } }
    public override Func<int, KMSelectable.OnInteractHandler>[] HandlePresses { get { return new Func<int, KMSelectable.OnInteractHandler>[] { select.ButtonPress }; } }

    public override bool IsSolved { get; set; }
    public override int ModuleId { get; set; }
    public override string HelpMessage { get { return TwitchHelpMessage; } }
    private const string TwitchHelpMessage = @"!{0} highlight [Hovers over all buttons] | !{0} submit 126 [Presses positions 1, 2, 6 and then hits submit]";

    internal static int moduleIdCounter;
    internal LinqSelect select;

    private bool _isRunningTwitchCommand;
    #endregion

    #region Methods
    public override void Activate()
    {
        select = new LinqSelect(this);

        ModuleSelectable.OnHighlight += select.OnHighlight(ModuleHighlightable);
        ModuleSelectable.OnHighlightEnded += select.OnHighlightEnded(ModuleHighlightable);

        TextSelectable.OnHighlight += select.OnHighlight(TextHighlightable);
        TextSelectable.OnHighlightEnded += select.OnHighlightEnded(TextHighlightable);

        for (int i = 0; i < Buttons.Length; i++)
        {
            Buttons[i].OnHighlight += select.OnHighlight(ButtonHighlightables[i]);
            Buttons[i].OnHighlightEnded += select.OnHighlightEnded(ButtonHighlightables[i]);
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
                Buttons[!select.isInverted ? (int)char.GetNumericValue(split[i][j]) - 1 : new[] { 0, 2, 4, 1, 3, 5 }[(int)char.GetNumericValue(split[i][j]) - 1]].OnInteract();
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
        for (int i = select.currentStage; i < LinqSelect.MaxStage; i++)
        {
            _isRunningTwitchCommand = true;

            bool[] answer = LinqValidate.Run(Info.GetSerialNumber(), select.initialButtonStates, select.functions[i], select.parameter);
            string answerIndexes = string.Empty;

            for (int j = 0; j < answer.Length; j++)
                if (answer[j] != select.buttonStates[j])
                    answerIndexes += (j + 1).ToString();

            StartCoroutine(TwitchSelect(new string[] { "submit", answerIndexes }));
            yield return new WaitForSecondsRealtime(0.2f);

            while (_isRunningTwitchCommand)
                yield return true;
        }
    }
    #endregion
}
