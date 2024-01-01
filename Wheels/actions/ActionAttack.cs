using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WheelsGodot.actions {
    [GlobalClass]
    public partial class ActionAttack : WheelsAction {
        [Export]
        public int Height { get; set; }

        [Export]
        public int CrownPower { get; set; }

        [Export]
        public int BulwarkPower { get; set; }

        public ActionAttack() {
            Type = "Attack";
        }

        public override bool Act(Board board, Player player, HeroInstance hero, WheelsFrontendPlayer frontend) {
            var other = board.Other(player);
            if (other.Bulwark >= Height) {
                other.DamageBulwark(BulwarkPower);
                frontend.AttackEnemyBulwark(hero, BulwarkPower, other.Bulwark);
            } else {
                other.DamageCrown(CrownPower);
                frontend.AttackEnemyCrown(hero, CrownPower, other.Crown);
            }
            return true;
        }
    }
}
