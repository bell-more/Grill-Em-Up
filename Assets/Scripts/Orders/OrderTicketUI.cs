using UnityEngine;
using UnityEngine.UI;

public class OrderTicketUI : MonoBehaviour
{
    public Image recipeIconDisplay;

    [Header("Patience Bar")]
    public Slider patienceSlider;
    public Image sliderFillImage;

    private float totalDuration;
    private float currentTimer;
    private bool isTimerRunning = false;

    public void Setup(Recipe recipeData, float duration)
    {
        if (recipeData.recipeIcon != null && recipeIconDisplay != null)
        {
            recipeIconDisplay.sprite = recipeData.recipeIcon;
        }

        totalDuration = duration;
        currentTimer = duration;

        if (patienceSlider != null)
        {
            patienceSlider.minValue = 0f;
            patienceSlider.maxValue = 1f;
            patienceSlider.value = 1f;
        }

        isTimerRunning = true;
    }

    void Update()
    {
        if (!isTimerRunning || patienceSlider == null) return;

        currentTimer -= Time.deltaTime;
        float progress = Mathf.Clamp01(currentTimer / totalDuration);
        patienceSlider.value = progress;

        if (sliderFillImage != null)
        {
            if (progress > 0.5f)
            {
                sliderFillImage.color = Color.Lerp(Color.yellow, Color.green, (progress - 0.5f) * 2f);
            }
            else
            {
                sliderFillImage.color = Color.Lerp(Color.red, Color.yellow, progress * 2f);
            }
        }
    }
}