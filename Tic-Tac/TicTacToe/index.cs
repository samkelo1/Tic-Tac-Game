
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace TicTacToe
{
    public partial class index : Form
    {
        private static int GridMargin = 8;  // Margin around Xs and Os
        private TicTacToePlayer UserPlayer { get; set; }
        private bool ComputerPlaysFirst { get; set; }

        private TicTacToeGameClass GameEngine;

        public index()
        {
            InitializeComponent();

            // Create game engine with default settings
            GameEngine = new TicTacToeGameClass();
            GameEngine.SetGridSize(4);
            UserPlayer = TicTacToePlayer.X;
            ComputerPlaysFirst = false;

            // Attach event handlers
            GameEngine.GridChanged += GameEngine_GridChanged;
            GameEngine.PlayerChanged += GameEngine_PlayerChanged;
            GameEngine.GameStarted += GameEngine_GameStarted;
            GameEngine.GameOver += GameEngine_GameOver;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            RestartGame();
            toolStripContainer1_ContentPanel_Resize(this, e);
        }

        private void tsbRestart_Click(object sender, EventArgs e)
        {
            RestartGame();
        }

        private void picGame_MouseDown(object sender, MouseEventArgs e)
        {
            // Make user's move
            Rectangle rect = picGame.ClientRectangle;
            int col = e.X / (rect.Width / GameEngine.GridSize);
            int row = e.Y / (rect.Height / GameEngine.GridSize);
            if (GameEngine.CanMove(row, col))
            {
                GameEngine.Move(row, col);  // Make user move
                GameEngine.AutoMove();      // Make computer move
            }
        }

        private void toolStripContainer1_ContentPanel_Resize(object sender, EventArgs e)
        {
            // Size game board to fit container, but keep board square
            Rectangle rect = picGame.Parent.ClientRectangle;
            int size = Math.Max(100, Math.Min(rect.Width, rect.Height) - 18);
            picGame.SetBounds((rect.Width - size) / 2, (rect.Height - size) / 2, size, size);
            picGame.Invalidate();
        }

        private void picGame_Paint(object sender, PaintEventArgs e)
        {
            PaintGameGrid(e.Graphics);
        }

        private void optionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            frmOptions frm = new frmOptions();
            frm.Options.UserPlayer = UserPlayer;
            frm.Options.ComputerPlaysFirst = ComputerPlaysFirst;
            frm.Options.GridSize = GameEngine.GridSize;
            frm.Options.GameInProgress = !GameEngine.GameIsOver;

            if (frm.ShowDialog() == DialogResult.OK)
            {
                UserPlayer = frm.Options.UserPlayer;
                ComputerPlaysFirst = frm.Options.ComputerPlaysFirst;
                if (frm.Options.GridSize != GameEngine.GridSize)
                {
                    GameEngine.SetGridSize(frm.Options.GridSize);
                    RestartGame();
                }
                else if (!GameEngine.GameIsOver &&
                    GameEngine.CurrentPlayer != UserPlayer)
                    GameEngine.AutoMove();
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void RestartGame()
        {
            // Start a new game
            GameEngine.Reset(ComputerPlaysFirst ? UserPlayer.GetOtherPlayer() : UserPlayer);
            if (ComputerPlaysFirst)
                GameEngine.AutoMove();
        }

        #region Game Engine Event Handlers

        void GameEngine_GameStarted(object sender, EventArgs e)
        {
            lblStatus.Text = String.Empty;
        }

        void GameEngine_GridChanged(object sender, GridChangedArgs e)
        {
            picGame.Invalidate();
        }

        void GameEngine_PlayerChanged(object sender, EventArgs e)
        {
            // Update status
            string status = String.Empty;
            if (GameEngine.CurrentPlayer != TicTacToePlayer.None)
            {
                if (GameEngine.CurrentPlayer == UserPlayer)
                    status = "Play It's your turn...";
                else
                    status = "Thinking...";
            }
            lblStatus.Text = status;
        }

        void GameEngine_GameOver(object sender, EventArgs e)
        {
            lblStatus.Text = (GameEngine.WinningPlayer == TicTacToePlayer.None) ?
                "Tough Game Draw" :
                String.Format("Computer  Wins!", GameEngine.WinningPlayer);
        }

        #endregion

        #region Game Grid Painting

        private void PaintGameGrid(Graphics g)
        {
            var size = GameEngine.GridSize;

            // Get grid rectangle
            Rectangle rect = picGame.ClientRectangle;
            // Create margin around grid
            rect.Inflate(-GridMargin, -GridMargin);
            if (rect.Width <= 0 || rect.Height <= 0)
                return;

            // Get the size of each grid cell
            int cxCell = rect.Width / size;
            int cyCell = rect.Height / size;

            // Create pen
            Pen line = new Pen(Color.Gray, 4);
            line.SetLineCap(LineCap.Round, LineCap.Round, DashCap.Round);
            g.SmoothingMode = SmoothingMode.HighQuality;
            // Draw grid lines
            for (int i = 1; i < size; i++)
            {
                // Vertical
                g.DrawLine(line, rect.Left + (i * cxCell), rect.Top, rect.Left + (i * cxCell), rect.Bottom);
                // Horizontal
                g.DrawLine(line, rect.Left, rect.Top + (i * cyCell), rect.Right, rect.Top + (i * cyCell));
            }
            // Draw player symbols
            for (int row = 0; row < GameEngine.GridSize; row++)
            {
                for (int col = 0; col < GameEngine.GridSize; col++)
                {
                    var value = GameEngine.GetPlayerAt(row, col);
                    if (value == TicTacToePlayer.X)
                        DrawX(g, new Rectangle(rect.Left + col * cxCell, rect.Top + row * cyCell, cxCell, cyCell));
                    else if (value == TicTacToePlayer.O)
                        DrawO(g, new Rectangle(rect.Left + col * cxCell, rect.Top + row * cyCell, cxCell, cyCell));
                }
            }
            // Draw line through winning path, if any
            DrawLineThroughWin(g, cxCell, cyCell);
        }

        private void DrawX(Graphics g, Rectangle rect)
        {
            // Create margin around symbol
            rect.Inflate(-GridMargin, -GridMargin);
            if (rect.Width <= 0 || rect.Height <= 0)
                return;
            // Draw "X"
            Pen line = new Pen(Color.Blue, 8);
            line.SetLineCap(LineCap.Round, LineCap.Round, DashCap.Round);
            g.DrawLine(line, rect.Left, rect.Top, rect.Right, rect.Bottom);
            g.DrawLine(line, rect.Right, rect.Top, rect.Left, rect.Bottom);
        }

        private void DrawO(Graphics g, Rectangle rect)
        {
            // Create margin around symbol
            rect.Inflate(-GridMargin, -GridMargin);
            if (rect.Width <= 0 || rect.Height <= 0)
                return;
            // Draw "O"
            Pen line = new Pen(Color.Magenta, 8);
            g.DrawEllipse(line, rect);
        }

        private void DrawLineThroughWin(Graphics g, int cxCell, int cyCell)
        {
            if (GameEngine.WinningPath != null)
            {
                var startMove = GameEngine.WinningPath.First();
                Point start = new Point(startMove.Column, startMove.Row);
                var endMove = GameEngine.WinningPath.Last();
                Point end = new Point(endMove.Column, endMove.Row);

                start.X = (start.X * cxCell) + (cxCell / 2) + GridMargin;
                start.Y = (start.Y * cyCell) + (cyCell / 2) + GridMargin;
                end.X = (end.X * cxCell) + (cxCell / 2) + GridMargin;
                end.Y = (end.Y * cyCell) + (cyCell / 2) + GridMargin;

                // Extend line to edge of grid
                if (startMove.Row != endMove.Row)
                {
                    int factor = (startMove.Row < endMove.Row) ? -1 : 1;
                    start.Y += (cyCell / 2) * factor;
                    end.Y -= (cyCell / 2) * factor;
                }
                if (startMove.Column != endMove.Column)
                {
                    int factor = (startMove.Column < endMove.Column) ? -1 : 1;
                    start.X += (cyCell / 2) * factor;
                    end.X -= (cyCell / 2) * factor;
                }

                Pen line = new Pen(Color.Red, 8);
                line.SetLineCap(LineCap.Round, LineCap.Round, DashCap.Round);
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.DrawLine(line, start, end);
            }
        }

        #endregion

    }
}
