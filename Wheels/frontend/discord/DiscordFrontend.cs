using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WheelsGodot.discord
{
    public class DiscordFrontend : WheelsFrontend<DiscordFrontendPlayer>
    {
        public List<string> CombinedLog => PhaseLogs.SelectMany(x => x.Logs).Concat(Log).ToList();

        public List<string> Log { get; set; } = new();

        public List<(string Phase, List<string> Logs)> PhaseLogs { get; } = new();

        private string lastPhase;

        public override void StartPhase(string name) {
            base.StartPhase(name);
            EndPhase();
            lastPhase = name;
        }

        public override void EndGame(WheelsFrontendPlayer winner) {
            EndPhase();
            base.EndGame(winner);
        }

        private void EndPhase() {
            if (lastPhase != null && Log.Count > 0) {
                PhaseLogs.Add((lastPhase, new(Log)));
                Log.Clear();
            }
        }

        public override void EndRound() {
            base.EndRound();
            EndPhase();
        }

        public override void SetPlayerFrontend(Player player, DiscordFrontendPlayer frontend) {
            frontend.RunningLog = Log;
            base.SetPlayerFrontend(player, frontend);
        }
    }
}
