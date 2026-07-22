using UnityEngine;

namespace RecycleRush.AI
{
    public class DroneIdleState : IState
    {
        private QCDroneController _drone;
        private StateMachine _stateMachine;
        private float _bobTimer;

        public DroneIdleState(QCDroneController drone, StateMachine stateMachine)
        {
            _drone = drone;
            _stateMachine = stateMachine;
        }

        public void Enter()
        {
            Debug.Log("[Drone FSM] Bekleme (Idle) Durumuna Geçildi.");
            _drone.ChangeColor(_drone.normalColor);
            _bobTimer = 0f;
        }

        public void Update()
        {
            // Bekleme modunda yavaşça yukarı aşağı kafa sallar (mimik)
            _bobTimer += Time.deltaTime * 2f;
            float tilt = Mathf.Sin(_bobTimer) * 10f; // 10 derece kafa sallama
            
            // Dronun ana rotasyonunu oyuncuya çevir, üstüne de kafa sallamayı ekle
            _drone.FacePlayer(new Vector3(tilt, 0, 0));
        }

        public void Exit()
        {
            // Çıkarken özel bir şey yapmaya gerek yok
        }
    }
}
