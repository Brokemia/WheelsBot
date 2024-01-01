using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WheelsGodot.actions {
    [GlobalClass]
    public partial class ActionAssassinate : WheelsAction {
        [Export]
        public int Power { get; set; }

        public ActionAssassinate() {
            Type = "Assassinate";
        }

        public override bool Act(Board board, Player player, HeroInstance hero, WheelsFrontendPlayer frontend) {
            var other = board.Other(player);
            other.DamageCrown(Power);
            frontend.AttackEnemyCrown(hero, Power, other.Crown);
            return true;
        }
    }
}
