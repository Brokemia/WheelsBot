using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WheelsGodot {
    public abstract class WheelsFrontend {
        public bool GameOver { get; private set; }

        public WheelsFrontendPlayer Winner { get; private set; }

        public abstract IReadOnlyDictionary<Player, WheelsFrontendPlayer> Players { get; }

        public virtual void StartPhase(string name) {
            
        }

        public virtual void EndRound() {
            
        }

        public virtual void EndGame(WheelsFrontendPlayer winner) {
            GameOver = true;
            Winner = winner;
        }

        public virtual void EndGame(Player winner) {
            EndGame(Players[winner]);
        }
    }

    public class WheelsFrontend<T> : WheelsFrontend where T : WheelsFrontendPlayer {
        protected Dictionary<Player, WheelsFrontendPlayer> players = new();
        
        public override IReadOnlyDictionary<Player, WheelsFrontendPlayer> Players => players;

        public virtual void SetPlayerFrontend(Player player, T frontend) {
            players[player] = frontend;
        }
    }
}
