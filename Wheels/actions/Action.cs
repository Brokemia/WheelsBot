using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WheelsGodot.actions {
    [GlobalClass]
    public abstract partial class WheelsAction : Resource {
        public string Type { get; set; }

        // Return true if this action executed normally, and false if this hero should stall until all other heroes have gone
        public abstract bool Act(Board board, Player player, HeroInstance hero, WheelsFrontendPlayer frontend);
    }
}
