using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using System.Text.RegularExpressions;
using rnd = UnityEngine.Random;

public class timeMachine : MonoBehaviour
{
    public new KMAudio audio;
    public KMBombInfo bomb;
    public KMBombModule module;

    public KMSelectable button;
    public Renderer[] leds;
    public Renderer[] tubes;
    public TextMesh screenText;
    public Material[] ledColors;
    public Material[] tubeColors;

    private int displayedNumber;
    private int selectedOffset;
    private List<int> validOffsets = new List<int>();
    private int[] offsetDisplayOrder = new int[10];

    private static readonly string[] holidayNames = new string[] { "Christmas", "Valentine's Day", "Veteran's Day", "Halloween", "New Year's", "New Year's Eve", "Christmas Eve", "Cinco de Mayo", "April Fools", "Earth Day" };
    private static readonly string[] conditionNames = new string[] { "a Monday", "a Tuesday", "a Wednesday", "a Thursday", "a Friday", "a Saturday", "a Sunday", "a weekend", "a weekday", "today's day" };
    private Coroutine cycleAnimation;

    private static int moduleIdCounter = 1;
    private int moduleId;
    private bool moduleSolved;

    private void Awake()
    {
        moduleId = moduleIdCounter++;
        button.OnInteract += delegate () { PressButton(); return false; };
    }

    private void Start()
    {
        Debug.LogFormat("[Time Machine #{0}] Welcome, young time explorer!", moduleId);
        var now = DateTime.Now;
        var offsetYears = new int[10];
        var offsets = new int[] { -5, -4, -3, -2, -1, 1, 2, 3, 4, 5 };
        for (int i = 0; i < 10; i++)
            offsetYears[i] = now.AddYears(offsets[i]).Year;
        var possibleDestinationTimes = new DateTime[10][];
        for (int i = 0; i < 10; i++)
            possibleDestinationTimes[i] = new DateTime[10];
        var holidayMonths = new int[] { 12, 2, 11, 10, 1, 12, 12, 5, 4, 4 };
        var holidayDates = new int[] { 25, 14, 11, 31, 1, 31, 24, 5, 1, 22 };
        for (int i = 0; i < 10; i++)
            for (int j = 0; j < 10; j++)
                possibleDestinationTimes[i][j] = new DateTime(offsetYears[i], holidayMonths[j], holidayDates[j]);

            tryAgain:
        displayedNumber = rnd.Range(0, 100);
        for (int i = 0; i < 10; i++)
        {
            switch (displayedNumber % 10)
            {
                case 0:
                    if (possibleDestinationTimes[i][displayedNumber / 10].DayOfWeek == DayOfWeek.Monday)
                        validOffsets.Add(offsets[i]);
                    break;
                case 1:
                    if (possibleDestinationTimes[i][displayedNumber / 10].DayOfWeek == DayOfWeek.Tuesday)
                        validOffsets.Add(offsets[i]);
                    break;
                case 2:
                    if (possibleDestinationTimes[i][displayedNumber / 10].DayOfWeek == DayOfWeek.Wednesday)
                        validOffsets.Add(offsets[i]);
                    break;
                case 3:
                    if (possibleDestinationTimes[i][displayedNumber / 10].DayOfWeek == DayOfWeek.Thursday)
                        validOffsets.Add(offsets[i]);
                    break;
                case 4:
                    if (possibleDestinationTimes[i][displayedNumber / 10].DayOfWeek == DayOfWeek.Friday)
                        validOffsets.Add(offsets[i]);
                    break;
                case 5:
                    if (possibleDestinationTimes[i][displayedNumber / 10].DayOfWeek == DayOfWeek.Saturday)
                        validOffsets.Add(offsets[i]);
                    break;
                case 6:
                    if (possibleDestinationTimes[i][displayedNumber / 10].DayOfWeek == DayOfWeek.Sunday)
                        validOffsets.Add(offsets[i]);
                    break;
                case 7:
                    if (possibleDestinationTimes[i][displayedNumber / 10].DayOfWeek == DayOfWeek.Saturday || possibleDestinationTimes[i][displayedNumber / 10].DayOfWeek == DayOfWeek.Sunday)
                        validOffsets.Add(offsets[i]);
                    break;
                case 8:
                    if (possibleDestinationTimes[i][displayedNumber / 10].DayOfWeek != DayOfWeek.Saturday && possibleDestinationTimes[i][displayedNumber / 10].DayOfWeek != DayOfWeek.Sunday)
                        validOffsets.Add(offsets[i]);
                    break;
                case 9:
                    if (possibleDestinationTimes[i][displayedNumber / 10].DayOfWeek == now.DayOfWeek)
                        validOffsets.Add(offsets[i]);
                    break;
            }
        }
        if (validOffsets.Count() == 0)
            goto tryAgain;
        Debug.LogFormat("[Time Machine #{0}] The displayed number is {1}.", moduleId, displayedNumber.ToString("00"));
        Debug.LogFormat("[Time Machine #{0}] {1} must be {2}.", moduleId, holidayNames[displayedNumber / 10], conditionNames[displayedNumber % 10]);
        Debug.LogFormat("[Time Machine #{0}] Valid offsets: {1}", moduleId, validOffsets.Join(", "));

        var binary1 = Convert.ToString(displayedNumber / 10, 2).PadLeft(4, '0');
        var binary2 = Convert.ToString(displayedNumber % 10, 2).PadLeft(4, '0');
        for (int i = 0; i < 4; i++)
        {
            if (binary1[i] == '1' && binary2[i] == '1')
                tubes[3 - i].material = tubeColors[2];
            else if (binary1[i] == '1')
                tubes[3 - i].material = tubeColors[0];
            else if (binary2[i] == '1')
                tubes[3 - i].material = tubeColors[1];
            else
                tubes[3 - i].material = tubeColors[3];
        }
        offsetDisplayOrder = offsets.ToList().Shuffle().ToArray();
        cycleAnimation = StartCoroutine(CycleLeds());
        StartCoroutine(FlickerText());
    }

    private void PressButton()
    {
        button.AddInteractionPunch(.25f);
        audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, button.transform);
        if (moduleSolved)
            return;
        Debug.LogFormat("[Time Machine #{0}] You attempted to travel {1} years.", moduleId, offsetDisplayOrder[selectedOffset]);
        if (validOffsets.Contains(offsetDisplayOrder[selectedOffset]))
        {
            Debug.LogFormat("[Time Machine #{0}] That was a valid offset, enjoy your trip! Module solved!", moduleId);
            module.HandlePass();
            moduleSolved = true;
            if (cycleAnimation != null)
            {
                StopCoroutine(cycleAnimation);
                cycleAnimation = null;
            }
            audio.PlaySoundAtTransform("solve", transform);
            screenText.text = "!!!";
            StartCoroutine(SolveAnimation());
        }
        else
        {
            Debug.LogFormat("[Time Machine #{0}] That was not a valid offset, and you have caused a time paradox! Strike!", moduleId);
            module.HandleStrike();
        }
    }

    private IEnumerator CycleLeds()
    {
        while (true)
        {
            leds[selectedOffset].material = offsetDisplayOrder[selectedOffset] < 0 ? ledColors[1] : ledColors[2];
            screenText.text = Math.Abs(offsetDisplayOrder[selectedOffset]).ToString();
            yield return new WaitForSeconds(.75f);
            leds[selectedOffset].material = ledColors[0];
            selectedOffset = (selectedOffset + 1) % 10;
        }
    }

    private IEnumerator FlickerText()
    {
        var sysRandom = new System.Random();
        while (true)
        {
            var num = .9 * sysRandom.NextDouble() + .1;
            screenText.color = new Color(0x43 / 255f, 0x43 / 255f, 0x43 / 255f, (float)(sysRandom.NextDouble() * num));
            yield return new WaitForSeconds((float)(.25 * (1.1 - num)));
        }
    }

    private IEnumerator SolveAnimation()
    {
        leds[selectedOffset].material = ledColors[0];
        foreach (Renderer tube in tubes)
            tube.material = tubeColors[3];
        for (int i = 0; i < 10; i++)
        {
            leds[i].material = ledColors[3];
            yield return new WaitForSeconds(.25f);
        }
    }

    // Twitch Plays
#pragma warning disable 414
    private readonly string TwitchHelpMessage = "!{0} <#> [Presses the button when LED # is highlighted. The north LED is 0, and they go clockwise.]";
#pragma warning restore 414

    private IEnumerator ProcessTwitchCommand(string input)
    {
        input = input.Trim();
        var digits = "0123456789".Select(x => x.ToString()).ToArray();
        if (!digits.Contains(input))
            yield break;
        while (selectedOffset != Array.IndexOf(digits, input))
            yield return "trycancel";
        yield return null;
        button.OnInteract();
    }

    private IEnumerator TwitchHandleForcedSolve()
    {
        while (!validOffsets.Contains(offsetDisplayOrder[selectedOffset]))
        {
            yield return true;
            yield return null;
        }
        yield return null;
        button.OnInteract();
    }
}
