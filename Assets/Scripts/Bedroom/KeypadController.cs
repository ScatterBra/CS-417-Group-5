using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.Events;

public class KeypadController : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private TextMeshProUGUI passwordDisplay;
    [SerializeField] private string correctCode = "1234";
    [SerializeField] private int maxDigits = 4; 

    [Header("Feedback Icons")]
    [SerializeField] private GameObject checkIcon;
    [SerializeField] private GameObject crossIcon;
    [SerializeField] private float feedbackDuration = 1.5f;

    [Header("Fade Effect")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeDuration = 1.0f;
    [SerializeField] private float targetOpacity = 1.0f;

    [Header("Success Events")]
    public UnityEvent onUnlockSuccess; 

    private string currentInput = "";
    private Coroutine feedbackCoroutine;
    private Coroutine fadeCoroutine;
    
    // Flag to disable input after success
    private bool isSolved = false;

    void OnEnable()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f; 
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeIn());
        }
    }

    void Start()
    {
        isSolved = false;
        currentInput = "";
        UpdateDisplay(); // This will show the initial "X DIGITS" prompt
        
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

    public void AddDigit(string digit)
    {
        // Block input if the puzzle is already solved
        if (isSolved) return;

        if (currentInput.Length < maxDigits)
        {
            currentInput += digit;
            UpdateDisplay();
        }
    }

    public void ClearInput()
    {
        // Block clearing if already solved
        if (isSolved) return;

        currentInput = "";
        UpdateDisplay();
    }

    public void SubmitCode()
    {
        // Block submission spam if already solved
        if (isSolved) return;

        if (currentInput == correctCode)
        {
            isSolved = true;
            
            if (passwordDisplay != null) 
            {
                passwordDisplay.text = "SOLVED";
                passwordDisplay.color = Color.skyBlue; 
            }
            
            TriggerFeedback(true);
            onUnlockSuccess.Invoke(); 
        }
        else
        {
            TriggerFeedback(false);
            ClearInput(); 
        }
    }

    private void UpdateDisplay()
    {
        if (passwordDisplay != null) 
        {
            // Show prompt if empty, otherwise show what the player typed
            if (string.IsNullOrEmpty(currentInput))
            {
                passwordDisplay.text = maxDigits + " DIGITS";
            }
            else
            {
                passwordDisplay.text = currentInput;
            }
        }
    }

    private void TriggerFeedback(bool isSuccess)
    {
        if (feedbackCoroutine != null) StopCoroutine(feedbackCoroutine);
        feedbackCoroutine = StartCoroutine(ShowFeedbackRoutine(isSuccess));
    }

    private IEnumerator ShowFeedbackRoutine(bool isSuccess)
    {
        if (checkIcon != null) checkIcon.SetActive(false);
        if (crossIcon != null) crossIcon.SetActive(false);
        
        yield return new WaitForSeconds(0.1f);

        if (isSuccess && checkIcon != null) checkIcon.SetActive(true);
        else if (!isSuccess && crossIcon != null) crossIcon.SetActive(true);

        yield return new WaitForSeconds(feedbackDuration);

        // Keep the check icon on permanently if it was successful, otherwise turn cross off
        if (!isSuccess)
        {
            if (crossIcon != null) crossIcon.SetActive(false);
        }
    }
}