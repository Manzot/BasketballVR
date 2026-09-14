using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ScoreUI : MonoBehaviour
{
    [SerializeField] private IntGameEvent m_scoreEvent;
    [SerializeField] private List<TextMeshProUGUI> m_scoreTexts;
    private int m_currentScore;

    private void Start()
    {
        OnScoreRaiesed(0);
    }

    private void OnEnable()
    {
        m_scoreEvent.OnRaised += OnScoreRaiesed;
    }

    private void OnDisable()
    {
        m_scoreEvent.OnRaised -= OnScoreRaiesed; 
    }

    private void OnScoreRaiesed(int scoreValue)
    {
        m_currentScore += scoreValue;
        foreach (var t in m_scoreTexts)
        {
            t.text = "Score: " + m_currentScore.ToString();
        }
    }
}
