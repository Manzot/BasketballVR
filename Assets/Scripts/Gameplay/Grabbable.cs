using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Grabbable : MonoBehaviour
{
    public event Action<Grabbable> Grabbed;
    public event Action<Grabbable> Released;

    private Rigidbody m_rigidbody;
    private HandController m_holder;
    private float m_defaultLinearDamping;
    private float m_defaultAngularDamping;
    private bool m_defaultUseGravity;

    public Rigidbody RigidBody => m_rigidbody;

    public bool IsHeld => m_holder != null;

    private void Awake()
    {
        m_rigidbody = GetComponent<Rigidbody>();
        m_defaultLinearDamping = m_rigidbody.linearDamping;
        m_defaultAngularDamping = m_rigidbody.angularDamping;
        m_defaultUseGravity = m_rigidbody.useGravity;
    }

    public bool TryGrab(HandController hand)
    {
        if(hand == null)
        {
            return false;
        }

        if(m_holder != null)
        {
            return false;
        }

        m_holder = hand;
        m_rigidbody.useGravity = false;
        m_rigidbody.linearDamping = 0f;
        m_rigidbody.angularDamping = 0f;

        if(Grabbed != null)
        {
            Grabbed(this);
        }

        return true;
    }

    public void Release(HandController hand, Vector3 velocity, Vector3 angularVelocity)
    {
        if(m_holder != hand)
        {
            return;
        }

        m_holder = null;
        m_rigidbody.useGravity = m_defaultUseGravity;
        m_rigidbody.linearDamping = m_defaultLinearDamping;
        m_rigidbody.angularDamping = m_defaultAngularDamping;
        m_rigidbody.linearVelocity = velocity;
        m_rigidbody.angularVelocity = angularVelocity;

        if(Released != null)
        {
            Released(this);
        }
    }
}
