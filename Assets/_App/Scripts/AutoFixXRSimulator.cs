using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using System.Reflection;

public class AutoFixXRSimulator : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        var go = new GameObject("XR_Simulator_Fixer");
        DontDestroyOnLoad(go);
        go.AddComponent<AutoFixXRSimulator>();
    }

    private System.Collections.IEnumerator Start()
    {
        int maxAttempts = 10;
        int attempts = 0;

        while (attempts < maxAttempts)
        {
            var sim = Object.FindAnyObjectByType<XRDeviceSimulator>();
            if (sim != null)
            {
                var cam = GameObject.FindWithTag("MainCamera");
                if (cam != null)
                {
                    FieldInfo field = typeof(XRDeviceSimulator).GetField("m_CameraTransform", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (field != null)
                    {
                        Transform currentVal = field.GetValue(sim) as Transform;
                        if (currentVal == null)
                        {
                            field.SetValue(sim, cam.transform);
                            Debug.Log("<b><color=green>[Tamir Robotu]</color></b> Reflection ile XR Device Simulator kamerası başarıyla bağlandı!");
                            Destroy(gameObject);
                            yield break;
                        }
                    }
                    else
                    {
                        if (sim.cameraTransform == null)
                        {
                            sim.cameraTransform = cam.transform;
                            Debug.Log("<b><color=green>[Tamir Robotu]</color></b> XR Device Simulator kamerası başarıyla bağlandı!");
                            Destroy(gameObject);
                            yield break;
                        }
                    }
                }
            }
            
            attempts++;
            yield return new WaitForSeconds(1f);
        }

        // Eğer 10 saniye içinde XR Simulator bulunamadıysa, (muhtemelen gerçek cihazdayız) kendi kendini yok et.
        Destroy(gameObject);
    }
}
