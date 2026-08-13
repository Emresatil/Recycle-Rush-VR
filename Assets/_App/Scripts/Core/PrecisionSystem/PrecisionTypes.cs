using UnityEngine;

namespace RecycleRush.Core.PrecisionSystem
{
    public enum PrecisionTier
    {
        Normal, // 0-59
        Good,   // 60-79
        Great,  // 80-94
        Perfect // 95-100
    }

    [System.Serializable]
    public struct PrecisionResult
    {
        public WasteType WasteType;
        public WasteType TargetBinType; // YENİ: Hangi kutuya atıldığı (Event standardizasyonu)
        public PrecisionTier Tier;
        public float Score; // 0-100 arası (100 = Tam merkez)
        public float Distance; // Fiziksel mesafe (metre)
        public float NormalizedDistance; // 0.0 (Merkez) ile 1.0 (Kenar) arası
        
        public int BonusScore;
        public int BonusXP;
        public int BonusCoin;
        
        public Vector3 HitPoint; // Görsel efektler ve Floating Text için gerekli olan fiziksel temas noktası
    }
}
