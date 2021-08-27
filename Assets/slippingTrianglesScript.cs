using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;
using System.Text.RegularExpressions;

public class slippingTrianglesScript : MonoBehaviour
{

    //public stuff
    public KMAudio Audio;
    public KMSelectable[] Buttons;
    public List<MeshRenderer> ButtonMesh;
    public KMBombModule Module;

    //readonly
    private readonly List<List<int>> connected = new List<List<int>> { new List<int> { 0, 2, 6 }, new List<int> { 1, 7, 8 }, new List<int> { 3, 4, 5 }, new List<int> { 0, 4, 8 }, new List<int> { 1, 3, 6 }, new List<int> { 2, 5, 7 } };

    //private stuff
    private List<int> values = new List<int> { };
    private List<bool> safe = new List<bool> { };
    private List<bool> pressed = new List<bool> { };
    private List<bool> highlighted = new List<bool> { };
    private bool solved = false;
    private List<Color> colour = new List<Color> { new Color(0.125f, 1, 0.125f), new Color(0.125f, 0.5f, 1), new Color(0.5f, 0.125f, 1), new Color(0.125f, 0.125f, 0.125f), new Color(1, 1, 1) };

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
                        Debug.LogFormat("[Slipping Triangles #{0}] Triangle {1} wasn't valid. Strike!", _moduleID, pos + 1);
                        Module.HandleStrike();
                    }
                    else
                    {
                        Debug.LogFormat("[Slipping Triangles #{0}] Triangle {1} was a valid press.", _moduleID, pos + 1);
                    }
                    for (int i = 0; i < 9; i++)
                        safe[i] = !pressed[i] && Enumerable.Range(0, 3).All(y => connected.Where(x => x.Contains(i)).ToList()[0].Select(x => pressed[x] ? -1 : values[x]).OrderBy(x => x).ToList()[y] == connected.Where(x => x.Contains(i)).ToList()[1].Select(x => pressed[x] ? -1 : values[x]).OrderBy(x => x).ToList()[y]);
                    if (safe.All(x => !x))
                    {
                        solved = true;
                        for (int i = 0; i < 9; i++)
                            ButtonMesh[i].material.color = colour[3];
                        Debug.LogFormat("[Slipping Triangles #{0}] There are no more valid triangles. Solved!", _moduleID);
                        Module.HandlePass();
                    }
                    else
                    {
                        Debug.LogFormat("[Slipping Triangles #{0}] The following triangles are still valid: {1}.", _moduleID, Enumerable.Range(0, 9).Where(x => safe[x]).Select(x => x + 1).Join(", "));
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
        Debug.LogFormat("[Slipping Triangles #{0}] The following colours are picked: {1}.", _moduleID, values.Select(x => "GBV"[x]).Join(", "));
        Debug.LogFormat("[Slipping Triangles #{0}] The following triangles are valid: {1}.", _moduleID, Enumerable.Range(0, 9).Where(x => safe[x]).Select(x => x + 1).Join(", "));
    }

    void Update()
    {
        if (!solved)
            for (int i = 0; i < 9; i++)
                if (pressed[i])
                    ButtonMesh[i].material.color = colour[3];
                else if (highlighted[i])
                    ButtonMesh[i].material.color = colour[4];
                else
                    ButtonMesh[i].material.color = colour[values[i]];
    }

    private void Generate()
    {
        regen:
        for (int i = 0; i < 9; i++)
        {
            values.Add(Rnd.Range(0, 3));
            safe.Add(true);
        }
        for (int i = 0; i < 9; i++)
            safe[i] = Enumerable.Range(0, 3).All(y => connected.Where(x => x.Contains(i)).ToList()[0].Select(x => values[x]).OrderBy(x => x).ToList()[y] == connected.Where(x => x.Contains(i)).ToList()[1].Select(x => values[x]).OrderBy(x => x).ToList()[y]);
        if (safe.All(x => !x) || values.All(x => x == 1) || values.All(x => x != 1))
        {
            values = new List<int> { };
            safe = new List<bool> { };
            goto regen;
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
