using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using TicTacToe;

namespace Server
{
    internal class Program
    {
        private static readonly string ServerIp = "192.168.100.81";
        private static readonly int ServerPort = 5001;

        private static UdpClient serverUdp;
        private static IPEndPoint clientEP;
        private static string localPlayerName = "";
        private static bool isMyTurn = false;

        private static int[,] board = new int[3, 3];
        private static GameData gameState = new GameData();

        static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.Write("Server Oyunçusunun Adı (Oyunçu 1 - X): ");
            localPlayerName = Console.ReadLine()?.Trim() ?? "Player1";

            IPEndPoint localEP = new IPEndPoint(IPAddress.Parse(ServerIp), ServerPort);
            serverUdp = new UdpClient(localEP);

            Console.WriteLine($"\nServer işə düşdü: {ServerIp}:{ServerPort}");
            Console.WriteLine("Client-in qoşulması gözlənilir...\n");

            UdpReceiveResult initResult = await serverUdp.ReceiveAsync();
            clientEP = initResult.RemoteEndPoint;

            string initJson = Encoding.UTF8.GetString(initResult.Buffer);
            GameData clientData = JsonSerializer.Deserialize<GameData>(initJson)!;

            gameState.Gamer1 = localPlayerName;
            gameState.Gamer2 = clientData.Gamer1 ?? "Player2";
            gameState.CurrentTurn = gameState.Gamer1;
            gameState.MessageType = "START";
            isMyTurn = true;

            SendToClient(gameState);

            Console.Clear();
            Console.WriteLine($"Oyun başladı! Siz 'X' simvolusunuz. Rəqib: {gameState.Gamer2}");
            DrawBoard();

            _ = Task.Run(() => ReceiveFromClient());

            while (true)
            {
                if (isMyTurn && string.IsNullOrEmpty(gameState.Winner))
                {
                    Console.WriteLine("\nSizin növbənizdir (X)!");
                    Console.Write("Gediş edin (Sətir [0-2] Sütun [0-2], məsələn: 1 1): ");
                    string input = Console.ReadLine() ?? "";

                    if (TryParseInput(input, out int row, out int col))
                    {
                        if (board[row, col] == 0)
                        {
                            board[row, col] = 1; 
                            UpdateGameStateFromBoard();

                            string winner = CheckWinner();
                            if (!string.IsNullOrEmpty(winner)) gameState.Winner = winner;

                            gameState.CurrentTurn = gameState.Gamer2;
                            gameState.MessageType = "MOVE";
                            isMyTurn = false;

                            Console.Clear();
                            DrawBoard();
                            SendToClient(gameState);

                            if (!string.IsNullOrEmpty(gameState.Winner))
                            {
                                DisplayResult(gameState.Winner);
                                break;
                            }
                        }
                        else Console.WriteLine("Bu xana doludur!");
                    }
                    else Console.WriteLine("Səhv format! Şəbəkə indeksi 0, 1 və ya 2 ola bilər (məsələn: 0 1)");
                }
                await Task.Delay(200);
            }
        }

        private static async Task ReceiveFromClient()
        {
            while (true)
            {
                try
                {
                    UdpReceiveResult res = await serverUdp.ReceiveAsync();
                    string json = Encoding.UTF8.GetString(res.Buffer);
                    GameData received = JsonSerializer.Deserialize<GameData>(json)!;

                    if (received != null && received.MessageType == "MOVE")
                    {
                        gameState = received;
                        UpdateBoardFromGameState();
                        Console.Clear();
                        DrawBoard();

                        if (!string.IsNullOrEmpty(gameState.Winner))
                        {
                            DisplayResult(gameState.Winner);
                            break;
                        }

                        if (gameState.CurrentTurn == localPlayerName) isMyTurn = true;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Xəta: {ex.Message}");
                }
            }
        }

        private static void SendToClient(GameData data)
        {
            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            serverUdp.Send(bytes, bytes.Length, clientEP);
        }

        private static void UpdateGameStateFromBoard()
        {
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    gameState.Game[i][j] = board[i, j];
        }

        private static void UpdateBoardFromGameState()
        {
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    board[i, j] = gameState.Game[i][j];
        }

        private static string CheckWinner()
        {
            for (int i = 0; i < 3; i++)
            {
                int rowSum = board[i, 0] + board[i, 1] + board[i, 2];
                int colSum = board[0, i] + board[1, i] + board[2, i];

                if (rowSum == 3 || colSum == 3) return "X";
                if (rowSum == -3 || colSum == -3) return "O";
            }

            int diag1 = board[0, 0] + board[1, 1] + board[2, 2];
            int diag2 = board[0, 2] + board[1, 1] + board[2, 0];

            if (diag1 == 3 || diag2 == 3) return "X";
            if (diag1 == -3 || diag2 == -3) return "O";

            bool isFull = true;
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    if (board[i, j] == 0) isFull = false;

            return isFull ? "DRAW" : "";
        }

        private static void DrawBoard()
        {
            Console.WriteLine($"\n SERVER (X): {gameState.Gamer1} | CLIENT (O): {gameState.Gamer2}");
            Console.WriteLine("    0   1   2");
            Console.WriteLine("  -------------");
            for (int i = 0; i < 3; i++)
            {
                Console.Write($"{i} | ");
                for (int j = 0; j < 3; j++)
                {
                    char sym = ' ';
                    if (board[i, j] == 1) sym = 'X';
                    else if (board[i, j] == -1) sym = 'O';
                    Console.Write($"{sym} | ");
                }
                Console.WriteLine("\n  -------------");
            }
        }
        private static void DisplayResult(string winner)
        {
            if (winner == "DRAW") Console.WriteLine("\nOyun Bitdi: Heç-heçə!");
            else
            {
                string? name = (winner == "X") ? gameState.Gamer1 : gameState.Gamer2;
                Console.WriteLine($"\nOyun Bitdi! Qalib: {name} ({winner})");
            }

            string fileName = $"{gameState.Gamer1}_{gameState.Gamer2}.json";
            string json = JsonSerializer.Serialize(gameState, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(fileName, json);
            Console.WriteLine($"Oyun fayla yadda saxlanıldı: {fileName}");
        }

        private static bool TryParseInput(string input, out int row, out int col)
        {
            row = col = -1;
            if (string.IsNullOrWhiteSpace(input)) return false;
            string[] p = input.Trim().Split(' ');
            return p.Length == 2 && int.TryParse(p[0], out row) && int.TryParse(p[1], out col) &&
                   row >= 0 && row < 3 && col >= 0 && col < 3;
        }
    }
}