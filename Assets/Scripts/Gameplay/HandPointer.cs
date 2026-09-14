using UnityEngine;

/// <summary>
/// In-headset aim pointer for force grab. Lives as a CHILD of the hand with local-space points,
/// so it inherits the same late-latched controller pose the hand is rendered with - that is why
/// it stays glued to the hand while a Debug.DrawRay computed in Update visibly lags behind it.
///
/// Colour is driven off HandController.Hovered, so the pointer can never promise a grab that
/// would not happen.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class HandPointer : MonoBehaviour
{
    [SerializeField] private HandController m_hand;

    [Header("Appearance")]
    [SerializeField] private float m_idleLength = 2.5f;
    [SerializeField] private float m_width = 0.006f;
    [SerializeField] private Color m_idleColour = new Color(1f, 1f, 1f, 0.25f);
    [SerializeField] private Color m_hoverColour = new Color(0.3f, 1f, 0.4f, 0.9f);

    [Header("Behaviour")]
    [SerializeField] private bool m_hideWhileHolding = true;
    [SerializeField] private bool m_stopAtTarget = true;

    private LineRenderer m_line;
    private Material m_material;

    private void Awake()
    {
        m_line = GetComponent<LineRenderer>();
        m_material = m_line.sharedMaterial;

        if(m_hand == null)
        {
            m_hand = GetComponentInParent<HandController>();
        }
        m_line.useWorldSpace = false;
        m_line.positionCount = 2;
        m_line.startWidth = m_width;
        m_line.endWidth = m_width;
        m_line.numCapVertices = 2;
        m_line.alignment = LineAlignment.View;
        m_line.textureMode = LineTextureMode.Stretch;
        m_line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        m_line.receiveShadows = false;
     }

    private void LateUpdate()
    {
        if(m_hand == null)
        {
            m_line.enabled = false;
            return;
        }

        bool holding = m_hand.Held != null;

        if(holding && m_hideWhileHolding)
        {
            m_line.enabled = false;
            return;
        }

        m_line.enabled = true;

        Grabbable hovered = m_hand.Hovered;
        float length = m_idleLength;

        if(hovered != null && m_stopAtTarget)
        {
            // Local Z distance to the target, so the line ends at the ball rather than through it.
            length = transform.InverseTransformPoint(hovered.transform.position).z;
            length = Mathf.Clamp(length, 0.05f, m_hand.PointerRange);
        }

        m_line.SetPosition(0, Vector3.zero);
        m_line.SetPosition(1, new Vector3(0f, 0f, length));

        Color colour = hovered != null ? m_hoverColour : m_idleColour;
        m_line.startColor = colour;
        m_line.endColor = colour;

        if(m_material != null)
        {
            m_material.color = colour;
        }
    }
}
