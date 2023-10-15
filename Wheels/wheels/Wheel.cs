using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WheelsGodot {
    [GlobalClass]
    public partial class Wheel : Resource {

        [Export]
        public Symbol[] Symbols { get; set; } = new Symbol[8];

        public bool Locked { get; set; }

        public int CurrentSymbolIdx { get; set; }

        public Symbol CurrentSymbol => Symbols[CurrentSymbolIdx];
    }
}
