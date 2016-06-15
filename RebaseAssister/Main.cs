﻿using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Kbg.NppPluginNET.PluginInfrastructure;
using RebaseAssister;

namespace Kbg.NppPluginNET
{
    class Main
    {
        internal const string PluginName = "RebaseAssister";

		private static bool isPluginActive = false;

		private static FirstWordOfLineSelector firstWordSelector = new FirstWordOfLineSelector();

		// reduce the amount of selections
		private static Position lastPositionWhenUiUpdate = null;

		public static void OnNotification(ScNotification notification)
		{
			if (notification.Header.Code == (ulong)NppMsg.NPPN_BUFFERACTIVATED)
			{
				isPluginActive = IsGitRebaseFile();
				return;
			}

			if (notification.Header.Code == (ulong)NppMsg.NPPN_FILEOPENED)
			{
				if (IsGitRebaseFile())
				{
					var scintillaGateway = new ScintillaGateway(PluginBase.GetCurrentScintilla());

					AddTextToRebaseFile(scintillaGateway);

					DisableAutoCompletePopup(scintillaGateway);

					SetSyntaxHighlighting();
				}

				return;
			}

			EnsureFirstWordIsSelected(notification);
		}

	    private static void EnsureFirstWordIsSelected(ScNotification notification)
	    {
		    if (isPluginActive)
		    {
			    if (notification.Header.Code == (ulong) SciMsg.SCN_UPDATEUI)
			    {
				    var scintillaGateway = new ScintillaGateway(PluginBase.GetCurrentScintilla());
				    var currentPosition = scintillaGateway.GetCurrentPos();
				    if (currentPosition != lastPositionWhenUiUpdate)
				    {
					    lastPositionWhenUiUpdate = firstWordSelector.SelectFirstWordOfLine(scintillaGateway);
				    }
				    return;
			    }

			    if (notification.Header.Code == (ulong) SciMsg.SCN_MODIFIED)
			    {
				    var isTextInsertedOrDeleted = (notification.ModificationType &
				                                   ((int) SciMsg.SC_MOD_INSERTTEXT | (int) SciMsg.SC_MOD_DELETETEXT)) > 0;
				    if (isTextInsertedOrDeleted)
				    {
					    var scintillaGateway = new ScintillaGateway(PluginBase.GetCurrentScintilla());
					    firstWordSelector.SelectFirstWordOfLine(scintillaGateway);
				    }
			    }
		    }
	    }

	    private static void SetSyntaxHighlighting()
		{
			new NotepadPPGateway().SetCurrentLanguage(LangType.L_INI);
		}

		private static void DisableAutoCompletePopup(ScintillaGateway scintillaGateway)
		{
			scintillaGateway.AutoCStops("abcdefghijklmnopqrstuvwxyz");
		}

		private static void AddTextToRebaseFile(ScintillaGateway scintillaGateway)
		{
			string additionalText = @"#
# Use Ctrl+Shift+Down to move lines down
# Use Ctrl+Shift+Up to move lines up
";
			scintillaGateway.AppendText(additionalText.Length, additionalText);
		}

		static bool IsGitRebaseFile()
		{
			const string GitRebaseFilename = "git-rebase-todo";

			var fileName = Path.GetFileName(new NotepadPPGateway().GetCurrentFilePath());

			return fileName == GitRebaseFilename;
		}

		internal static void CommandMenuInit()
        {
            StringBuilder sbIniFilePath = new StringBuilder(Win32.MAX_PATH);
            Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_GETPLUGINSCONFIGDIR, Win32.MAX_PATH, sbIniFilePath);

            PluginBase.SetCommand(0, "About RebaseAssister", ShowAbout, new ShortcutKey(false, false, false, Keys.None));
        }

        internal static void SetToolBarIcon()
        {
        }

        internal static void PluginCleanUp()
        {
        }

        private static void ShowAbout()
        {
            var message = @"Version: 1.00
Assist you when you are doing interactive rebasing in Git/Hg/...

License: This is freeware (Apache v2.0 license).

Author: Kasper B. Graversen 2016-

Website: https://github.com/kbilsted/NppPluginRebaseAssister";
            var title = "RebaseAssister plugin";
            MessageBox.Show(message, title, MessageBoxButtons.OK);
        }
    }
}
