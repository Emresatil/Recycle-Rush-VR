using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.XR.ARFoundation;
using UnityEngine.InputSystem.XR;
using Unity.XR.CoreUtils;
using UnityEngine.SceneManagement;

namespace RecycleRush.Editor
{
    /// <summary>
    /// Editor utility for setting up AR environments and optimizing assets for Mixed Reality.
    /// </summary>
    public class ARSetupTool : EditorWindow
    {
        private const string AR_SCENE_NAME = "AR_SceneUnderstanding_Test";
        private const string URP_SIMPLE_LIT_SHADER = "Universal Render Pipeline/Simple Lit";
        private const string PREFABS_ENVIRONMENT_PATH = "Assets/_App/Prefabs/RecyclingBins";

        [MenuItem("Recycle Rush AR/1. Create Scene Understanding Test Scene")]
        public static void CreateARScene()
        {
            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            newScene.name = AR_SCENE_NAME;

            CreateARSession();
            XROrigin xrOrigin = CreateXROrigin();
            AddSceneUnderstanding(xrOrigin.gameObject);
            CreateDefaultLighting();

            Debug.Log($"<color=green><b>[Recycle Rush AR]</b></color> {AR_SCENE_NAME} successfully created! Please save it via File -> Save As.");
        }

        [MenuItem("Recycle Rush AR/2. Optimize Bins for AR Passthrough")]
        public static void OptimizeBinsForAR()
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { PREFABS_ENVIRONMENT_PATH });
            int updatedCount = 0;
            Shader targetShader = Shader.Find(URP_SIMPLE_LIT_SHADER);

            if (targetShader == null)
            {
                Debug.LogError($"Shader '{URP_SIMPLE_LIT_SHADER}' not found! Are you missing URP?");
                return;
            }

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (IsBinPrefab(path) && OptimizePrefabMaterials(path, targetShader))
                {
                    updatedCount++;
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"<color=green><b>[Recycle Rush AR]</b></color> {updatedCount} Bin prefabs optimized for AR Passthrough!");
        }

        [MenuItem("Recycle Rush AR/3. Add Blob Shadows to Bins (AR Polish)")]
        public static void AddBlobShadowsToBins()
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { PREFABS_ENVIRONMENT_PATH });
            int updatedCount = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (IsBinPrefab(path) && AddShadowPlaneToPrefab(path))
                {
                    updatedCount++;
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"<color=green><b>[Recycle Rush AR]</b></color> {updatedCount} Bin prefabs received AR Blob Shadows!");
        }

        #region Helper Methods - Scene Setup

        private static void CreateARSession()
        {
            GameObject arSessionObj = new GameObject("AR Session");
            arSessionObj.AddComponent<ARSession>();
            arSessionObj.AddComponent<ARInputManager>();
        }

        private static XROrigin CreateXROrigin()
        {
            GameObject xrOriginObj = new GameObject("XR Origin (AR)");
            XROrigin xrOrigin = xrOriginObj.AddComponent<XROrigin>();
            
            GameObject cameraOffset = new GameObject("Camera Offset");
            cameraOffset.transform.SetParent(xrOriginObj.transform);
            xrOrigin.CameraFloorOffsetObject = cameraOffset;

            GameObject mainCamera = new GameObject("Main Camera");
            mainCamera.transform.SetParent(cameraOffset.transform);
            mainCamera.tag = "MainCamera";
            
            Camera cam = mainCamera.AddComponent<Camera>();
            mainCamera.AddComponent<AudioListener>();
            
            ConfigurePassthroughCamera(cam);
            mainCamera.AddComponent<TrackedPoseDriver>();
            mainCamera.AddComponent<ARCameraManager>();
            mainCamera.AddComponent<ARCameraBackground>();

            xrOrigin.Camera = cam;
            return xrOrigin;
        }

        private static void ConfigurePassthroughCamera(Camera cam)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0, 0, 0, 0); // Passthrough requires transparent/black background
        }

        private static void AddSceneUnderstanding(GameObject originObj)
        {
            ARPlaneManager planeManager = originObj.AddComponent<ARPlaneManager>();
            planeManager.requestedDetectionMode = UnityEngine.XR.ARSubsystems.PlaneDetectionMode.Horizontal | UnityEngine.XR.ARSubsystems.PlaneDetectionMode.Vertical;
            
            originObj.AddComponent<ARPointCloudManager>();
        }

        private static void CreateDefaultLighting()
        {
            GameObject lightObj = new GameObject("Directional Light");
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);
        }

        #endregion

        #region Helper Methods - Asset Optimization

        private static bool IsBinPrefab(string path)
        {
            return path.Contains("Bin") || path.Contains("Box");
        }

        private static bool OptimizePrefabMaterials(string path, Shader targetShader)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) return false;

            bool wasModified = false;
            Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer rend in renderers)
            {
                foreach (Material mat in rend.sharedMaterials)
                {
                    if (mat != null && mat.shader != targetShader)
                    {
                        string matPath = AssetDatabase.GetAssetPath(mat);
                        // Unity'nin dahili veya paket materyallerini değiştirmekten kaçın (Sarı uyarı çözümlemesi)
                        if (string.IsNullOrEmpty(matPath) || matPath.StartsWith("Packages/") || matPath.StartsWith("Resources/unity_builtin_extra"))
                        {
                            continue;
                        }

                        mat.shader = targetShader;
                        EditorUtility.SetDirty(mat);
                        wasModified = true;
                    }
                }
            }
            return wasModified;
        }

        private static bool AddShadowPlaneToPrefab(string path)
        {
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefabAsset == null) return false;

            // Gölge zaten var mı diye önce diskteki aset üzerinde kontrol et
            Transform existingShadow = prefabAsset.transform.Find("AR_BlobShadow");
            if (existingShadow != null) return false;

            // Prefab'ı sahneye geçici olarak çıkart (Unity güvenlik kuralı)
            GameObject instantiatedPrefab = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset);
            if (instantiatedPrefab == null) return false;

            try
            {
                CreateAndAttachShadow(instantiatedPrefab.transform);
                
                // Değişiklikleri diske kaydet
                PrefabUtility.SaveAsPrefabAsset(instantiatedPrefab, path);
                return true;
            }
            finally
            {
                // Sahnedeki geçici kopyayı sil
                DestroyImmediate(instantiatedPrefab);
            }
        }

        private static void CreateAndAttachShadow(Transform parentTransform)
        {
            GameObject shadowObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            shadowObj.name = "AR_BlobShadow";
            
            Material shadowMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            shadowMat.color = new Color(0, 0, 0, 0.4f);
            
            shadowMat.SetFloat("_Surface", 1); 
            shadowMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            shadowMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            shadowMat.SetInt("_ZWrite", 0);
            shadowMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

            shadowObj.GetComponent<Renderer>().sharedMaterial = shadowMat;
            DestroyImmediate(shadowObj.GetComponent<Collider>(), true);

            shadowObj.transform.SetParent(parentTransform);
            shadowObj.transform.localPosition = new Vector3(0, 0.01f, 0);
            shadowObj.transform.localRotation = Quaternion.Euler(90, 0, 0);
            shadowObj.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
        }

        #endregion
    }
}
