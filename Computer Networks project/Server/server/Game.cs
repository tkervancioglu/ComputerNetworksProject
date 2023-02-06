using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace server
{
    public class Game
    {
        private List<Player> PlayersInGame;
        private List<Player> PlayersInWaitingRoom;
        private List<Player> DisconnectedPlayers;

        private Socket ServerSocket;
        private Quiz Quiz;
        private Thread AcceptConnectionsThread;
        private bool IsServerListening;
        private bool IsGameStarted;
        private RichTextBox LogTextBox;
        private Button StartGameButton;
        private TextBox NumberOfQuestionsTextBox;
        private int NumberOfQuestionsTobeAsked;
        public Game(RichTextBox logTextBox, Button startGameButton, TextBox numberOfQuestionsTextBox)
        {
            ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            PlayersInGame = new List<Player>();
            PlayersInWaitingRoom = new List<Player>();
            DisconnectedPlayers = new List<Player>();
            IsServerListening = false;
            LogTextBox = logTextBox;
            StartGameButton = startGameButton;
            NumberOfQuestionsTextBox = numberOfQuestionsTextBox;
        }

        private void Log(string message)
        {
            LogTextBox.AppendText(message + "\n");
        }

        public void AcceptConnections()
        {
            while (IsServerListening)
            {
                var clientSocket = ServerSocket.Accept();
                Log("A new client is connected. Starting listening...");
                var thread = new Thread(() => StartToListenNewClient(clientSocket));
                thread.Start();
            }
        }

        private void StartToListenNewClient(Socket clientSocket)
        {
            Player player = null;
            while (IsSocketConnected(clientSocket))
            {
                if (clientSocket.Available > 0)
                {
                    var buffer = new byte[64];
                    clientSocket.Receive(buffer, 0);
                    var incomingMessage = Encoding.UTF8.GetString(buffer);
                    incomingMessage = incomingMessage.Substring(0, incomingMessage.IndexOf("\0"));
                    var messageSegments = incomingMessage.Split(',');
                    var messageType = messageSegments[0];
                    if (messageType == "Name")
                    {
                        var name = messageSegments[1];
                        player = AddPlayerToWaitingRoom(name, clientSocket);
                        if (player != null)
                        {
                            Log("Player " + name + " connected and added to waiting room");
                            HandleStartGameButton(NumberOfQuestionsTextBox);
                        }
                        else
                        {
                            SendNameNotUniqueMessage(clientSocket);
                        }

                    }
                    else if (player != null && messageType == "Answer" && IsGameStarted)
                    {
                        //TODO: send answer is not accepted message to client when there is no ongoing game.
                        var answer = int.Parse(messageSegments[1]);
                        if (ShouldAcceptAnswer(player))
                        {
                            Log("Player " + player.Name + " responded with: " + answer);
                            player.AddAnswer(answer);
                        }
                        if (IsTurnEnded())
                        {
                            var trueAnswer = Quiz.GetAnswer();

                            UpdateScores(trueAnswer);
                            PrintScoreBoard(true, trueAnswer);

                            if (!Quiz.IsQuizFinished())
                            {
                                AskQuestion();

                            }
                            else
                            {
                                DeclareWinner();
                                ResetGame();
                            }
                        }
                    }
                }
            }
            if (player != null)
            {
                PlayerDisconnected(player);
            }
        }
        private int NumberOfplayers()
        {
            return PlayersInGame.Count();
        }

        private void PlayerDisconnected(Player player)
        {

            var message = player.Name + " disconnected";
            Log(message);
            PlayersInGame.Remove(player);
            PlayersInWaitingRoom.Remove(player);

            if (CheckIfPlayerNameIsUniqueAmongDisconnectedUsers(player.Name))
            {
                player.Score = 0;
                DisconnectedPlayers.Add(player);
            }

            
            if(!IsGameStarted && CanGameStart())
            {
                StartGameButton.Enabled = true;
            }

            //biri kopunca tek bir bağlı oyuncu kaldığı durum
            if (IsGameStarted && PlayersInGame.Count == 1)
            {
                PlayersInGame[0].SendMessage(message);
                PrintScoreBoard(false, 0);
                DeclareWinner();
                ResetGame();
            }
            else if(IsGameStarted && IsTurnEnded())
            {
                var trueAnswer = Quiz.GetAnswer();

                UpdateScores(trueAnswer);
                PrintScoreBoard(true, trueAnswer);

                if (!Quiz.IsQuizFinished())
                {
                    AskQuestion();
                }
                else
                {
                    DeclareWinner();
                    ResetGame();
                }
            }
            HandleStartGameButton(NumberOfQuestionsTextBox);

        }

        private bool IsSocketConnected(Socket socket)
        {
            try
            {
                if (!socket.Connected)
                    return false;

                bool part1 = socket.Poll(1000, SelectMode.SelectRead);
                bool part2 = (socket.Available == 0);
                return !(part1 && part2);
            }
            catch
            {
                return false;
            }
        }
        private void DeclareWinner()
        {
            string message = "";
            var maxPoint = PlayersInGame.Select(x => x.Score).Max();
            var winners = PlayersInGame.Where(x => x.Score == maxPoint).ToList();
            if (winners.Count > 1)
            {
                message = "The following players are winners: ";
                foreach (var winner in winners)
                {
                    message += winner.Name + ", ";
                }
            }
            else
            {
                message = "Player " + winners.First().Name + " has won!";
            }
            Log(message);
            SendMessageToAllPlayersInGame(message);
        }

        private void SendMessageToAllPlayersInGame(string message)
        {
            foreach (var player in PlayersInGame)
            {
                player.SendMessage(message);
            }
        }

        public void ResetGame()
        {
            //TODO:
            foreach (var player in PlayersInGame)
            {
                player.Score = 0;
                player.Answers.Clear();
                PlayersInWaitingRoom.Add(player);
            }
            PlayersInGame.Clear();
            DisconnectedPlayers.Clear();
            Quiz.ResetQuiz(NumberOfQuestionsTobeAsked);
            IsGameStarted = false;
            StartGameButton.Enabled = true;
            NumberOfQuestionsTextBox.Enabled = true;
        }

        private void UpdateScores(int trueAnswer)
        {
            var distancesToTrueAnswer = new List<Tuple<Player, int>>();
            foreach (var player in PlayersInGame)
            {
                var distance = Math.Abs(trueAnswer - player.Answers[player.Answers.Count - 1]);
                distancesToTrueAnswer.Add(new Tuple<Player, int>(player, distance));
            }
            var distancesOrdered = distancesToTrueAnswer.OrderByDescending(x => x.Item2);
            var maxPoint = distancesOrdered.Min(x => x.Item2);
            var winners = distancesOrdered.Where(x => x.Item2 == maxPoint).Select(x => x.Item1).ToList();
            var numberOfWinners = winners.Count;
            var pointsPerPlayer = 1.0m / numberOfWinners;
            foreach (var player in winners)
            {
                player.AddPoints(pointsPerPlayer);
            }
            var message = "True answer to last question is " + trueAnswer + ".\n";
            var sent = "";
            foreach (var player in PlayersInGame)
            {
                sent += "Player " + player.Name + " responded: " + player.Answers.Last() + ".\n";

            }
            Log(sent);
            if (numberOfWinners == 1)
            {
                var roundWinner = winners[0];
                message += "player " + winners[0].Name + " won the round" + ".\n";
                Log(message);
                SendMessageToAllPlayersInGame(message);
            }
            else
            {
                message += "There were multiple winners this round " + ".\n";
                message += "Players that won the round are " + ".\n";
                foreach (var player in winners)
                {
                    message += player.Name + " ";
                }
                Log(message);
                SendMessageToAllPlayersInGame(message);

            }
            SortPlayersByScore();
        }

        private void SortPlayersByScore()
        {
            PlayersInGame = PlayersInGame.OrderByDescending(x => x.Score).ToList();
        }

        private void PrintScoreBoard(bool takeAnswerIntoConsideration, int trueAnswer)
        {
            var message = "";

            if (takeAnswerIntoConsideration)
            {
                message += "True answer to last question is " + trueAnswer + ".\n";
                foreach (var player in PlayersInGame)
                {
                    message += "Player " + player.Name + " responded: " + player.Answers.Last() + ".\n";
                }
            }

            message += "Current points as following:\n";
            foreach (var player in PlayersInGame)
            {
                message += player.GetScoreMessage();
            }
            foreach (var player in DisconnectedPlayers)
            {
                message += player.GetScoreMessage();
            }
            foreach (var player in PlayersInGame)
            {
                player.SendMessage(message);
            }
            Log(message);

        }

        private bool IsTurnEnded()
        {
            var numbersOfQuestionsAsked = Quiz.GetNumberOfQuestionsAsked();
            var answers = PlayersInGame.Select(x => x.Answers);
            var isTurnEnded = answers.All(y => y.Count == numbersOfQuestionsAsked);
            return isTurnEnded;
        }

        private bool ShouldAcceptAnswer(Player responder)
        {
            int minAnswerCountAmongPlayers = NumberOfQuestionsTobeAsked;
            foreach (var player in PlayersInGame)
            {
                if (responder != player && minAnswerCountAmongPlayers > player.Answers.Count)
                {
                    minAnswerCountAmongPlayers = player.Answers.Count;
                }
            }
            return responder.Answers.Count <= minAnswerCountAmongPlayers;

        }

        private void AskQuestion()
        {
            var question = Quiz.GetQuestion();
            foreach (var player in PlayersInGame)
            {
                player.AskQuestion(question);
            }
            Log("Asked the following question: " + question);

        }


        public bool StartServer(int port)
        {
            if ( port > 0)
            {
                var endPoint = new IPEndPoint(IPAddress.Any, port);
                ServerSocket.Bind(endPoint);
                ServerSocket.Listen(3);
                IsServerListening = true;
                AcceptConnectionsThread = new Thread(AcceptConnections);
                AcceptConnectionsThread.Start();
                return true;
            }
            else
            {
                return false;
            }
        }


        private void StopServer()
        {
        }
        private Player AddPlayerToWaitingRoom(string playerName, Socket playerSocket)
        {
            if (CheckIfPlayerNameIsUnique(playerName))
            {
                var player = new Player(playerName, playerSocket);
                PlayersInWaitingRoom.Add(player);
                player.SendWelcomeToWaitingRoomMessage();
                return player;
            }
            else
            {
                return null;
            }
        }

        private bool CheckIfPlayerNameIsUnique(string playerName)
        {
            var allPlayers = PlayersInGame.Concat(PlayersInWaitingRoom);
            foreach (var player in allPlayers)
            {
                var isNameEqual = player.Name == playerName;
                if (isNameEqual)
                {
                    return false;
                }
            }
            return true;
        }
        
        private bool CheckIfPlayerNameIsUniqueAmongDisconnectedUsers(string playerName)
        {
            foreach (var player in DisconnectedPlayers)
            {
                var isNameEqual = player.Name == playerName;
                if (isNameEqual)
                {
                    return false;
                }
            }
            return true;
        }

        public void SendNameNotUniqueMessage(Socket socket)
        {
            var message = "Your name is already used. Try connecting with different name again";
            Log("Player with already used name connected. Going to close connection.");
            var messageBuffer = Encoding.Default.GetBytes(message);
            socket.Send(messageBuffer);
            socket.Close();
        }

        public bool TryToStartGame(string fileName, int numberOfQuestionsToBeAsked)
        {
            if (CanGameStart() && !IsGameStarted)
            {
                NumberOfQuestionsTobeAsked = numberOfQuestionsToBeAsked;
                Quiz = new Quiz(fileName, numberOfQuestionsToBeAsked);
                foreach (var player in PlayersInWaitingRoom)
                {
                    PlayersInGame.Add(player);
                }
                PlayersInWaitingRoom.Clear();

                SendGameStartingMessage();
                NumberOfQuestionsTextBox.Enabled = false;
                AskQuestion();
                IsGameStarted = true;
                Log("Game started.");
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool CanGameStart()
        {
            var playerCount = PlayersInWaitingRoom.Count;
            if (playerCount >= 2)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void SendGameStartingMessage()
        {
            foreach (var player in PlayersInGame)
            {
                player.SendGameStartingMessage(NumberOfQuestionsTobeAsked);
            }
            Log("Game starting..." + NumberOfQuestionsTobeAsked + " questions will be asked.");
        }

        public void HandleStartGameButton(TextBox numberOfQuestionsTextBox)
        {
            int number;
            var isInputNumber = int.TryParse(numberOfQuestionsTextBox.Text, out number);
            if (isInputNumber && CanGameStart())
            {
                StartGameButton.Enabled = true;
            }
            else
            {
                StartGameButton.Enabled = false;
            }
        }
    }
}
