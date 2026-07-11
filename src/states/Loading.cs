namespace MatchUp.states;

public class LoadingState : BaseState
{
    public override void Enter(GameState oldState)
    {
        Console.WriteLine("Switched to Loading state");

        // Run file I/O on background thread to avoid blocking game thread
        _ = Task.Run(() =>
        {
            MatchConfig.LoadMaps();
            MatchConfig.LoadSettings();
        });
    }

    public override void Leave() { }

    public override void OnMapStart()
{
    Utils.DelayedCall(TimeSpan.FromSeconds(1), () => {
        // Only transition if still in Loading — config may have already moved us past this
        if (StateMachine.GetCurrentGameState() != GameState.Loading) return;

        StateMachine.SwitchState(
            PelipajaConfig.Mode == "pelipaja" ? GameState.PelipajaWaiting : GameState.Setup
        );
    });
}
}