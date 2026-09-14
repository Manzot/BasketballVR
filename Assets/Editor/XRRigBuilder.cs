using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;

/// <summary>
/// Builds the VR rig from scratch - camera plus two tracked hands - using only the Input
/// System's TrackedPoseDriver. No XR Interaction Toolkit, no vendor interaction SDK.
/// Menu: Tools/Basketball/Build XR Rig
/// </summary>
public static class XRRigBuilder
{
    private const string k_rigName = "XRRig";
    private const float k_handCubeSize = 0.05f;

    [MenuItem("Tools/Basketball/Build XR Rig")]
    public static void BuildRig()
    {
        GameObject existing = GameObject.Find(k_rigName);

        if(existing != null)
        {
            Debug.LogWarning("[XRRigBuilder] An XRRig already exists in the scene. Delete it before rebuilding.");
            Selection.activeGameObject = existing;
            return;
        }

        GameObject rig = new GameObject(k_rigName);
        rig.transform.position = Vector3.zero;
        rig.transform.rotation = Quaternion.identity;
        rig.AddComponent<XRRigSetup>();

        Camera camera = Camera.main;

        if(camera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
        }

        camera.transform.SetParent(rig.transform, false);
        camera.transform.localPosition = Vector3.zero;
        camera.transform.localRotation = Quaternion.identity;
        camera.nearClipPlane = 0.01f;
        camera.farClipPlane = 200f;

        AddTrackedPoseDriver(camera.gameObject, "<XRHMD>/centerEyePosition", "<XRHMD>/centerEyeRotation");

        CreateHand(rig.transform, "LeftHand");
        CreateHand(rig.transform, "RightHand");

        Undo.RegisterCreatedObjectUndo(rig, "Build XR Rig");
        Selection.activeGameObject = rig;

        Debug.Log("[XRRigBuilder] XRRig built. Save the scene, then Build And Run.");
    }

    private static void CreateHand(Transform parent, string handName)
    {
        GameObject hand = new GameObject(handName);
        hand.transform.SetParent(parent, false);

        AddTrackedPoseDriver(hand,
            "<XRController>{" + handName + "}/devicePosition",
            "<XRController>{" + handName + "}/deviceRotation");

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = "Visual";
        visual.transform.SetParent(hand.transform, false);
        visual.transform.localScale = Vector3.one * k_handCubeSize;

        Collider visualCollider = visual.GetComponent<Collider>();

        if(visualCollider != null)
        {
            Object.DestroyImmediate(visualCollider);
        }
    }

    private static void AddTrackedPoseDriver(GameObject target, string positionBinding, string rotationBinding)
    {
        TrackedPoseDriver driver = target.AddComponent<TrackedPoseDriver>();
        driver.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
        driver.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;

        InputAction positionAction = new InputAction(target.name + " Position", InputActionType.Value, positionBinding, null, null, "Vector3");
        InputAction rotationAction = new InputAction(target.name + " Rotation", InputActionType.Value, rotationBinding, null, null, "Quaternion");

        driver.positionInput = new InputActionProperty(positionAction);
        driver.rotationInput = new InputActionProperty(rotationAction);
    }
}
