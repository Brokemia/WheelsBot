using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WheelsGodot {
    [GlobalClass]
    public partial class Symbol : Resource {
        public enum SymbolType {
            Energy_A, XP_A,
            Energy_B, XP_B,
            Hammer, Empty
        }

        [Export]
        public SymbolType Type { get; set; } = SymbolType.Empty;

        [Export]
        public int Amount { get; set; } = 0;
    }
}
