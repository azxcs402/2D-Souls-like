public interface ICounterable
{
    bool IsCounterWindowActive { get; }
    void EnableCounterWindow();
    void DisableCounterWindow();
    bool TryCounter();
}
