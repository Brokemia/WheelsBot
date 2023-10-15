using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WheelsGodot.actions {
    [GlobalClass]
    public partial class ActionFortify : Action {
        [Export]
        public int Amount { get; set; }

        public ActionFortify() {
            Type = "Fortify";
        }

        public override void Act(Board board, Player player, HeroInstance hero, WheelsFrontendPlayer frontend) {
            player.GrowBulwark(Amount);
            frontend.HeroGrowBulwark(hero, Amount);
        }
    }
}
