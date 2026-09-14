using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

/// <summary>
/// One per hand. Owns input, both grab modes, the pose history used for the throw, and haptics.
/// No XR Interaction Toolkit - input comes straight from Input System XR bindings.
///
/// Grip    = close grab. Only picks up a ball already next to the hand, so dribbling works.
/// Trigger = force grab. Spherecast out, then the ball flies to the hand and is auto-caught.
/// </summary>
public class HandController : MonoBehaviour
{
    public enum Handedness
    {
        Left,
        Right
    }

    private struct PoseSample
    {
        public Vector3 position;
        public Quaternion rotation;
        public float time;
    }

    [Header("Identity")]
    [SerializeField] private Handedness m_handedness = Handedness.Right;
    [SerializeField] private Transform m_holdPoint;

    [Header("Close grab (grip)")]
    [SerializeField] private float m_closeGrabRadius = 0.16f;

    [Header("Force grab (trigger)")]
    [SerializeField] private float m_forceGrabCastRadius = 0.25f;
    [SerializeField] private float m_forceGrabRange = 15f;
    [SerializeField] private float m_flyToHandTime = 0.45f;
    [SerializeField] private float m_catchRadius = 0.22f;
    [SerializeField] private float m_flightTimeout = 1.5f;

    [Header("Aim debug")]
    [SerializeField] private bool m_debugLogging = true;

    [Header("Throw")]
    [SerializeField] private int m_poseSampleCount = 10;
    [SerializeField] private int m_fastestSamplesUsed = 3;
    [SerializeField] private float m_throwMultiplier = 1.15f;
    [SerializeField] private float m_maxThrowSpeed = 12f;

    [Header("Haptics")]
    [SerializeField] private float m_grabAmplitude = 0.3f;
    [SerializeField] private float m_grabDuration = 0.08f;
    [SerializeField] private float m_catchAmplitude = 0.6f;
    [SerializeField] private float m_catchDuration = 0.12f;
    [SerializeField] private float m_releaseAmplitude = 0.15f;
    [SerializeField] private float m_releaseDuration = 0.04f;

    private InputAction m_closeGrabAction;
    private InputAction m_forceGrabAction;

    private readonly List<PoseSample> m_poseSamples = new List<PoseSample>();
    private readonly Collider[] m_overlapBuffer = new Collider[16];
    private readonly RaycastHit[] m_castBuffer = new RaycastHit[16];

    private Grabbable m_held;
    private Grabbable m_incoming;
    private Grabbable m_hovered;
    private float m_incomingStartTime;

    public Grabbable Held
    {
        get { return m_held; }
    }

    /// <summary>
    /// The grabbable currently under the aim direction, or null. Read by HandPointer so the
    /// visible pointer and the actual grab can never disagree.
    /// </summary>
    public Grabbable Hovered
    {
        get { return m_hovered; }
    }

    public float PointerRange
    {
        get { return m_forceGrabRange; }
    }

    private Transform HoldTarget
    {
        get { return m_holdPoint != null ? m_holdPoint : transform; }
    }

    private void OnEnable()
    {
        string hand = m_handedness == Handedness.Left ? "LeftHand" : "RightHand";

        m_closeGrabAction = new InputAction(name + " CloseGrab", InputActionType.Button, "<XRController>{" + hand + "}/gripPressed");
        m_forceGrabAction = new InputAction(name + " ForceGrab", InputActionType.Button, "<XRController>{" + hand + "}/triggerPressed");

        m_closeGrabAction.Enable();
        m_forceGrabAction.Enable();
    }

    private void OnDisable()
    {
        if(m_closeGrabAction != null)
        {
            m_closeGrabAction.Disable();
            m_closeGrabAction.Dispose();
            m_closeGrabAction = null;
        }

        if(m_forceGrabAction != null)
        {
            m_forceGrabAction.Disable();
            m_forceGrabAction.Dispose();
            m_forceGrabAction = null;
        }
    }

    private void Update()
    {
        RecordPose();

        UpdateHover();

        bool closePressed = m_closeGrabAction != null && m_closeGrabAction.WasPressedThisFrame();
        bool forcePressed = m_forceGrabAction != null && m_forceGrabAction.WasPressedThisFrame();
        bool closeReleased = m_closeGrabAction != null && m_closeGrabAction.WasReleasedThisFrame();
        bool forceReleased = m_forceGrabAction != null && m_forceGrabAction.WasReleasedThisFrame();

        if(m_debugLogging && (closePressed || forcePressed))
        {
            Debug.Log(name + (closePressed ? " GRIP pressed (close grab)" : " TRIGGER pressed (force grab)"));
        }

        if(m_held == null)
        {
            if(closePressed)
            {
                TryCloseGrab();
            }
            else if(forcePressed)
            {
                TryForceGrab();
            }
        }
        else if(closeReleased || forceReleased)
        {
            ReleaseHeld();
        }
    }

    private void FixedUpdate()
    {
        if(m_held != null)
        {
            FollowHand(m_held.Body);
            return;
        }

        UpdateIncoming();
    }

    // ---------------------------------------------------------------- pose history

    private void RecordPose()
    {
        PoseSample sample;
        sample.position = transform.position;
        sample.rotation = transform.rotation;
        sample.time = Time.time;

        m_poseSamples.Add(sample);

        while(m_poseSamples.Count > m_poseSampleCount)
        {
            m_poseSamples.RemoveAt(0);
        }
    }

    // ---------------------------------------------------------------- grabbing

    private void TryCloseGrab()
    {
        int count = Physics.OverlapSphereNonAlloc(HoldTarget.position, m_closeGrabRadius, m_overlapBuffer, ~0, QueryTriggerInteraction.Ignore);
        Grabbable nearest = null;
        float nearestDistance = float.MaxValue;

        for(int i = 0; i < count; i++)
        {
            Grabbable candidate = m_overlapBuffer[i].GetComponentInParent<Grabbable>();

            if(candidate == null || candidate.IsHeld)
            {
                continue;
            }

            float distance = Vector3.Distance(HoldTarget.position, candidate.transform.position);

            if(distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = candidate;
            }
        }

        if(nearest == null)
        {
            return;
        }

        Attach(nearest, m_grabAmplitude, m_grabDuration);
    }

    /// <summary>
    /// Runs the aim cast every frame so the ray colour and the actual grab always agree:
    /// green means pressing the trigger right now would pull that ball.
    /// </summary>
    private void UpdateHover()
    {
        m_hovered = m_held != null ? null : FindAimTarget();

    }

    private Grabbable FindAimTarget()
    {
        int count = Physics.SphereCastNonAlloc(m_holdPoint.position, m_forceGrabCastRadius, m_holdPoint.forward,
            m_castBuffer, m_forceGrabRange, ~0, QueryTriggerInteraction.Ignore);

        Grabbable best = null;
        float bestDistance = float.MaxValue;

        for(int i = 0; i < count; i++)
        {
            Grabbable candidate = m_castBuffer[i].collider.GetComponentInParent<Grabbable>();

            if(candidate == null || candidate.IsHeld)
            {
                continue;
            }

            if(m_castBuffer[i].distance < bestDistance)
            {
                bestDistance = m_castBuffer[i].distance;
                best = candidate;
            }
        }

        return best;
    }

    private void TryForceGrab()
    {
        Grabbable target = m_hovered != null ? m_hovered : FindAimTarget();

        if(target == null)
        {
            return;
        }

        LaunchTowardsHand(target);
    }

    /// <summary>
    /// Solves the launch velocity that lands the ball at the hand after m_flyToHandTime under
    /// normal gravity, so the ball arcs to you instead of teleporting.
    /// </summary>
    private void LaunchTowardsHand(Grabbable grabbable)
    {
        Vector3 start = grabbable.transform.position;
        Vector3 target = HoldTarget.position;
        float time = Mathf.Max(0.05f, m_flyToHandTime);

        Vector3 velocity = ((target - start) - (0.5f * Physics.gravity * time * time)) / time;

        Rigidbody body = grabbable.Body;
        body.WakeUp();
        body.linearVelocity = velocity;
        body.angularVelocity = Vector3.zero;

        m_incoming = grabbable;
        m_incomingStartTime = Time.time;

        SendHaptics(m_grabAmplitude * 0.5f, m_grabDuration * 0.5f);
    }

    private void UpdateIncoming()
    {
        if(m_incoming == null)
        {
            return;
        }

        if(m_incoming.IsHeld)
        {
            m_incoming = null;
            return;
        }

        if(Time.time - m_incomingStartTime > m_flightTimeout)
        {
            m_incoming = null;
            return;
        }

        if(Vector3.Distance(m_incoming.transform.position, HoldTarget.position) <= m_catchRadius)
        {
            Grabbable caught = m_incoming;
            m_incoming = null;
            Attach(caught, m_catchAmplitude, m_catchDuration);
        }
    }

    private void Attach(Grabbable grabbable, float hapticAmplitude, float hapticDuration)
    {
        if(!grabbable.TryGrab(this))
        {
            return;
        }

        m_held = grabbable;
        m_incoming = null;
        SendHaptics(hapticAmplitude, hapticDuration);
    }

    // ---------------------------------------------------------------- holding and release

    /// <summary>
    /// Drives the ball with velocity rather than parenting it, so it keeps colliding with the
    /// rim, backboard and floor while in hand.
    /// </summary>
    private void FollowHand(Rigidbody body)
    {
        Vector3 positionDelta = HoldTarget.position - body.position;
        body.linearVelocity = positionDelta / Time.fixedDeltaTime;

        Quaternion rotationDelta = HoldTarget.rotation * Quaternion.Inverse(body.rotation);
        float angle;
        Vector3 axis;
        rotationDelta.ToAngleAxis(out angle, out axis);

        if(float.IsInfinity(axis.x) || float.IsNaN(axis.x))
        {
            return;
        }

        if(angle > 180f)
        {
            angle -= 360f;
        }

        body.angularVelocity = axis.normalized * (angle * Mathf.Deg2Rad / Time.fixedDeltaTime);
    }

    private void ReleaseHeld()
    {
        Grabbable released = m_held;
        m_held = null;

        Vector3 angularVelocity;
        Vector3 linearVelocity = ComputeReleaseVelocity(out angularVelocity);

        // Wrist flick: a spinning hand adds tangential speed at the ball's offset from the hand.
        Vector3 lever = released.transform.position - HoldTarget.position;
        linearVelocity += Vector3.Cross(angularVelocity, lever);

        linearVelocity *= m_throwMultiplier;

        if(linearVelocity.magnitude > m_maxThrowSpeed)
        {
            linearVelocity = linearVelocity.normalized * m_maxThrowSpeed;
        }

        released.Release(this, linearVelocity, angularVelocity);
        SendHaptics(m_releaseAmplitude, m_releaseDuration);
    }

    /// <summary>
    /// Averages the fastest few samples in the pose history instead of using the last frame's
    /// delta, which is noisy and almost always reads slower than the real throw.
    /// </summary>
    private Vector3 ComputeReleaseVelocity(out Vector3 angularVelocity)
    {
        angularVelocity = Vector3.zero;

        if(m_poseSamples.Count < 2)
        {
            return Vector3.zero;
        }

        List<Vector3> velocities = new List<Vector3>();

        for(int i = 1; i < m_poseSamples.Count; i++)
        {
            float deltaTime = m_poseSamples[i].time - m_poseSamples[i - 1].time;

            if(deltaTime <= 0.0001f)
            {
                continue;
            }

            velocities.Add((m_poseSamples[i].position - m_poseSamples[i - 1].position) / deltaTime);
        }

        if(velocities.Count == 0)
        {
            return Vector3.zero;
        }

        velocities.Sort(CompareBySpeedDescending);

        int used = Mathf.Clamp(m_fastestSamplesUsed, 1, velocities.Count);
        Vector3 sum = Vector3.zero;

        for(int i = 0; i < used; i++)
        {
            sum += velocities[i];
        }

        angularVelocity = ComputeAngularVelocity();
        return sum / used;
    }

    private static int CompareBySpeedDescending(Vector3 a, Vector3 b)
    {
        return b.sqrMagnitude.CompareTo(a.sqrMagnitude);
    }

    private Vector3 ComputeAngularVelocity()
    {
        int last = m_poseSamples.Count - 1;

        if(last < 1)
        {
            return Vector3.zero;
        }

        float deltaTime = m_poseSamples[last].time - m_poseSamples[last - 1].time;

        if(deltaTime <= 0.0001f)
        {
            return Vector3.zero;
        }

        Quaternion delta = m_poseSamples[last].rotation * Quaternion.Inverse(m_poseSamples[last - 1].rotation);
        float angle;
        Vector3 axis;
        delta.ToAngleAxis(out angle, out axis);

        if(float.IsInfinity(axis.x) || float.IsNaN(axis.x))
        {
            return Vector3.zero;
        }

        if(angle > 180f)
        {
            angle -= 360f;
        }

        return axis.normalized * (angle * Mathf.Deg2Rad / deltaTime);
    }

    // ---------------------------------------------------------------- haptics

    private void SendHaptics(float amplitude, float duration)
    {
        if(amplitude <= 0f || duration <= 0f)
        {
            return;
        }

        XRNode node = m_handedness == Handedness.Left ? XRNode.LeftHand : XRNode.RightHand;
        UnityEngine.XR.InputDevice device = InputDevices.GetDeviceAtXRNode(node);

        if(!device.isValid)
        {
            return;
        }

        HapticCapabilities capabilities;

        if(!device.TryGetHapticCapabilities(out capabilities) || !capabilities.supportsImpulse)
        {
            return;
        }

        device.SendHapticImpulse(0, Mathf.Clamp01(amplitude), duration);
    }

    private void OnDrawGizmosSelected()
    {
        Transform target = HoldTarget;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(target.position, m_closeGrabRadius);

        Gizmos.color = Color.magenta;
        Gizmos.DrawRay(m_holdPoint.position, m_holdPoint.forward * m_forceGrabRange);
    }
}
