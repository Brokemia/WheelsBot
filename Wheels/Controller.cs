using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using WheelsGodot;

public partial class Controller : Node
{
    public const int XP_FROM_ATTACK = 2;

    public bool Spin(Player player) {
        return player.Spin();
    }

    public void Act(WheelsFrontend frontend, Board board) {
        var selfFrontend = frontend.Players[board.Player1];
        var enemyFrontend = frontend.Players[board.Player2];

        // 1. Gain XP
        frontend.StartPhase("Gain XP");
        GainXP(board.Player1, selfFrontend);
        GainXP(board.Player2, enemyFrontend);

        // 2. Walls
        frontend.StartPhase("Grow Bulwark");
        GainWalls(board.Player1, selfFrontend);
        GainWalls(board.Player2, enemyFrontend);

        // 3. Level up
        frontend.StartPhase("Level Up");
        LevelUp(board, board.Player1, selfFrontend);
        LevelUp(board, board.Player2, enemyFrontend);

        // 4. Add energy
        frontend.StartPhase("Gain Energy");
        GainEnergy(board.Player1, selfFrontend);
        GainEnergy(board.Player2, enemyFrontend);

        // 5. Act
        frontend.StartPhase("Act");
        var toAct = HeroesToAct(board);
        while (toAct.MoveNext()) {
            var hero = toAct.Current.Hero;
            var player = toAct.Current.Player;
            var pFrontend = frontend.Players[player];
            hero.Energy = 0;
            foreach (var action in hero.HeroLevel.Actions) {
                action.Act(board, player, hero, pFrontend);
            }
            // XP from attacking
            hero.XP += XP_FROM_ATTACK;
            pFrontend.AddXP(hero, XP_FROM_ATTACK);

            LevelUpHero(board, player, hero, pFrontend);
        }

        // 6. Check victory condition
        if (board.Player1.Crown <= 0 && board.Player2.Crown <= 0) {
            frontend.EndGame((WheelsFrontendPlayer)null);
        } else if (board.Player1.Crown <= 0) {
            frontend.EndGame(board.Player2);
        } else if (board.Player2.Crown <= 0) {
            frontend.EndGame(board.Player1);
        }

        frontend.EndRound();

        // Cleanup
        board.Player1.Reset();
        board.Player2.Reset();
    }

    private int ThreeOAKPlus(int number) {
        if (number < 3) {
            return 0;
        }
        return number - 2;
    }

    private void GainXP(Player player, WheelsFrontendPlayer frontend) {
        int[] xpGain = new int[player.Heroes.Count];

        foreach (var wheel in player.Wheels) {
            if (wheel.CurrentSymbol.Type == Symbol.SymbolType.XP_A) {
                xpGain[0]++;
            } else if (wheel.CurrentSymbol.Type == Symbol.SymbolType.XP_B) {
                xpGain[1]++;
            }
        }

        for (int i = 0; i < xpGain.Length; i++) {
            if (xpGain[i] > 0) {
                player.Heroes[i].XP += xpGain[i];
                frontend.AddXP(player.Heroes[i], xpGain[i]);
            }
        }
    }

    private void GainWalls(Player player, WheelsFrontendPlayer frontend) {
        int amount = 0;

        foreach (var wheel in player.Wheels) {
            if (wheel.CurrentSymbol.Type == Symbol.SymbolType.Hammer) {
                amount += wheel.CurrentSymbol.Amount;
            }
        }
        amount = ThreeOAKPlus(amount);

        if (amount > 0) {
            player.GrowBulwark(amount);
            frontend.GrowBulwark(amount);
        }
    }

    private void LevelUp(Board board, Player player, WheelsFrontendPlayer frontend) {
        foreach (var hero in player.Heroes) {
            LevelUpHero(board, player, hero, frontend);
        }
    }

    private void LevelUpHero(Board board, Player player, HeroInstance hero, WheelsFrontendPlayer frontend) {
        if (hero.XP >= HeroInstance.MAX_XP) {
            if (hero.LevelUp()) {
                frontend.LevelUpHero(hero);
            } else {
                frontend.SpawnBomb(hero);
                board.SpawnBomb(player);
            }

            hero.XP = 0;
        }
    }

    private void GainEnergy(Player player, WheelsFrontendPlayer frontend) {
        int[] energyGain = new int[player.Heroes.Count];

        foreach (var wheel in player.Wheels) {
            if (wheel.CurrentSymbol.Type == Symbol.SymbolType.Energy_A || wheel.CurrentSymbol.Type == Symbol.SymbolType.XP_A) {
                energyGain[0] += wheel.CurrentSymbol.Amount;
            } else if (wheel.CurrentSymbol.Type == Symbol.SymbolType.Energy_B || wheel.CurrentSymbol.Type == Symbol.SymbolType.XP_B) {
                energyGain[1] += wheel.CurrentSymbol.Amount;
            }
        }

        for (int i = 0; i < energyGain.Length; i++) {
            energyGain[i] = ThreeOAKPlus(energyGain[i]);
            if (energyGain[i] > 0) {
                player.Heroes[i].Energy += energyGain[i];
                frontend.AddEnergy(player.Heroes[i], energyGain[i]);
            }
        }
    }

    // Returns heroes that are ready to act ordered by priority
    // Accounts for the possibility that some heroes will become ready later on
    // Whatever calls this, must be updating energy after each hero is returned
    private IEnumerator<(HeroInstance Hero, Player Player)> HeroesToAct(Board board) {
        PriorityQueue<(HeroInstance Hero, Player Player), int> ready = new();
        ready.EnqueueRange(GetReadyHeroes(board.Player1).Select(hero => ((hero.Hero, board.Player1), hero.Priority)));
        ready.EnqueueRange(GetReadyHeroes(board.Player2).Select(hero => ((hero.Hero, board.Player2), hero.Priority)));
        while (ready.Count > 0) {
            while (ready.Count > 0) {
                var hero = ready.Dequeue();
                if (hero.Hero.Energy >= hero.Hero.EnergyNeeded) {
                    yield return hero;
                }
            }
            ready.EnqueueRange(GetReadyHeroes(board.Player1).Select(hero => ((hero.Hero, board.Player1), hero.Priority)));
            ready.EnqueueRange(GetReadyHeroes(board.Player2).Select(hero => ((hero.Hero, board.Player2), hero.Priority)));
        }
    }

    private IEnumerable<(HeroInstance Hero, int Priority)> GetReadyHeroes(Player player) {
        return player.Heroes.Where(hero => hero.Energy >= hero.EnergyNeeded).Select(hero => (hero, hero.HeroLevel.Priority));
    }
}
