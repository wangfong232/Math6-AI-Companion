#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using Math6Companion.Gameplay3D;

namespace Math6Companion.EditorTools
{
    [InitializeOnLoad]
    public static class VillageWorldBuilder
    {
        private const string VILLAGE_SCENE_PATH  = "Assets/Scenes/02_VillageWorld.unity";
        private const string VIETNAM_DEMO_SCENE  = "Assets/PolyOne/Polygon World - Vietnam/Scene/Polygon World - Vietnam _Demo.unity";
        private static readonly Vector3 PLAYER_SPAWN = new Vector3(0f, 1.0f, -22f);

        static VillageWorldBuilder()
        {
            // Auto-build disabled to preserve user's manual object placements in the scene.
            // Build only runs when user explicitly clicks the MenuItem.
        }

        [MenuItem("Math6 / RESET and BUILD Village World (Clean V23)", priority = 1)]
        public static void BuildVillageWorld() => ExecuteBuild(!Application.isBatchMode);
        public static void BuildVillageWorldBatch() => ExecuteBuild(false);

        public static void ExecuteBuild(bool showDialog)
        {
            try
            {
                var villageScene = OpenOrCreateScene();
                EnsureEventSystem();
                CleanupOldGeneratedObjects();
                SetupOriginalDiorama();
                var playerObj = SetupPlayerCharacter();
                SetupAICompanion(playerObj);
                SetupThirdPersonCamera(playerObj);
                SetupInvisibleBoundaries();
                PopulateVillageEnvironment(); // Cay coi, hang rao, duong mon
                SetupPhoDiningCorner();
                SetupNoticeBoard();
                SetupTerraceFieldsTrigger();
                SetupPagodaSymmetryTrigger();
                SetupVillageCanvasHUD();
                EditorSceneManager.MarkSceneDirty(villageScene);
                EditorSceneManager.SaveScene(villageScene, VILLAGE_SCENE_PATH);
                RegisterSceneInBuildSettings(VILLAGE_SCENE_PATH);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                if (showDialog)
                    EditorUtility.DisplayDialog("Thanh cong!",
                        "Da reset va dung lai Village Scene!\n\n" +
                        "Diorama Chua Mot Cot giu nguyen ty le goc (scale 1,1,1)\n" +
                        "Player dung truoc chan cau thang\n" +
                        "Camera isometric ngam tron ven Chua\n" +
                        "Canvas HUD, AI Companion, Trigger zones day du",
                        "OK!");
            }
            catch (Exception ex)
            {
                Debug.LogError("[VillageWorldBuilder] Error: " + ex);
                if (showDialog) EditorUtility.DisplayDialog("Loi", ex.Message, "OK");
            }
        }

        private static UnityEngine.SceneManagement.Scene OpenOrCreateScene()
        {
            if (File.Exists(VIETNAM_DEMO_SCENE))
            {
                File.Copy(VIETNAM_DEMO_SCENE, VILLAGE_SCENE_PATH, true);
                AssetDatabase.Refresh();
                return EditorSceneManager.OpenScene(VILLAGE_SCENE_PATH, OpenSceneMode.Single);
            }
            return EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        }

        private static void CleanupOldGeneratedObjects()
        {
            string[] names = {
                "VillageGround_Courtyard","VillageGround_VastTerrain","VillageExpanded_Nature",
                "Village_ExpandedGround","Ground_South_Plaza","Ground_North_Grove",
                "Ground_West_Hills","Ground_East_Meadow","Ground_Full_Base",
                "SafetyGround",
                "VillageEnvironment",
                "InvisibleBoundaries",
                "Player_Student","AI_Companion_ThayMinh","AI_Companion_Lumi",
                "Canvas_VillageHUD",
                "Zone_QuanPhoCuBa","Zone_BangCaoThi","Zone_RuongBacThang","Zone_ChuaMotCot_DoiXung",
            };
            foreach (var n in names) { var go = GameObject.Find(n); if (go != null) UnityEngine.Object.DestroyImmediate(go); }
            var demoCam = GameObject.Find("Scene/Camera");
            if (demoCam != null) UnityEngine.Object.DestroyImmediate(demoCam);
            var oldPlayer = GameObject.FindGameObjectWithTag("Player");
            if (oldPlayer != null) UnityEngine.Object.DestroyImmediate(oldPlayer);
        }

        private static void SetupOriginalDiorama()
        {
            var modelObj = GameObject.Find("Model");
            if (modelObj == null) { Debug.LogWarning("[VillageWorldBuilder] 'Model' not found!"); return; }

            // Scale 22: ty le goc da duoc kiem chung - Chua cao hon player, cot da ro rang
            // (scale 16 = mai ngang vai nguoi, scale 22 = cong trinh kien truc dung chuan)
            modelObj.transform.position   = Vector3.zero;
            modelObj.transform.rotation   = Quaternion.identity;
            modelObj.transform.localScale = Vector3.one * 22f;

            foreach (Transform t in modelObj.GetComponentsInChildren<Transform>(true))
            {
                t.gameObject.SetActive(true);
                var mr = t.GetComponent<MeshRenderer>();
                if (mr != null) { mr.enabled = true; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; mr.receiveShadows = true; }
                var mf = t.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null && t.GetComponent<Collider>() == null)
                {
                    var mc = t.gameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = mf.sharedMesh;
                    mc.convex = false;
                }
            }

            // XÃ³a PlayerPrefs vá»‹ trÃ­ cÅ© - trÃ¡nh vÃ²ng láº·p rÆ¡i vÃ´ cá»±c
            PlayerPrefs.DeleteKey("Village_HasSavedPos");
            PlayerPrefs.DeleteKey("Village_PlayerPosX");
            PlayerPrefs.DeleteKey("Village_PlayerPosY");
            PlayerPrefs.DeleteKey("Village_PlayerPosZ");
            PlayerPrefs.DeleteKey("Village_PlayerRotY");
            PlayerPrefs.Save();

            var dirLight = GameObject.Find("Directional Light");
            if (dirLight != null)
            {
                var lc = dirLight.GetComponent<Light>();
                if (lc != null) { lc.color = new Color(1f,0.96f,0.88f); lc.intensity = 1.5f; lc.shadows = LightShadows.Soft; }
                dirLight.transform.rotation = Quaternion.Euler(48f,150f,0f);
            }
            RenderSettings.ambientMode  = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.90f,0.86f,0.82f);

            // San co tai y=-4: nam DUOI day diorama de khong che phu cot da, cau thang, ho nuoc
            // Diorama island base o y=0 (scale 22) -> san co chi lam nen phia xa
            var grassPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            grassPlane.name = "SafetyGround";
            grassPlane.transform.position   = new Vector3(0f, -4f, 0f);
            grassPlane.transform.localScale  = new Vector3(20f, 1f, 20f); // 200x200m
            // Gan mau co xanh low-poly
            var grassRend = grassPlane.GetComponent<MeshRenderer>();
            if (grassRend != null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                var grassMat = new Material(shader);
                grassMat.name = "GrassGreen_Runtime";
                Color gc = new Color(0.38f, 0.62f, 0.22f, 1f);
                grassMat.color = gc;
                if (grassMat.HasProperty("_BaseColor"))  grassMat.SetColor("_BaseColor", gc);
                if (grassMat.HasProperty("_Smoothness")) grassMat.SetFloat("_Smoothness", 0f);
                if (grassMat.HasProperty("_Glossiness")) grassMat.SetFloat("_Glossiness", 0f);
                grassRend.sharedMaterial = grassMat;
                grassRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                grassRend.receiveShadows   = true;
            }
        }

        private static void SetupInvisibleBoundaries()
        {
            var root = new GameObject("InvisibleBoundaries");
            // Scale 22 -> diorama ~50m -> tuong cach tam 55m
            float half = 55f, thick = 2f, h = 25f, len = 115f;
            CreateBoundaryWall(root.transform, "Wall_North", new Vector3( 0, h*0.4f,  half), new Vector3(len,h,thick));
            CreateBoundaryWall(root.transform, "Wall_South", new Vector3( 0, h*0.4f, -half), new Vector3(len,h,thick));
            CreateBoundaryWall(root.transform, "Wall_East",  new Vector3( half, h*0.4f, 0),  new Vector3(thick,h,len));
            CreateBoundaryWall(root.transform, "Wall_West",  new Vector3(-half, h*0.4f, 0),  new Vector3(thick,h,len));
            var kp = new GameObject("KillPlane_VoidTrigger");
            kp.transform.SetParent(root.transform, false);
            kp.transform.position = new Vector3(0,-12f,0);
            var kc = kp.AddComponent<BoxCollider>(); kc.isTrigger = true; kc.size = new Vector3(300f,2f,300f);
        }

        private static void CreateBoundaryWall(Transform parent, string name, Vector3 pos, Vector3 size)
        {
            var wall = new GameObject(name);
            wall.transform.SetParent(parent,false);
            wall.transform.position = pos;
            wall.AddComponent<BoxCollider>().size = size;
        }

        private static GameObject SetupPlayerCharacter()
        {
            var player = new GameObject("Player_Student");
            player.tag = "Player";
            float spawnY = PLAYER_SPAWN.y;
            // Raycast tu cao hon (y=15) de bat duoc san co o y=-4
            Ray ray = new Ray(new Vector3(PLAYER_SPAWN.x, 15f, PLAYER_SPAWN.z), Vector3.down);
            if (Physics.Raycast(ray, out RaycastHit hit, 40f)) spawnY = hit.point.y + 0.05f;
            player.transform.position = new Vector3(PLAYER_SPAWN.x, spawnY, PLAYER_SPAWN.z);
            player.transform.rotation = Quaternion.identity;
            var cc = player.AddComponent<CharacterController>();
            cc.height=1.8f; cc.radius=0.35f; cc.center=new Vector3(0f,0.9f,0f); cc.stepOffset=0.3f; cc.slopeLimit=45f;
            var mover = player.AddComponent<PlayerThirdPersonController>();
            mover.SetSpawnPoint(player.transform.position, player.transform.rotation);
            string baseMeshPath = "Assets/ithappy/Creative_Characters_FREE/Prefabs/Base_Mesh.prefab";
            var meshPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(baseMeshPath);
            if (meshPrefab != null)
            {
                var modelInst = (GameObject)PrefabUtility.InstantiatePrefab(meshPrefab, player.transform);
                modelInst.name = "Model_Mesh";
                modelInst.transform.localPosition = Vector3.zero;
                modelInst.transform.localRotation = Quaternion.identity;
                modelInst.transform.localScale    = Vector3.one;
                // DÃ¹ng kiá»ƒm tra null tÆ°á»ng minh thay vÃ¬ ?? (Unity fake-null khÃ´ng tÆ°Æ¡ng thÃ­ch vá»›i ??)
                var anim = modelInst.GetComponent<Animator>();
                if (anim == null) anim = modelInst.AddComponent<Animator>();
                if (anim != null)
                {
                    var ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                        "Assets/ithappy/Creative_Characters_FREE/Animations/Animation_Controllers/Character_Movement.controller");
                    if (ctrl != null) anim.runtimeAnimatorController = ctrl;
                }
                DressUpStudentModel(modelInst);
            }
            else
            {
                var cap = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                cap.transform.SetParent(player.transform, false);
                cap.transform.localPosition = new Vector3(0f,0.9f,0f);
                UnityEngine.Object.DestroyImmediate(cap.GetComponent<Collider>());
            }
            return player;
        }

        private static void DressUpStudentModel(GameObject m)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/ithappy/Creative_Characters_FREE/Materials/Color.mat");
            AssignPartMesh(m,"Body",      "Assets/ithappy/Creative_Characters_FREE/Meshes/Body/Body_010.fbx",mat);
            AssignPartMesh(m,"Face",      "Assets/ithappy/Creative_Characters_FREE/Meshes/Faces/Male_emotion_happy_002.fbx",mat);
            AssignPartMesh(m,"Hairstyle", "Assets/ithappy/Creative_Characters_FREE/Meshes/Hairstyle/Hairstyle_Male_001.fbx",mat);
            AssignPartMesh(m,"Outerwear", "Assets/ithappy/Creative_Characters_FREE/Meshes/Outwear/Outwear_004.fbx",mat);
            AssignPartMesh(m,"Pants",     "Assets/ithappy/Creative_Characters_FREE/Meshes/Pants/Pants_009.fbx",mat);
            AssignPartMesh(m,"Shoes",     "Assets/ithappy/Creative_Characters_FREE/Meshes/Shoes/Shoe_Sneakers_009.fbx",mat);
            foreach (var part in new[]{"Mustache","Glasses","Hat","Full_body","Gloves","Accessories"})
                foreach (Transform c in m.GetComponentsInChildren<Transform>(true))
                    if (c.name.StartsWith(part, StringComparison.OrdinalIgnoreCase)) c.gameObject.SetActive(false);
        }

        private static void AssignPartMesh(GameObject root, string prefix, string fbxPath, Material mat)
        {
            Transform target = null;
            foreach (Transform c in root.GetComponentsInChildren<Transform>(true))
                if (c.name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) { target = c; break; }
            if (target == null) return;
            var smr = target.GetComponent<SkinnedMeshRenderer>();
            if (smr == null) return;
            Mesh mesh = null;
            var fbxGo = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (fbxGo != null) { var s = fbxGo.GetComponentInChildren<SkinnedMeshRenderer>(true); if (s?.sharedMesh != null) mesh = s.sharedMesh; }
            if (mesh == null) foreach (var a in AssetDatabase.LoadAllAssetsAtPath(fbxPath)) if (a is Mesh x && !x.name.StartsWith("__preview__")) { mesh = x; break; }
            if (mesh != null) { smr.sharedMesh = mesh; smr.localBounds = mesh.bounds; target.gameObject.SetActive(true); }
            if (mat != null) smr.sharedMaterial = mat;
        }

        private static void SetupAICompanion(GameObject player)
        {
            var old = GameObject.Find("AI_Companion_ThayMinh");
            if (old != null) UnityEngine.Object.DestroyImmediate(old);
            string botPath = "Assets/PolyOne/Free Stylized Sci-Fi Scout Bot/Prefabs/SR_01_Scout_Bot.prefab";
            var botPrefab  = AssetDatabase.LoadAssetAtPath<GameObject>(botPath);
            var comp = botPrefab != null ? (GameObject)PrefabUtility.InstantiatePrefab(botPrefab) : GameObject.CreatePrimitive(PrimitiveType.Sphere);
            comp.name = "AI_Companion_ThayMinh";
            comp.transform.localScale = new Vector3(0.18f,0.18f,0.18f);
            comp.transform.position   = player.transform.position + new Vector3(0.45f,1.15f,0.2f);
            foreach (Transform t in comp.GetComponentsInChildren<Transform>(true))
                if (t.name.IndexOf("Bomd",StringComparison.OrdinalIgnoreCase)>=0 || t.name.IndexOf("Weapon",StringComparison.OrdinalIgnoreCase)>=0)
                    { t.gameObject.SetActive(false); var r=t.GetComponent<Renderer>(); if(r!=null) r.enabled=false; }
            var anim = comp.GetComponent<Animator>();
            if (anim != null)
            {
                var ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/PolyOne/Free Stylized Sci-Fi Scout Bot/Controler/Controler_01_Scout_Bot.controller");
                if (ctrl != null) anim.runtimeAnimatorController = ctrl;
                anim.applyRootMotion = false;
            }
            var fc = comp.AddComponent<FloatingAICompanion>(); fc.SetTarget(player.transform);
            var bubble = new GameObject("SpeechBubbleRoot");
            bubble.transform.SetParent(comp.transform,false);
            bubble.transform.localPosition = new Vector3(0f,0.7f,0f);
            var tmp = bubble.AddComponent<TextMeshPro>();
            tmp.text="Thay Minh chao em! Cung on luyen Toan 6 nhe!"; tmp.fontSize=3.2f; tmp.alignment=TextAlignmentOptions.Center; tmp.color=Color.white; tmp.rectTransform.sizeDelta=new Vector2(4.5f,1.5f);
            SetPrivateField(fc,"speechBubbleRoot",bubble); SetPrivateField(fc,"speechBubbleText",tmp); bubble.SetActive(false);
        }

        private static void SetupThirdPersonCamera(GameObject player)
        {
            var old = GameObject.Find("Scene/Camera");
            if (old != null) UnityEngine.Object.DestroyImmediate(old);
            var cam = Camera.main;
            if (cam == null)
            {
                var co = new GameObject("Main Camera"); cam = co.AddComponent<Camera>(); co.tag="MainCamera"; co.AddComponent<AudioListener>();
            }
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.55f,0.78f,0.93f);
            cam.fieldOfView = 50f; cam.nearClipPlane = 0.1f; cam.farClipPlane = 200f;
            var orbit = cam.GetComponent<SmoothOrbitCamera>() ?? cam.gameObject.AddComponent<SmoothOrbitCamera>();
            orbit.SetTarget(player.transform);
            // Camera: dat sau player, cao, nhin ve diorama o giua
            // Player tai (0, ~0.3, -22) -> camera tai (0, ~10, -36) nhin ve (0, 4, 0)
            var p = player.transform.position;
            cam.transform.position = new Vector3(p.x, p.y + 10f, p.z - 14f);
            cam.transform.LookAt(new Vector3(0f, 4f, 0f)); // tam diorama
        }

        private static void SetupPhoDiningCorner()
        {
            var root = new GameObject("Zone_QuanPhoCuBa");
            // Dat phia Nam diorama, tren san co
            root.transform.position = new Vector3(-8f, 0f, -10f);
            root.transform.rotation = Quaternion.Euler(0f, 30f, 0f);
            TryInstantiate("Assets/_Project/Art/Props/PhoDiningSet/AsianStreetFoodPack_PhoDiningSet/FBX/SM_Furniture_TableLow_Rectangular_Blue_01.fbx", root.transform, Vector3.zero, Quaternion.identity, Vector3.one);
            string sBlue="Assets/_Project/Art/Props/PhoDiningSet/AsianStreetFoodPack_PhoDiningSet/FBX/SM_Furniture_StoolLow_Blue_01.fbx";
            string sRed ="Assets/_Project/Art/Props/PhoDiningSet/AsianStreetFoodPack_PhoDiningSet/FBX/SM_Furniture_StoolLow_Red_01.fbx";
            TryInstantiate(sBlue,root.transform,new Vector3(-0.9f,0,0.3f),Quaternion.identity,Vector3.one);
            TryInstantiate(sBlue,root.transform,new Vector3( 0.9f,0,0.3f),Quaternion.identity,Vector3.one);
            TryInstantiate(sRed, root.transform,new Vector3(-0.4f,0,-0.5f),Quaternion.identity,Vector3.one);
            TryInstantiate(sRed, root.transform,new Vector3( 0.4f,0,-0.5f),Quaternion.identity,Vector3.one);
            TryInstantiate("Assets/_Project/Art/Props/PhoDiningSet/AsianStreetFoodPack_PhoDiningSet/FBX/SM_Cook_PhoBowl_Full_01.fbx",root.transform,new Vector3(-0.3f,0.55f,0),Quaternion.identity,Vector3.one);
            TryInstantiate("Assets/_Project/Art/Props/PhoDiningSet/AsianStreetFoodPack_PhoDiningSet/FBX/SM_Cook_HerbTray_Blue_01.fbx",root.transform,new Vector3(0.3f,0.55f,0),Quaternion.identity,Vector3.one);
            AddTriggerZone(root,new Vector3(4f,2.5f,4f),new Vector3(0,1,0),WorldInteractable.InteractableType.PhoShop,"[E] Quan Pho Cu Ba (Phan So)");
        }

        private static void SetupNoticeBoard()
        {
            var root = new GameObject("Zone_BangCaoThi");
            // Dat doi xung phia Dong Nam, tren san co
            root.transform.position = new Vector3(8f, 0f, -10f);
            root.transform.rotation = Quaternion.Euler(0f,-30f,0f);
            TryInstantiate("Assets/Notice/Prefabs/Board01.prefab",root.transform,Vector3.zero,Quaternion.Euler(0,-145,0),Vector3.one*1.1f);
            AddTriggerZone(root,new Vector3(3f,2.5f,3f),new Vector3(0,1,0),WorldInteractable.InteractableType.NoticeBoard,"[E] Bang Cao Thi Lang");
        }

        private static void SetupTerraceFieldsTrigger()
        {
            var root = new GameObject("Zone_RuongBacThang");
            root.transform.position = SnapToGround(-2.5f, 0f);
            AddTriggerZone(root,new Vector3(4f,3f,4f),new Vector3(0,1,0),WorldInteractable.InteractableType.TerraceFields,"[E] Ruong Bac Thang (Truc So Nguyen)");
        }

        private static void SetupPagodaSymmetryTrigger()
        {
            var root = new GameObject("Zone_ChuaMotCot_DoiXung");
            root.transform.position = SnapToGround(1f, 2f);
            AddTriggerZone(root,new Vector3(4f,3f,4f),new Vector3(0,1,0),WorldInteractable.InteractableType.PagodaSymmetry,"[E] Chua Mot Cot (Hinh Hoc Doi Xung)");
        }

        private static Vector3 SnapToGround(float x, float z)
        {
            Ray r = new Ray(new Vector3(x,15f,z), Vector3.down);
            return Physics.Raycast(r, out RaycastHit hit, 30f) ? hit.point : new Vector3(x,0f,z);
        }

        private static void AddTriggerZone(GameObject root, Vector3 size, Vector3 center, WorldInteractable.InteractableType type, string prompt)
        {
            var bc = root.AddComponent<BoxCollider>(); bc.isTrigger=true; bc.size=size; bc.center=center;
            var wi = root.AddComponent<WorldInteractable>();
            SetPrivateField(wi,"interactionType",type); SetPrivateField(wi,"interactPrompt",prompt);
            var po = new GameObject("PromptCanvas"); po.transform.SetParent(root.transform,false); po.transform.localPosition=new Vector3(0f,2.3f,0f);
            var tmp=po.AddComponent<TextMeshPro>(); tmp.text=prompt; tmp.fontSize=3.6f; tmp.alignment=TextAlignmentOptions.Center; tmp.color=new Color(1f,0.9f,0.2f); tmp.rectTransform.sizeDelta=new Vector2(6f,1.2f);
            SetPrivateField(wi,"promptCanvasObject",po); SetPrivateField(wi,"promptText",tmp);
        }

        private static GameObject TryInstantiate(string path, Transform parent, Vector3 pos, Quaternion rot, Vector3 scale)
        {
            var pf = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (pf == null) return null;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(pf, parent);
            go.transform.localPosition=pos; go.transform.localRotation=rot; go.transform.localScale=scale;
            return go;
        }

        private static void SetupVillageCanvasHUD()
        {
            var co = new GameObject("Canvas_VillageHUD");
            var cv = co.AddComponent<Canvas>(); cv.renderMode=RenderMode.ScreenSpaceOverlay; cv.sortingOrder=50;
            var sc = co.AddComponent<CanvasScaler>(); sc.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution=new Vector2(1920,1080); sc.matchWidthOrHeight=0.5f;
            co.AddComponent<GraphicRaycaster>();
            var hud = co.AddComponent<VillageHUDManager>();

            var topBar = CreateUI("TopBar",co.transform); SetRect(topBar,new Vector2(0,1),new Vector2(1,1),new Vector2(0.5f,1),Vector2.zero,new Vector2(0,88)); topBar.AddComponent<Image>().color=new Color(0.07f,0.11f,0.18f,0.9f);
            var nickTxt = MakeTMP(topBar.transform,"PlayerNick","<b>Trạng Tí</b> (Lớp 6A)",24,TextAlignmentOptions.Left,Color.white); SetRect(nickTxt.gameObject,new Vector2(0,0.5f),new Vector2(0,0.5f),new Vector2(0,0.5f),new Vector2(40,0),new Vector2(360,50));
            var starTxt = MakeTMP(topBar.transform,"StarText","Sao: 12",26,TextAlignmentOptions.MidlineRight,new Color(1f,0.85f,0.2f)); SetRect(starTxt.gameObject,new Vector2(1,0.5f),new Vector2(1,0.5f),new Vector2(1,0.5f),new Vector2(-420,0),new Vector2(140,50));
            var coinTxt = MakeTMP(topBar.transform,"CoinText","Xu: 250",26,TextAlignmentOptions.MidlineRight,new Color(0.4f,0.9f,0.5f)); SetRect(coinTxt.gameObject,new Vector2(1,0.5f),new Vector2(1,0.5f),new Vector2(1,0.5f),new Vector2(-260,0),new Vector2(140,50));
            var btnMenu = MakeBtn(topBar.transform,"BtnTopMenu","Menu chính (O)",new Vector2(-30,0),new Vector2(180,48),new Color(0.2f,0.55f,0.9f)); SetRect(btnMenu.gameObject,new Vector2(1,0.5f),new Vector2(1,0.5f),new Vector2(1,0.5f),new Vector2(-30,0),new Vector2(180,48));

            var qBox=CreateUI("QuestBox",co.transform); SetRect(qBox,new Vector2(1,1),new Vector2(1,1),new Vector2(1,1),new Vector2(-25,-108),new Vector2(350,105)); qBox.AddComponent<Image>().color=new Color(0.07f,0.11f,0.18f,0.85f);
            var qT=MakeTMP(qBox.transform,"QuestTitle","Khám phá Làng Sen",18,TextAlignmentOptions.TopLeft,Color.white); SetRect(qT.gameObject,new Vector2(0,1),new Vector2(1,1),new Vector2(0.5f,1),new Vector2(0,-12),new Vector2(-24,28));
            var qD=MakeTMP(qBox.transform,"QuestDesc","Ghé Bảng Cáo Thị [TAB] hoặc Quán Phở Cụ Ba!",15,TextAlignmentOptions.TopLeft,new Color(0.85f,0.88f,0.92f)); SetRect(qD.gameObject,new Vector2(0,0),new Vector2(1,1),new Vector2(0.5f,0.5f),new Vector2(0,-18),new Vector2(-24,-48));

            var pill=CreateUI("HotkeyPill",co.transform); SetRect(pill,new Vector2(0.5f,0),new Vector2(0.5f,0),new Vector2(0.5f,0),new Vector2(0,30),new Vector2(500,38)); pill.AddComponent<Image>().color=new Color(0.04f,0.07f,0.12f,0.85f);
            var pT=MakeTMP(pill.transform,"PillText","[E] Tương tác  |  [F] Thầy Minh AI  |  [TAB] Bảng Cáo Thị  |  [O] Menu chính",15,TextAlignmentOptions.Center,new Color(0.9f,0.95f,1f)); SetFull(pT.GetComponent<RectTransform>());

            var panelNotice   = MakePanel(co.transform,"Panel_BangCaoThi","BẢNG CÁO THỊ LÀNG SEN","Chào mừng sĩ tử đến Làng Sen!\nGhé các khu vực để học Toán.","VÀO BÀI HỌC",()=>UnityEngine.SceneManagement.SceneManager.LoadScene("03_LessonList"));
            var panelPho      = MakePanel(co.transform,"Panel_QuanPho","QUÁN PHỞ CỤ BA - PHÂN SỐ","Chia 6 tô phở đều cho 8 người -> mỗi người được bao nhiêu tô?\nGợi ý: 6/8 = 3/4","LUYỆN PHÂN SỐ",()=>UnityEngine.SceneManagement.SceneManager.LoadScene("05_MCQPractice"));
            var panelTerrace  = MakePanel(co.transform,"Panel_RuongBacThang","RUỘNG BẬC THANG - TRỤC SỐ","Đáy hồ: -2  Cỏ trung tâm: 0  Tầng 1: +1  Tầng 2: +2","THỬ THÁCH TRỤC SỐ",()=>UnityEngine.SceneManagement.SceneManager.LoadScene("05_MCQPractice"));
            var panelPagoda   = MakePanel(co.transform,"Panel_ChuaMotCot","CHÙA MỘT CỘT - ĐỐI XỨNG","Cột đá = Trục đối xứng thẳng đứng. Mái ngói 4 góc đối xứng qua tâm.","LUYỆN HÌNH HỌC",()=>UnityEngine.SceneManagement.SceneManager.LoadScene("06_EssayPractice"));
            var panelAI       = MakeAIPanel(co.transform,"Panel_AITutorChat");

            SetPrivateField(hud,"txtPlayerNick",nickTxt); SetPrivateField(hud,"txtStars",starTxt); SetPrivateField(hud,"txtCoins",coinTxt);
            SetPrivateField(hud,"btnReturnMainMenu",btnMenu);
            SetPrivateField(hud,"panelNoticeBoard",panelNotice); SetPrivateField(hud,"panelPhoMinigame",panelPho);
            SetPrivateField(hud,"panelTerraceFields",panelTerrace); SetPrivateField(hud,"panelPagodaSymmetry",panelPagoda); SetPrivateField(hud,"panelAITutorChat",panelAI);
            var companion = UnityEngine.Object.FindAnyObjectByType<FloatingAICompanion>();
            if (companion != null) SetPrivateField(hud,"aiCompanion",companion);
        }

        private static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() == null)
            { var es=new GameObject("EventSystem"); es.AddComponent<EventSystem>(); es.AddComponent<StandaloneInputModule>(); }
        }
        private static GameObject MakePanel(Transform parent, string name, string title, string body, string btnTxt, UnityEngine.Events.UnityAction act)
        {
            var p=CreateUI(name,parent); SetFull(p.GetComponent<RectTransform>()); p.AddComponent<Image>().color=new Color(0,0,0,0.7f);
            var card=CreateUI("Card",p.transform); SetRect(card,new Vector2(0.5f,0.5f),new Vector2(0.5f,0.5f),new Vector2(0.5f,0.5f),Vector2.zero,new Vector2(820,540)); card.AddComponent<Image>().color=new Color(0.12f,0.16f,0.24f,0.98f);
            var t=MakeTMP(card.transform,"Title",title,24,TextAlignmentOptions.Center,new Color(1f,0.85f,0.25f)); SetRect(t.gameObject,new Vector2(0,1),new Vector2(1,1),new Vector2(0.5f,1),new Vector2(0,-28),new Vector2(-40,52));
            var b=MakeTMP(card.transform,"Body",body,17,TextAlignmentOptions.TopLeft,new Color(0.92f,0.95f,1f)); SetRect(b.gameObject,new Vector2(0,0),new Vector2(1,1),new Vector2(0.5f,0.5f),new Vector2(0,30),new Vector2(-80,-180));
            var bAct=MakeBtn(card.transform,"BtnAction",btnTxt,new Vector2(-120,-205),new Vector2(320,56),new Color(0.18f,0.75f,0.45f));
            var bCls=MakeBtn(card.transform,"BtnClose","ĐÓNG [ESC]",new Vector2(185,-205),new Vector2(220,56),new Color(0.85f,0.28f,0.25f));
            if (act != null) bAct.onClick.AddListener(act);
            bCls.onClick.AddListener(() => p.SetActive(false));
            p.SetActive(false); return p;
        }

        private static GameObject MakeAIPanel(Transform parent, string name)
        {
            var p=CreateUI(name,parent); SetFull(p.GetComponent<RectTransform>()); p.AddComponent<Image>().color=new Color(0,0,0,0.7f);
            var card=CreateUI("Card",p.transform); SetRect(card,new Vector2(0.5f,0.5f),new Vector2(0.5f,0.5f),new Vector2(0.5f,0.5f),Vector2.zero,new Vector2(860,580)); card.AddComponent<Image>().color=new Color(0.1f,0.14f,0.22f,0.98f);
            var tT=MakeTMP(card.transform,"Title","GIA SƯ AI THẦY MINH",26,TextAlignmentOptions.Center,new Color(0.35f,0.85f,1f)); SetRect(tT.gameObject,new Vector2(0,1),new Vector2(1,1),new Vector2(0.5f,1),new Vector2(0,-22),new Vector2(-40,50));
            var scroll=CreateUI("ChatScroll",card.transform); SetRect(scroll,new Vector2(0,0),new Vector2(1,1),new Vector2(0.5f,0.5f),new Vector2(0,35),new Vector2(-60,-175)); scroll.AddComponent<Image>().color=new Color(0.06f,0.09f,0.15f,0.95f);
            var chatTxt=MakeTMP(scroll.transform,"ChatHistory",
                "<color=#5dade2><b>Thầy Minh:</b></color> Chào em! Thầy đồng hành cùng em trên Làng Sen.\nChọn chủ đề bên dưới để hỏi thầy nhé!",
                16,TextAlignmentOptions.TopLeft,Color.white);
            chatTxt.rectTransform.anchorMin=Vector2.zero; chatTxt.rectTransform.anchorMax=Vector2.one;
            chatTxt.rectTransform.offsetMin=new Vector2(8,8); chatTxt.rectTransform.offsetMax=new Vector2(-8,-8);
            MakeBtn(card.transform,"BtnAsk1","[?] Phân Số",   new Vector2(-265,-225),new Vector2(210,48),new Color(0.2f,0.55f,0.85f));
            MakeBtn(card.transform,"BtnAsk2","[123] Số Nguyên", new Vector2(-35,-225), new Vector2(210,48),new Color(0.2f,0.55f,0.85f));
            var bC=MakeBtn(card.transform,"BtnClose","ĐÓNG [ESC]",new Vector2(230,-225),new Vector2(220,48),new Color(0.85f,0.28f,0.25f));
            bC.onClick.AddListener(()=>p.SetActive(false));
            p.SetActive(false); return p;
        }
        private static GameObject CreateUI(string name, Transform parent) { var go=new GameObject(name,typeof(RectTransform)); go.transform.SetParent(parent,false); return go; }
        private static TextMeshProUGUI MakeTMP(Transform parent, string name, string text, float size, TextAlignmentOptions align, Color color)
        { var go=new GameObject(name,typeof(RectTransform)); go.transform.SetParent(parent,false); var t=go.AddComponent<TextMeshProUGUI>(); t.text=text; t.fontSize=size; t.alignment=align; t.color=color; t.raycastTarget=false; return t; }
        private static Button MakeBtn(Transform parent, string name, string text, Vector2 pos, Vector2 size, Color bg)
        { var go=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(Button)); go.transform.SetParent(parent,false); var rt=go.GetComponent<RectTransform>(); rt.anchoredPosition=pos; rt.sizeDelta=size; var img=go.GetComponent<Image>(); img.color=bg; img.raycastTarget=true; var btn=go.GetComponent<Button>(); var c=btn.colors; c.highlightedColor=bg*1.15f; c.pressedColor=bg*0.85f; btn.colors=c; var lbl=MakeTMP(go.transform,"Text",text,18,TextAlignmentOptions.Center,Color.white); SetFull(lbl.GetComponent<RectTransform>()); return btn; }
        private static void SetRect(GameObject go, Vector2 aMin, Vector2 aMax, Vector2 piv, Vector2 pos, Vector2 size) { var rt=go.GetComponent<RectTransform>(); rt.anchorMin=aMin; rt.anchorMax=aMax; rt.pivot=piv; rt.anchoredPosition=pos; rt.sizeDelta=size; }
        private static void SetFull(RectTransform rt) { rt.anchorMin=Vector2.zero; rt.anchorMax=Vector2.one; rt.offsetMin=Vector2.zero; rt.offsetMax=Vector2.zero; }
        private static void SetPrivateField(object target, string field, object value) { if(target==null) return; target.GetType().GetField(field,System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)?.SetValue(target,value); }
        private static void RegisterSceneInBuildSettings(string path) { var cur=EditorBuildSettings.scenes; foreach(var s in cur) if(s.path==path) return; var arr=new EditorBuildSettingsScene[cur.Length+1]; for(int i=0;i<cur.Length;i++) arr[i]=cur[i]; arr[cur.Length]=new EditorBuildSettingsScene(path,true); EditorBuildSettings.scenes=arr; }

        // ==============================================================
        //  TRANG TRI MOI TRUONG: cay coi, hang rao, duong mon, da cuoi
        // ==============================================================
        private static void PopulateVillageEnvironment()
        {
            var envRoot = new GameObject("VillageEnvironment");
            float groundY = -4f; // san co tai y=-4

            // --- PATH SHORTCUTS ---
            const string BUSH_BASE  = "Assets/VertexRageStudio/LowPolyNatureEssentials/Prefabs/Env/Bushes/";
            const string TREE_BASE  = "Assets/VertexRageStudio/LowPolyNatureEssentials/Prefabs/Env/Trees/";
            const string ROCK_BASE  = "Assets/VertexRageStudio/LowPolyNatureEssentials/Prefabs/Env/Rocks/";
            const string FENCE_BASE = "Assets/VertexRageStudio/LowPolyNatureEssentials/Prefabs/Arch/Fence/SimWoodFnc/SWFTilesSimple/";

            // -------------------------------------------------------
            // 1. HANG RAO GO 2 BEN DUONG MON
            //    Duong mon tu (0, groundY, -22) den (0, groundY, -12)
            //    Hang rao dat cach duong 3m moi ben, cach nhau 2.5m
            // -------------------------------------------------------
            var fenceRoot = new GameObject("Fences");
            fenceRoot.transform.SetParent(envRoot.transform, false);

            string[] fencePrefabs = {
                FENCE_BASE + "SWFSimpleTilingLongA.prefab",
                FENCE_BASE + "SWFSimpleTilingLongB.prefab",
                FENCE_BASE + "SWFSimpleTilingLongC.prefab",
            };

            // Hang rao trai (x=-3.5) va phai (x=+3.5), chay tu z=-22 den z=-10
            for (int side = -1; side <= 1; side += 2)
            {
                float fx = side * 3.5f;
                for (float fz = -22f; fz < -10f; fz += 2.5f)
                {
                    int fi = UnityEngine.Random.Range(0, fencePrefabs.Length);
                    var fence = TryInstantiate(fencePrefabs[fi], fenceRoot.transform,
                        new Vector3(fx, groundY, fz),
                        Quaternion.Euler(0f, 90f, 0f), // xoay doc theo Z
                        Vector3.one * 0.8f);
                }
            }

            // -------------------------------------------------------
            // 2. DA LAT DUONG MON (flat rocks lam dau thach)
            //    Dat theo duong thang tu z=-22 den z=-12, gian cach 1.5m
            // -------------------------------------------------------
            var pathRoot = new GameObject("PathStones");
            pathRoot.transform.SetParent(envRoot.transform, false);

            string[] pathStonePrefabs = {
                ROCK_BASE + "RockSmallBFlat.prefab",
                ROCK_BASE + "RockSmallCFlat.prefab",
                ROCK_BASE + "RockSmallA.prefab",
            };

            var rng = new System.Random(42);
            for (float pz = -21f; pz < -12f; pz += 1.5f)
            {
                float px = (float)(rng.NextDouble() - 0.5) * 1.2f; // lech nhe
                int pi = rng.Next(pathStonePrefabs.Length);
                float rotY = (float)(rng.NextDouble() * 360f);
                TryInstantiate(pathStonePrefabs[pi], pathRoot.transform,
                    new Vector3(px, groundY, pz),
                    Quaternion.Euler(0f, rotY, 0f),
                    Vector3.one * (0.6f + (float)rng.NextDouble() * 0.5f));
            }

            // -------------------------------------------------------
            // 3. CAY CO - VONG 1: HANG COC BEN PHAI VA TRAI DUONG MON
            //    Cay lua bo Willow goc doc truoc cua, xa 7-9m
            // -------------------------------------------------------
            var treeRoot = new GameObject("Trees");
            treeRoot.transform.SetParent(envRoot.transform, false);

            string[] willowPrefabs = {
                TREE_BASE + "Willows/WillowTreeAdultA.prefab",
                TREE_BASE + "Willows/WillowTreeAdultB.prefab",
                TREE_BASE + "Willows/WillowTreeMatureA.prefab",
                TREE_BASE + "Willows/WillowTreeYoungA.prefab",
            };

            // Hang liu 2 ben duong: x=+-6 den +-12, z=-20 den -12
            float[] willowX  = { -6f, -9f, -12f,  6f,  9f, 12f };
            float[] willowZ  = { -20f, -17f, -14f, -20f, -17f, -14f };
            float[] willowSc = { 1.4f, 1.7f, 1.3f, 1.6f, 1.5f, 1.8f };

            for (int i = 0; i < willowX.Length; i++)
            {
                int wi = (int)(rng.NextDouble() * willowPrefabs.Length);
                float wr = (float)(rng.NextDouble() * 360f);
                TryInstantiate(willowPrefabs[wi], treeRoot.transform,
                    new Vector3(willowX[i], groundY, willowZ[i]),
                    Quaternion.Euler(0f, wr, 0f),
                    Vector3.one * willowSc[i]);
            }

            // -------------------------------------------------------
            // 4. CAY CO - VONG 2: VONG CUNG XA XU QUANH DIORAMA
            //    Dat theo vong ban kinh 30-38m quanh goc toa do (0,0,0)
            //    Nhung chi o phia Bac (z>0) va 2 canh Dong Tay
            // -------------------------------------------------------
            string[] forestPrefabs = {
                TREE_BASE + "OakTrees/NormalOaks/OakTreeAdultA.prefab",
                TREE_BASE + "OakTrees/NormalOaks/OakTreeAdultB.prefab",
                TREE_BASE + "OakTrees/NormalOaks/OakTreeMatureA.prefab",
                TREE_BASE + "BirchTrees/NormalBirches/BirchTreeAdultA.prefab",
                TREE_BASE + "BirchTrees/NormalBirches/BirchTreeAdultB.prefab",
                TREE_BASE + "BirchTrees/NormalBirches/BirchTreeMatureA.prefab",
                TREE_BASE + "Willows/WillowTreeAdultA.prefab",
            };

            int forestCount = 24;
            for (int i = 0; i < forestCount; i++)
            {
                double angle = i * (360.0 / forestCount) + rng.NextDouble() * 15.0;
                double rad = angle * Math.PI / 180.0;
                float dist  = 30f + (float)(rng.NextDouble() * 8f);
                float tx    = (float)(Math.Sin(rad) * dist);
                float tz    = (float)(Math.Cos(rad) * dist);

                // Bo qua khu vuc phia Nam (z<-18) de khong che duong mon
                if (tz < -18f) continue;

                int ti = (int)(rng.NextDouble() * forestPrefabs.Length);
                float tr = (float)(rng.NextDouble() * 360f);
                float ts = 1.5f + (float)(rng.NextDouble() * 0.8f);

                TryInstantiate(forestPrefabs[ti], treeRoot.transform,
                    new Vector3(tx, groundY, tz),
                    Quaternion.Euler(0f, tr, 0f),
                    Vector3.one * ts);
            }

            // -------------------------------------------------------
            // 5. BUI CAY (Bush / Shrub) ven duong va quanh diorama
            // -------------------------------------------------------
            var bushRoot = new GameObject("Bushes");
            bushRoot.transform.SetParent(envRoot.transform, false);

            string[] bushPrefabs = {
                BUSH_BASE + "Bush1.prefab",
                BUSH_BASE + "Bush3.prefab",
                BUSH_BASE + "BushA.prefab",
                BUSH_BASE + "BushB.prefab",
                BUSH_BASE + "BushClusterA.prefab",
                BUSH_BASE + "BushClusterB.prefab",
                BUSH_BASE + "Shrubs/ShrubA.prefab",
            };

            // Bui ven 2 ben duong mo vao (x=+-5 den +-8, z=-20 den -12)
            for (int i = 0; i < 10; i++)
            {
                int side = (i % 2 == 0) ? -1 : 1;
                float bx = side * (5f + (float)(rng.NextDouble() * 3f));
                float bz = -20f + (float)(rng.NextDouble() * 8f);
                int bi = (int)(rng.NextDouble() * bushPrefabs.Length);
                float bs = 0.6f + (float)(rng.NextDouble() * 0.5f);
                float br = (float)(rng.NextDouble() * 360f);
                TryInstantiate(bushPrefabs[bi], bushRoot.transform,
                    new Vector3(bx, groundY, bz),
                    Quaternion.Euler(0f, br, 0f),
                    Vector3.one * bs);
            }

            // Bui phan tan quanh diorama (radius 15-25m)
            for (int i = 0; i < 12; i++)
            {
                double angle = rng.NextDouble() * 360.0;
                double rad = angle * Math.PI / 180.0;
                float dist = 15f + (float)(rng.NextDouble() * 10f);
                float bx = (float)(Math.Sin(rad) * dist);
                float bz = (float)(Math.Cos(rad) * dist);
                if (bz < -12f) continue; // tranh duong mon
                int bi = (int)(rng.NextDouble() * bushPrefabs.Length);
                float bs = 0.7f + (float)(rng.NextDouble() * 0.4f);
                TryInstantiate(bushPrefabs[bi], bushRoot.transform,
                    new Vector3(bx, groundY, bz),
                    Quaternion.Euler(0f, (float)(rng.NextDouble()*360f), 0f),
                    Vector3.one * bs);
            }

            // -------------------------------------------------------
            // 6. DA (Rocks) phan tan tu nhien
            // -------------------------------------------------------
            var rockRoot = new GameObject("Rocks");
            rockRoot.transform.SetParent(envRoot.transform, false);

            string[] rockPrefabs = {
                ROCK_BASE + "RockNormA.prefab",
                ROCK_BASE + "RockNormB.prefab",
                ROCK_BASE + "RockBigA.prefab",
                ROCK_BASE + "RockPiles/RockPilesPebblesA.prefab",
                ROCK_BASE + "RockPiles/RockPilesBigA.prefab",
            };

            for (int i = 0; i < 10; i++)
            {
                double angle = rng.NextDouble() * 360.0;
                double rad = angle * Math.PI / 180.0;
                float dist = 18f + (float)(rng.NextDouble() * 15f);
                float rx = (float)(Math.Sin(rad) * dist);
                float rz = (float)(Math.Cos(rad) * dist);
                if (rz < -14f) continue;
                int ri = (int)(rng.NextDouble() * rockPrefabs.Length);
                float rs = 0.5f + (float)(rng.NextDouble() * 0.6f);
                TryInstantiate(rockPrefabs[ri], rockRoot.transform,
                    new Vector3(rx, groundY, rz),
                    Quaternion.Euler(0f, (float)(rng.NextDouble()*360f), 0f),
                    Vector3.one * rs);
            }

            Debug.Log("[VillageWorldBuilder] PopulateVillageEnvironment: cay coi, hang rao, da lat duong xong.");
        }

    }
}
#endif

