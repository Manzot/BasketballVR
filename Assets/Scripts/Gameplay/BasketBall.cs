using UnityEngine;

public class BasketBall : MonoBehaviour
{
    private Grabbable m_grabbable;
    private Rigidbody m_rigidbody;
    private int m_bounceCount;
    private bool m_isGrounded;
    [SerializeField] private bool m_ignoreWhileHeld = true; 
    [SerializeField] private LayerMask m_groundLayers;

    public int BounceCount => m_bounceCount;
    public Rigidbody RigidBody => m_rigidbody;

    private void Awake()
    {
        m_grabbable = GetComponent<Grabbable>();
        m_rigidbody = GetComponent<Rigidbody>();
    }

    public void ResetBounces()
    {
        m_bounceCount = 0;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (m_ignoreWhileHeld && m_grabbable != null && m_grabbable.IsHeld)
        {
            return;
        }

        if (IsGround(collision.collider))
        {
            m_isGrounded = true;
            m_bounceCount = 0;
            return;
        }
        m_bounceCount++;
    }

    private void OnCollisionExit(Collision collision)
    {
        if (!IsGround(collision.collider))
        {
            return;
        }

        m_isGrounded = false;
        m_bounceCount = 0;
    }

    private bool IsGround(Collider other)
    {
        if (other == null)
        {
            return false;
        }

        if ((m_groundLayers.value & (1 << other.gameObject.layer)) != 0)
        {
            return true;
        }

        return false;
    }
}
