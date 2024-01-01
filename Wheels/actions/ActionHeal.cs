using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WheelsGodot.actions {
    [GlobalClass]
    public partial class ActionHeal : WheelsAction {
        [Export]
        public int Amount { get; set; }

        public ActionHeal() {
            Type = "Heal";
        }

        public override bool Act(Board board, Player player, HeroInstance hero, WheelsFrontendPlayer frontend) {
            player.Crown += Amount;
            frontend.HealCrown(hero, Amount);
            return true;
        }
    }
}
