using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Builds the playable court scene from primitives: floor, backboard, rim and support.
/// Menu: Tools/Basketball/Build Court Scene
///
/// Layout convention: the player stands at the world origin facing +Z, the hoop is at +Z.
/// The rim is built from capsule colliders rather than a mesh collider so the ball gets
/// clean, cheap bounces off it.
/// </summary>
public static class CourtBuilder
{
    private const string k_scenePath = "Assets/Scenes/Court.unity";

    private const float k_rimHeight = 3.05f;
    private const float k_rimRadius = 0.275f;
    private const float k_rimTubeRadius = 0.02f;
    private const int k_rimSegments = 16;
    private const float k_rimCentreZ = 4.0f;

    private const float k_backboardWidth = 1.83f;
    private const float k_backboardHeight = 1.07f;
    private const float k_backboardThickness = 0.05f;
    private const float k_backboardBottom = 2.90f;
    private const float k_rimArmLength = 0.15f;

    private const float k_poleZ = 5.4f;
    private const float k_poleHeight = 3.6f;

    [MenuItem("Tools/Basketball/Build Court Scene")]
    public static void BuildCourtScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateLight();
        CreateFloor();
        CreateHoop();

        XRRigBuilder.BuildRig();

        EnsureFolder("Assets", "Scenes");
        EditorSceneManager.SaveScene(scene, k_scenePath);
        AddSceneToBuildSettings();

        Debug.Log("[CourtBuilder] Court scene built and saved to " + k_scenePath);
    }

    private static void CreateLight()
    {
        GameObject lightObject = new GameObject("Directional Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.1f;
        light.shadows = LightShadows.Soft;
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    }

    private static void CreateFloor()
    {
        GameObject floor = CreateBox("Floor", null, new Vector3(0f, -0.1f, 4f), new Vector3(20f, 0.2f, 20f), new Color(0.55f, 0.36f, 0.20f));
        GameObjectUtility.SetStaticEditorFlags(floor, StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI);
    }

    private static void CreateHoop()
    {
        GameObject hoop = new GameObject("Hoop");

        float backboardFaceZ = k_rimCentreZ + k_rimRadius + k_rimArmLength;
        float backboardCentreZ = backboardFaceZ + (k_backboardThickness * 0.5f);
        float backboardCentreY = k_backboardBottom + (k_backboardHeight * 0.5f);

        CreateBox("Backboard", hoop.transform,
            new Vector3(0f, backboardCentreY, backboardCentreZ),
            new Vector3(k_backboardWidth, k_backboardHeight, k_backboardThickness),
            new Color(0.92f, 0.92f, 0.92f));

        CreateBox("BackboardInnerSquare", hoop.transform,
            new Vector3(0f, k_rimHeight + 0.30f, backboardFaceZ - 0.005f),
            new Vector3(0.59f, 0.45f, 0.01f),
            new Color(0.85f, 0.25f, 0.12f));

        GameObject pole = CreateCylinder("Pole", hoop.transform,
            new Vector3(0f, k_poleHeight * 0.5f, k_poleZ),
            new Vector3(0.12f, k_poleHeight * 0.5f, 0.12f),
            new Color(0.22f, 0.22f, 0.24f));
        GameObjectUtility.SetStaticEditorFlags(pole, StaticEditorFlags.BatchingStatic);

        float armCentreZ = (backboardCentreZ + (k_backboardThickness * 0.5f) + k_poleZ) * 0.5f;
        float armLength = k_poleZ - backboardCentreZ - (k_backboardThickness * 0.5f);

        CreateBox("PoleArm", hoop.transform,
            new Vector3(0f, 3.5f, armCentreZ),
            new Vector3(0.12f, 0.12f, armLength),
            new Color(0.22f, 0.22f, 0.24f));

        CreateBox("RimArm", hoop.transform,
            new Vector3(0f, k_rimHeight, backboardFaceZ - (k_rimArmLength * 0.5f)),
            new Vector3(0.08f, 0.04f, k_rimArmLength),
            new Color(0.90f, 0.35f, 0.05f));

        CreateRim(hoop.transform);
    }

    private static void CreateRim(Transform parent)
    {
        GameObject rim = new GameObject("Rim");
        rim.transform.SetParent(parent, false);
        rim.transform.localPosition = new Vector3(0f, k_rimHeight, k_rimCentreZ);

        Material rimMaterial = GetOrCreateMaterial("Rim", new Color(0.90f, 0.35f, 0.05f));
        float chord = 2f * k_rimRadius * Mathf.Sin(Mathf.PI / k_rimSegments);

        for(int i = 0; i < k_rimSegments; i++)
        {
            float angle = (Mathf.PI * 2f * i) / k_rimSegments;
            Vector3 offset = new Vector3(Mathf.Cos(angle) * k_rimRadius, 0f, Mathf.Sin(angle) * k_rimRadius);
            Vector3 tangent = new Vector3(-Mathf.Sin(angle), 0f, Mathf.Cos(angle));

            GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            segment.name = "RimSegment_" + i;
            segment.transform.SetParent(rim.transform, false);
            segment.transform.localPosition = offset;
            segment.transform.localRotation = Quaternion.FromToRotation(Vector3.up, tangent);
            segment.transform.localScale = new Vector3(k_rimTubeRadius * 2f, chord * 0.5f, k_rimTubeRadius * 2f);
            segment.GetComponent<MeshRenderer>().sharedMaterial = rimMaterial;
        }
    }

    private static GameObject CreateBox(string name, Transform parent, Vector3 position, Vector3 size, Color colour)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;

        if(parent != null)
        {
            box.transform.SetParent(parent, false);
        }

        box.transform.localPosition = position;
        box.transform.localScale = size;
        box.GetComponent<MeshRenderer>().sharedMaterial = GetOrCreateMaterial(name, colour);
        return box;
    }

    private static GameObject CreateCylinder(string name, Transform parent, Vector3 position, Vector3 scale, Color colour)
    {
        GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cylinder.name = name;

        if(parent != null)
        {
            cylinder.transform.SetParent(parent, false);
        }

        cylinder.transform.localPosition = position;
        cylinder.transform.localScale = scale;
        cylinder.GetComponent<MeshRenderer>().sharedMaterial = GetOrCreateMaterial(name, colour);
        return cylinder;
    }

    private static Material GetOrCreateMaterial(string name, Color colour)
    {
        EnsureFolder("Assets", "Art");
        EnsureFolder("Assets/Art", "Materials");

        string path = "Assets/Art/Materials/" + name + ".mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);

        if(existing != null)
        {
            return existing;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");

        if(shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = new Material(shader);
        material.color = colour;
        material.SetFloat("_Smoothness", 0.15f);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void EnsureFolder(string parent, string folder)
    {
        if(!AssetDatabase.IsValidFolder(parent + "/" + folder))
        {
            AssetDatabase.CreateFolder(parent, folder);
        }
    }

    private static void AddSceneToBuildSettings()
    {
        EditorBuildSettingsScene[] current = EditorBuildSettings.scenes;

        for(int i = 0; i < current.Length; i++)
        {
            if(current[i].path == k_scenePath)
            {
                return;
            }
        }

        EditorBuildSettingsScene[] updated = new EditorBuildSettingsScene[current.Length + 1];
        updated[0] = new EditorBuildSettingsScene(k_scenePath, true);

        for(int i = 0; i < current.Length; i++)
        {
            updated[i + 1] = current[i];
        }

        EditorBuildSettings.scenes = updated;
    }
}
