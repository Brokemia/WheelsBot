using Godot;
using WheelsGodot.actions;

namespace WheelsGodot.heroes {
    [GlobalClass]
    public partial class HeroLevel : Resource {
        [Export]
        public int EnergyNeeded { get; set; }

        [Export]
        public int Priority { get; set; }

        [Export]
        public string[] ImmuneTo { get; set; }

        [Export]
        public WheelsAction[] Actions { get; set; }
    }
}
