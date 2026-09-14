using TMPro;
using UnityEngine;

public class WowEffects : MonoBehaviour
{
    private const string Perfect = "Perfect!";
    private const string Great = "Great!";
    private const string Nice = "Nice!";

    private static Color PerfectColor = Color.green;
    private static Color GreatColor = Color.blue;
    private static Color NiceColor = Color.gray;

    [SerializeField] private TextMeshProUGUI m_text;
    [SerializeField] private Animator m_textAnimator;
    [SerializeField] private IntGameEvent m_scoreEvent;
    [SerializeField] private ParticleSystem m_vfxPerfect;
    [SerializeField] private AudioSource m_audioSource;
    [SerializeField] private AudioClip[] m_soundEffects;
    
    private void OnEnable()
    {
        m_scoreEvent.OnRaised += ShowFloatingText;
    }

    private void OnDisable()
    {
        m_scoreEvent.OnRaised -= ShowFloatingText;
    }

    private void ShowFloatingText(int score)
    {
        m_text.gameObject.SetActive(true);
        m_textAnimator.SetTrigger("floating");
        if(score >= GameConstants.MaxScore)
        {
            m_text.text = Perfect;
            m_text.color = PerfectColor;
            m_vfxPerfect.Play();
            m_audioSource.clip = m_soundEffects[0];
            m_audioSource.Play();
            return;
        }
        if (score < GameConstants.MaxScore && score > GameConstants.MinScore)
        {
            m_text.text = Great;
            m_text.color = GreatColor;
            m_audioSource.clip = m_soundEffects[1];
            m_audioSource.Play();
            return;
        }

        m_text.text = Nice;
        m_text.color = NiceColor;
        m_audioSource.clip = m_soundEffects[2];
        m_audioSource.Play();
        return;
    }
}
