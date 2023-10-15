using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WheelsGodot {
    public class Board {
        public const int BOMB_DAMAGE = 2;

        public Rules Rules { get; set; }

        public Player Player1 { get; set; }

        public Player Player2 { get; set; }

        public Player Other(Player player) {
            if (player == Player2) {
                return Player1;
            } else if (player == Player1) {
                return Player2;
            } else {
                throw new Exception("Player not found");
            }
        }

        public void SpawnBomb(Player player) {
            // Damage whichever player didn't send the bomb
            Other(player).DamageCrown(BOMB_DAMAGE);
        }
    }
}
