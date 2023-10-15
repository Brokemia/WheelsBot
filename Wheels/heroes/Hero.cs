using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WheelsGodot.heroes {
    [GlobalClass]
    public partial class Hero : Resource {
        [Export]
        public string Name { get; set; }

        [Export]
        public HeroLevel[] Levels { get; set; }
    }
}
