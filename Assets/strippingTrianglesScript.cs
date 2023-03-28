using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Rnd = UnityEngine.Random;
using System.Text.RegularExpressions;

public class strippingTrianglesScript : MonoBehaviour
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
    private List<bool> highlighted = new List<bool> { };
    private bool solved = false;
    private List<Color> colour = new List<Color> { new Color(1, 0.125f, 0.125f), new Color(1, 0.875f, 0.125f), new Color(0.125f, 0.5f, 1), new Color(0.125f, 1, 0.125f), new Color(0.5f, 0.125f, 1), new Color(1, 0.5f, 0.125f), new Color(0.125f, 0.125f, 0.125f), new Color(1, 1, 1) };

    //logging
    static int _moduleIdCounter = 1;
    int _moduleID = 0;

    private KMSelectable.OnInteractHandler Press(int pos)
    {
        return delegate
        {
            if (!solved)
            {
                if (values[pos] == 6)
                    return false;
                Buttons[pos].AddInteractionPunch(1f);
                Audio.PlaySoundAtTransform("Beep", Buttons[pos].transform);
                if (!safe[pos])
                {
                    Debug.LogFormat("[Stripping Triangles #{0}] Triangle {1} wasn't valid. Strike!", _moduleID, pos + 1);
                    Module.HandleStrike();
                }
                else
                {
                    Debug.LogFormat("[Stripping Triangles #{0}] Triangle {1} was a valid press.", _moduleID, pos + 1);
                }
                RemoveComponent(pos);
                GetValidTriangles(false);
                if (safe.All(x => !x))
                {
                    solved = true;
                    for (int i = 0; i < 9; i++)
                        ButtonMesh[i].material.color = colour[6];
                    Debug.LogFormat("[Stripping Triangles #{0}] There are no more valid triangles. Solved!", _moduleID);
                    Module.HandlePass();
                }
                else
                {
                    Debug.LogFormat("[Stripping Triangles #{0}] The following triangles are still valid: {1}.", _moduleID, Enumerable.Range(0, 9).Where(x => safe[x]).Select(x => x + 1).Join(", "));
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
            Buttons[i].OnHighlight += delegate { if (!solved) { highlighted[x] = values[x] != 6; } };
            Buttons[i].OnHighlightEnded += delegate { if (!solved) { highlighted[x] = false; } };
            ButtonMesh.Add(Buttons[i].GetComponent<MeshRenderer>());
            highlighted.Add(false);
        }
    }

    void Start()
    {
        Generate();
        Debug.LogFormat("[Stripping Triangles #{0}] The following colours are picked: {1}.", _moduleID, values.Select(x => "RYBGVO"[x]).Join(", "));
        Debug.LogFormat("[Stripping Triangles #{0}] The following triangles are valid: {1}.", _moduleID, Enumerable.Range(0, 9).Where(x => safe[x]).Select(x => x + 1).Join(", "));
    }

    void Update()
    {
        if (!solved)
            for (int i = 0; i < 9; i++)
                if (highlighted[i])
                    ButtonMesh[i].material.color = colour[7];
                else
                    ButtonMesh[i].material.color = colour[values[i]];
    }

    private void Generate()
    {
        regen:
        for (int i = 0; i < 9; i++)
            values.Add(Rnd.Range(0, 6));
        GetValidTriangles(true);
        if (safe.All(x => !x) || values.Distinct().Count() < 4)
        {
            values = new List<int> { };
            safe = new List<bool> { };
            goto regen;
        }
    }

    private void GetValidTriangles(bool makeList)
    {
        for (int i = 0; i < 9; i++)
        {
            bool good = false;
            if ((connected[i].All(x => values[x].EqualsAny(0, 4, 5, 6)) && values[i].EqualsAny(0, 4, 5)) || (connected[i].All(x => values[x].EqualsAny(1, 3, 5, 6)) && values[i].EqualsAny(1, 3, 5)) || (connected[i].All(x => values[x].EqualsAny(2, 3, 4, 6)) && values[i].EqualsAny(2, 3, 4)))
                good = true;
            if (makeList)
                safe.Add(good);
            else
                safe[i] = good;
        }
    }

    private void RemoveComponent(int index)
    {
        var prevValues = values.ToArray();
        if (connected[index].All(x => prevValues[x].EqualsAny(0, 4, 5, 6)) && prevValues[index].EqualsAny(0, 4, 5))
        {
            if (values[index] == 0)
                values[index] = 6;
            else if (values[index] == 4)
                values[index] = 2;
            else if (values[index] == 5)
                values[index] = 1;
            for (int i = 0; i < connected[index].Count; i++)
            {
                if (values[connected[index][i]] == 0)
                    values[connected[index][i]] = 6;
                else if (values[connected[index][i]] == 4)
                    values[connected[index][i]] = 2;
                else if (values[connected[index][i]] == 5)
                    values[connected[index][i]] = 1;
            }
        }
        if (connected[index].All(x => prevValues[x].EqualsAny(1, 3, 5, 6)) && prevValues[index].EqualsAny(1, 3, 5))
        {
            if (values[index] == 1)
                values[index] = 6;
            else if (values[index] == 3)
                values[index] = 2;
            else if (values[index] == 5)
                values[index] = 0;
            for (int i = 0; i < connected[index].Count; i++)
            {
                if (values[connected[index][i]] == 1)
                    values[connected[index][i]] = 6;
                else if (values[connected[index][i]] == 3)
                    values[connected[index][i]] = 2;
                else if (values[connected[index][i]] == 5)
                    values[connected[index][i]] = 0;
            }
        }
        if (connected[index].All(x => prevValues[x].EqualsAny(2, 3, 4, 6)) && prevValues[index].EqualsAny(2, 3, 4))
        {
            if (values[index] == 2)
                values[index] = 6;
            else if (values[index] == 3)
                values[index] = 1;
            else if (values[index] == 4)
                values[index] = 0;
            for (int i = 0; i < connected[index].Count; i++)
            {
                if (values[connected[index][i]] == 2)
                    values[connected[index][i]] = 6;
                else if (values[connected[index][i]] == 3)
                    values[connected[index][i]] = 1;
                else if (values[connected[index][i]] == 4)
                    values[connected[index][i]] = 0;
            }
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
