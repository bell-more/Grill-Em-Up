using UnityEngine;
using UnityEngine.UI;

public class LevelSelectorItem : MonoBehaviour
{
    [SerializeField] private int levelNumber = 1;

    //stars
    [SerializeField] private Image[] stars;
    [SerializeField] private Sprite activeStar;
    [SerializeField] private Sprite inactiveStar;

    [SerializeField] private GameObject objectToHide;
    [SerializeField] private GameObject playButton;

    private void Start()
    {
        UpdateLevel();
    }

    public void UpdateLevel()
    {
        int starCount = LevelProgressManager.GetLevelStars(levelNumber);
        int previousLevelStarCount = LevelProgressManager.GetLevelStars(levelNumber-1);

        for (int i = 0; i < stars.Length; i++)
        {
            if (stars[i] != null)
                stars[i].sprite = i < starCount ? activeStar : inactiveStar;
        }

        if (objectToHide != null && levelNumber != 1)
        {
            objectToHide.SetActive(starCount == 0);

            if (previousLevelStarCount != 0)
            {
               if(playButton != null) playButton.SetActive(true);
                objectToHide.SetActive(false);
            }
            
        }
    }
}
