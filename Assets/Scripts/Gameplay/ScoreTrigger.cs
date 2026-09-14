using System.Collections;
using UnityEngine;

public class ScoreTrigger : MonoBehaviour
{
    [SerializeField] private IntGameEvent m_scoreEvent;
    [SerializeField] private LayerMask m_ballLayer;
    private Collider m_validBall;
    private Coroutine m_ballValidationRoutine;

    private void OnTriggerEnter(Collider other)
    {
        if (m_validBall == null || m_validBall != other)
            return;

        if ((m_ballLayer.value & (1 << other.gameObject.layer)) != 0)
        {
            if (other.TryGetComponent<BasketBall>(out BasketBall ball))
            {
                if(ball.RigidBody.linearVelocity.y < 0)
                {
                    AddScores(ball.BounceCount);
                }
            }
        }
    }

    private void AddScores(int bouncesCount)
    {
        int scoresGained = bouncesCount <= 1 ? GameConstants.MaxScore : GameConstants.MaxScore / ((bouncesCount -1) * GameConstants.BouncePenalty);
        scoresGained = Mathf.Max(scoresGained, GameConstants.MinScore);
        m_scoreEvent?.Raise(scoresGained);
    }

    public void AddValidBall(Collider validBall)
    {
        if(m_ballValidationRoutine != null)
            StopCoroutine(m_ballValidationRoutine);
        
        m_validBall = validBall;
        m_ballValidationRoutine = StartCoroutine(ResetValidBall());
    }

    private IEnumerator ResetValidBall()
    {
        yield return new WaitForSeconds(2f);
        m_validBall = null;
    }
}
