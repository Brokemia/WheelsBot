using Godot;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WheelsGodot.heroes;

namespace WheelsGodot {
    public class Player {
        public const int SPINS_MAX = 3;
        public const int BULWARK_MAX = 5;

        public const int CROWN_START = 10;
        public const int CROWN_MAX = 12;

        public List<HeroInstance> Heroes { get; set; } = new();
        
        public int Bulwark { get; set; }

        private int crown = CROWN_START;

        public int Crown {
            get => crown;
            set {
                crown = value;
                if (crown > CROWN_MAX) {
                    crown = CROWN_MAX;
                } else if (crown < 0) {
                    crown = 0;
                }
            }
        }


        public int Spins { get; set; }

        public List<Wheel> Wheels { get; set; } = LoadWheels("Wheel0", "Wheel1", "Wheel2", "Wheel3", "Platinum");

        private static List<Wheel> LoadWheels(params string[] wheelNames) {
            var res = new List<Wheel>();
            foreach (var name in wheelNames) {
                // Duplicate wheels so we can edit properties separately per player
                res.Add(GD.Load<Wheel>($"res://wheels/{name}.tres").Duplicate() as Wheel);
            }

            return res;
        }

        // Returns true if the player can't spin again
        public bool Spin() {
            bool allLocked = true;
            foreach (var wheel in Wheels) {
                if (wheel.Locked) continue;
                wheel.CurrentSymbolIdx = (int)(GD.Randi() % wheel.Symbols.Length);
                allLocked = false;
            }
            Spins++;
            return Spins >= SPINS_MAX || allLocked;
        }

        public void GrowBulwark(int amount) {
            Bulwark += amount;
            if (Bulwark > BULWARK_MAX) {
                Bulwark = BULWARK_MAX;
            }
        }

        public void DamageBulwark(int amount) {
            Bulwark -= amount;
            if (Bulwark < 0) {
                Bulwark = 0;
            }
        }

        public void DamageCrown(int amount) {
            Crown -= amount;
        }

        public void Reset() {
            foreach (var wheel in Wheels) {
                wheel.Locked = false;
            }

            Spins = 0;
        }

        public void AddHero(Hero hero) {
            var heroInstance = new HeroInstance {
                Hero = hero,
                Index = Heroes.Count
            };
            Heroes.Add(heroInstance);
        }

        public bool HasHero(Hero hero) {
            return Heroes.Any(h => h.Hero.ResourcePath == hero.ResourcePath);
        }
    }
}
