using UnityEngine;

namespace RecycleRush.AI
{
    public class DroneHappyState : IState
    {
        private QCDroneController _drone;
        private StateMachine _stateMachine;
        private float _timer;
        private float _danceTimer;

        public DroneHappyState(QCDroneController drone, StateMachine stateMachine)
        {
            _drone = drone;
            _stateMachine = stateMachine;
        }

        public void Enter()
        {
            Debug.Log("[Drone FSM] Mutlu (Happy) Durumuna Geçildi!");
            _drone.ChangeColor(_drone.happyColor);
            _timer = 0f;
            _danceTimer = 0f;
        }

        public void Update()
        {
            _timer += Time.deltaTime;
            _danceTimer += Time.deltaTime * 15f; // Hızlı sevinme hareketi

            // Sevinçten hızlıca sağa sola sallanma mimiği
            float spin = Mathf.Sin(_danceTimer) * 30f; 
            _drone.FacePlayer(new Vector3(0, 0, spin));

            // 2 Saniye sonra normale dön
            if (_timer >= 2f)
            {
                _stateMachine.ChangeState(_drone.IdleState);
            }
        }

        public void Exit()
        {
            // Çıkış
        }
    }
}
