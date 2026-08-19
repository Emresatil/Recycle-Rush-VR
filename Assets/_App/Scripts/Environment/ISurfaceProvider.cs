using System.Collections.Generic;
using UnityEngine;

namespace RecycleRush.Environment
{
    public enum SurfaceType
    {
        Floor,
        Table, // Yüksek zeminler (Masa, Sehpa vb.)
        Wall,
        Any
    }

    public struct SurfaceData
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public SurfaceType Type;
        public Vector3 BoundsSize; // Yüzeyin ne kadar büyük olduğu
    }

    /// <summary>
    /// Gerçek AR (Quest) veya Sahte (Editor) yüzey sağlayıcılarının ortak iletişim dili.
    /// WasteSpawner çöpleri üretirken bu arayüze başvurur.
    /// </summary>
    public interface ISurfaceProvider
    {
        /// <summary>
        /// İstenen türdeki (Zemin, Masa vb.) bir yüzeyden rastgele bir nokta verir.
        /// </summary>
        bool TryGetRandomSurfacePoint(SurfaceType surfaceType, out SurfaceData surfaceData);
        
        /// <summary>
        /// Sistemde tanımlı tüm yüzeylerin listesini döndürür.
        /// </summary>
        List<SurfaceData> GetAllSurfaces();
    }
}
