using System.Collections;
using UnityEngine;
using TMPro;

public class KeypadController : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private TextMeshProUGUI passwordDisplay;
    [SerializeField] private string correctCode = "1234";
    [SerializeField] private int maxDigits = 10;

    [Header("Feedback Icons")]
    [SerializeField] private GameObject checkIcon;
    [SerializeField] private GameObject crossIcon;
    [SerializeField] private float feedbackDuration = 1.5f;

    [Header("Fade Effect")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeDuration = 1.0f;
    [SerializeField] private float targetOpacity = 1.0f;

    private string currentInput = "";
    private Coroutine feedbackCoroutine;
    private Coroutine fadeCoroutine;

    void OnEnable()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f; // Start completely transparent
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeIn());
        }
    }

    void Start()
    {
        ClearInput();
        if (checkIcon != null) checkIcon.SetActive(false);
        if (crossIcon != null) crossIcon.SetActive(false);
    }

    private IEnumerator FadeIn()
    {
        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            canvasGroup.alpha = Mathf.Lerp(0f, targetOpacity, elapsedTime / fadeDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        canvasGroup.alpha = targetOpacity;
    }

    // Called by the 0-9 buttons
    public void AddDigit(string digit)
    {
        if (currentInput.Length < maxDigits)
        {
            currentInput += digit;
            Debug.Log(digit);
            UpdateDisplay();
        }
    }

    // Called by the Clear button
    public void ClearInput()
    {
        currentInput = "";
        UpdateDisplay();
    }

    // Called by the Enter button
    public void SubmitCode()
    {
        if (currentInput == correctCode)
        {
            TriggerFeedback(true);
            // Add your success logic here
            Debug.Log("Laptop unlocked!");
        }
        else
        {
            TriggerFeedback(false);
            ClearInput(); 
        }
    }

    private void UpdateDisplay()
    {
        if (passwordDisplay != null) passwordDisplay.text = currentInput;
    }

    private void TriggerFeedback(bool isSuccess)
    {
        // Stop any existing feedback to reset the timer
        if (feedbackCoroutine != null) StopCoroutine(feedbackCoroutine);
        feedbackCoroutine = StartCoroutine(ShowFeedbackRoutine(isSuccess));
    }

    private IEnumerator ShowFeedbackRoutine(bool isSuccess)
    {
        // Turn both off for a split second to create a noticeable blink
        if (checkIcon != null) checkIcon.SetActive(false);
        if (crossIcon != null) crossIcon.SetActive(false);
        
        yield return new WaitForSeconds(0.1f);

        // Turn on the correct icon
        if (isSuccess && checkIcon != null) checkIcon.SetActive(true);
        else if (!isSuccess && crossIcon != null) crossIcon.SetActive(true);

        // Wait, then hide them again
        yield return new WaitForSeconds(feedbackDuration);

        if (checkIcon != null) checkIcon.SetActive(false);
        if (crossIcon != null) crossIcon.SetActive(false);
    }
}