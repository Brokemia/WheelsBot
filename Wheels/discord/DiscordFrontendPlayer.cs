using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WheelsGodot.discord {
    public class DiscordFrontendPlayer : WheelsFrontendPlayer {
        public string PlayerName { get; set; }

        public List<string> RunningLog { get; set; } = new();

        private string HeroIdentifier(HeroInstance hero) {
            return $"{Emojis.HeroIcons[hero.Index]} {hero.Name}";
        }

        private void AddLog(string log) {
            RunningLog.Add($"{PlayerName}: {log}");
        }

        public void AddXP(HeroInstance hero, int amount) {
            AddLog($"{HeroIdentifier(hero)} gained {amount} XP");
        }

        public void GrowBulwark(int amount) {
            AddLog($"Bulwark grew by {amount}");
        }

        public void LevelUpHero(HeroInstance hero) {
            AddLog($"{HeroIdentifier(hero)} leveled up");
        }

        public void SpawnBomb(HeroInstance hero) {
            AddLog($"{HeroIdentifier(hero)} spawned a bomb, -2 {Emojis.CrownIcon}");
        }

        public void AddEnergy(HeroInstance hero, int amount) {
            AddLog($"{HeroIdentifier(hero)} gained {amount} energy, {hero.EnergyLeft} left to act");
        }

        public void AttackEnemyBulwark(HeroInstance hero, int amount, int remaining) {
            AddLog($"{HeroIdentifier(hero)} attacked, -{amount} {Emojis.BulwarkIcon}");
        }

        public void AttackEnemyCrown(HeroInstance hero, int amount, int remaining) {
            AddLog($"{HeroIdentifier(hero)} attacked, -{amount} {Emojis.CrownIcon}");
        }

        public void DelayEnemyHero(HeroInstance hero, HeroInstance delayed, int amount) {
            AddLog($"{HeroIdentifier(hero)} delayed {HeroIdentifier(delayed)} by {amount}");
        }

        public void HeroGrowBulwark(HeroInstance hero, int amount) {
            AddLog($"{HeroIdentifier(hero)} grew the Bulwark by {amount}");
        }

        public void HeroAddEnergy(HeroInstance hero, HeroInstance recieving, int amount) {
            AddLog($"{HeroIdentifier(hero)} energized {HeroIdentifier(recieving)} by {amount} energy, {recieving.EnergyLeft} left to act");
        }

        public void HealCrown(HeroInstance hero, int amount) {
            AddLog($"{HeroIdentifier(hero)} healed the crown, +{amount} {Emojis.CrownIcon}");
        }
    }
}
