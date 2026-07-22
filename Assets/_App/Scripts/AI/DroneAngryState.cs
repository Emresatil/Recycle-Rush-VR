using UnityEngine;

namespace RecycleRush.AI
{
    public class DroneAngryState : IState
    {
        private QCDroneController _drone;
        private StateMachine _stateMachine;
        private float _timer;
        private float _shakeTimer;

        public DroneAngryState(QCDroneController drone, StateMachine stateMachine)
        {
            _drone = drone;
            _stateMachine = stateMachine;
        }

        public void Enter()
        {
            Debug.Log("[Drone FSM] Kızgın (Angry) Durumuna Geçildi!");
            _drone.ChangeColor(_drone.angryColor);
            _timer = 0f;
            _shakeTimer = 0f;
        }

        public void Update()
        {
            _timer += Time.deltaTime;
            _shakeTimer += Time.deltaTime * 30f; // Titreme hızı

            // Sinirden titreme mimiği
            float shakeX = Mathf.Sin(_shakeTimer) * 15f; 
            float shakeZ = Mathf.Cos(_shakeTimer) * 15f;
            _drone.FacePlayer(new Vector3(shakeX, 0, shakeZ));

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
