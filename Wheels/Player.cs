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

        public List<HeroInstance> Heroes { get; set; } = new();
        
        public int Bulwark { get; set; }

        public int Crown { get; set; } = CROWN_START;


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
            if (Crown < 0) {
                Crown = 0;
            }
        }

        public void Reset() {
            foreach (var wheel in Wheels) {
                wheel.Locked = false;
            }

            Spins = 0;
        }

        private Hero GetHero(string name) {
            return GD.Load<Hero>($"res://heroes/{name}.tres");
        }

        public void AddHero(string heroName) {
            var heroInstance = new HeroInstance {
                Name = heroName,
                Hero = GetHero(heroName),
                Index = Heroes.Count
            };
            Heroes.Add(heroInstance);
        }

        public bool HasHero(string heroName) {
            return Heroes.Any(hero => hero.Name == heroName);
        }
    }
}
