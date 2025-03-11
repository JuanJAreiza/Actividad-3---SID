using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ScoreManager : MonoBehaviour
{
    public TMP_Text scoreText;

    private int currentScore = 0;

    void Start()
    {
        UpdateScoreText();
    }

    public void IncreaseScore()
    {
        currentScore++;
        UpdateScoreText();
    }

    public void DecreaseScore()
    {
        if (currentScore > 0)
        {
            currentScore--;
            UpdateScoreText();
        }
    }

    private void UpdateScoreText()
    {
        scoreText.text = currentScore.ToString();
    }

    public int GetCurrentScore()
    {
        return currentScore;
    }
}
