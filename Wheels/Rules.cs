using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WheelsGodot {
    [GlobalClass]
    public partial class Rules : Resource {
        [Export(PropertyHint.MultilineText)]
        public string Description { get; set; }

        [Export]
        public Godot.Collections.Array<string> AvailableHeroes { get; set; }
    }
}
