using UnityEngine;

public class ScoreTrigger : MonoBehaviour
{
    [SerializeField] private IntGameEvent m_scoreEvent;
    [SerializeField] private LayerMask m_ballLayer;
    private Collider m_validBall;

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
        int scoresGained = bouncesCount <= 0 ? GameConstants.MaxScore : GameConstants.MaxScore / (bouncesCount * GameConstants.BouncePenalty);
        m_scoreEvent?.Raise(scoresGained);
    }

    public void AddValidBall(Collider validBall)
    {
        m_validBall = validBall;
    }
}
