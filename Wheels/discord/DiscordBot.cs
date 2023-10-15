﻿using Discord;
using Discord.Net;
using Discord.WebSocket;
using Godot;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WheelsGodot.discord;

namespace WheelsGodot
{
    public partial class DiscordBot : Node {

        private readonly ConfigFile Config;

        private DiscordSocketClient client;

        private Controller controller;

        private Dictionary<ulong, DiscordMatch> ongoingGames = new();

        private Dictionary<ulong, SocketUser> inviteIfCancel = new();

        private string[] levelNames = new string[] {
            "Bronze",
            "Silver",
            "Gold"
        };

        public DiscordBot() {
            Config = new ConfigFile();
            Config.Load("res://config/auth.cfg");
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
                    .Build());
                
                await client.CreateGlobalApplicationCommandAsync(new SlashCommandBuilder()
                    .WithName("accept")
                    .WithDescription("Accept a challenge to play wheels")
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

        private async Task HandleCancelExistingGame(SocketMessageComponent component) {
            var user = component.User;
            var invite = inviteIfCancel[user.Id];
            inviteIfCancel.Remove(user.Id);
            if (ongoingGames.TryGetValue(user.Id, out DiscordMatch existingGame)) {
                await existingGame.EnemyUser.SendMessageAsync(embed: new EmbedBuilder()
                    .WithDescription($"{user.Mention} has cancelled the game").Build());
            }

            InitGame(user, invite, component);

            var embed = new EmbedBuilder()
                .WithDescription($"Previous game cancelled.\nWaiting for {invite.Mention} to accept using the /accept command");

            await component.RespondAsync(embed: embed.Build(), ephemeral: true);
            await invite.SendMessageAsync(embed: new EmbedBuilder()
                .WithDescription($"You have been challenged to a game of Wheels by {user.Mention}! Use the /accept command in a channel of your choice to accept the challenge.").Build());
        }

        private async Task HandleSpin(SocketMessageComponent component) {
            var game = ongoingGames[component.User.Id];
            var board = game.Board;
            var finalSpin = controller.Spin(game.Self);

            await RemovePreviousComponents(component);
            await component.FollowupAsync(SpinResultDisplay(game.Self), components: finalSpin ? null : BuildSpinterface(game.Self), ephemeral: true);

            if (!finalSpin) {
                return;
            }

            // We need to wait for the other player
            if (!game.OpponentReady) {
                ongoingGames[game.EnemyUser.Id].OpponentReady = true;
                ongoingGames[game.EnemyUser.Id].LastInteraction = component;
                await component.FollowupAsync(embed: new EmbedBuilder().WithDescription("Waiting for opponent to finish spinning").Build(), ephemeral: true);
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

            await component.FollowupAsync(embed: embed.Build());
            if (component.ChannelId != game.LastInteraction.ChannelId) {
                await game.LastInteraction.FollowupAsync(embed: embed.Build());
            }

            var enemySpinEmbed = new EmbedBuilder()
                .WithDescription($"{enemyPlayer.PlayerName}'s Spin");
            await component.FollowupAsync(SpinResultDisplay(game.Enemy, false), embed: enemySpinEmbed.Build(), components: components?.Build(), ephemeral: true);
            
            enemySpinEmbed = new EmbedBuilder()
                .WithDescription($"{selfPlayer.PlayerName}'s Spin");
            await game.LastInteraction.FollowupAsync(SpinResultDisplay(game.Self, false), embed: enemySpinEmbed.Build(), components: components?.Build(), ephemeral: true);

            // Display the winner if the game ended
            if (frontend.GameOver) {
                ongoingGames.Remove(component.User.Id);
                ongoingGames.Remove(game.EnemyUser.Id);
                var gameOverEmbed = new EmbedBuilder()
                    .WithTitle("Game Over")
                    .WithDescription(frontend.Winner == null ? "It's a tie!" : $"{((DiscordFrontendPlayer)frontend.Winner).PlayerName} wins!");
                await component.FollowupAsync(embed: gameOverEmbed.Build());
                if (component.ChannelId != game.LastInteraction.ChannelId) {
                    await game.LastInteraction.FollowupAsync(embed: gameOverEmbed.Build());
                }
            }
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
                res += $"{Emojis.HeroIcons[hero.Index]} {hero.Name} ({levelNames[hero.Level]} - {hero.XP} XP): {hero.EnergyLeft} To Act\n";
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
            await component.UpdateAsync((msg) => {
                msg.Content = SpinResultDisplay(player);
                msg.Components = BuildSpinterface(player);
            });
        }

        private async Task UnlockWheel(SocketMessageComponent component) {
            var player = ongoingGames[component.User.Id].Self;
            player.Wheels[int.Parse(component.Data.CustomId.Split(',')[1])].Locked = false;
            await component.UpdateAsync((msg) => {
                msg.Content = SpinResultDisplay(player);
                msg.Components = BuildSpinterface(player);
            });
        }

        private async Task HandleStartCommand(SocketSlashCommand command) {
            var opponentUser = command.Data.Options.First(o => o.Name == "opponent").Value as SocketUser;
            if (ongoingGames.TryGetValue(command.User.Id, out DiscordMatch ongoing)) {
                var yesNoComponents = new ComponentBuilder()
                    .WithButton(new ButtonBuilder()
                        .WithCustomId("cancelExistingGame")
                        .WithLabel(ongoing.SelfIsPlayer1 ? "Cancel" : "Decline")
                        .WithStyle(ButtonStyle.Danger));

                var inProgressEmbed = new EmbedBuilder()
                    .WithDescription(ongoing.AcceptedInvite ? "You already have a game in progress!\nWould you like to forfeit it and send a new invite?"
                        : $"You already have a pending invite!\nWould you like to {(ongoing.SelfIsPlayer1 ? "cancel" : "decline")} it and send a new invite?");
                await command.RespondAsync(embed: inProgressEmbed.Build(), components: yesNoComponents.Build(), ephemeral: true);

                inviteIfCancel[command.User.Id] = opponentUser;
                return;
            }
            if (opponentUser.Id == command.User.Id) {
                var selfChallengeEmbed = new EmbedBuilder().WithDescription("You can't challenge yourself!");
                await command.RespondAsync(embed: selfChallengeEmbed.Build(), ephemeral: true);
                return;
            }
            if (ongoingGames.TryGetValue(opponentUser.Id, out DiscordMatch enemyGame)) {
                var inProgressEmbed = new EmbedBuilder()
                    .WithDescription($"That player already has a {(enemyGame.AcceptedInvite ? "game in progress" : "pending invite")}!");
                await command.RespondAsync(embed: inProgressEmbed.Build(), ephemeral: true);
                return;
            }

            InitGame(command.User, opponentUser, command);

            var embed = new EmbedBuilder()
                .WithDescription($"Waiting for {opponentUser.Mention} to accept using the /accept command");

            await command.RespondAsync(embed: embed.Build(), ephemeral: true);
            await opponentUser.SendMessageAsync(embed: new EmbedBuilder()
                .WithDescription($"You have been challenged to a game of Wheels by {command.User.Mention}! Use the /accept command in a channel of your choice to accept the challenge.").Build());
        }

        private void InitGame(SocketUser player1, SocketUser player2, SocketInteraction lastInteraction) {
            var board = new Board() {
                Rules = new Rules() {
                    AvailableHeroes = new List<string>() {
                        "Warrior",
                        "Mage",
                        "Archer",
                        "Engineer",
                        "Assassin",
                        "Priest"
                    }
                },
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
            if (!ongoingGames.ContainsKey(command.User.Id)) {
                var noInviteEmbed = new EmbedBuilder()
                    .WithDescription("You have no pending invite!");
                await command.RespondAsync(embed: noInviteEmbed.Build(), ephemeral: true);
                return;
            }
            var game = ongoingGames[command.User.Id];
            if (game.SelfIsPlayer1) {
                var noInviteEmbed = new EmbedBuilder()
                    .WithDescription("You have no pending invite!");
                await command.RespondAsync(embed: noInviteEmbed.Build(), ephemeral: true);
                return;
            }

            game.AcceptedInvite = true;
            ongoingGames[game.EnemyUser.Id].AcceptedInvite = true;
            var selectMenu = new SelectMenuBuilder()
                    .WithCustomId("heroA");

            foreach (var hero in game.Board.Rules.AvailableHeroes) {
                selectMenu = selectMenu.AddOption(new SelectMenuOptionBuilder().WithLabel(hero).WithValue(hero));
            }

            var components = new ComponentBuilder()
                .WithSelectMenu(selectMenu);
            var embed = new EmbedBuilder()
                .WithDescription("Choose a hero for slot A");

            await command.RespondAsync(embed: embed.Build(), components: components.Build(), ephemeral: true);
            await game.LastInteraction.FollowupAsync(embed: embed.Build(), components: components.Build(), ephemeral: true);
        }

        private async Task HandleHeroASelectMenu(SocketMessageComponent component) {
            var game = ongoingGames[component.User.Id];
            game.Self.AddHero(component.Data.Values.First());

            var selectMenu = new SelectMenuBuilder()
                    .WithCustomId("heroB");

            foreach (var hero in game.Board.Rules.AvailableHeroes) {
                if (!game.Self.HasHero(hero)) {
                    selectMenu = selectMenu.AddOption(new SelectMenuOptionBuilder().WithLabel(hero).WithValue(hero));
                }
            }

            var components = new ComponentBuilder()
                .WithSelectMenu(selectMenu);
            var embed = new EmbedBuilder()
                .WithTitle($"{component.Data.Values.First()} selected")
                .WithDescription("Choose a hero for slot B");

            await RemovePreviousComponents(component);
            await component.FollowupAsync(embed: embed.Build(), components: components.Build(), ephemeral: true);
        }

        private async Task HandleHeroBSelectMenu(SocketMessageComponent component) {
            var game = ongoingGames[component.User.Id];
            game.Self.AddHero(component.Data.Values.First());

            var components = new ComponentBuilder()
                .WithButton(
                    new ButtonBuilder()
                        .WithLabel("Spin")
                        .WithCustomId("spin")
                        .WithStyle(ButtonStyle.Primary)
                );
            var embed = new EmbedBuilder()
                .WithTitle($"{component.Data.Values.First()} selected")
                .WithDescription(game.OpponentReady ? $"Your opponent chose {game.Enemy.Heroes[0].Name} and {game.Enemy.Heroes[1].Name}.\n\nBEGIN!" : "Waiting for opponent to choose heroes");

            if (game.OpponentReady) {
                embed = AddPlayersStatus(embed, game);
            }

            await RemovePreviousComponents(component);
            await component.FollowupAsync(embed: embed.Build(), components: game.OpponentReady ? components.Build() : null, ephemeral: true);

            if (game.OpponentReady) {
                game.OpponentReady = false;
                var startEmbed = AddPlayersStatus(new EmbedBuilder()
                    .WithDescription($"Your opponent chose {game.Enemy.Heroes[0].Name} and {game.Enemy.Heroes[1].Name}.\n\nBEGIN!"),
                    game);
                await game.LastInteraction.FollowupAsync(embed: startEmbed.Build(), components: components.Build(), ephemeral: true);
            } else {
                var enemyGame = ongoingGames[game.EnemyUser.Id];
                enemyGame.LastInteraction = component;
                enemyGame.OpponentReady = true;
            }
        }
    }
}
