using UnityEngine;
using System.Collections;
using System;

/// <summary>
/// Module Sample Script created by Emik.
/// </summary>
public abstract class ModuleScript : MonoBehaviour
{
    public abstract KMAudio KMAudio { get; }
    public abstract KMBombInfo KMBombInfo { get; }
    public abstract KMBombModule KMBombModule { get; }
    public abstract KMSelectable[][] KMSelectables { get; }

    public abstract void Activate();
    public abstract Func<KMSelectable.OnInteractHandler>[] HandlePress { get; }
    public abstract Func<int, KMSelectable.OnInteractHandler>[] HandlePresses { get; }
    public abstract IEnumerator ProcessTwitchCommand(string command);
    public abstract IEnumerator TwitchHandleForcedSolve();

    public abstract bool IsSolved { get; set; }
    public abstract int ModuleId { get; set; }
    public abstract string HelpMessage { get; }

    /// <summary>
    /// This runs the activate method, then initalizes the buttons.
    /// </summary>
    private void Start()
    {
        KMBombModule.OnActivate += delegate ()
        {
            Activate();
            int j = 0, k = 0;
            for (int i = 0; i < KMSelectables.Length; i++)
                if (KMSelectables[i].Length == 1)
                    OnInteract(KMSelectables[i][0], HandlePress[j++]);
                else
                    OnInteractArray(KMSelectables[i], HandlePresses[k++]);
        };
    }

    /// <summary>
    /// Assigns a KMSelectable.OnInteract event handler. Reminder that your method should have no parameters.
    /// </summary>
    /// <param name="selectable"></param>
    /// <param name="method"></param>
    internal static void OnInteract(KMSelectable selectable, Func<KMSelectable.OnInteractHandler> method)
    {
        selectable.OnInteract += method();
    }

    /// <summary>
    /// Assigns KMSelectable.OnInteract event handlers. Reminder that your method should have only a single integer parameter, which will be used to pass the index of the button pressed.
    /// </summary>
    /// <param name="selectables">The array to create event handlers for.</param>
    /// <param name="method">The method that will be called whenever an event is triggered.</param>
    internal static void OnInteractArray(KMSelectable[] selectables, Func<int, KMSelectable.OnInteractHandler> method)
    {
        for (int i = 0; i < selectables.Length; i++)
            selectables[i].OnInteract += method(i);
    }
}
