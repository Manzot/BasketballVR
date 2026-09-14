using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns the starting balls in a row at this transform. Ball count is a serialized field,
/// so the number is a tuning knob rather than a code change.
/// </summary>
public class BallRack : MonoBehaviour
{
    [SerializeField] private GameObject m_ballPrefab;
    [SerializeField] private int m_ballCount = 5;
    [SerializeField] private float m_spacing = 0.32f;
    [SerializeField] private Vector3 m_rowDirection = Vector3.right;
    [SerializeField] private bool m_centreRow = true;

    private readonly List<Rigidbody> m_balls = new List<Rigidbody>();
    private readonly List<Vector3> m_slotPositions = new List<Vector3>();

    public int SpawnedCount
    {
        get { return m_balls.Count; }
    }

    private void Start()
    {
        SpawnBalls();
    }

    public void SpawnBalls()
    {
        if(m_ballPrefab == null)
        {
            Debug.LogError("[BallRack] No ball prefab assigned.");
            return;
        }

        m_balls.Clear();
        m_slotPositions.Clear();

        Vector3 direction = m_rowDirection.sqrMagnitude < 0.0001f ? Vector3.right : m_rowDirection.normalized;
        float offset = m_centreRow ? (m_ballCount - 1) * m_spacing * 0.5f : 0f;

        for(int i = 0; i < m_ballCount; i++)
        {
            Vector3 position = transform.position + (direction * ((i * m_spacing) - offset));
            GameObject ball = Instantiate(m_ballPrefab, position, Quaternion.identity, transform);
            ball.name = m_ballPrefab.name + "_" + i;

            Rigidbody body = ball.GetComponent<Rigidbody>();

            if(body == null)
            {
                Debug.LogError("[BallRack] Ball prefab has no Rigidbody.");
                continue;
            }

            m_balls.Add(body);
            m_slotPositions.Add(position);
        }
    }

    /// <summary>
    /// Puts every spawned ball back in its slot, at rest. Useful for a reset button later.
    /// </summary>
    public void ResetBalls()
    {
        for(int i = 0; i < m_balls.Count; i++)
        {
            Rigidbody body = m_balls[i];

            if(body == null)
            {
                continue;
            }

            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.position = m_slotPositions[i];
            body.rotation = Quaternion.identity;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 direction = m_rowDirection.sqrMagnitude < 0.0001f ? Vector3.right : m_rowDirection.normalized;
        float offset = m_centreRow ? (m_ballCount - 1) * m_spacing * 0.5f : 0f;

        Gizmos.color = Color.yellow;

        for(int i = 0; i < m_ballCount; i++)
        {
            Gizmos.DrawWireSphere(transform.position + (direction * ((i * m_spacing) - offset)), 0.15f);
        }
    }
}
