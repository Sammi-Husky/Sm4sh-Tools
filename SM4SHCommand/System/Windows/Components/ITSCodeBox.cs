using FastColoredTextBoxNS;
using SALT.Scripting.AnimCMD;
using Sm4shCommand.Classes;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using SALT.Scripting;

namespace Sm4shCommand
{
    public class ITS_EDITOR : FastColoredTextBox
    {
        TextStyle keywordStyle = new TextStyle(Brushes.Blue, null, FontStyle.Regular);
        TextStyle HexStyle = new TextStyle(Brushes.DarkCyan, null, FontStyle.Regular);
        TextStyle DecStyle = new TextStyle(Brushes.Red, null, FontStyle.Regular);
        TextStyle StrStyle = new TextStyle(Brushes.Chocolate, null, FontStyle.Regular);
        TextStyle CommentStyle = new TextStyle(Brushes.DarkGreen, null, FontStyle.Regular);

        public AutocompleteMenu AutocompleteMenu { get; set; }

        public ITS_EDITOR()
        {
            this.TextChanged += NewBox_TextChanged;
            this.AutoCompleteBrackets = true;
            this.AutoIndent = true;
        }
        public ITS_EDITOR(IScript script) : this()
        {
            Script = script;
            Text = script.Deserialize();
        }
        public ITS_EDITOR(IScript script, string[] autocomplete) : this()
        {
            Script = script;
            Text = script.Deserialize();
            if (autocomplete != null)
            {
                this.AutocompleteMenu = new AutocompleteMenu(this) { AppearInterval = 1 };
                this.AutocompleteMenu.Items.SetAutocompleteItems(autocomplete);
                this.AutoCompleteBrackets = true;
                this.AutoIndent = true;
            }
        }
        public IScript Script { get; set; }
        public void ApplyChanges()
        {
            try
            {
                Script.Serialize(Text);
            }
            catch {; }
        }
        private void NewBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            //clear previous highlighting
            e.ChangedRange.ClearStyle(StyleIndex.All);
            //highlight tags
            e.ChangedRange.SetStyle(keywordStyle, @"(?<=[\(,])+[^=)]+(?==)\b");
            e.ChangedRange.SetStyle(HexStyle, @"0x[^\)\s,\r\n]+");
            e.ChangedRange.SetStyle(DecStyle, @"\b(?:[0-9]*\\.)?[0-9]+\b");
            e.ChangedRange.SetStyle(StrStyle, "\"(\\.|[^\"])*\"");
        }
    }
}