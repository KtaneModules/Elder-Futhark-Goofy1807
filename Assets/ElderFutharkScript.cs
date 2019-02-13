using System;
using System.Collections;
using System.Linq;
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
    public KMSelectable Module;
    public GameObject[] RuneLetters_1;
    public GameObject[] RuneLetters_2;
    public GameObject[] RuneLetters_3;
    public GameObject[] RuneLetters_4;
    public GameObject[] RuneLetters_5;
    public GameObject[] RuneLetters_6;
    public Material[] Materials;
    public AudioClip[] Sounds;

    public GameObject[] DustSystemLetters;
    public GameObject[] DustSystemRunes;

    private GameObject[][] RuneLetters;
    private Transform[] RuneTransforms;

    private int[] pickedRuneLetters = new int[6];
    private string[] pickedRuneNames = new string[6];
    private string[] pickedRuneNamesCipher = new string[6];
    private string[] Keywords = new string[6];

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
            if (ElderFutharkTranslated[rune].Contains(pickedRuneNamesCipher[currentRune][timesPressed]))
            {
                Debug.LogFormat(@"[Elder Futhark #{0}] You pressed {1}, expecting {2}. Well Done", moduleId, ElderFutharkTranslated[rune], pickedRuneNamesCipher[currentRune][timesPressed]);
                timesPressed++;
                if (timesPressed == pickedRuneNamesCipher[currentRune].Length)
                {
                    timesPressed = 0;
                    RuneLetters[currentRune][pickedRuneLetters[currentRune]].GetComponent<MeshRenderer>().material = Materials[2];
                    currentRune++;
                    RuneLetters[currentRune][pickedRuneLetters[currentRune]].GetComponent<MeshRenderer>().material = Materials[1];
                    if (currentRune == 6)
                        GetComponent<KMBombModule>().HandlePass();
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
        Module.OnInteract += delegate
        {
            StartCoroutine(CrashDownSetup());
            moduleStarted = true;
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
            RuneLetters_3,
            RuneLetters_4,
            RuneLetters_5,
            RuneLetters_6
        };
        //Assigning RuneTransforms with their childs
        RuneTransforms = Runes.Select(rune => rune.transform.parent).ToArray();

        //Generating a random 6-letter word
        for (int i = 0; i < RuneLetters.Length; i++)
        {
            pickedRuneLetters[i] = Random.Range(0, RuneLetters[i].Length);
            pickedRuneNames[i] = ElderFuthark[pickedRuneLetters[i]];
            Debug.LogFormat(@"[Elder Futhark #{0}] The {1}th rune is {2}", moduleId, i + 1, pickedRuneNames[i]);
        }

        //Generating 6 keywords

        for (int i = 0; i < Keywords.Length; i++)
        {
            if (pickedRuneNames[i].ToUpper().Any(x => x == 'E' || x == 'O'))
                Keywords[i] += AlphabetNumbers[(Bomb.GetSerialNumberNumbers().Last() + 10) % 26];
            if ((i + 1) % 2 == 0)
                Keywords[i] += AlphabetNumbers[Bomb.GetSolvableModuleNames().Count() / Bomb.GetSerialNumberLetters().Count() * Bomb.GetPortPlateCount() % 26];
            if ((i + 1) % 3 == 0)
                Keywords[i] += AlphabetNumbers[Factorial(Bomb.GetOffIndicators().Count()) % 26];
            if (pickedRuneNames[i].Length < 5)
                Keywords[i] += AlphabetNumbers[Bomb.GetIndicators().Count() + Bomb.GetBatteryHolderCount() % 26];
            Keywords[i] += AlphabetNumbers[i];
            Keywords[i] += AlphabetNumbers[pickedRuneNames[i].Length * 10 % 26];
            Keywords[i] += AlphabetNumbers[(i + 1 + pickedRuneNames[i].Length) % 26];
            Debug.LogFormat(@"[Elder Futhark #{0}] The keyword for the {1}th rune is {2}", moduleId, i + 1, Keywords[i]);

        }

        //Crypting the name of each rune in the 6-letter word
        for (int i = 0; i < pickedRuneNames.Length; i++)
        {
            string pickedRuneName = pickedRuneNames[i];

            for (int j = 0; j < pickedRuneName.Length; j++)

            {
                pickedRuneNamesCipher[i] += AlphabetNumbers[(Array.IndexOf(AlphabetNumbers, char.ToLowerInvariant(pickedRuneName[j])) + Array.IndexOf(AlphabetNumbers, char.ToLowerInvariant(Keywords[i][j % Keywords[i].Length]))) % 26];
            }
            Debug.LogFormat(@"[Elder Futhark #{0}] The encrypted name of the {1}th rune is {2}. The required sequence for the {1}th rune is: {3}", moduleId, i + 1, pickedRuneNamesCipher[i], "NEEDS WORK");
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
}
