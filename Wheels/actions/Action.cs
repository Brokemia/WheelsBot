using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WheelsGodot.actions {
    [GlobalClass]
    public abstract partial class Action : Resource {
        public string Type { get; set; }

        public abstract void Act(Board board, Player player, HeroInstance hero, WheelsFrontendPlayer frontend);
    }
}
