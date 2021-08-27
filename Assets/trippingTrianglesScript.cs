using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;
using System.Text.RegularExpressions;

public class trippingTrianglesScript : MonoBehaviour
{

    //public stuff
    public KMAudio Audio;
    public KMSelectable[] Buttons;
    public List<MeshRenderer> ButtonMesh;
    public KMBombModule Module;

    //readonly
    private readonly int[] table = new int[] { 0, 2, 1, 2, 1, 0, 1, 0, 2 };
    private readonly List<List<int>> connected = new List<List<int>> { new List<int> { 2, -1, -1 }, new List<int> { 5, -1, 2 }, new List<int> { 0, 3, 1 }, new List<int> { 7, 2, -1 }, new List<int> { -1, -1, 5 }, new List<int> { 1, 6, 4 }, new List<int> { -1, 5, 7 }, new List<int> { 3, 8, 6 }, new List<int> { -1, 7, -1 } }; // { | \ / }

    //private stuff
    private List<int> values = new List<int> { };
    private List<bool> safe = new List<bool> { };
    private List<bool> pressed = new List<bool> { };
    private List<bool> highlighted = new List<bool> { };
    private bool solved = false;
    private int t = 0;
    private List<Color> colour = new List<Color> { new Color(1, 0.125f, 0.125f), new Color(1, 0.875f, 0.125f), new Color(0.125f, 0.5f, 1), new Color(0.125f, 0.125f, 0.125f), new Color(1, 1, 1) };

    //logging
    static int _moduleIdCounter = 1;
    int _moduleID = 0;

    private KMSelectable.OnInteractHandler Press(int pos)
    {
        return delegate
        {
            if (!solved)
            {
                if (!pressed[pos])
                {
                    Buttons[pos].AddInteractionPunch(1f);
                    Audio.PlaySoundAtTransform("Beep", Buttons[pos].transform);
                    pressed[pos] = true;
                    if (!safe[pos])
                    {
                        Debug.LogFormat("[Tripping Triangles #{0}] Triangle {1} wasn't valid. Strike!", _moduleID, pos + 1);
                        Module.HandleStrike();
                    }
                    else
                    {
                        Debug.LogFormat("[Tripping Triangles #{0}] Triangle {1} was a valid press.", _moduleID, pos + 1);
                    }
                    for (int i = 0; i < 9; i++)
                        safe[i] = !pressed[i];
                    for (int i = 0; i < 9; i++)
                        if (!pressed[i] && connected[i][table[values[i]]] != -1)
                            safe[connected[i][table[values[i]]]] = false;
                    if (safe.All(x => !x))
                    {
                        solved = true;
                        for (int i = 0; i < 9; i++)
                            ButtonMesh[i].material.color = colour[3];
                        Debug.LogFormat("[Tripping Triangles #{0}] There are no more valid triangles. Solved!", _moduleID);
                        Module.HandlePass();
                    }
                    else
                    {
                        Debug.LogFormat("[Tripping Triangles #{0}] The following triangles are now valid: {1}.", _moduleID, Enumerable.Range(0, 9).Where(x => safe[x]).Select(x => x + 1).Join(", "));
                    }
                }
            }
            return false;
        };
    }

    void Awake()
    {
        _moduleID = _moduleIdCounter++;
        for (int i = 0; i < Buttons.Length; i++)
        {
            Buttons[i].OnInteract += Press(i);
            int x = i;
            Buttons[i].OnHighlight += delegate { if (!solved) { highlighted[x] = true; } };
            Buttons[i].OnHighlightEnded += delegate { if (!solved) { highlighted[x] = false; } };
            ButtonMesh.Add(Buttons[i].GetComponent<MeshRenderer>());
            pressed.Add(false);
            highlighted.Add(false);
        }
    }

    void Start()
    {
        Generate();
        Debug.LogFormat("[Tripping Triangles #{0}] The following colours are picked: {1}.", _moduleID, values.Select(x => "RYB"[x / 3].ToString() + "RYB"[x % 3].ToString()).Join(", "));
        Debug.LogFormat("[Tripping Triangles #{0}] The following triangles are valid: {1}.", _moduleID, Enumerable.Range(0, 9).Where(x => safe[x]).Select(x => x + 1).Join(", "));
        StartCoroutine(TogglingTimer());
    }

    void Update()
    {
        if (!solved)
            for (int i = 0; i < 9; i++)
                if (pressed[i])
                    ButtonMesh[i].material.color = colour[3];
                else if (highlighted[i])
                    ButtonMesh[i].material.color = colour[4];
                else if (t % 2 == 0)
                    ButtonMesh[i].material.color = colour[values[i] / 3];
                else
                    ButtonMesh[i].material.color = colour[values[i] % 3];
    }

    private void Generate()
    {
        regen:
        for (int i = 0; i < 9; i++)
            safe.Add(true);
        for (int i = 0; i < 9; i++)
        {
            values.Add(Rnd.Range(0, 9));
            if (connected[i][table[values[i]]] != -1)
                safe[connected[i][table[values[i]]]] = false;
        }
        if (safe.All(x => !x) || values.All(x => x / 3 == x % 3))
        {
            values = new List<int> { };
            safe = new List<bool> { };
            goto regen;
        }
    }

    private IEnumerator TogglingTimer()
    {
        while (!solved)
        {
            t++;
            yield return new WaitForSeconds(1f);
        }
    }

#pragma warning disable 414
    private string TwitchHelpMessage = "'!{0} inspect 6 9' to highlight those triangles. '!{0} press 6 9' to press them.";
#pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        yield return null;
        command = command.ToLowerInvariant();
        if (Regex.IsMatch(command, @"^press(\s(1|2|3|4|5|6|7|8|9))+$"))
        {
            MatchCollection matches = Regex.Matches(command, @"(1|2|3|4|5|6|7|8|9)");
            foreach (Match match in matches)
            {
                int subcmd = match.ToString()[0] - '1';
                Buttons[subcmd].OnInteract();
                yield return null;
            }
        }
        else if (Regex.IsMatch(command, @"^inspect(\s(1|2|3|4|5|6|7|8|9))+$"))
        {
            MatchCollection matches = Regex.Matches(command, @"(1|2|3|4|5|6|7|8|9)");
            foreach (Match match in matches)
            {
                int subcmd = match.ToString()[0] - '1';
                Buttons[subcmd].OnHighlight();
                yield return new WaitForSeconds(1f);
                Buttons[subcmd].OnHighlightEnded();
                yield return null;
            }
        }
        else
            yield return "sendtochaterror Invalid command.";
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        yield return true;
        while (!solved)
            for (int i = 0; i < 9; i++)
                if (safe[i])
                {
                    Buttons[i].OnInteract();
                    yield return true;
                }
    }
}
