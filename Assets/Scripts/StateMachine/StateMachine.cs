public class StateMachine
{
    public IState CurrentState { get; private set; }

    public void Initialize(IState startState)
    {
        if (startState == null)
        {
            return;
        }

        CurrentState = startState;
        CurrentState.Enter();
    }

    public void ChangeState(IState newState)
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
