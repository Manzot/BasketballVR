using UnityEngine;

public class ScoreTopZoneValidator : MonoBehaviour
{
    [SerializeField] private ScoreTrigger m_scoreTrigger;

    private void OnTriggerEnter(Collider other)
    {
        if (m_scoreTrigger != null)
        {
            m_scoreTrigger.AddValidBall(other);
        }
    }
}
