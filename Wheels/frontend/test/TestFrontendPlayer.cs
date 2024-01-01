using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WheelsGodot.heroes;

namespace WheelsGodot.frontend.test {
	public partial class TestFrontendPlayer : Control, WheelsFrontendPlayer {
		// TODO Dynamically create wheel selectors based on the number of wheels
		public const int WHEEL_COUNT = 5;
		private const string HERO_FOLDER = "res://heroes/";

		[Export]
		public string PlayerName { get; set; }

		[Export]
		public string SlotPathFormat;

		[Export]
		public NodePath HeroSelectPath;

		[Export]
		public PackedScene HeroControl;

		public List<string> RunningLog { get; set; } = new();

		private OptionButton[] slots;

		private OptionButton heroSelect;

		public override void _Ready() {
			heroSelect = GetNode<OptionButton>(HeroSelectPath);
			GetNode<Label>("Name").Text = PlayerName;

			int j = 0;
			foreach (string file in DirAccess.GetFilesAt(HERO_FOLDER).Where(f => f.EndsWith(".tres"))) {
				heroSelect.AddItem(file);
				heroSelect.SetItemMetadata(j, GD.Load<Hero>($"{HERO_FOLDER}/{file}"));
				j++;
			}

			slots = new OptionButton[WHEEL_COUNT];
			for (int i = 1; i <= WHEEL_COUNT; i++) {
				slots[i-1] = GetNode<OptionButton>(string.Format(SlotPathFormat, i));
			}
		}

		public void Init(Player player) {
			for (int i = 0; i < WHEEL_COUNT; i++) {
				InitWheel(player.Wheels[i], slots[i]);
			}
		}

		private void InitWheel(Wheel wheel, OptionButton slot) {
			slot.Clear();
			for (int i = 0; i < wheel.Symbols.Length; i++) {
				var symbol = wheel.Symbols[i];
				slot.AddItem($"{symbol.Type} x{symbol.Amount}");
				slot.SetItemMetadata(i, wheel.Symbols[i]);
			}
		}

		public void UpdateWheels(Player player) {
			for (int i = 0; i < WHEEL_COUNT; i++) {
				player.Wheels[i].CurrentSymbolIdx = slots[i].Selected;
			}
		}

		private string HeroIdentifier(HeroInstance hero) {
			return $"{hero.Hero.Name}[{hero.Index}]";
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
			AddLog($"{HeroIdentifier(hero)} spawned a bomb, -2 Crown");
		}

		public void AddEnergy(HeroInstance hero, int amount) {
			AddLog($"{HeroIdentifier(hero)} gained {amount} energy, {hero.EnergyLeft} left to act");
		}

		public void AttackEnemyBulwark(HeroInstance hero, int amount, int remaining) {
			AddLog($"{HeroIdentifier(hero)} attacked, -{amount} Bulwark");
		}

		public void AttackEnemyCrown(HeroInstance hero, int amount, int remaining) {
			AddLog($"{HeroIdentifier(hero)} attacked, -{amount} Crown");
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
			AddLog($"{HeroIdentifier(hero)} healed the crown, +{amount} Crown");
		}

		public void OnAddHero() {
			var hero = heroSelect.GetSelectedMetadata().As<Hero>();
		}
	}
}
