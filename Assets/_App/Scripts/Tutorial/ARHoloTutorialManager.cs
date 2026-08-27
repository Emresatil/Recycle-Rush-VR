using UnityEngine;
using System;

namespace RecycleRush.Tutorial
{
    public enum TutorialStep
    {
        None,
        Placement,          // Adım 1: Kutuları Yerleştirme
        GrabAndDrop,        // Adım 2: İlk Çöpü Tut ve At
        GravityPull,        // Adım 3: Uzaktan Çekme
        CompositeWaste,     // Adım 4: Yapışık Çöpleri Ayırma
        Completed           // Adım 5: Eğitim Tamamlandı
    }

    public class ARHoloTutorialManager : MonoBehaviour
    {
        public static ARHoloTutorialManager Instance { get; private set; }

        public TutorialStep CurrentStep { get; private set; } = TutorialStep.None;

        public static event Action<TutorialStep> OnTutorialStepStarted;
        public static event Action<TutorialStep> OnTutorialStepCompleted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // Eğitim tamamlanmış mı kontrol et
            if (PlayerPrefs.GetInt(""TutorialDone"", 0) == 1)
            {
                CurrentStep = TutorialStep.Completed;
            }
        }

        public void StartTutorial()
        {
            if (CurrentStep != TutorialStep.Completed)
            {
                SetStep(TutorialStep.Placement);
            }
        }

        private void SetStep(TutorialStep step)
        {
            CurrentStep = step;
            OnTutorialStepStarted?.Invoke(CurrentStep);
            Debug.Log($""<color=cyan>[Tutorial]</color> Adım Başladı: {CurrentStep}"");
        }

        public void CompleteCurrentStep()
        {
            OnTutorialStepCompleted?.Invoke(CurrentStep);
            Debug.Log($""<color=green>[Tutorial]</color> Adım Tamamlandı: {CurrentStep}"");

            // Bir sonraki adıma geç
            if (CurrentStep < TutorialStep.Completed)
            {
                SetStep(CurrentStep + 1);
            }
            
            if (CurrentStep == TutorialStep.Completed)
            {
                PlayerPrefs.SetInt(""TutorialDone"", 1);
                PlayerPrefs.Save();
                Debug.Log(""<color=yellow>[Tutorial]</color> Eğitim tamamen bitirildi!"");
            }
        }
    }
}
