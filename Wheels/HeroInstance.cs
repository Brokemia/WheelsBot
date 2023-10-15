using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WheelsGodot.heroes;

namespace WheelsGodot {
    public class HeroInstance {
        public const int MAX_XP = 6;
        public const int MAX_LEVEL = 2;
        
        public int Index { get; set; }

        public string Name { get; set; }
        
        public Hero Hero { get; set; }

        public HeroLevel HeroLevel => Hero.Levels[Level];

        public int EnergyLeft => EnergyNeeded - Energy;
        
        public int EnergyNeeded => HeroLevel.EnergyNeeded;

        private int energy;
        
        public int Energy {
            get => energy;
            set {
                energy = value;
                var energyNeeded = EnergyNeeded;
                if (energy > energyNeeded) {
                    energy = energyNeeded;
                } else if (energy < 0) {
                    energy = 0;
                }
            }
        }

        private int xp;

        public int XP {
            get => xp;
            set {
                xp = value;
                if (xp > MAX_XP) {
                    xp = MAX_XP;
                }
            }
        }

        private int level;

        public int Level {
            get => level;
            set {
                level = value;
                if (level > MAX_LEVEL) {
                    level = MAX_LEVEL;
                }
            }
        }

        // Returns true if the hero can actually level up more
        public bool LevelUp() {
            if (level >= MAX_LEVEL) {
                return false;
            }
            Level++;
            return true;
        }
    }
}
