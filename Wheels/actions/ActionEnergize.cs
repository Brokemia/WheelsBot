using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WheelsGodot.actions {
    [GlobalClass]
    public partial class ActionEnergize : WheelsAction {
        [Export]
        public int Amount { get; set; }

        public ActionEnergize() {
            Type = "Energize";
        }

        public override bool Act(Board board, Player player, HeroInstance hero, WheelsFrontendPlayer frontend) {
            // Wait until the other hero actually needs it
            if (player.Heroes.Any(h => h != hero && h.Energy >= h.EnergyNeeded)) {
                return false;
            }
            foreach (HeroInstance h in player.Heroes) {
                if (h == hero) continue;
                
                h.Energy += Amount;
                frontend.HeroAddEnergy(hero, h, Amount);
            }
            return true;
        }
    }
}
