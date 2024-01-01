using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WheelsGodot {
    public interface WheelsFrontendPlayer {
        void AddXP(HeroInstance hero, int amount);

        void GrowBulwark(int amount);

        void LevelUpHero(HeroInstance hero);

        void SpawnBomb(HeroInstance hero);

        void AddEnergy(HeroInstance hero, int amount);

        void AttackEnemyBulwark(HeroInstance hero, int amount, int remaining);

        void AttackEnemyCrown(HeroInstance hero, int amount, int remaining);

        void DelayEnemyHero(HeroInstance hero, HeroInstance delayed, int amount);

        void HeroGrowBulwark(HeroInstance hero, int amount);

        void HeroAddEnergy(HeroInstance hero, HeroInstance recieving, int amount);

        void HealCrown(HeroInstance hero, int amount);
    }
}
