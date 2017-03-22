using OpenCNCPilot.Communication;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace OpenCNCPilot
{
	partial class MainWindow
	{
		List<Tuple<string, string>> Macros = new List<Tuple<string, string>>();

		private void RunMacro(string commands)
		{
			if (machine.Mode != Machine.OperatingMode.Manual)
				return;

			foreach (string c in commands.Split('\n'))
				machine.SendLine(c);
		}

		private void RefreshMacroButtons()
		{
			StackPanelMacros.Children.Clear();
			for (int i = 0; i < Macros.Count; i++)
			{
				int index = i;

				Button b = new Button();
				b.Content = Macros[i].Item1;
				b.Margin = new Thickness(2, 0, 2, 2);
				b.Click += (sender, e) => { RunMacro(Macros[index].Item2); };

				MenuItem editItem = new MenuItem();
				editItem.Header = "Edit";
				editItem.Click += (s, e) =>
				{
					var emiw = new EditMacroItemWindow(Macros[index].Item1, Macros[index].Item2);
					emiw.ShowDialog();
					if (emiw.Ok)
					{
						Macros[index] = new Tuple<string, string>(emiw.MacroName, emiw.Commands);
						SaveMacros();
						RefreshMacroButtons();
					}
				};

				MenuItem removeItem = new MenuItem();
				removeItem.Header = "Remove";
				removeItem.Click += (s, e) => { Macros.RemoveAt(index); RefreshMacroButtons(); };

				ContextMenu menu = new ContextMenu();
				menu.Items.Add(editItem);
				menu.Items.Add(removeItem);

				if (i > 0)
				{
					MenuItem moveUpItem = new MenuItem();
					moveUpItem.Header = "Move Up";

					moveUpItem.Click += (s, e) =>
					{
						var macro = Macros[index];
						Macros.RemoveAt(index);
						Macros.Insert(index - 1, macro);

						SaveMacros();
						RefreshMacroButtons();
					};

					menu.Items.Add(moveUpItem);
                }

				b.ContextMenu = menu;

				StackPanelMacros.Children.Add(b);
			}
		}

		private void SaveMacros()
		{
			StringBuilder b = new StringBuilder();

			foreach (var m in Macros)
			{
				b.Append($"{m.Item1}:{m.Item2};");
			}

			Properties.Settings.Default.Macros = b.ToString();
		}

		private void LoadMacros()
		{
			Macros.Clear();

			var regexMacro = new Regex("([^:;]+):([^:;]+);");

			foreach (System.Text.RegularExpressions.Match m in regexMacro.Matches(Properties.Settings.Default.Macros))
			{
				Macros.Add(new Tuple<string, string>(m.Groups[1].Value, m.Groups[2].Value));
			}

			RefreshMacroButtons();
		}

		private void ButtonAddMacro_Click(object sender, RoutedEventArgs e)
		{
			Macros.Add(new Tuple<string, string>("New Macro", ""));
			RefreshMacroButtons();
		}
	}
}
