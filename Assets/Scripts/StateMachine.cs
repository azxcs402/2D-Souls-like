public class StateMachine
{
    public EntityState CurrentState { get; private set; }

    public void Initialize(EntityState startState)
    {
        if (startState == null)
        {
            return;
        }

        CurrentState = startState;
        CurrentState.Enter();
    }

    public void ChangeState(EntityState newState)
    {
        if (newState == null || newState == CurrentState)
        {
            return;
        }

        CurrentState?.Exit();

        CurrentState = newState;

        CurrentState.Enter();
    }
}
