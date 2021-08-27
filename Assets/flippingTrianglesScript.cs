using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;
using System.Text.RegularExpressions;

public class flippingTrianglesScript : MonoBehaviour
{

    //public stuff
    public KMAudio Audio;
    public KMSelectable[] Buttons;
    public List<MeshRenderer> ButtonMesh;
    public KMBombModule Module;
    
    //private stuff
    private List<bool> safe = new List<bool> { };
    private List<bool> pressed = new List<bool> { };
    private List<bool> highlighted = new List<bool> { };
    private bool solved = false;
    private List<Color> colour = new List<Color> { new Color(0.375f, 0.375f, 0.375f), new Color(0.125f, 0.125f, 0.125f), new Color(1, 1, 1) };

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
                        Debug.LogFormat("[Flipping Triangles #{0}] Triangle {1} wasn't valid. Strike!", _moduleID, pos + 1);
                        Module.HandleStrike();
                    }
                    else
                    {
                        Debug.LogFormat("[Flipping Triangles #{0}] Triangle {1} was a valid press.", _moduleID, pos + 1);
                    }
                    int n = 0;
                    for (int i = 0; i < 9; i++)
                    {
                        n = n * 2 + (pressed[i] ? 0 : 1);
                        safe[i] = false;
                    }
                    safe[(n + 8) % 9] = !pressed[(n + 8) % 9];
                    if (safe.All(x => !x))
                    {
                        solved = true;
                        for (int i = 0; i < 9; i++)
                            ButtonMesh[i].material.color = colour[1];
                        Debug.LogFormat("[Flipping Triangles #{0}] There are no more valid triangles. Solved!", _moduleID);
                        Module.HandlePass();
                    }
                    else
                    {
                        Debug.LogFormat("[Flipping Triangles #{0}] The following triangle is now valid: {1}.", _moduleID, Enumerable.Range(0, 9).Where(x => safe[x]).Select(x => x + 1).Join(", "));
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
        Debug.LogFormat("[Flipping Triangles #{0}] The following binary is picked: {1}.", _moduleID, pressed.Select(x => x ? 0 : 1).Join(", "));
        Debug.LogFormat("[Flipping Triangles #{0}] The following triangle is valid: {1}.", _moduleID, Enumerable.Range(0, 9).Where(x => safe[x]).Select(x => x + 1).Join(", "));
    }

    void Update()
    {
        if (!solved)
            for (int i = 0; i < 9; i++)
                if (pressed[i])
                    ButtonMesh[i].material.color = colour[1];
                else if (highlighted[i])
                    ButtonMesh[i].material.color = colour[2];
                else
                    ButtonMesh[i].material.color = colour[0];
    }

    private void Generate()
    {
        regen:
        int n = 0;
        for (int i = 0; i < 9; i++)
        {
            n = n * 2 + Rnd.Range(0, 2);
            pressed[i] = n % 2 == 0;
            safe.Add(false);
        }
        safe[(n + 8) % 9] = !pressed[(n + 8) % 9];
        if (safe.All(x => !x))
        {
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
