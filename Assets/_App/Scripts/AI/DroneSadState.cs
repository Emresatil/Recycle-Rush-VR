using UnityEngine;

namespace RecycleRush.AI
{
    public class DroneSadState : IState
    {
        private QCDroneController _drone;
        private StateMachine _stateMachine;
        private float _timer;

        public DroneSadState(QCDroneController drone, StateMachine stateMachine)
        {
            _drone = drone;
            _stateMachine = stateMachine;
        }

        public void Enter()
        {
            Debug.Log("[Drone FSM] Üzgün (Sad) Durumuna Geçildi!");
            _drone.ChangeColor(_drone.sadColor);
            _timer = 0f;
        }

        public void Update()
        {
            _timer += Time.deltaTime;

            // Üzüntüden kafayı öne eğme (boynunu bükme) mimiği
            float bowAngle = Mathf.Lerp(0, 45f, _timer * 2f); // Yavaşça 45 derece öne eğil
            _drone.FacePlayer(new Vector3(bowAngle, 0, 0));

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
