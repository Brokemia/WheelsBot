using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WheelsGodot.actions {
    [GlobalClass]
    public partial class ActionHeal : Action {
        [Export]
        public int Amount { get; set; }

        public ActionHeal() {
            Type = "Heal";
        }

        public override void Act(Board board, Player player, HeroInstance hero, WheelsFrontendPlayer frontend) {
            player.Crown += Amount;
            frontend.HealCrown(hero, Amount);
        }
    }
}
