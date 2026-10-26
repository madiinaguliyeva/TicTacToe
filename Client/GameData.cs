namespace TicTacToe
{
    public class GameData
    {
        public string? Gamer1 { get; set; }
        public string? Gamer2 { get; set; }
        public string? CurrentTurn { get; set; }
        public string? MessageType { get; set; }
        public string? Winner { get; set; }

        public int[][] Game { get; set; } = new int[3][]
        {
            new int[] { 0, 0, 0 },
            new int[] { 0, 0, 0 },
            new int[] { 0, 0, 0 }
        };
    }
}