using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.XR.CoreUtils;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Rebuilds Assets/Scenes/office.unity from scratch: the room, the three Key
/// Props, the three Locks, every signifier and the escape logic.
/// Re-runnable - it clears the scene first - so the layout lives in source
/// control as code rather than as hand-placed objects nobody can review.
/// </summary>
public static class OfficeEscapeRoomBuilder
{
    private const string ScenePath = "Assets/Scenes/office.unity";
    private const string PropDir = "Assets/Asset/office/vicevoxel/Ultimate Office Props-URP/Prefabs/";
    private const string StarterAssetDir = "Assets/Samples/XR Interaction Toolkit/3.6.0/Starter Assets/Prefabs/";
    private const string MaterialDir = "Assets/Asset/office/EscapeRoomMaterials";

    private static readonly Color AmberGlow = new Color(1f, 0.78f, 0.18f, 1f);
    private static readonly Color GhostBlue = new Color(0.32f, 0.88f, 1f, 0.52f);
    private static readonly Color LockedRed = new Color(0.95f, 0.15f, 0.12f, 1f);
    private static readonly Color SolvedGreen = new Color(0.20f, 1f, 0.35f, 1f);

    private static Material floorMaterial;
    private static Material wallMaterial;
    private static Material ceilingMaterial;
    private static Material trimMaterial;
    private static Material doorMaterial;
    private static Material panelMaterial;
    private static Material statusMaterial;
    private static Material metalMaterial;
    private static Material ghostMaterial;

    [MenuItem("Tools/CS417 Group 5/Build Office Escape Room")]
    public static void Build()
    {
        CreateMaterials();
        RaiseAdditionalLightLimit();

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Object.DestroyImmediate(root);
        }

        GameObject world = new GameObject("OFFICE_ESCAPE_ROOM");

        BuildLighting(world.transform);
        BuildArchitecture(world.transform);
        GameObject doorPanel = BuildExitDoor(world.transform);
        BuildDressing(world.transform);

        XRGrabInteractable mug = BuildKey(world.transform, "VV_cup02a", "Key_Mug", new Vector3(-0.55f, 1.02f, 1.55f), 15f, 0.35f);
        XRGrabInteractable file = BuildKey(world.transform, "VV_file", "Key_File", new Vector3(0.02f, 1.02f, 1.62f), -20f, 0.6f);
        XRGrabInteractable handset = BuildKey(world.transform, "VV_CordlessPhone", "Key_Handset", new Vector3(0.58f, 1.02f, 1.55f), 8f, 0.35f);

        GameObject exitPanel = BuildExitPanel(world.transform);
        EasedStateChange mugSlot = exitPanel.transform.Find("Slot_Mug").GetComponent<EasedStateChange>();
        EasedStateChange fileSlot = exitPanel.transform.Find("Slot_Files").GetComponent<EasedStateChange>();
        EasedStateChange phoneSlot = exitPanel.transform.Find("Slot_Phone").GetComponent<EasedStateChange>();

        Transform locks = new GameObject("Locks").transform;
        locks.SetParent(world.transform, false);

        LockReceptacle coffeeLock = BuildCoffeeStation(locks, mug, mugSlot);
        LockReceptacle fileLock = BuildFileStation(locks, file, fileSlot);
        LockReceptacle phoneLock = BuildPhoneStation(locks, handset, phoneSlot);

        BuildTeachingBoard(world.transform);

        GameObject rig = BuildPlayer(world.transform);
        GameObject escaped = BuildEscapedBanner(world.transform);

        EscapeController controller = new GameObject("Escape Controller").AddComponent<EscapeController>();
        controller.transform.SetParent(world.transform, false);
        controller.locks = new[] { coffeeLock, fileLock, phoneLock };
        controller.doorOpens = doorPanel.GetComponent<EasedStateChange>();
        controller.unlockStateChanges = new[]
        {
            world.transform.Find("Exit_Signifiers/Exit_Panel/Door_Frame_Glow").GetComponent<EasedStateChange>(),
        };
        controller.progressText = exitPanel.transform.Find("Progress_Label/Text").GetComponent<Text>();
        controller.doorStatusText = exitPanel.transform.Find("Status_Label/Text").GetComponent<Text>();
        controller.escapedText = escaped.GetComponentInChildren<Text>();

        EscapeExitTrigger exitTrigger = world.transform.Find("Architecture/Escape_Landing/Exit_Trigger")
            .GetComponent<EscapeExitTrigger>();
        exitTrigger.escapeController = controller;
        exitTrigger.playerRoot = rig.transform;

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.22f, 0.22f, 0.26f, 1f);
        RenderSettings.fog = false;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings();

        Debug.Log("Office escape room rebuilt: 3 Key Props, 3 Locks, eased signifiers, escape trigger.");
    }

    // ------------------------------------------------------------------ rooms

    private static void BuildLighting(Transform parent)
    {
        Transform lighting = new GameObject("Lighting").transform;
        lighting.SetParent(parent, false);

        GameObject sun = new GameObject("Directional Light");
        sun.transform.SetParent(lighting, false);
        sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        Light sunLight = sun.AddComponent<Light>();
        sunLight.type = LightType.Directional;
        sunLight.intensity = 0.35f;
        sunLight.color = new Color(0.75f, 0.80f, 1f);
        sunLight.shadows = LightShadows.None;

        Vector3[] spots =
        {
            new Vector3(-2.1f, 2.75f, -2.1f),
            new Vector3(2.1f, 2.75f, -2.1f),
            new Vector3(-2.1f, 2.75f, 2.1f),
            new Vector3(2.1f, 2.75f, 2.1f),
        };

        for (int i = 0; i < spots.Length; i++)
        {
            GameObject fixtureRoot = new GameObject("Ceiling_Light_" + (i + 1));
            fixtureRoot.transform.SetParent(lighting, false);
            fixtureRoot.transform.localPosition = spots[i];

            Light lamp = fixtureRoot.AddComponent<Light>();
            lamp.type = LightType.Point;
            lamp.range = 9f;
            lamp.intensity = 1.5f;
            lamp.color = new Color(1f, 0.96f, 0.90f);
            lamp.shadows = LightShadows.None;

            GameObject panel = Box("Panel", fixtureRoot.transform, new Vector3(0f, 0.28f, 0f), new Vector3(1.2f, 0.04f, 0.5f), ceilingMaterial);
            Material glow = new Material(ceilingMaterial);
            glow.EnableKeyword("_EMISSION");
            glow.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            glow.SetColor("_EmissionColor", new Color(1f, 0.97f, 0.9f) * 1.6f);
            AssetDatabase.CreateAsset(glow, MaterialDir + "/M_CeilingGlow_" + (i + 1) + ".mat");
            panel.GetComponent<Renderer>().sharedMaterial = glow;
        }
    }

    private static void BuildArchitecture(Transform parent)
    {
        Transform arch = new GameObject("Architecture").transform;
        arch.SetParent(parent, false);

        // A thick slab, not a thin plate: a CharacterController that starts a frame
        // interpenetrating a 0.2 m floor can be depenetrated straight through it.
        Box("Floor", arch, new Vector3(0f, -0.5f, 0f), new Vector3(8.4f, 1f, 8.4f), floorMaterial);
        Box("Ceiling", arch, new Vector3(0f, 3.1f, 0f), new Vector3(8.4f, 0.2f, 8.4f), ceilingMaterial);
        Box("Wall_West", arch, new Vector3(-4.1f, 1.5f, 0f), new Vector3(0.2f, 3f, 8.4f), wallMaterial);
        Box("Wall_East", arch, new Vector3(4.1f, 1.5f, 0f), new Vector3(0.2f, 3f, 8.4f), wallMaterial);
        Box("Wall_North", arch, new Vector3(0f, 1.5f, -4.1f), new Vector3(8f, 3f, 0.2f), wallMaterial);

        // The exit wall is split so a 1.5 x 2.3 doorway is left open at x = 0.
        Box("Wall_South_Left", arch, new Vector3(-2.4f, 1.5f, 4.1f), new Vector3(3.3f, 3f, 0.2f), wallMaterial);
        Box("Wall_South_Right", arch, new Vector3(2.4f, 1.5f, 4.1f), new Vector3(3.3f, 3f, 0.2f), wallMaterial);
        Box("Wall_South_Header", arch, new Vector3(0f, 2.65f, 4.1f), new Vector3(1.5f, 0.7f, 0.2f), wallMaterial);

        // Bright trim around the only opening in the room: the everyday "this is the way out".
        Box("Door_Trim_Left", arch, new Vector3(-0.83f, 1.15f, 3.97f), new Vector3(0.16f, 2.4f, 0.06f), trimMaterial);
        Box("Door_Trim_Right", arch, new Vector3(0.83f, 1.15f, 3.97f), new Vector3(0.16f, 2.4f, 0.06f), trimMaterial);
        Box("Door_Trim_Top", arch, new Vector3(0f, 2.37f, 3.97f), new Vector3(1.82f, 0.16f, 0.06f), trimMaterial);

        Transform landing = new GameObject("Escape_Landing").transform;
        landing.SetParent(arch, false);
        Box("Landing_Floor", landing, new Vector3(0f, -0.5f, 6.1f), new Vector3(4.4f, 1f, 4.2f), floorMaterial);
        Box("Landing_Ceiling", landing, new Vector3(0f, 3.1f, 6.1f), new Vector3(4.4f, 0.2f, 4.2f), ceilingMaterial);
        Box("Landing_Wall_West", landing, new Vector3(-2.1f, 1.5f, 6.1f), new Vector3(0.2f, 3f, 4.2f), wallMaterial);
        Box("Landing_Wall_East", landing, new Vector3(2.1f, 1.5f, 6.1f), new Vector3(0.2f, 3f, 4.2f), wallMaterial);
        Box("Landing_Wall_Far", landing, new Vector3(0f, 1.5f, 8.1f), new Vector3(4.4f, 3f, 0.2f), wallMaterial);

        GameObject landingLightGo = new GameObject("Landing_Light");
        landingLightGo.transform.SetParent(landing, false);
        landingLightGo.transform.localPosition = new Vector3(0f, 2.7f, 6.1f);
        Light landingLight = landingLightGo.AddComponent<Light>();
        landingLight.type = LightType.Point;
        landingLight.range = 8f;
        landingLight.intensity = 1.4f;
        landingLight.color = new Color(0.85f, 0.95f, 1f);
        landingLight.shadows = LightShadows.None;

        GameObject trigger = new GameObject("Exit_Trigger");
        trigger.transform.SetParent(landing, false);
        trigger.transform.localPosition = new Vector3(0f, 1.1f, 5.1f);
        BoxCollider triggerCollider = trigger.AddComponent<BoxCollider>();
        triggerCollider.isTrigger = true;
        triggerCollider.size = new Vector3(1.6f, 2.2f, 0.8f);
        trigger.AddComponent<EscapeExitTrigger>();
    }

    private static GameObject BuildExitDoor(Transform parent)
    {
        GameObject pivot = new GameObject("Exit_Door");
        pivot.transform.SetParent(parent, false);
        pivot.transform.localPosition = new Vector3(0f, 1.15f, 4.1f);

        GameObject panel = Box("Door_Panel", pivot.transform, Vector3.zero, new Vector3(1.5f, 2.3f, 0.14f), doorMaterial);

        // Slides sideways and clears the wall: eased in and out, never snapping.
        EasedStateChange slide = panel.AddComponent<EasedStateChange>();
        slide.duration = 1.8f;
        slide.delay = 0.35f;
        slide.movesLocally = true;
        slide.targetLocalPosition = new Vector3(-1.62f, 0f, 0.2f);

        return panel;
    }

    private static void BuildDressing(Transform parent)
    {
        Transform dressing = new GameObject("Office_Dressing").transform;
        dressing.SetParent(parent, false);

        Prop("VV_Desk03", dressing, new Vector3(0f, 0f, 1.4f), 90f, "Key_Desk");
        Prop("VV_Chair2", dressing, new Vector3(0f, 0f, 0.35f), 0f, "Chair_Center");
        Prop("VV_flowerpot_001", dressing, new Vector3(-3.4f, 0f, 3.2f), 0f, "Plant_Corner");
        Prop("VV_Wastepaper_Basket", dressing, new Vector3(3.4f, 0f, -1.6f), 0f, "Bin");
        Prop("VV_LoungeCouch", dressing, new Vector3(3.2f, 0f, 3.0f), 200f, "Couch");
        Prop("VV_filing_basket", dressing, new Vector3(-3.5f, 0f, -2.6f), 40f, "Basket");
        Prop("VV_Fan01", dressing, new Vector3(-3.6f, 0f, -3.4f), 30f, "Fan");
    }

    // ------------------------------------------------------------------- keys

    private static XRGrabInteractable BuildKey(Transform parent, string prefabName, string name, Vector3 position, float yaw, float mass)
    {
        Transform keys = parent.Find("Key_Props");
        if (keys == null)
        {
            keys = new GameObject("Key_Props").transform;
            keys.SetParent(parent, false);
        }

        GameObject key = Prop(prefabName, keys, position, yaw, name);

        Rigidbody body = key.AddComponent<Rigidbody>();
        body.mass = mass;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        XRGrabInteractable grab = key.AddComponent<XRGrabInteractable>();
        grab.useDynamicAttach = true;
        grab.throwOnDetach = false;
        grab.attachEaseInTime = 0.1f;

        GrabSignifier signifier = key.AddComponent<GrabSignifier>();
        signifier.glowColor = AmberGlow;

        GameObject haloGo = new GameObject("Grab_Halo");
        haloGo.transform.SetParent(key.transform, false);
        haloGo.transform.localPosition = new Vector3(0f, 0.12f, 0f);
        Light halo = haloGo.AddComponent<Light>();
        halo.type = LightType.Point;
        halo.range = 0.55f;
        halo.intensity = 0.5f;
        halo.color = AmberGlow;
        halo.shadows = LightShadows.None;
        signifier.halo = halo;

        return grab;
    }

    // ------------------------------------------------------------------ locks

    private static LockReceptacle BuildCoffeeStation(Transform parent, XRGrabInteractable mug, EasedStateChange exitSlot)
    {
        Transform station = NewStation(parent, "Station_Coffee");

        Prop("VV_vendingmachine01", station, new Vector3(-3.95f, 0f, 0.8f), 90f, "Coffee_Machine");
        Box("Drip_Tray", station, new Vector3(-2.80f, 0.74f, 0.8f), new Vector3(0.44f, 0.04f, 0.44f), metalMaterial);
        Box("Drip_Tray_Lip", station, new Vector3(-2.60f, 0.78f, 0.8f), new Vector3(0.04f, 0.09f, 0.44f), metalMaterial);

        EasedStateChange bezel = StatusBezel(station, "Bezel_Coffee", new Vector3(-2.85f, 1.55f, 0.8f), new Vector3(0.05f, 0.12f, 0.34f));
        EasedStateChange lamp = StatusLamp(station, "Status_Light_Coffee", new Vector3(-2.65f, 1.9f, 0.8f));

        Label("Station_Label_Coffee", station, new Vector3(-2.80f, 1.95f, 0.8f), new Vector3(0f, -90f, 0f),
            new Vector2(520f, 120f), 0.0016f, "MUG RETURN", 54, AmberGlow);

        return Lock(station, "Lock_Coffee", "MUG", mug, "VV_cup02a",
            new Vector3(-2.80f, 0.78f, 0.8f), 15f, 0.2f, bezel, lamp, exitSlot);
    }

    private static LockReceptacle BuildFileStation(Transform parent, XRGrabInteractable file, EasedStateChange exitSlot)
    {
        Transform station = NewStation(parent, "Station_Files");

        Prop("VV_file_cabinet", station, new Vector3(3.6f, 0f, 0.8f), -90f, "File_Cabinet");
        Box("Open_Drawer_Base", station, new Vector3(3.00f, 0.88f, 0.8f), new Vector3(0.48f, 0.03f, 0.86f), metalMaterial);
        Box("Open_Drawer_Side_A", station, new Vector3(3.00f, 0.95f, 1.21f), new Vector3(0.48f, 0.16f, 0.04f), metalMaterial);
        Box("Open_Drawer_Side_B", station, new Vector3(3.00f, 0.95f, 0.39f), new Vector3(0.48f, 0.16f, 0.04f), metalMaterial);
        Box("Open_Drawer_Front", station, new Vector3(2.77f, 0.97f, 0.8f), new Vector3(0.04f, 0.22f, 0.86f), metalMaterial);

        EasedStateChange bezel = StatusBezel(station, "Bezel_Files", new Vector3(3.21f, 1.34f, 0.8f), new Vector3(0.05f, 0.10f, 0.32f));
        EasedStateChange lamp = StatusLamp(station, "Status_Light_Files", new Vector3(2.95f, 1.8f, 0.8f));

        Label("Station_Label_Files", station, new Vector3(3.18f, 1.72f, 0.8f), new Vector3(0f, 90f, 0f),
            new Vector2(520f, 120f), 0.0016f, "FILE DRAWER", 54, AmberGlow);

        return Lock(station, "Lock_Files", "FILES", file, "VV_file",
            new Vector3(3.02f, 0.94f, 0.8f), -90f, 0.24f, bezel, lamp, exitSlot);
    }

    private static LockReceptacle BuildPhoneStation(Transform parent, XRGrabInteractable handset, EasedStateChange exitSlot)
    {
        Transform station = NewStation(parent, "Station_Phone");

        Prop("VV_OfficeDesk_01", station, new Vector3(-1.55f, 0f, -2.15f), 0f, "Phone_Desk");
        Prop("VV_CordlessPhone_Base", station, new Vector3(0.35f, 1.1f, -2.95f), 0f, "Phone_Cradle");
        Prop("VV_Display", station, new Vector3(-0.75f, 1.1f, -3.15f), 0f, "Monitor");
        Prop("VV_Keyboard", station, new Vector3(-0.75f, 1.1f, -2.62f), 0f, "Keyboard");
        Prop("VV_desklamp01a", station, new Vector3(1.05f, 1.1f, -3.1f), -25f, "Desk_Lamp");

        EasedStateChange bezel = StatusBezel(station, "Bezel_Phone", new Vector3(0.35f, 1.72f, -3.94f), new Vector3(0.32f, 0.10f, 0.05f));
        EasedStateChange lamp = StatusLamp(station, "Status_Light_Phone", new Vector3(0.35f, 2.15f, -3.4f));

        Label("Station_Label_Phone", station, new Vector3(0.35f, 2.1f, -3.9f), new Vector3(0f, 180f, 0f),
            new Vector2(520f, 120f), 0.0016f, "PHONE CRADLE", 54, AmberGlow);

        return Lock(station, "Lock_Phone", "PHONE", handset, "VV_CordlessPhone",
            new Vector3(0.35f, 1.19f, -2.95f), 0f, 0.17f, bezel, lamp, exitSlot);
    }

    private static Transform NewStation(Transform parent, string name)
    {
        Transform station = new GameObject(name).transform;
        station.SetParent(parent, false);
        return station;
    }

    private static LockReceptacle Lock(Transform station, string name, string displayName, XRGrabInteractable key,
        string ghostPrefab, Vector3 position, float yaw, float radius,
        EasedStateChange bezel, EasedStateChange lamp, EasedStateChange exitSlot)
    {
        GameObject lockGo = new GameObject(name);
        lockGo.transform.SetParent(station, false);
        lockGo.transform.localPosition = position;
        lockGo.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

        SphereCollider zone = lockGo.AddComponent<SphereCollider>();
        zone.isTrigger = true;
        zone.radius = radius;

        XRSocketInteractor socket = lockGo.AddComponent<XRSocketInteractor>();
        socket.showInteractableHoverMeshes = true;
        socket.interactableHoverMeshMaterial = ghostMaterial;

        GameObject ghost = Ghost(ghostPrefab, lockGo.transform);

        LockReceptacle receptacle = lockGo.AddComponent<LockReceptacle>();
        receptacle.acceptedKey = key;
        receptacle.displayName = displayName;
        receptacle.ghostPreview = ghost;
        receptacle.solvedStateChanges = new[] { bezel, lamp, exitSlot };

        return receptacle;
    }

    private static GameObject Ghost(string prefabName, Transform parent)
    {
        GameObject ghost = Prop(prefabName, parent, Vector3.zero, 0f, "Ghost_Preview");
        PrefabUtility.UnpackPrefabInstance(ghost, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        foreach (Collider collider in ghost.GetComponentsInChildren<Collider>(true))
        {
            Object.DestroyImmediate(collider);
        }

        foreach (Renderer renderer in ghost.GetComponentsInChildren<Renderer>(true))
        {
            renderer.sharedMaterial = ghostMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        EasedStateChange fade = ghost.AddComponent<EasedStateChange>();
        fade.duration = 0.7f;
        fade.fadesOut = true;
        fade.scales = true;
        fade.targetLocalScale = ghost.transform.localScale * 1.4f;

        return ghost;
    }

    private static EasedStateChange StatusBezel(Transform parent, string name, Vector3 position, Vector3 scale)
    {
        GameObject bezel = Box(name, parent, position, scale, statusMaterial);
        EasedStateChange change = bezel.AddComponent<EasedStateChange>();
        change.duration = 0.8f;
        change.recolors = true;
        change.targetBaseColor = SolvedGreen;
        change.targetEmissionColor = SolvedGreen;
        change.targetEmissionIntensity = 1.2f;
        return change;
    }

    private static EasedStateChange StatusLamp(Transform parent, string name, Vector3 position)
    {
        GameObject lampGo = new GameObject(name);
        lampGo.transform.SetParent(parent, false);
        lampGo.transform.localPosition = position;

        Light lamp = lampGo.AddComponent<Light>();
        lamp.type = LightType.Point;
        lamp.range = 1.5f;
        lamp.intensity = 0.8f;
        lamp.color = LockedRed;
        lamp.shadows = LightShadows.None;

        EasedStateChange change = lampGo.AddComponent<EasedStateChange>();
        change.duration = 1f;
        change.relights = true;
        change.targetLightColor = SolvedGreen;
        change.targetLightIntensity = 1.1f;
        return change;
    }

    // -------------------------------------------------------------- signifiers

    private static GameObject BuildExitPanel(Transform parent)
    {
        Transform signifiers = new GameObject("Exit_Signifiers").transform;
        signifiers.SetParent(parent, false);

        GameObject panel = new GameObject("Exit_Panel");
        panel.transform.SetParent(signifiers, false);

        Box("Panel_Body", panel.transform, new Vector3(-2.1f, 1.5f, 3.95f), new Vector3(1.7f, 1.1f, 0.08f), panelMaterial);

        Slot(panel.transform, "Slot_Mug", new Vector3(-2.68f, 1.62f, 3.88f), "MUG");
        Slot(panel.transform, "Slot_Files", new Vector3(-2.1f, 1.62f, 3.88f), "FILES");
        Slot(panel.transform, "Slot_Phone", new Vector3(-1.52f, 1.62f, 3.88f), "PHONE");

        Label("Status_Label", panel.transform, new Vector3(-2.1f, 1.94f, 3.88f), Vector3.zero,
            new Vector2(900f, 130f), 0.0016f, "DOOR LOCKED", 64, LockedRed);
        Label("Progress_Label", panel.transform, new Vector3(-2.1f, 1.14f, 3.88f), Vector3.zero,
            new Vector2(900f, 120f), 0.0016f, "0 / 3 PUT BACK", 52, new Color(0.85f, 0.9f, 1f));

        // Runs along the floor of the doorway and turns green when the door unlocks.
        GameObject frameGlow = Box("Door_Frame_Glow", panel.transform, new Vector3(0f, 0.08f, 3.93f), new Vector3(1.9f, 0.1f, 0.06f), statusMaterial);
        EasedStateChange frameChange = frameGlow.AddComponent<EasedStateChange>();
        frameChange.duration = 1.2f;
        frameChange.recolors = true;
        frameChange.targetBaseColor = SolvedGreen;
        frameChange.targetEmissionColor = SolvedGreen;
        frameChange.targetEmissionIntensity = 1.6f;

        return panel;
    }

    private static void Slot(Transform parent, string name, Vector3 position, string caption)
    {
        GameObject slot = Box(name, parent, position, new Vector3(0.34f, 0.34f, 0.06f), statusMaterial);

        EasedStateChange change = slot.AddComponent<EasedStateChange>();
        change.duration = 0.9f;
        change.recolors = true;
        change.targetBaseColor = SolvedGreen;
        change.targetEmissionColor = SolvedGreen;
        change.targetEmissionIntensity = 1.3f;
        change.scales = true;
        change.targetLocalScale = slot.transform.localScale;
        change.scalePop = 1.35f;

        Label(name + "_Caption", parent, position + new Vector3(0f, -0.26f, -0.01f), Vector3.zero,
            new Vector2(420f, 100f), 0.0013f, caption, 52, new Color(0.8f, 0.86f, 1f));
    }

    private static void BuildTeachingBoard(Transform parent)
    {
        Transform signifiers = parent.Find("Exit_Signifiers");

        GameObject board = new GameObject("Teaching_Board");
        board.transform.SetParent(signifiers, false);

        Prop("VV_whiteboard1", board.transform, new Vector3(2.2f, 0.85f, 3.9f), 180f, "Whiteboard");

        Label("Rules", board.transform, new Vector3(2.2f, 1.42f, 3.79f), Vector3.zero,
            new Vector2(1000f, 620f), 0.0016f,
            "END OF SHIFT - 22:41\n\nThe badge reader is down. The door stays\nlocked until the office is put back in order.\n\nAMBER GLOW = you can pick it up\nBLUE OUTLINE = something belongs here\n\nPut all three back to go home.",
            42, new Color(0.12f, 0.14f, 0.18f));
    }

    private static GameObject BuildEscapedBanner(Transform parent)
    {
        GameObject banner = new GameObject("Escaped_Banner");
        banner.transform.SetParent(parent, false);

        Text text = Label("Escaped_Text", banner.transform, new Vector3(0f, 1.8f, 7.9f), Vector3.zero,
            new Vector2(1400f, 300f), 0.0026f, "YOU ESCAPED", 120, SolvedGreen);

        EasedStateChange pop = text.gameObject.AddComponent<EasedStateChange>();
        pop.duration = 1f;
        pop.scales = true;
        pop.targetLocalScale = text.transform.localScale;
        pop.scalePop = 1.5f;

        return banner;
    }

    // ----------------------------------------------------------------- player

    private static GameObject BuildPlayer(Transform parent)
    {
        GameObject manager = new GameObject("XR Interaction Manager");
        manager.AddComponent<XRInteractionManager>();
        manager.transform.SetParent(parent, false);

        GameObject rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(StarterAssetDir + "XR Origin (XR Rig).prefab");
        GameObject rig = (GameObject)PrefabUtility.InstantiatePrefab(rigPrefab, parent);

        // Spawn a hand's width above the floor. XRI parks the character capsule's
        // bottom at exactly skinWidth above the rig origin, and sizes the capsule
        // from the camera height - which is still zero on the first frame, before
        // the headset reports a pose. Starting flush with the floor lets that first
        // frame begin inside the slab, and the player gets pushed out underneath it.
        rig.transform.localPosition = new Vector3(0f, 0.15f, -2.2f);
        rig.transform.localRotation = Quaternion.identity;

        // Room-scale: the floor of the play area is the floor of the office.
        // Leaving this unspecified makes the first few frames depend on the runtime.
        XROrigin origin = rig.GetComponent<XROrigin>();
        if (origin != null)
        {
            origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;
        }

        GameObject directPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(StarterAssetDir + "Interactors/Direct Interactor.prefab");
        foreach (string hand in new[] { "Left Controller", "Right Controller" })
        {
            Transform controller = rig.transform.Find("Camera Offset/" + hand);
            if (controller == null || directPrefab == null)
            {
                continue;
            }

            GameObject direct = (GameObject)PrefabUtility.InstantiatePrefab(directPrefab, controller);
            direct.name = "Direct Interactor";
            direct.transform.localPosition = Vector3.zero;
            direct.transform.localRotation = Quaternion.identity;

            XRInteractionGroup group = controller.GetComponent<XRInteractionGroup>();
            XRDirectInteractor interactor = direct.GetComponent<XRDirectInteractor>();
            if (group != null && interactor != null && !group.startingGroupMembers.Contains(interactor))
            {
                // Sits between poke and near-far so a hand touching a Key wins over a far ray.
                group.startingGroupMembers.Insert(Mathf.Min(1, group.startingGroupMembers.Count), interactor);
                EditorUtility.SetDirty(group);
            }
        }

        return rig;
    }

    // ---------------------------------------------------------------- helpers

    private static GameObject Box(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.SetParent(parent, false);
        box.transform.localPosition = localPosition;
        box.transform.localScale = localScale;
        box.GetComponent<Renderer>().sharedMaterial = material;
        return box;
    }

    private static GameObject Prop(string prefabName, Transform parent, Vector3 localPosition, float yaw, string name)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PropDir + prefabName + ".prefab");
        if (prefab == null)
        {
            Debug.LogError("Missing prop prefab: " + prefabName);
            return null;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.transform.localPosition = localPosition;
        instance.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        instance.name = name;
        return instance;
    }

    private static Text Label(string name, Transform parent, Vector3 localPosition, Vector3 localEuler,
        Vector2 size, float scale, string content, int fontSize, Color color)
    {
        GameObject canvasGo = new GameObject(name, typeof(RectTransform), typeof(Canvas));
        canvasGo.transform.SetParent(parent, false);
        canvasGo.transform.localPosition = localPosition;
        canvasGo.transform.localEulerAngles = localEuler;
        canvasGo.transform.localScale = Vector3.one * scale;

        RectTransform canvasRect = (RectTransform)canvasGo.transform;
        canvasRect.sizeDelta = size;

        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textGo.transform.SetParent(canvasGo.transform, false);

        RectTransform textRect = (RectTransform)textGo.transform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text text = textGo.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        return text;
    }

    private static void CreateMaterials()
    {
        if (!AssetDatabase.IsValidFolder(MaterialDir))
        {
            AssetDatabase.CreateFolder("Assets/Asset/office", "EscapeRoomMaterials");
        }

        floorMaterial = Lit("M_Room_Floor", new Color(0.19f, 0.20f, 0.23f), 0f, 0.15f);
        wallMaterial = Lit("M_Room_Wall", new Color(0.78f, 0.75f, 0.69f), 0f, 0.1f);
        ceilingMaterial = Lit("M_Room_Ceiling", new Color(0.92f, 0.92f, 0.94f), 0f, 0.05f);
        trimMaterial = Lit("M_Door_Trim", new Color(0.95f, 0.72f, 0.16f), 0.2f, 0.5f);
        doorMaterial = Lit("M_Door_Panel", new Color(0.34f, 0.37f, 0.42f), 0.7f, 0.55f);
        panelMaterial = Lit("M_Exit_Panel", new Color(0.07f, 0.08f, 0.10f), 0.3f, 0.4f);
        metalMaterial = Lit("M_Metal", new Color(0.42f, 0.44f, 0.47f), 0.8f, 0.6f);

        statusMaterial = Lit("M_Status_Locked", LockedRed, 0f, 0.4f);
        statusMaterial.EnableKeyword("_EMISSION");
        statusMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        statusMaterial.SetColor("_EmissionColor", LockedRed * 0.9f);
        EditorUtility.SetDirty(statusMaterial);

        ghostMaterial = Transparent("M_Ghost_Preview", GhostBlue);

        AssetDatabase.SaveAssets();
    }

    private static Material Lit(string name, Color color, float metallic, float smoothness)
    {
        string path = MaterialDir + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, path);
        }

        material.SetColor("_BaseColor", color);
        material.SetFloat("_Metallic", metallic);
        material.SetFloat("_Smoothness", smoothness);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material Transparent(string name, Color color)
    {
        string path = MaterialDir + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, path);
        }

        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0f);
        material.SetFloat("_AlphaClip", 0f);
        material.SetFloat("_Smoothness", 0.85f);
        material.SetOverrideTag("RenderType", "Transparent");
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_ALPHATEST_ON");
        material.renderQueue = (int)RenderQueue.Transparent;
        material.SetColor("_BaseColor", color);
        material.EnableKeyword("_EMISSION");
        material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        material.SetColor("_EmissionColor", new Color(color.r, color.g, color.b) * 1.3f);
        EditorUtility.SetDirty(material);
        return material;
    }

    /// The room runs ten small point lights (ceiling, station status lamps, Key halos), so the
    /// URP per-object limit of four would pop lights on and off as the player walks.
    private static void RaiseAdditionalLightLimit()
    {
        RenderPipelineAsset pipeline = GraphicsSettings.defaultRenderPipeline;
        if (pipeline == null)
        {
            return;
        }

        SerializedObject serialized = new SerializedObject(pipeline);
        SerializedProperty maxLights = serialized.FindProperty("m_AdditionalLightsPerObjectLimit");
        if (maxLights != null && maxLights.intValue < 8)
        {
            maxLights.intValue = 8;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void AddSceneToBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (EditorBuildSettingsScene existing in scenes)
        {
            if (existing.path == ScenePath)
            {
                return;
            }
        }

        scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
