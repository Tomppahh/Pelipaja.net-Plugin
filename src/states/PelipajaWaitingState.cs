using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace MatchUp.states;

public class PelipajaWaitingState : BaseState
{
    public override void Enter(GameState oldState)
    {
        Console.WriteLine("[Pelipaja] Waiting for config from Next.js...");

        // Workshop map loaded — config was already received and StartMatch() issued host_workshop_map.
        // We ended up here because LoadingState.OnMapStart() transitions to PelipajaWaiting for pelipaja mode.
        // Since the map is already loaded and config is set, go straight to ReadyUp.
        if (PelipajaConfig.Mode == "pelipaja" && !string.IsNullOrEmpty(PelipajaConfig.MatchId))
        {
            Console.WriteLine("[Pelipaja] Config already received, transitioning to ReadyUp");
            Utils.DelayedCall(TimeSpan.FromSeconds(1), () =>
            {
                StateMachine.SwitchState(GameState.ReadyUp);
            });
        }
    }

    public override void Leave() { }
}