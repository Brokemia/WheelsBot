using Discord;
using Discord.Net;
using Discord.WebSocket;
using Godot;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WheelsGodot.discord;
using WheelsGodot.heroes;

namespace WheelsGodot
{
	public partial class DiscordBot : Node {

		private const string AnalyticsFile = "user://analytics.csv";
		private const string PersistentDataFile = "user://persistent.cfg";

		private readonly ConfigFile Config;

		private readonly ConfigFile PersistentData;

		private readonly Godot.Collections.Dictionary<string, int> defaultUserStats = new() {
			{ "Wins", 0 },
			{ "Losses", 0 },
			{ "Ties", 0 }
		};

		private DiscordSocketClient client;

		private Controller controller;

		private Dictionary<ulong, DiscordMatch> ongoingGames = new();

		private Dictionary<ulong, PendingInvite> inviteIfCancel = new();

		private string[] levelNames = new string[] {
			"Bronze",
			"Silver",
			"Gold"
		};

		public DiscordBot() {
			Config = new ConfigFile();
			Config.Load("res://config/auth.cfg");
			PersistentData = new ConfigFile();
			PersistentData.Load(PersistentDataFile);
        }

		public override void _Ready() {
			base._Ready();
			controller = GetNode<Controller>("/root/Controller");

			AsyncReady();
		}

		public async Task AsyncReady() {
			client = new DiscordSocketClient();

			client.Log += Client_Log;
			client.Ready += Client_Ready;
			client.SlashCommandExecuted += Client_SlashCommandExecuted;
			client.SelectMenuExecuted += Client_SelectMenuExecuted;
			client.ButtonExecuted += Client_ButtonExecuted;

			await client.LoginAsync(TokenType.Bot, Config.GetValue("token", "discordToken").AsString());
			await client.StartAsync();
		}

		private async Task CatchAndContinue(Func<Task> action) {
			try {
				await action();
			} catch (Exception e) {
				GD.Print(e.ToString());
			}
		}

		private Task Client_Log(LogMessage arg) {
			GD.Print(arg.Message.ToString());
			if (arg.Exception != null) {
				GD.Print(arg.Exception.ToString());
			}
			
			return Task.CompletedTask;
		}

		private async Task Client_ButtonExecuted(SocketMessageComponent component) {
			if (component.Data.CustomId.StartsWith("lock")) {
				await LockWheel(component);
				return;
			}
			if (component.Data.CustomId.StartsWith("unlock")) {
				await UnlockWheel(component);
				return;
			}

			switch (component.Data.CustomId) {
				case "spin":
					await HandleSpin(component);
					break;
				case "cancelExistingGame":
					await HandleCancelExistingGame(component);
					break;
				default:
					await component.RespondAsync($"Unknown button {component.Data.CustomId}");
					break;
			}
		}

		private async Task Client_SelectMenuExecuted(SocketMessageComponent component) {
			switch(component.Data.CustomId) {
				case "heroA":
					await HandleHeroASelectMenu(component);
					break;
				case "heroB":
					await HandleHeroBSelectMenu(component);
					break;
				default:
					await component.RespondAsync($"Unknown select menu {component.Data.CustomId}");
					break;
			}
		}

		private async Task Client_Ready() {
			try {
				await client.CreateGlobalApplicationCommandAsync(new SlashCommandBuilder()
					.WithName("wheels")
					.WithDescription("Start a game of wheels")
					.AddOption("opponent", ApplicationCommandOptionType.User, "The user to challenge", true)
					.AddOption("ruleset", ApplicationCommandOptionType.String, "The ruleset to use", false)
					.Build());
				
				await client.CreateGlobalApplicationCommandAsync(new SlashCommandBuilder()
					.WithName("accept")
					.WithDescription("Accept a challenge to play wheels")
					.Build());
				
				await client.CreateGlobalApplicationCommandAsync(new SlashCommandBuilder()
					.WithName("stats")
					.WithDescription("Check your stats")
					.Build());
				// Using the ready event is a simple implementation for the sake of the example. Suitable for testing and development.
				// For a production bot, it is recommended to only run the CreateGlobalApplicationCommandAsync() once for each command.
			} catch (HttpException exception) {
				var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);

				GD.Print(json);
			}
		}

		private async Task Client_SlashCommandExecuted(SocketSlashCommand command) {
			switch (command.Data.Name) {
				case "wheels":
					await HandleStartCommand(command);
					break;
				case "accept":
					await HandleAcceptCommand(command);
					break;
				case "stats":
					await HandleStatsCommand(command);
					break;
				default:
					await command.RespondAsync($"Unknown command {command.Data.Name}");
					break;
			}
		}

		private async Task RemovePreviousComponents(SocketMessageComponent component) {
			await component.UpdateAsync(msg => {
				msg.Components = new ComponentBuilder().Build();
			});
		}

		private async Task HandleStatsCommand(SocketSlashCommand command) {
			var stats = PersistentData.GetValue("leaderboard", command.User.Id.ToString(), defaultUserStats.Duplicate()).AsGodotDictionary<string, int>();

			await command.RespondAsync(embed: new EmbedBuilder()
				.WithTitle(command.User.GlobalName)
				.AddField("Wins", stats["Wins"])
				.AddField("Losses", stats["Losses"])
				.AddField("Ties", stats["Ties"])
				.Build());
		}
		
		private async Task HandleCancelExistingGame(SocketMessageComponent component) {
			var user = component.User;
			var invite = inviteIfCancel[user.Id];
			inviteIfCancel.Remove(user.Id);
			if (ongoingGames.TryGetValue(user.Id, out DiscordMatch existingGame)) {
				await CatchAndContinue(async() => await existingGame.EnemyUser.SendMessageAsync(embed: new EmbedBuilder()
					.WithDescription($"{user.Mention} has cancelled the game").Build()));
			}

			InitGame(user, invite.To, invite.Ruleset, component);

			var embed = new EmbedBuilder()
				.WithDescription($"Previous game cancelled.\nWaiting for {invite.To.Mention} to accept using the /accept command");

			await CatchAndContinue(async() => await component.RespondAsync(embed: embed.Build(), ephemeral: true));
			await CatchAndContinue(async() => await invite.To.SendMessageAsync(embed: new EmbedBuilder()
				.WithDescription($"You have been challenged to a game of Wheels by {user.Mention}! Use the /accept command in a channel of your choice to accept the challenge.").Build()));
		}

		private async Task HandleSpin(SocketMessageComponent component) {
			var game = ongoingGames[component.User.Id];
			var board = game.Board;
			var finalSpin = controller.Spin(game.Self);

			await CatchAndContinue(async () => await RemovePreviousComponents(component));
			await CatchAndContinue(async () => await component.FollowupAsync(SpinResultDisplay(game.Self), components: finalSpin ? null : BuildSpinterface(game.Self), ephemeral: true));

			if (!finalSpin) {
				return;
			}

			// We need to wait for the other player
			if (!game.OpponentReady) {
				ongoingGames[game.EnemyUser.Id].OpponentReady = true;
				ongoingGames[game.EnemyUser.Id].LastInteraction = component;
				await CatchAndContinue(async () => await component.FollowupAsync(embed: new EmbedBuilder().WithDescription("Waiting for opponent to finish spinning").Build(), ephemeral: true));
				return;
			}
			game.OpponentReady = false;

			// Run the outcome and generate the event log
			var selfPlayer = new DiscordFrontendPlayer() {
				PlayerName = game.SelfUser.GlobalName
			};
			var enemyPlayer = new DiscordFrontendPlayer() {
				PlayerName = game.EnemyUser.GlobalName
			};
			var frontend = new DiscordFrontend();
			frontend.SetPlayerFrontend(game.Self, selfPlayer);
			frontend.SetPlayerFrontend(game.Enemy, enemyPlayer);

			controller.Act(frontend, board);

			var phasedLogs = frontend.PhaseLogs.SelectMany(x => x.Logs.Prepend($"__{x.Phase}__"));

			// Display the results
			var embed = AddPlayersStatus(new EmbedBuilder()
				.WithTitle("Round Outcome")
				.WithDescription(string.Join('\n', phasedLogs)),
				game);
			var components = frontend.GameOver ? null : new ComponentBuilder()
			.WithButton(
				new ButtonBuilder()
					.WithLabel("Spin")
					.WithCustomId("spin")
					.WithStyle(ButtonStyle.Primary)
			);

			await CatchAndContinue(async () => await component.FollowupAsync(embed: embed.Build()));
			if (component.ChannelId != game.LastInteraction.ChannelId) {
				await CatchAndContinue(async () => await game.LastInteraction.FollowupAsync(embed: embed.Build()));
			}

			var enemySpinEmbed = new EmbedBuilder()
				.WithDescription($"{TextManipulation.Possessivize(enemyPlayer.PlayerName)} Spin");
			await CatchAndContinue(async () => await component.FollowupAsync(SpinResultDisplay(game.Enemy, false), embed: enemySpinEmbed.Build(), components: components?.Build(), ephemeral: true));
			
			enemySpinEmbed = new EmbedBuilder()
				.WithDescription($"{TextManipulation.Possessivize(selfPlayer.PlayerName)} Spin");
			await CatchAndContinue(async () => await game.LastInteraction.FollowupAsync(SpinResultDisplay(game.Self, false), embed: enemySpinEmbed.Build(), components: components?.Build(), ephemeral: true));

			// Display the winner if the game ended
			if (frontend.GameOver) {
				ongoingGames.Remove(component.User.Id);
				ongoingGames.Remove(game.EnemyUser.Id);
				var gameOverEmbed = new EmbedBuilder()
					.WithTitle("Game Over")
					.WithDescription(frontend.Winner == null ? "It's a tie!" : $"{((DiscordFrontendPlayer)frontend.Winner).PlayerName} wins!");
				await CatchAndContinue(async () => await component.FollowupAsync(embed: gameOverEmbed.Build()));
				if (component.ChannelId != game.LastInteraction.ChannelId) {
					await CatchAndContinue(async () => await game.LastInteraction.FollowupAsync(embed: gameOverEmbed.Build()));
				}

				SaveGameResults(game);
			}
		}

		private void SaveGameResults(DiscordMatch game) {
			using var file = FileAccess.Open(AnalyticsFile, FileAccess.FileExists(AnalyticsFile) ? FileAccess.ModeFlags.ReadWrite : FileAccess.ModeFlags.Write);
			file.SeekEnd();
			file.StoreCsvLine(new string[] {
				DateTime.UtcNow.ToString(),
				game.SelfUser.Id.ToString(),
				game.EnemyUser.Id.ToString(),
				game.Board.Rules.ResourcePath,
				game.Self.Crown.ToString(),
				game.Self.Bulwark.ToString(),
				string.Join('-', game.Self.Heroes.Select(h => $"{h.Index};{h.Hero.ResourcePath};{h.Level};{h.XP};{h.Energy}")),
				game.Enemy.Crown.ToString(),
				game.Enemy.Bulwark.ToString(),
				string.Join('-', game.Enemy.Heroes.Select(h => $"{h.Index};{h.Hero.ResourcePath};{h.Level};{h.XP};{h.Energy}")),
			});
			var selfResults = PersistentData.GetValue("leaderboard", game.SelfUser.Id.ToString(), defaultUserStats.Duplicate()).AsGodotDictionary<string, int>();
			var enemyResults = PersistentData.GetValue("leaderboard", game.EnemyUser.Id.ToString(), defaultUserStats.Duplicate()).AsGodotDictionary<string, int>();
			
			if (game.Self.Crown <= 0) {
				if (game.Enemy.Crown <= 0) {
                    selfResults["Ties"]++;
                    enemyResults["Ties"]++;
                } else {
                    selfResults["Losses"]++;
                    enemyResults["Wins"]++;
                }
            } else {
                if (game.Enemy.Crown <= 0) {
                    selfResults["Wins"]++;
                    enemyResults["Losses"]++;
                } else {
                    selfResults["???"] = selfResults.GetValueOrDefault("???", 0) + 1;
                    enemyResults["???"] = enemyResults.GetValueOrDefault("???", 0) + 1;
                }
            }

            PersistentData.SetValue("leaderboard", game.SelfUser.Id.ToString(), selfResults);
            PersistentData.SetValue("leaderboard", game.EnemyUser.Id.ToString(), enemyResults);

            PersistentData.Save(PersistentDataFile);
        }

		private string SpinResultDisplay(Player player, bool includeLocks = true) {
			var topRow = "";
			var symbols = "";
			var botRow = "";

			foreach (var wheel in player.Wheels) {
				if (wheel.Locked) {
					topRow += Emojis.LockTop;
					botRow += Emojis.LockBottom;
				} else {
					topRow += Emojis.Blank;
					botRow += Emojis.Blank;
				}
				symbols += Emojis.SymbolToEmoji[$"{wheel.CurrentSymbol.Type},{wheel.CurrentSymbol.Amount}"];
			}

			return includeLocks ? $"{topRow}\n{symbols}\n{botRow}" : symbols;
		}

		private MessageComponent BuildSpinterface(Player player) {
			var locksBuilder = new ActionRowBuilder();
			for (int i = 0; i < player.Wheels.Count; i++) {
				var text = player.Wheels[i].Locked ? "Unlock" : "Lock";
				locksBuilder.WithButton(new ButtonBuilder()
					.WithCustomId($"{text.ToLowerInvariant()},{i}")
					.WithLabel(text)
					.WithStyle(player.Wheels[i].Locked ? ButtonStyle.Success : ButtonStyle.Danger)
				);
			}

			var components = new ComponentBuilder()
				.WithRows(new List<ActionRowBuilder>() {
					locksBuilder,
					new ActionRowBuilder()
						.WithButton(new ButtonBuilder()
							.WithCustomId("spin")
							.WithLabel("Spin")
							.WithStyle(ButtonStyle.Primary)
						)
				});

			return components.Build();
		}

		private string PlayerStatusDisplay(Player player) {
			var res = "";
			res += $"{Emojis.CrownIcon} Crown: {player.Crown} HP\n{Emojis.BulwarkIcon} Bulwark: {player.Bulwark}\n";
			foreach (var hero in player.Heroes) {
				res += $"{Emojis.HeroIcons[hero.Index]} {hero.Hero.Name} ({levelNames[hero.Level]} - {hero.XP} XP): {hero.EnergyLeft} To Act\n";
			}

			return res;
		}

		private EmbedBuilder AddPlayersStatus(EmbedBuilder builder, DiscordMatch game) {
			return builder.AddField(game.SelfUser.GlobalName, PlayerStatusDisplay(game.Self))
				.AddField(game.EnemyUser.GlobalName, PlayerStatusDisplay(game.Enemy));
		}

		private async Task LockWheel(SocketMessageComponent component) {
			var player = ongoingGames[component.User.Id].Self;
			player.Wheels[int.Parse(component.Data.CustomId.Split(',')[1])].Locked = true;
			await CatchAndContinue(async () => await component.UpdateAsync((msg) => {
				msg.Content = SpinResultDisplay(player);
				msg.Components = BuildSpinterface(player);
			}));
		}

		private async Task UnlockWheel(SocketMessageComponent component) {
			var player = ongoingGames[component.User.Id].Self;
			player.Wheels[int.Parse(component.Data.CustomId.Split(',')[1])].Locked = false;
			await CatchAndContinue(async () => await component.UpdateAsync((msg) => {
				msg.Content = SpinResultDisplay(player);
				msg.Components = BuildSpinterface(player);
			}));
		}

		private async Task HandleStartCommand(SocketSlashCommand command) {
			var opponentUser = command.Data.Options.First(o => o.Name == "opponent").Value as SocketUser;
			if (opponentUser.IsBot) {
				await CatchAndContinue(async () => await command.RespondAsync(
					embed: new EmbedBuilder()
						.WithDescription("You can't play against a bot!").Build(),
					ephemeral: true));
				return;
			}
			var ruleset = GD.Load<Rules>($"res://rulesets/{(command.Data.Options.FirstOrDefault(o => o.Name == "ruleset")?.Value ?? "default") as string}.tres");
			if (ruleset == null) {
				await CatchAndContinue(async () => await command.RespondAsync(
					embed: new EmbedBuilder()
						.WithDescription("That ruleset doesn't exist!").Build(),
					ephemeral: true));
				return;
			}
			if (ongoingGames.TryGetValue(command.User.Id, out DiscordMatch ongoing)) {
				var yesNoComponents = new ComponentBuilder()
					.WithButton(new ButtonBuilder()
						.WithCustomId("cancelExistingGame")
						.WithLabel(ongoing.SelfIsPlayer1 ? "Cancel" : "Decline")
						.WithStyle(ButtonStyle.Danger));

				var inProgressEmbed = new EmbedBuilder()
					.WithDescription(ongoing.AcceptedInvite ? "You already have a game in progress!\nWould you like to forfeit it and send a new invite?"
						: $"You already have a pending invite!\nWould you like to {(ongoing.SelfIsPlayer1 ? "cancel" : "decline")} it and send a new invite?");
				await CatchAndContinue(async () => await command.RespondAsync(embed: inProgressEmbed.Build(), components: yesNoComponents.Build(), ephemeral: true));

				inviteIfCancel[command.User.Id] = new() {
					From = command.User,
					To = opponentUser,
					Ruleset = ruleset
				};
				return;
			}
			if (opponentUser.Id == command.User.Id) {
				var selfChallengeEmbed = new EmbedBuilder().WithDescription("You can't challenge yourself!");
				await CatchAndContinue(async () => await command.RespondAsync(embed: selfChallengeEmbed.Build(), ephemeral: true));
				return;
			}
			if (ongoingGames.TryGetValue(opponentUser.Id, out DiscordMatch enemyGame)) {
				var inProgressEmbed = new EmbedBuilder()
					.WithDescription($"That player already has a {(enemyGame.AcceptedInvite ? "game in progress" : "pending invite")}!");
				await CatchAndContinue(async () => await command.RespondAsync(embed: inProgressEmbed.Build(), ephemeral: true));
				return;
			}
			
			InitGame(command.User, opponentUser, ruleset, command);

			var embed = new EmbedBuilder()
				.WithDescription($"Waiting for {opponentUser.Mention} to accept using the /accept command");

			await CatchAndContinue(async () => await command.RespondAsync(embed: embed.Build(), ephemeral: true));
			await CatchAndContinue(async () => await opponentUser.SendMessageAsync(embed: new EmbedBuilder()
				.WithDescription($"You have been challenged to a game of Wheels by {command.User.Mention}! Use the /accept command in a channel of your choice to accept the challenge.").Build()));
		}

		private void InitGame(SocketUser player1, SocketUser player2, Rules rules, SocketInteraction lastInteraction) {
			var board = new Board() {
				Rules = rules,
				Player1 = new Player(),
				Player2 = new Player()
			};

			ongoingGames[player1.Id] = new DiscordMatch(board, true) {
				SelfUser = player1,
				EnemyUser = player2
			};
			ongoingGames[player2.Id] = new DiscordMatch(board, false) {
				SelfUser = player2,
				EnemyUser = player1,
				LastInteraction = lastInteraction
			};
		}

		private async Task HandleAcceptCommand(SocketSlashCommand command) {
			var noInviteEmbed = new EmbedBuilder()
					.WithDescription("You have no pending invite!");
			if (!ongoingGames.ContainsKey(command.User.Id)) {
				await CatchAndContinue(async () => await command.RespondAsync(embed: noInviteEmbed.Build(), ephemeral: true));
				return;
			}
			var game = ongoingGames[command.User.Id];
			if (game.SelfIsPlayer1) {
				await CatchAndContinue(async () => await command.RespondAsync(embed: noInviteEmbed.Build(), ephemeral: true));
				return;
			}
			if (game.AcceptedInvite) {
				var alreadyAcceptedEmbed = new EmbedBuilder()
					.WithDescription("You are already in a game!");
				await CatchAndContinue(async () => await command.RespondAsync(embed: alreadyAcceptedEmbed.Build(), ephemeral: true));
				return;
			}

			game.AcceptedInvite = true;
			ongoingGames[game.EnemyUser.Id].AcceptedInvite = true;
			var selectMenu = new SelectMenuBuilder()
					.WithCustomId("heroA");

			foreach (var hero in game.Board.Rules.AvailableHeroes) {
				selectMenu = selectMenu.AddOption(new SelectMenuOptionBuilder().WithLabel(GetHero(hero).Name).WithValue(hero));
			}

			var components = new ComponentBuilder()
				.WithSelectMenu(selectMenu);
			var embed = new EmbedBuilder()
				.WithDescription("Choose a hero for slot A");

			await CatchAndContinue(async () => await command.RespondAsync(embed: embed.Build(), components: components.Build(), ephemeral: true));
			await CatchAndContinue(async () => await game.LastInteraction.FollowupAsync(embed: embed.Build(), components: components.Build(), ephemeral: true));
		}

		private async Task HandleHeroASelectMenu(SocketMessageComponent component) {
			var game = ongoingGames[component.User.Id];
			game.Self.AddHero(GetHero(component.Data.Values.First()));

			var selectMenu = new SelectMenuBuilder()
					.WithCustomId("heroB");

			foreach (var hero in game.Board.Rules.AvailableHeroes) {
				var h = GetHero(hero);
				if (!game.Self.HasHero(h)) {
					selectMenu = selectMenu.AddOption(new SelectMenuOptionBuilder().WithLabel(h.Name).WithValue(hero));
				}
			}

			var components = new ComponentBuilder()
				.WithSelectMenu(selectMenu);
			var embed = new EmbedBuilder()
				.WithTitle($"{component.Data.Values.First()} selected")
				.WithDescription("Choose a hero for slot B");

			await CatchAndContinue(async () => await RemovePreviousComponents(component));
			await CatchAndContinue(async () => await component.FollowupAsync(embed: embed.Build(), components: components.Build(), ephemeral: true));
		}

		private async Task HandleHeroBSelectMenu(SocketMessageComponent component) {
			var game = ongoingGames[component.User.Id];
			game.Self.AddHero(GetHero(component.Data.Values.First()));

			var components = new ComponentBuilder()
				.WithButton(
					new ButtonBuilder()
						.WithLabel("Spin")
						.WithCustomId("spin")
						.WithStyle(ButtonStyle.Primary)
				);
			var embed = new EmbedBuilder()
				.WithTitle($"{component.Data.Values.First()} selected")
				.WithDescription(game.OpponentReady ? $"Your opponent chose {TextManipulation.ListItems(game.Enemy.Heroes.Select(h => h.Hero.Name))}.\n\nBEGIN!" : "Waiting for opponent to choose heroes");

			if (game.OpponentReady) {
				embed = AddPlayersStatus(embed, game);
			}

			await CatchAndContinue(async () => await RemovePreviousComponents(component));
			await CatchAndContinue(async () => await component.FollowupAsync(embed: embed.Build(), components: game.OpponentReady ? components.Build() : null, ephemeral: true));

			if (game.OpponentReady) {
				game.OpponentReady = false;
				var startEmbed = AddPlayersStatus(new EmbedBuilder()
					.WithDescription($"Your opponent chose {TextManipulation.ListItems(game.Self.Heroes.Select(h => h.Hero.Name))}.\n\nBEGIN!"),
					game);
				await CatchAndContinue(async () => await game.LastInteraction.FollowupAsync(embed: startEmbed.Build(), components: components.Build(), ephemeral: true));
			} else {
				var enemyGame = ongoingGames[game.EnemyUser.Id];
				enemyGame.LastInteraction = component;
				enemyGame.OpponentReady = true;
			}
		}

		private Dictionary<string, Hero> heroCache = new();

		private Hero GetHero(string name) {
			if (heroCache.TryGetValue(name, out var hero)) {
				return hero;
			}
			hero = GD.Load<Hero>($"res://heroes/{name}.tres");
			heroCache[name] = hero;
			return hero;
		}
	}
}
