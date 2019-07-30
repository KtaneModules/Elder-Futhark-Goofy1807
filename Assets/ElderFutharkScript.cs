using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using KModkit;
using UnityEngine;
using Random = UnityEngine.Random;

public class ElderFutharkScript : MonoBehaviour
{
    public KMAudio Audio;
    public KMBombInfo Bomb;

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    public KMSelectable[] Runes;
    public KMSelectable Activator;
    public KMSelectable Module;
    public GameObject[] RuneLetters_1;
    public GameObject[] RuneLetters_2;
    public Material[] Materials;
    public AudioClip[] Sounds;

    public GameObject[] DustSystemLetters;
    public GameObject[] DustSystemRunes;

    private GameObject[][] RuneLetters;
    private Transform[] RuneTransforms;
    private Vector3[] RuneParentPos;

    private int[] pickedRuneLetters = new int[2];
    private string[] pickedRuneNames = new string[2];
    private string[] pickedRuneNamesCipher = new string[2];
    private string[] Keywords = new string[2];

    private static readonly string[] ElderFuthark = { "Algiz", "Ansuz", "Berkana", "Dagaz", "Ehwaz", "Eihwaz", "Fehu", "Gebo", "Hagalaz", "Isa", "Jera", "Kenaz", "Laguz", "Mannaz", "Nauthiz", "Othila", "Perthro", "Raido", "Sowulo", "Teiwaz", "Thurisaz", "Uruz", "Wunjo" };
    private static readonly string[] ElderFutharkTranslated = { "z", "a", "b", "d", "e", "y", "f", "g", "h", "i", "j", "c, q, k", "l", "m", "n", "o", "p", "r", "s", "t", "x", "u", "v, w" };
    private static readonly char[] AlphabetNumbers = { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z' };

    private bool moduleStarted = false;

    private int currentRune = 0;
    private int timesPressed = 0;

    private KMSelectable.OnInteractHandler RunePressed(int rune)
    {
        return delegate
        {
            StartCoroutine(PebbleWiggle(rune, RuneTransforms[rune].localEulerAngles));
            Audio.PlaySoundAtTransform("RockClick", transform);
            Runes[rune].AddInteractionPunch();
            if (moduleSolved)
                return false;
            if (ElderFutharkTranslated[rune].Contains(pickedRuneNamesCipher[currentRune][timesPressed]))
            {
                Debug.LogFormat(@"[Elder Futhark #{0}] You pressed {1}, expecting {2}. Well Done", moduleId, ElderFutharkTranslated[rune], pickedRuneNamesCipher[currentRune][timesPressed]);
                timesPressed++;
                if (timesPressed == pickedRuneNamesCipher[currentRune].Length)
                {
                    timesPressed = 0;
                    RuneLetters[currentRune][pickedRuneLetters[currentRune]].GetComponent<MeshRenderer>().material = Materials[2];
                    currentRune++;
                    Debug.LogFormat(@"[Elder Futhark #{0}] Rune no. {1} was solved", moduleId, currentRune);
                    if (currentRune == 2)
                    {
                        GetComponent<KMBombModule>().HandlePass();
                        moduleSolved = true;
                        for (int i = 0; i < currentRune; i++)
                        {
                            RuneLetters[i][pickedRuneLetters[i]].gameObject.SetActive(false);
                        }

                        return false;
                    }

                    RuneLetters[currentRune][pickedRuneLetters[currentRune]].GetComponent<MeshRenderer>().material = Materials[1];
                }
            }
            else
            {
                GetComponent<KMBombModule>().HandleStrike();
                Debug.LogFormat(@"[Elder Futhark #{0}] You pressed {1}, expecting {2}. Strike. Rune resetted", moduleId, ElderFutharkTranslated[rune], pickedRuneNamesCipher[currentRune][timesPressed]);
                timesPressed = 0;
            }

            return false;
        };
    }

    void Start()
    {
        moduleId = moduleIdCounter++;

        // Show the runes the first time the module is selected
        Activator.OnInteract += delegate
        {
            StartCoroutine(CrashDownSetup());
            moduleStarted = true;
            Activator.gameObject.SetActive(false);
            Module.Children = Runes;
            UpdateChildren();
            return true;
        };

        // Set the runes to invisible until they appear
        for (int i = 0; i < Runes.Length; i++)
        {
            Runes[i].gameObject.SetActive(false);
            Runes[i].OnInteract += RunePressed(i);
        }

        //Assigning RuneLetters with the different positions
        RuneLetters = new[]
        {
            RuneLetters_1,
            RuneLetters_2,
        };
        //Assigning RuneTransforms with their childs
        RuneTransforms = Runes.Select(rune => rune.transform.parent).ToArray();
        RuneParentPos = Runes.Select(rune => rune.transform.parent.transform.localPosition).ToArray();

        //Shuffling positions
        var randPos = Enumerable.Range(0, RuneTransforms.Length).ToList();

        for (int i = 0; i < RuneTransforms.Length; i++)
        {
            int index = Random.Range(0, randPos.Count);
            Runes[i].transform.parent.transform.localPosition = RuneParentPos[randPos[index]];
            DustSystemRunes[i].transform.localPosition = RuneParentPos[randPos[index]];
            Vector3 DustPos = DustSystemRunes[i].transform.localPosition;
            DustPos.y = 0.01f;
            DustSystemRunes[i].transform.localPosition = DustPos;
            randPos.RemoveAt(index);
        }

        //Generating a random 2-letter word
        for (int i = 0; i < RuneLetters.Length; i++)
        {
            pickedRuneLetters[i] = Random.Range(0, RuneLetters[i].Length);
            pickedRuneNames[i] = ElderFuthark[pickedRuneLetters[i]];
            Debug.LogFormat(@"[Elder Futhark #{0}] The {1}th rune is {2}", moduleId, i + 1, pickedRuneNames[i]);
        }

        //Generating 2 keywords

        for (int i = 0; i < Keywords.Length; i++)
        {
            if (pickedRuneNames[i].ToUpperInvariant().Any(x => x == 'E' || x == 'O'))
                Keywords[i] += AlphabetNumbers[(Bomb.GetSerialNumberNumbers().Last() + 10) % 26];
            if (pickedRuneNames[i].Length < 5)
                Keywords[i] += AlphabetNumbers[Bomb.GetIndicators().Count() + Bomb.GetBatteryHolderCount() % 26];
            Keywords[i] += AlphabetNumbers[i];
            Keywords[i] += AlphabetNumbers[pickedRuneNames[i].Length * 10 % 26];
            Keywords[i] += AlphabetNumbers[(i + 1 + pickedRuneNames[i].Length) % 26];
            Debug.LogFormat(@"[Elder Futhark #{0}] The keyword for the {1}th rune is {2}", moduleId, i + 1, Keywords[i]);

        }

        //Crypting the name of each rune in the 2-letter word
        for (int i = 0; i < pickedRuneNames.Length; i++)
        {
            string pickedRuneName = pickedRuneNames[i];

            for (int j = 0; j < pickedRuneName.Length; j++)
            {
                pickedRuneNamesCipher[i] += AlphabetNumbers[(Array.IndexOf(AlphabetNumbers, char.ToLowerInvariant(pickedRuneName[j])) + Array.IndexOf(AlphabetNumbers, char.ToLowerInvariant(Keywords[i][j % Keywords[i].Length]))) % 26];
            }
            Debug.LogFormat(@"[Elder Futhark #{0}] The encrypted name of the {1}th rune is {2}.", moduleId, i + 1, pickedRuneNamesCipher[i]);
        }
    }

    //Placing the word on the module
    private IEnumerator SetWord()
    {
        for (int i = 0; i < RuneLetters.Length; i++)
        {
            Audio.PlaySoundAtTransform("RuneLetters", transform);
            DustSystemLetters[i].GetComponent<ParticleSystem>().Play();
            yield return new WaitForSeconds(0.3f);
            RuneLetters[i][pickedRuneLetters[i]].SetActive(true);
            yield return new WaitForSeconds(0.2f);
        }

        yield return new WaitForSeconds(1f);
        RuneLetters[currentRune][pickedRuneLetters[currentRune]].GetComponent<MeshRenderer>().material = Materials[1];
    }

    //Making the pebbles wiggle
    private IEnumerator PebbleWiggle(int pos, Vector3 startingRot)
    {
        var duration = 0.3f;
        var elapsed = 0f;

        while (elapsed < duration)
        {

            Vector3 Rot = startingRot;

            Rot.x = Rot.x + Mathf.Sin(Time.time * 30f) * 10f;
            Rot.y = Rot.y + Mathf.Sin(Time.time * 20f) * 5f;
            Rot.z = Rot.z + Mathf.Sin(Time.time * 30f) * 10f;

            RuneTransforms[pos].localEulerAngles = Rot;

            yield return null;
            elapsed += Time.deltaTime;

        }
        RuneTransforms[pos].localEulerAngles = startingRot;
    }

    //Let the pebbles fly down on the board
    private IEnumerator CrashDown(int pos)
    {
        while (RuneTransforms[pos].localPosition.y > 0.013f)
        {
            Vector3 newPos = RuneTransforms[pos].localPosition;
            newPos.y = newPos.y - 0.05f;
            RuneTransforms[pos].localPosition = newPos;
            yield return null;
        }

        Vector3 endPos = RuneTransforms[pos].localPosition;
        endPos.y = 0.013f;
        RuneTransforms[pos].localPosition = endPos;
        DustSystemRunes[pos].GetComponent<ParticleSystem>().Play();
        Audio.PlaySoundAtTransform("RockSpawn", transform);
        StartCoroutine(PebbleWiggle(pos, RuneTransforms[pos].localEulerAngles));
    }

    //Generating a random order for the pebbles to crash down
    private IEnumerator CrashDownSetup()
    {
        if (moduleStarted)
            yield break;

        var positions = Enumerable.Range(0, Runes.Length).ToList();

        for (int i = 0; i < Runes.Length; i++)
        {
            int index = Random.Range(0, positions.Count);
            int pos = positions[index];
            Runes[pos].gameObject.SetActive(true);
            StartCoroutine(CrashDown(pos));
            positions.RemoveAt(index);
            yield return new WaitForSeconds(0.1f);
        }

        yield return new WaitForSeconds(0.5f);
        StartCoroutine(SetWord());
    }

    int Factorial(int n)
    {
        int fact = 1;
        for (int i = n; i > 0; i--)
            fact = fact * i;
        return fact;
    }

    public void UpdateChildren()
    {
        GetComponent<KMSelectable>().UpdateChildren();
    }

#pragma warning disable 0414
    private readonly string TwitchHelpMessage = "!{0} activate [to start the module] | !{0} submit eihwaz, hagalaz, fehu [submit the runenames]";
#pragma warning restore 0414

    private static int IndexOf<T>(IEnumerable<T> source, Func<T, bool> predicate)
    {
        var i = 0;
        foreach (var obj in source)
        {
            if (predicate(obj))
                return i;
            i++;
        }
        return -1;
    }

    private IEnumerable<KMSelectable> ProcessTwitchCommand(string command)
    {
        var runetoPress = new List<KMSelectable>();
        Match m;
        if ((m = Regex.Match(command, @"^\s*(click|enter|submit|type|press|touch|fiddle)\s+(?<runes>[ABCDEFGHIJKLMNOPQRSTUVWXYZ, ]+)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).Success && moduleStarted)
        {
            var runenames = m.Groups["runes"].Value.Split(',').ToArray();
            for (int i = 0; i < runenames.Length; i++)
            {
                if (Regex.IsMatch(runenames[i], @"^\s*$"))
                    continue;

                var ix = IndexOf(ElderFuthark, rune => rune.EqualsIgnoreCase(runenames[i].Trim()));
                
                if (ix == -1)
                    return null;
                runetoPress.Add(Runes[ix]);
            }

            return runetoPress;
        }
        else if ((m = Regex.Match(command, @"^\s*(start|go|activate|engage|tap|touch)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).Success && !moduleStarted)
        {
            return new[] { Activator };
        }
        return null;
    }

}

