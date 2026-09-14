using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// Forces the XR tracking origin to Floor, so y = 0 in the scene is the player's real floor
/// and the camera reports true head height. Retries for a few seconds because the input
/// subsystem is not guaranteed to be running on the first frame on device.
/// </summary>
public class XRRigSetup : MonoBehaviour
{
    [SerializeField] private float m_timeoutSeconds = 5f;
    [SerializeField] private bool m_logResult = true;

    private readonly List<XRInputSubsystem> m_subsystems = new List<XRInputSubsystem>();

    private void Start()
    {
        StartCoroutine(ApplyFloorOrigin());
    }

    private IEnumerator ApplyFloorOrigin()
    {
        float deadline = Time.realtimeSinceStartup + m_timeoutSeconds;

        while(Time.realtimeSinceStartup < deadline)
        {
            SubsystemManager.GetSubsystems(m_subsystems);

            for(int i = 0; i < m_subsystems.Count; i++)
            {
                XRInputSubsystem subsystem = m_subsystems[i];

                if(subsystem == null || !subsystem.running)
                {
                    continue;
                }

                if(subsystem.TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor))
                {
                    if(m_logResult)
                    {
                        Debug.Log("[XRRigSetup] Tracking origin set to Floor.");
                    }

                    yield break;
                }
            }

            yield return null;
        }

        Debug.LogWarning("[XRRigSetup] Could not set a Floor tracking origin within " + m_timeoutSeconds + "s. Height will be runtime-provided.");
    }
}
