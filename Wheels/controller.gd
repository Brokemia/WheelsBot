var actions = {
	"Attack": {
		params = ["height", "crown_power", "bulwark_power"],
		action = (
		func attack(board, params):
			if board.enemy.bulwark >= params.height:
				board.enemy.damage_bulwark(params.bulwark_power)
			else:
				board.enemy.damage_crown(params.crown_power)
			)
	},
	"Assassinate": {
		params = ["power"],
		action = (
		func assassinate(board, params):
			board.enemy.damage_crown(params.power)
			)
	},
	"Delay": {
		params = ["target", "amount"],
		action = (
		func delay(board, params):
			var best_hero = null
			if params.target == "Closest":
				for hero in board.enemy.heroes:
					if not "Delay" in hero.immune and (best_hero == null or best_hero.energy > hero.energy):
						best_hero = hero
			
			if best_hero != null:
				best_hero.delay(params.amount)
			)
	},
	"Defend": {
		params = ["amount"],
		action = (
		func defend(board, params):
			board.self.grow_bulwark(params.amount)
			)
	}
}

