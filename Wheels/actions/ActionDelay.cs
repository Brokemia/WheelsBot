using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WheelsGodot.actions {
    [GlobalClass]
    public partial class ActionDelay : WheelsAction {
        [Export(PropertyHint.Enum, "Closest")]
        public string Target { get; set; } = "Closest";

        [Export]
        public int Amount { get; set; }

        public ActionDelay() {
            Type = "Delay";
        }

        public override bool Act(Board board, Player player, HeroInstance hero, WheelsFrontendPlayer frontend) {
            var other = board.Other(player);
            var valid = other.Heroes.Where(h => !h.HeroLevel.ImmuneTo.Contains("Delay"));

            var chosen = Target switch {
                "Closest" => valid.OrderBy(h => h.EnergyLeft).FirstOrDefault(),
                _ => null
            };

            if (chosen != null) {
                chosen.Energy -= Amount;
                frontend.DelayEnemyHero(hero, chosen, Amount);
            }
            return true;
        }
    }
}
