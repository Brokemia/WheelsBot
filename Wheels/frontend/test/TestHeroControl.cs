using Godot;
using System;
using WheelsGodot;

public partial class TestHeroControl : HBoxContainer {
	private Label name;
	private Label energy;
	private Label xp;

	public override void _Ready() {
		name = GetNode<Label>("Name");
		energy = GetNode<Label>("Energy");
		xp = GetNode<Label>("XP");
	}
	
	public void Update(HeroInstance hero) {
		name.Text = hero.Hero.Name;
		energy.Text = $"{hero.Energy}/{hero.EnergyNeeded} Energy";
		// TODO get max xp from ruleset
		xp.Text = $"{hero.XP}/{6} XP";
	}
}
