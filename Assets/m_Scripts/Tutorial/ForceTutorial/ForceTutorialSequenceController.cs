using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class ForceTutorialSequenceController : MonoBehaviour
{
    private static readonly Regex TutorialOrderRegex = new Regex(@"(\d+)$", RegexOptions.Compiled);

    [Header("Queue Behavior")]
    [Tooltip("Disable the ForceTutorial root after all child tutorials under the same parent complete.")]
    [SerializeField] private bool deactivateRootWhenFinished = true;

    private readonly List<MandatoryTutorialTrigger> orderedTriggers = new List<MandatoryTutorialTrigger>();
    private MandatoryTutorialTrigger currentTrigger;
    private bool initialized;

    public void InitializeIfNeeded()
    {
        if (initialized)
        {
            return;
        }

        initialized = true;
        RebuildTriggerList();
        ActivateNextIncompleteTrigger();
    }

    public void TryStartTrigger(MandatoryTutorialTrigger trigger)
    {
        if (trigger == null)
        {
            return;
        }

        InitializeIfNeeded();

        if (currentTrigger != trigger)
        {
            return;
        }

        if (!trigger.CanStartFromSequence() || !trigger.ShouldAutoStartFromSequence())
        {
            return;
        }

        trigger.StartFromSequence();
    }

    public void NotifyTriggerCompleted(MandatoryTutorialTrigger completedTrigger)
    {
        if (completedTrigger == null)
        {
            return;
        }

        InitializeIfNeeded();

        if (currentTrigger == completedTrigger)
        {
            currentTrigger = null;
        }

        ActivateNextIncompleteTrigger();
    }

    private void RebuildTriggerList()
    {
        orderedTriggers.Clear();

        MandatoryTutorialTrigger[] triggers = GetComponentsInChildren<MandatoryTutorialTrigger>(true);
        for (int i = 0; i < triggers.Length; i++)
        {
            MandatoryTutorialTrigger trigger = triggers[i];
            if (trigger == null)
            {
                continue;
            }

            trigger.BindSequenceController(this);
            orderedTriggers.Add(trigger);
        }

        orderedTriggers.Sort(CompareByTutorialOrder);
    }

    private void ActivateNextIncompleteTrigger()
    {
        MandatoryTutorialTrigger nextTrigger = FindNextIncompleteTrigger();
        currentTrigger = nextTrigger;
        ApplyActivationState(nextTrigger);

        if (nextTrigger != null &&
            nextTrigger.CanStartFromSequence() &&
            nextTrigger.ShouldAutoStartFromSequence())
        {
            nextTrigger.StartFromSequence();
        }
    }

    private MandatoryTutorialTrigger FindNextIncompleteTrigger()
    {
        for (int i = 0; i < orderedTriggers.Count; i++)
        {
            MandatoryTutorialTrigger trigger = orderedTriggers[i];
            if (trigger == null)
            {
                continue;
            }

            Tutorial triggerTutorialSystem = trigger.GetTutorialSystem();
            if (triggerTutorialSystem == null || !triggerTutorialSystem.IsTutorialCompleted(trigger.TutorialKey))
            {
                return trigger;
            }
        }

        return null;
    }

    private void ApplyActivationState(MandatoryTutorialTrigger activeTrigger)
    {
        for (int i = 0; i < orderedTriggers.Count; i++)
        {
            MandatoryTutorialTrigger trigger = orderedTriggers[i];
            if (trigger == null)
            {
                continue;
            }

            bool shouldBeActive = trigger == activeTrigger;
            if (trigger.gameObject.activeSelf != shouldBeActive)
            {
                trigger.gameObject.SetActive(shouldBeActive);
            }
        }

        if (activeTrigger == null && deactivateRootWhenFinished && gameObject.activeSelf)
        {
            gameObject.SetActive(false);
        }
    }

    private static int CompareByTutorialOrder(MandatoryTutorialTrigger a, MandatoryTutorialTrigger b)
    {
        int orderA = ExtractTutorialOrder(a != null ? a.TutorialKey : string.Empty);
        int orderB = ExtractTutorialOrder(b != null ? b.TutorialKey : string.Empty);
        int comparison = orderA.CompareTo(orderB);

        if (comparison != 0)
        {
            return comparison;
        }

        string nameA = a != null ? a.name : string.Empty;
        string nameB = b != null ? b.name : string.Empty;
        return string.CompareOrdinal(nameA, nameB);
    }

    private static int ExtractTutorialOrder(string tutorialKey)
    {
        if (string.IsNullOrEmpty(tutorialKey))
        {
            return int.MaxValue;
        }

        Match match = TutorialOrderRegex.Match(tutorialKey);
        if (!match.Success)
        {
            return int.MaxValue;
        }

        int parsedOrder;
        return int.TryParse(match.Groups[1].Value, out parsedOrder) ? parsedOrder : int.MaxValue;
    }
}
