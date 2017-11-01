using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace TicTacToe
{
    public enum TicTacToePlayer : byte
    {
        None,
        X,
        O
    }

    public class GridChangedArgs : EventArgs
    {       
        /// Row number of the cell that changed, or -1 if multiple cells have changed.
        
        public int GridRow { get; set; }
        /// Column number of the cell that changed, or -1 if multiple cells have changed.
        public int GridColumn { get; set; }
    }

    public delegate void GridChangedEventHandler(object sender, GridChangedArgs e);
    public delegate void PlayerChangedEventHandler(object sender, EventArgs e);
    public delegate void GameStartedEventHandler(object sender, EventArgs e);
    public delegate void GameOverEventHandler(object sender, EventArgs e);

    /// <summary>
    /// Engine to play a game of TicTacToe.
    /// 
    /// Supports arbitrarily sized playing grid, with the restriction that the number of rows must equal the number
    /// of columns.
    
    /// - To start a new game, call Reset().
    /// - To implement a player move, determine the grid coordinates from user input and call Move(). Use CanMove()
    ///   to determine if a particular move is valid. This moves for the current player.
    /// - To implement a automated computer move, call AutoMove(). This moves for the current player.
    /// 
    /// GameIsOver returns true when the game is over. When true, WinningPlayer has the player that won, or
    /// TicTacToePlayer.None if the grid is full and the game is tied. And WinningPath contains the list of
    /// game cells that consitute the winning cells.
    /// </remarks>
    public class TicTacToeGameClass
    {
        // Private member variables
        private TicTacToePlayer[,] Grid;
        private List<WinPath> WinPaths;

        // Public events
        public event GridChangedEventHandler GridChanged;
        public event PlayerChangedEventHandler PlayerChanged;
        public event GameStartedEventHandler GameStarted;
        public event GameOverEventHandler GameOver;

        /// <summary>
        /// Returns the current player, or TicTacToePlayer.None if the game is over.
        /// </summary>
        public TicTacToePlayer CurrentPlayer { get; private set; }

        /// <summary>
        /// Returns the winning player, or TicTacToePlayer.None if the game is not
        /// over or the game ended in a tie.
        /// </summary>
        public TicTacToePlayer WinningPlayer { get; private set; }

        /// <summary>
        /// Returns the path (series of grid coordinates) that won the game, or null
        /// if the game is not over or the game ended in a tie.
        /// </summary>
        public WinPath WinningPath { get; private set; }

        /// <summary>
        /// Indicates if the game has ended.
        /// </summary>
        public bool GameIsOver { get { return (CurrentPlayer == TicTacToePlayer.None); } }

        /// <summary>
        /// Gets the width and height of the game grid. Use SetGridSize() to change.
        /// </summary>
        public int GridSize { get; private set; }

        /// <summary>
        /// Constructs a new instance of the TicTacToeEngine class.
        /// </summary>
        public TicTacToeGameClass(TicTacToePlayer initialPlayer = TicTacToePlayer.X)
        {
            GridSize = 4;
            // Reset grid
            InitializeGrid();
            // Restart game
            Reset(initialPlayer);
        }

        /// <summary>
        /// Resets and restarts the game.
        /// </summary>
        public void Reset(TicTacToePlayer initialPlayer = TicTacToePlayer.X)
        {
            // Reset grid
            Grid.UpdateEach((p, r, c) => TicTacToePlayer.None);
            WinningPlayer = TicTacToePlayer.None;
            WinningPath = null;
            // Set first player
            CurrentPlayer = initialPlayer;
            // Signal changes
            FireGameStarted();
            FireGridChanged(-1, -1);
            FirePlayerChanged();
        }

        /// <summary>
        /// Indicates if the current player can move to the specified location.
        /// </summary>
        /// <param name="row">Location's row position</param>
        /// <param name="col">Location's column position</param>
        public bool CanMove(int row, int col)
        {
            return (!GameIsOver && Grid[row, col] == TicTacToePlayer.None);
        }

        /// <summary>
        /// Implements a move for the current player at the specified location. This method takes
        /// appropriate action if the move ends the game; otherwise, it makes the other player the
        /// current player. Returns false if the move is invalid.
        /// </summary>
        /// <param name="row">Location's row position</param>
        /// <param name="col">Location's column position</param>
        public bool Move(int row, int col)
        {
            // Verify move is valid
            if (!CanMove(row, col))
                return false;
            // Make move
            Grid[row, col] = CurrentPlayer;
            FireGridChanged(row, col);
            // Check for game over
            UpdateGameState();
            if (!GameIsOver)
            {
                // Switch players
                Debug.Assert(CurrentPlayer != TicTacToePlayer.None);
                CurrentPlayer = CurrentPlayer.GetOtherPlayer();
                FirePlayerChanged();
            }
            return true;
        }

        /// <summary>
        /// Uses heuristics to make the best move for the current player.
        /// </summary>
        public void AutoMove()
        {
            if (GameIsOver)
                return;

            // Can player win?
            Random random = new Random();
            WinPath pathToWin = WinPaths.Where(p => p.MovesToWin(Grid, CurrentPlayer) == 1).Random(random);
            if (pathToWin != null)
            {
                var move = pathToWin.First(m => Grid[m.Row, m.Column] == TicTacToePlayer.None);
                Move(move.Row, move.Column);
                return;
            }

            // Do we need to block win from other player?
            TicTacToePlayer otherPlayer = CurrentPlayer.GetOtherPlayer();
            WinPath pathToLose = WinPaths.Where(p => p.MovesToWin(Grid, otherPlayer) == 1).Random(random);
            if (pathToLose != null)
            {
                var move = pathToLose.First(m => Grid[m.Row, m.Column] == TicTacToePlayer.None);
                Move(move.Row, move.Column);
                return;
            }

            // Get additonal information about available moves
            var movesAvailable = GetMoves(TicTacToePlayer.None);
            Debug.Assert(movesAvailable.Count > 0);

            // Score available moves according to the number of moves required to complete
            // each potential win
            foreach (var move in movesAvailable)
            {
                // Create grid to test this move
                var grid = (TicTacToePlayer[,])Grid.Clone();
                grid[move.Row, move.Column] = CurrentPlayer;
                // Score move based on number of moves required for each potential win
                move.UserData = (int)WinPaths.Sum(p => {
                    int count = p.MovesToWin(grid, CurrentPlayer);
                    return (count < 0) ? count : (GridSize - count);
                });
            }
            // Make move with the highest score
            int maxScore = movesAvailable.Max(m => m.UserData);
            var mov = movesAvailable.Where(m => m.UserData == maxScore).Random(random);
            Move(mov.Row, mov.Column);
            return;
        }

        /// <summary>
        /// Returns the player occupying the specified game cell, or
        /// TicTacTowPlayer.None if no player occupies that cell.
        /// </summary>
        /// <param name="row">Row position of game cell</param>
        /// <param name="col">Column position of game cell</param>
        public TicTacToePlayer GetPlayerAt(int row, int col)
        {
            return Grid[row, col];
        }

        /// <summary>
        /// Returns the path (series of game cells) the specified player used to win the game.
        /// Returns null if the specified player did not win the current game.
        /// </summary>
        private WinPath FindWinningPath(out TicTacToePlayer winningPlayer)
        {
            winningPlayer = TicTacToePlayer.None;
            foreach (WinPath path in WinPaths)
            {
                winningPlayer = path.GetWinningPlayer(Grid);
                if (winningPlayer != TicTacToePlayer.None)
                    return path;
            }
            return null;
        }

        /// <summary>
        /// Sets the width and height of the game grid. Setting the grid size will reset
        /// any game in progress.
        /// </summary>
        public void SetGridSize(int gridSize, TicTacToePlayer initialPlayer = TicTacToePlayer.X)
        {
            // Don't allow any value
            if (gridSize < 2 || gridSize > 32)
                throw new ApplicationException("Invalid grid size specified");
            GridSize = gridSize;
            // Reset grid
            InitializeGrid();
            // Restart game
            Reset(initialPlayer);
        }

        #region Private Helper Methods

        /// <summary>
        /// Allocates the grid and builds lists of winning paths
        /// </summary>
        private void InitializeGrid()
        {
            // Allocate grid
            Grid = new TicTacToePlayer[GridSize, GridSize];

            // Build list of winning paths
            WinPaths = new List<WinPath>();
            // Diagonals
            var rightDownPath = new WinPath();
            var rightUpPath = new WinPath();
            for (int i = 0; i < GridSize; i++)
            {
                rightDownPath.Add(new TicTacToeMove(i, i));
                rightUpPath.Add(new TicTacToeMove(i, GridSize - i - 1));
            }
            WinPaths.Add(rightDownPath);
            WinPaths.Add(rightUpPath);
            // Rows
            for (int row = 0; row < GridSize; row++)
            {
                var path = new WinPath();
                for (int col = 0; col < GridSize; col++)
                    path.Add(new TicTacToeMove(row, col));
                WinPaths.Add(path);
            }
            // Columns
            for (int col = 0; col < GridSize; col++)
            {
                var path = new WinPath();
                for (int row = 0; row < GridSize; row++)
                    path.Add(new TicTacToeMove(row, col));
                WinPaths.Add(path);
            }
        }

        /// <summary>
        /// Checks if the game is over and sets the current game state accordingly.
        /// </summary>
        private void UpdateGameState()
        {
            if (!GameIsOver)
            {
                TicTacToePlayer winningPlayer;
                WinningPath = FindWinningPath(out winningPlayer);
                if (WinningPath != null)
                {
                    // A player has won
                    WinningPlayer = winningPlayer;
                    CurrentPlayer = TicTacToePlayer.None;
                }
                else if (!IsMoveAvailable)
                {
                    // Tied game
                    WinningPlayer = TicTacToePlayer.None;
                    CurrentPlayer = TicTacToePlayer.None;
                }
                // Fire notification events
                if (GameIsOver)
                {
                    FirePlayerChanged();
                    FireGameOver();
                }
            }
        }

        /// <summary>
        /// Returns all game cells that contain the specified player.
        /// </summary>
        /// <param name="player">The player to match</param>
        private List<TicTacToeMove> GetMoves(TicTacToePlayer player)
        {
            List<TicTacToeMove> moves = new List<TicTacToeMove>();
            Grid.ForEach((p, r, c) => { if (p == player) moves.Add(new TicTacToeMove(r, c)); });
            return moves;
        }

        /// <summary>
        /// Indicates if the grid has any empty cells where a move could be made. Does not consider the case
        /// where a user has already won.
        /// </summary>
        private bool IsMoveAvailable
        {
            get { return Grid.Any((p, r, c) => (p == TicTacToePlayer.None)); }
        }

        #endregion

        #region Event generators

        private void FireGridChanged(int row = -1, int col = -1)
        {
            if (GridChanged != null)
            {
                var args = new GridChangedArgs();
                args.GridRow = row;
                args.GridColumn = col;
                GridChanged(this, args);
            }
        }

        private void FirePlayerChanged()
        {
            if (PlayerChanged != null)
            {
                var args = new EventArgs();
                PlayerChanged(this, args);
            }
        }

        private void FireGameStarted()
        {
            if (GameStarted != null)
            {
                var args = new EventArgs();
                GameStarted(this, args);
            }
        }

        private void FireGameOver()
        {
            if (GameOver != null)
            {
                var args = new EventArgs();
                GameOver(this, args);
            }
        }

        #endregion

    }

    #region TicTacToe Move

    /// <summary>
    /// Stores a information about a particular game move, including an extra member
    /// for storing additional integer data about that move.
    /// </summary>
    public class TicTacToeMove
    {
        public int Row { get; set; }
        public int Column { get; set; }
        public int UserData { get; set; }

        public TicTacToeMove(int row, int col)
        {
            Row = row; Column = col; UserData = 0;
        }

        public bool IsCorner(int gridSize)
        {
            return (Row == 0 || Row == (gridSize - 1)) && (Column == 0 || Column == (gridSize - 1));
        }
    }

    #endregion

    #region Winning paths

    /// <summary>
    /// Tracks a winning path, which is a series of game moves that can be used to win the game.
    /// </summary>
    public class WinPath : List<TicTacToeMove>
    {
        /// <summary>
        /// Returns the number of moves required for the given player to win this path. Returns
        /// -1 of the player cannot win this path.
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="player"></param>
        /// <returns></returns>
        public int MovesToWin(TicTacToePlayer[,] grid, TicTacToePlayer player)
        {
            Debug.Assert(player != TicTacToePlayer.None);
            // Cannot win path if other player owns cell
            TicTacToePlayer otherPlayer = player.GetOtherPlayer();
            if (this.Any(p => grid[p.Row, p.Column] == otherPlayer))
                return -1;
            // Else return number of unowned cells
            return this.Count(p => grid[p.Row, p.Column] == TicTacToePlayer.None);
        }

        /// <summary>
        /// Returns the winning player for this path, or TicTacToePlayer.None if no
        /// player has won this path.
        /// </summary>
        /// <param name="grid"></param>
        /// <returns></returns>
        public TicTacToePlayer GetWinningPlayer(TicTacToePlayer[,] grid)
        {
            Debug.Assert(Count > 0);
            TicTacToePlayer player = grid[this[0].Row, this[0].Column];
            if (player != TicTacToePlayer.None && this.All(p => grid[p.Row, p.Column] == player))
                return player;
            return TicTacToePlayer.None;
        }
    }

    #endregion

    #region Extension method helpers

    /// <summary>
    /// Helper extension methods
    /// </summary>
    public static class GridHelper
    {
        /// <summary>
        /// Returns true if any items in the grid meet the specified condition.
        /// </summary>
        /// <typeparam name="T">Type of object stored in the grid</typeparam>
        /// <param name="predicate">A function that tests if each item meets the required condition</param>
        public static bool Any<T>(this T[,] grid, Func<T, int, int, bool> predicate) where T : struct
        {
            for (int row = grid.GetLowerBound(0); row <= grid.GetUpperBound(0); row++)
                for (int col = grid.GetLowerBound(1); col <= grid.GetUpperBound(1); col++)
                    if (predicate(grid[row, col], row, col))
                        return true;
            return false;
        }

        /// <summary>
        /// Counts the number of items in the grid that meet the specified condition.
        /// </summary>
        /// <typeparam name="T">Type of object stored in the grid</typeparam>
        /// <param name="predicate">A function that tests if each item meets the required condition</param>
        public static int Count<T>(this T[,] grid, Func<T, int, int, bool> predicate) where T : struct
        {
            int count = 0;
            for (int row = grid.GetLowerBound(0); row <= grid.GetUpperBound(0); row++)
                for (int col = grid.GetLowerBound(1); col <= grid.GetUpperBound(1); col++)
                    if (predicate(grid[row, col], row, col))
                        count++;
            return count;
        }

        /// <summary>
        /// Returns the first grid item that meet the specified condition.
        /// </summary>
        /// <typeparam name="T">Type of object stored in the grid</typeparam>
        /// <param name="predicate">A function that tests if each item meets the required condition</param>
        public static T First<T>(this T[,] grid, Func<T, int, int, bool> predicate) where T : struct
        {
            for (int row = grid.GetLowerBound(0); row <= grid.GetUpperBound(0); row++)
                for (int col = grid.GetLowerBound(1); col <= grid.GetUpperBound(1); col++)
                    if (predicate(grid[row, col], row, col))
                        return grid[row, col];
            return default(T);
        }

        /// <summary>
        /// Calls the given action for each item in the grid.
        /// </summary>
        /// <typeparam name="T">Type of object stored in the grid</typeparam>
        /// <param name="grid"></param>
        /// <param name="action">Action that performs a task on each item</param>
        public static void ForEach<T>(this T[,] grid, Action<T, int, int> action) where T : struct
        {
            for (int row = grid.GetLowerBound(0); row <= grid.GetUpperBound(0); row++)
                for (int col = grid.GetLowerBound(1); col <= grid.GetUpperBound(1); col++)
                    action(grid[row, col], row, col);
        }

        /// <summary>
        /// Updates the value of each item in the grid.
        /// </summary>
        /// <typeparam name="T">Type of object stored in the grid</typeparam>
        /// <param name="func">Function that returns the new value for each item</param>
        public static void UpdateEach<T>(this T[,] grid, Func<T, int, int, T> func) where T : struct
        {
            for (int row = grid.GetLowerBound(0); row <= grid.GetUpperBound(0); row++)
                for (int col = grid.GetLowerBound(1); col <= grid.GetUpperBound(1); col++)
                    grid[row, col] = func(grid[row, col], row, col);
        }

        /// <summary>
        /// Returns a random element from an IEnumerable<T> collection.
        /// </summary>
        /// <typeparam name="T">Type of object stored in the collection</typeparam>
        /// <param name="list">Collection of items</param>
        /// <param name="random">An instance of a random number generator</param>
        public static T Random<T>(this IEnumerable<T> list, Random random)
        {
            if (list == null || list.Count() == 0)
                return default(T);
            return list.ElementAt(random.Next(list.Count()));
        }

        /// <summary>
        /// Returns the player who is the opponent of the current player. Returns TicTacToePlayer.X
        /// if the current player is TicTacTowPlayer.None.
        /// </summary>
        public static TicTacToePlayer GetOtherPlayer(this TicTacToePlayer player)
        {
            Debug.Assert(player != TicTacToePlayer.None);
            return (player == TicTacToePlayer.X) ? TicTacToePlayer.O : TicTacToePlayer.X;
        }

        #endregion

    }
}
