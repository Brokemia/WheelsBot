using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WheelsGodot.heroes;

namespace WheelsGodot.frontend.test {
	public partial class TestScene : Control {
		private const string RULESET_FOLDER = "res://rulesets";

		[Export]
		public NodePath Player1Path;

		[Export]
		public NodePath Player2Path;

		[Export]
		public NodePath RulesetSelectPath;
		
		[Export]
		public NodePath LogPath;

		private Controller controller = new();

		private Board board;

		private TestFrontend frontend = new();
		private TestFrontendPlayer p1Frontend;
		private TestFrontendPlayer p2Frontend;

		private OptionButton rulesetSelect;

		private Label log;

		public override void _Ready() {
			rulesetSelect = GetNode<OptionButton>(RulesetSelectPath);
			p1Frontend = GetNode<TestFrontendPlayer>(Player1Path);
			p2Frontend = GetNode<TestFrontendPlayer>(Player2Path);
			log = GetNode<Label>(LogPath);

			rulesetSelect.Clear();
			var ruleFiles = DirAccess.GetFilesAt(RULESET_FOLDER);
			for(int i = 0; i < ruleFiles.Length; i++) {
				rulesetSelect.AddItem(ruleFiles[i], i);
				rulesetSelect.SetItemMetadata(i, RULESET_FOLDER + "/" + ruleFiles[i]);
			}

			InitGame();
		}

		private void InitGame() {
			board = new Board() {
				Rules = GD.Load<Rules>(rulesetSelect.GetSelectedMetadata().AsString()),
				Player1 = new Player(),
				Player2 = new Player()
			};

			p1Frontend.Init(board.Player1);
			p2Frontend.Init(board.Player2);

			frontend.SetPlayerFrontend(board.Player1, p1Frontend);
			frontend.SetPlayerFrontend(board.Player2, p2Frontend);
		}

		private void OnActPressed() {
			p1Frontend.UpdateWheels(board.Player1);
			p2Frontend.UpdateWheels(board.Player2);

			controller.Act(frontend, board);
			log.Text = string.Join('\n', frontend.PhaseLogs.SelectMany(x => x.Logs.Prepend($"__{x.Phase}__")));
			
		}
	}
}
