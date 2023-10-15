using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WheelsGodot.actions {
    [GlobalClass]
    public partial class ActionEnergize : Action {
        [Export]
        public int Amount { get; set; }

        public ActionEnergize() {
            Type = "Energize";
        }

        public override void Act(Board board, Player player, HeroInstance hero, WheelsFrontendPlayer frontend) {
            foreach (HeroInstance h in player.Heroes) {
                if (h == hero) continue;
                h.Energy += Amount;
                frontend.HeroAddEnergy(hero, h, Amount);
            }
        }
    }
}
