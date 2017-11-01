using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace TicTacToe
{
    public partial class frmOptions : Form
    {
        public Options Options = new Options();

        public frmOptions()
        {
            InitializeComponent();
        }

        private void frmOptions_Load(object sender, EventArgs e)
        {
            cboUserPlayer.Items.Clear();
            cboUserPlayer.Items.Add(new KeyValuePair<string,TicTacToePlayer>("X", TicTacToePlayer.X));
            cboUserPlayer.Items.Add(new KeyValuePair<string,TicTacToePlayer>("O", TicTacToePlayer.O));
            cboUserPlayer.LookupAndSetValue(Options.UserPlayer);

            chkComputerPlaysFirst.Checked = Options.ComputerPlaysFirst;

            cboGridSize.Items.Clear();
            for (int i = 2; i <= 12; i++)
                cboGridSize.Items.Add(new KeyValuePair<string,int>(i.ToString(), i));
            cboGridSize.LookupAndSetValue(Options.GridSize);
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            // Alert user if change we reset current game
            int gridSize = ((KeyValuePair<string, int>)cboGridSize.SelectedItem).Value;
            if (gridSize != Options.GridSize && Options.GameInProgress &&
                MessageBox.Show(this, "Changing the grid size will reset the current game.", "Reset Game",
                    MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) != DialogResult.OK)
            {
                DialogResult = DialogResult.None;
                return;
            }
            // Update options
            Options.GridSize = gridSize;
            Options.UserPlayer = ((KeyValuePair<string, TicTacToePlayer>)cboUserPlayer.SelectedItem).Value;
            Options.ComputerPlaysFirst = chkComputerPlaysFirst.Checked;
        }
    }

    /// <summary>
    /// Options displayed in the Options dialog box.
    /// </summary>
    public class Options
    {
        public int GridSize { get; set; }
        public TicTacToePlayer UserPlayer { get; set; }
        public bool ComputerPlaysFirst { get; set; }
        public bool GameInProgress { get; set; }
    }

    /// <summary>
    /// Extension method class to implement setting a ComboBox selected index from
    /// the value that should be set. (SelectedValue only sets with bound data.)
    /// </summary>
    static class ComboBoxHelper
    {
        public static void LookupAndSetValue(this ComboBox combobox, object value)
        {
            if (combobox.Items.Count > 0)
            {
                for (int i = 0; i < combobox.Items.Count; i++)
                {
                    object item = combobox.Items[i];
                    object thisValue = item.GetType().GetProperty(combobox.ValueMember).GetValue(item);
                    if (thisValue != null && thisValue.Equals(value))
                    {
                        combobox.SelectedIndex = i;
                        return;
                    }
                }
                // Select first item if requested item was not found
                combobox.SelectedIndex = 0;
            }
        }
    }
}
