using Discord.WebSocket;

namespace WheelsGodot.discord {
    public class DiscordMatch {
        public Board Board { get; set; }

        public SocketUser SelfUser { get; set; }

        public SocketUser EnemyUser { get; set; }

        public Player Self { get; }
        
        public Player Enemy { get; }

        public bool SelfIsPlayer1 { get; set; }

        // Where to follow-up after the other player is ready
        public SocketInteraction LastInteraction { get; set; }

        // Whether the opponent is waiting on us
        public bool OpponentReady { get; set; }
        
        // Differentiates pending challenges from actual games
        public bool AcceptedInvite { get; set; }

        public DiscordMatch(Board b, bool selfIsPlayer1) {
            SelfIsPlayer1 = selfIsPlayer1;
            Board = b;
            if (selfIsPlayer1) {
                Self = b.Player1;
                Enemy = b.Player2;
            } else {
                Self = b.Player2;
                Enemy = b.Player1;
            }
        }
    }
}
