using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;
using System.Text.RegularExpressions;

public class drippingTrianglesScript : MonoBehaviour
{

    //public stuff
    public KMAudio Audio;
    public KMSelectable[] Buttons;
    public List<MeshRenderer> ButtonMesh;
    public KMBombModule Module;

    //readonly
    private readonly List<List<int>> connected = new List<List<int>> { new List<int> { 2 }, new List<int> { 2, 5 }, new List<int> { 0, 1, 3 }, new List<int> { 2, 7 }, new List<int> { 5 }, new List<int> { 1, 4, 6 }, new List<int> { 5, 7 }, new List<int> { 3, 6, 8 }, new List<int> { 7 } };

    //private stuff
    private List<int> values = new List<int> { };
    private List<bool> safe = new List<bool> { };
    private List<bool> pressed = new List<bool> { };
    private List<bool> highlighted = new List<bool> { };
    private bool solved = false;
    private List<Color> colour = new List<Color> { new Color(1, 0.875f, 0.125f), new Color(1, 0.5f, 0.125f), new Color(1, 0.125f, 0.125f), new Color(0.125f, 0.125f, 0.125f), new Color(1, 1, 1) };

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
                        Debug.LogFormat("[Dripping Triangles #{0}] Triangle {1} wasn't valid. Strike!", _moduleID, pos + 1);
                        Module.HandleStrike();
                    }
                    else
                    {
                        Debug.LogFormat("[Dripping Triangles #{0}] Triangle {1} was a valid press.", _moduleID, pos + 1);
                    }
                    List<int> grid = values.Select(x => x + 1).ToList();
                    while (grid.Any(x => x >= 3))
                        for (int i = 0; i < 9; i++)
                            if (pressed[i])
                            {
                                grid[i] = 0;
                            }
                            else if (grid[i] >= 3)
                            {
                                grid[i] -= 3;
                                foreach (int j in connected[i])
                                    grid[j]++;
                            }
                    for (int i = 0; i < 9; i++)
                        safe[i] = grid[i] == pressed.Count(x => x) && !pressed[i];
                    if (safe.All(x => !x))
                    {
                        solved = true;
                        for (int i = 0; i < 9; i++)
                            ButtonMesh[i].material.color = colour[3];
                        Debug.LogFormat("[Dripping Triangles #{0}] There are no more valid triangles. Solved!", _moduleID);
                        Module.HandlePass();
                    }
                    else
                    {
                        Debug.LogFormat("[Dripping Triangles #{0}] The following triangles are now valid: {1}.", _moduleID, Enumerable.Range(0, 9).Where(x => safe[x]).Select(x => x + 1).Join(", "));
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
        Debug.LogFormat("[Dripping Triangles #{0}] The following colours are picked: {1}.", _moduleID, values.Select(x => "YOR"[x]).Join(", "));
        Debug.LogFormat("[Dripping Triangles #{0}] The following triangles are valid: {1}.", _moduleID, Enumerable.Range(0, 9).Where(x => safe[x]).Select(x => x + 1).Join(", "));
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
        List<int> grid = values.Select(x => x + 1).ToList();
        while (grid.Any(x => x >= 3))
            for (int i = 0; i < 9; i++)
                if (grid[i] >= 3)
                {
                    grid[i] -= 3;
                    foreach (int j in connected[i])
                        grid[j]++;
                }
        for (int i = 0; i < 9; i++)
            safe[i] = grid[i] == 0;
        if (safe.All(x => !x) || values.All(x => x != 1))
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
