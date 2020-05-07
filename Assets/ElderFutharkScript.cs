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
    public GameObject[] RuneLetters_3;
    public Material[] Materials;
    public AudioClip[] Sounds;
    public KMRuleSeedable RuleSeedable;

    public GameObject[] DustSystemLetters;
    public GameObject[] DustSystemRunes;

    private GameObject[][] RuneLetters;
    private Transform[] RuneTransforms;
    private Vector3[] RuneParentPos;
    private List<KMSelectable> RuneOrder = new List<KMSelectable>();

    private int[] pickedRuneLetters = new int[3];
    private string[] pickedRuneNames = new string[3];
    private int[][] pickedRuneNamesCipher;
    //The order of runes on the module [Gamepad Support]
    private readonly int[] selectableLocations = new[]
    {
        22, 5, 0, 17,
        7, 8, 2, 10,
        4, 9, 6,
        16, 19, 3, 1, 11,
        21, 13, 15,
        18, 14, 20, 12
    };

    private static readonly string[] ElderFuthark = { "Ansuz", "Berkana", "Kenaz", "Dagaz", "Ehwaz", "Fehu", "Gebo", "Hagalaz", "Isa", "Jera", "Eihwaz", "Laguz", "Mannaz", "Nauthiz", "Othila", "Perthro", "Algiz", "Raido", "Sowulo", "Teiwaz", "Uruz", "Wunjo", "Thurisaz" };
    private static readonly string[] ElderFutharkTranslated = { "a", "b", "c, q, k", "d", "e", "f", "g", "h", "i", "j", "y", "l", "m", "n", "o", "p", "z", "r", "s", "t", "u", "v, w", "x" };

    private bool moduleStarted = false;

    private int currentRune = 0;
    private int timesPressed = 0;
    private bool setupDone = false;

    private KMSelectable.OnInteractHandler RunePressed(int rune)
    {
        return delegate
        {
            StartCoroutine(PebbleWiggle(rune, RuneTransforms[rune].localEulerAngles));
            Audio.PlaySoundAtTransform("RockClick", transform);
            Runes[rune].AddInteractionPunch();
            if (moduleSolved)
                return false;
            if (rune == pickedRuneNamesCipher[currentRune][timesPressed])
            {
                Debug.LogFormat(@"[Elder Futhark #{0}] You pressed {1}, expecting {2}. Well done!", moduleId, ElderFuthark[rune], ElderFuthark[pickedRuneNamesCipher[currentRune][timesPressed]]);
                timesPressed++;
                if (timesPressed == pickedRuneNamesCipher[currentRune].Length)
                {
                    timesPressed = 0;
                    RuneLetters[currentRune][pickedRuneLetters[currentRune]].GetComponent<MeshRenderer>().material = Materials[2];
                    currentRune++;
                    Debug.LogFormat(@"[Elder Futhark #{0}] Rune no. {1} is solved.", moduleId, currentRune);
                    if (currentRune == 3)
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
                Debug.LogFormat(@"[Elder Futhark #{0}] You pressed {1}, expecting {2}. Strike. Rune resetted", moduleId, ElderFuthark[rune], ElderFuthark[pickedRuneNamesCipher[currentRune][timesPressed]]);
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
            if (moduleStarted)
                return false;
            StartCoroutine(CrashDownSetup());
            moduleStarted = true;
            Activator.gameObject.SetActive(false);
            GamepadSupport();
            Module.Children = RuneOrder.ToArray();
            RuneOrder.Clear();
            UpdateChildren();
            return false;
        };

        // Set the runes to invisible until they appear
        for (int i = 0; i < Runes.Length; i++)
        {
            Runes[i].gameObject.SetActive(false);
            Runes[i].OnInteract += RunePressed(i);
        }

        // Assign RuneLetters with the different positions
        RuneLetters = new[] { RuneLetters_1, RuneLetters_2, RuneLetters_3 };

        // Assign RuneTransforms with their childs
        RuneTransforms = Runes.Select(rune => rune.transform.parent).ToArray();
        RuneParentPos = Runes.Select(rune => rune.transform.parent.localPosition).ToArray();

        // Shuffle positions
        var randPos = Enumerable.Range(0, RuneTransforms.Length).ToList();
        var runeOrder = new KMSelectable[Runes.Length];

        for (int i = 0; i < RuneTransforms.Length; i++)
        {
            int index = Random.Range(0, randPos.Count);
            RuneTransforms[i].localPosition = RuneParentPos[randPos[index]];
            DustSystemRunes[i].transform.localPosition = RuneParentPos[randPos[index]];
            Vector3 DustPos = DustSystemRunes[i].transform.localPosition;
            DustPos.y = 0.01f;
            DustSystemRunes[i].transform.localPosition = DustPos;
            runeOrder[Array.IndexOf(selectableLocations, randPos[index])] = Runes[i];
            randPos.RemoveAt(index);
        }

        RuneOrder = runeOrder.ToList();

        // Generate a random 3-letter word
        for (int i = 0; i < RuneLetters.Length; i++)
        {
            pickedRuneLetters[i] = Random.Range(0, RuneLetters[i].Length);
            pickedRuneNames[i] = ElderFuthark[pickedRuneLetters[i]];
            Debug.LogFormat(@"[Elder Futhark #{0}] The {1} rune is {2}", moduleId, i == 0 ? "first" : i == 1 ? "second" : "third", pickedRuneNames[i]);
        }

        // Rule-seeded rules
        var rnd = RuleSeedable.GetRNG();
        Debug.LogFormat(@"[Elder Futhark #{0}] Using rule seed: {1}.", moduleId, rnd.Seed);
        var cycleLeft = rnd.Seed == 1 ? false : rnd.Next(0, 2) != 0;
        var order = rnd.Seed == 1 ? new[] { 0, 1, 2 } : rnd.ShuffleFisherYates(Enumerable.Range(0, 3).ToArray());

        var edgeworkConditions = newArray
        (
            // the sum of the digits in the serial number
            Bomb.GetSerialNumberNumbers().Sum(),
            // the sum of the first three characters in the serial number (use alphabetic positions for letters)
            Bomb.GetSerialNumber().Take(3).Select(ch => ch >= 'A' && ch <= 'Z' ? ch - 'A' + 1 : ch - '0').Sum(),
            // the sum of the last three characters in the serial number (use alphabetic positions for letters)
            Bomb.GetSerialNumber().Skip(3).Select(ch => ch >= 'A' && ch <= 'Z' ? ch - 'A' + 1 : ch - '0').Sum(),
            // the number of ports
            Bomb.GetPortCount(),
            // the number of indicators
            Bomb.GetIndicators().Count(),
            // the number of batteries
            Bomb.GetBatteryCount(),
            // the number of ports plus indicators
            Bomb.GetPortCount() + Bomb.GetIndicators().Count(),
            // the number of ports plus batteries
            Bomb.GetPortCount() + Bomb.GetBatteryCount(),
            // the number of indicators plus batteries
            Bomb.GetIndicators().Count() + Bomb.GetBatteryCount()
        );
        var edgeworkCondition = rnd.Seed == 1 ? 0 : rnd.Next(0, edgeworkConditions.Length);
        var modulo = rnd.Seed == 1 ? 6 : rnd.Next(4, 9);

        var rowShuffle = rnd.ShuffleFisherYates(Enumerable.Range(0, 23).ToArray());
        var columnShuffle = rnd.ShuffleFisherYates(Enumerable.Range(0, 23).ToArray());

        // Find the encryption key
        var name1 = pickedRuneNames[order[0]].ToLowerInvariant();
        var name2 = pickedRuneNames[order[1]].ToLowerInvariant();
        var name3 = pickedRuneNames[order[2]].ToLowerInvariant();

        var maxLength = Math.Max(name1.Length, Math.Max(name2.Length, name3.Length));

        var interweaved = Enumerable.Range(0, maxLength).Select(ix => name1[ix % name1.Length].ToString() + name2[ix % name2.Length].ToString() + name3[ix % name3.Length].ToString()).Join("");
        var cycleAmount = edgeworkConditions[edgeworkCondition] % modulo;
        if (cycleLeft)
            cycleAmount = interweaved.Length - cycleAmount;
        var encryptionKey = (interweaved.Substring(interweaved.Length - cycleAmount) + interweaved.Substring(0, interweaved.Length - cycleAmount)).Substring(0, pickedRuneNames[0].Length + pickedRuneNames[1].Length + pickedRuneNames[2].Length);
        Debug.LogFormat(@"[Elder Futhark #{0}] The encryption key is {1}.", moduleId, encryptionKey);

        // Encrypt the name of both runes together
        var transliteratedRunes = (pickedRuneNames[0] + pickedRuneNames[1] + pickedRuneNames[2]).ToLowerInvariant().Select(ch => ElderFutharkTranslated.IndexOf(tr => tr.Contains(ch))).ToArray();
        Debug.LogFormat(@"[Elder Futhark #{0}] transliteratedRunes = {1}", moduleId, transliteratedRunes.Select(ix => ElderFuthark[ix]).Join(", "));
        var transliteratedKey = encryptionKey.Select(ch => ElderFutharkTranslated.IndexOf(tr => tr.Contains(ch))).ToArray();
        Debug.LogFormat(@"[Elder Futhark #{0}] transliteratedKey = {1}", moduleId, transliteratedKey.Select(ix => ElderFuthark[ix]).Join(", "));
        var ciphered = transliteratedRunes.Select((ch, ix) => (columnShuffle[ch] + rowShuffle[transliteratedKey[ix]]) % 23).ToArray();
        pickedRuneNamesCipher = newArray(
            ciphered.Take(pickedRuneNames[0].Length).ToArray(),
            ciphered.Skip(pickedRuneNames[0].Length).Take(pickedRuneNames[1].Length).ToArray(),
            ciphered.Skip(pickedRuneNames[0].Length + pickedRuneNames[1].Length).ToArray()
        );
        Debug.LogFormat(@"[Elder Futhark #{0}] The encrypted first rune is: {1}.", moduleId, pickedRuneNamesCipher[0].Select(ix => ElderFuthark[ix]).Join(", "));
        Debug.LogFormat(@"[Elder Futhark #{0}] The encrypted second rune is: {1}.", moduleId, pickedRuneNamesCipher[1].Select(ix => ElderFuthark[ix]).Join(", "));
        Debug.LogFormat(@"[Elder Futhark #{0}] The encrypted third rune is: {1}.", moduleId, pickedRuneNamesCipher[2].Select(ix => ElderFuthark[ix]).Join(", "));
    }

    private T[] newArray<T>(params T[] array) { return array; }

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
        setupDone = true;
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
        yield return StartCoroutine(SetWord());
    }

    public void UpdateChildren()
    {
        Module.UpdateChildren(Module);
    }

    private void GamepadSupport()
    {
        //These are empty spots in the grid
        var pattern = new[] {
            2, 4, 6, 7,
            9, 10, 13, 15,
            16, 17, 19, 20, 22,
            26, 29, 31,
            32, 33, 35, 36, 38,
            40, 44
        };

        //Insert the empty spots into the list of selectables
        for (int i = 0; i < pattern.Length; i++)
            RuneOrder.Insert(pattern[i], null);
        //The last two slots are empty
        RuneOrder.AddRange(new KMSelectable[] { null, null });
    }

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

#pragma warning disable 0414
    private readonly string TwitchHelpMessage = "!{0} activate [to start the module] | !{0} submit eihwaz, hagalaz, fehu [submit the runenames]";
#pragma warning restore 0414

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
        else

            return null;
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        Debug.LogFormat(@"[Elder Futhark #{0}] Module was force solved by TP", moduleId);

        Activator.OnInteract();
        while (!setupDone)
            yield return true;

        while (!moduleSolved)
        {
            Runes[pickedRuneNamesCipher[currentRune][timesPressed]].OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
    }
}

