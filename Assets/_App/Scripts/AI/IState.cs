namespace RecycleRush.AI
{
    /// <summary>
    /// FSM (Sonlu Durum Makinesi) içindeki her durumun uygulaması gereken arayüz.
    /// </summary>
    public interface IState
    {
        void Enter();
        void Update();
        void Exit();
    }
}
