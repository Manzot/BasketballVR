using System;
using UnityEngine;

/// <summary>
/// Anything a hand can pick up. Owns nothing about input - it only tracks who is holding it
/// and restores its own physics state on release, so two hands can never fight over one ball.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class Grabbable : MonoBehaviour
{
    public event Action<Grabbable> Grabbed;
    public event Action<Grabbable> Released;

    private Rigidbody m_body;
    private HandController m_holder;
    private float m_defaultLinearDamping;
    private float m_defaultAngularDamping;
    private bool m_defaultUseGravity;

    public Rigidbody Body
    {
        get { return m_body; }
    }

    public bool IsHeld
    {
        get { return m_holder != null; }
    }

    private void Awake()
    {
        m_body = GetComponent<Rigidbody>();
        m_defaultLinearDamping = m_body.linearDamping;
        m_defaultAngularDamping = m_body.angularDamping;
        m_defaultUseGravity = m_body.useGravity;
    }

    /// <summary>
    /// Returns false when another hand already holds this object.
    /// While held, gravity and damping are off so the velocity-follow in HandController
    /// is not fighting them every step.
    /// </summary>
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
        m_body.useGravity = false;
        m_body.linearDamping = 0f;
        m_body.angularDamping = 0f;

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
        m_body.useGravity = m_defaultUseGravity;
        m_body.linearDamping = m_defaultLinearDamping;
        m_body.angularDamping = m_defaultAngularDamping;
        m_body.linearVelocity = velocity;
        m_body.angularVelocity = angularVelocity;

        if(Released != null)
        {
            Released(this);
        }
    }
}
