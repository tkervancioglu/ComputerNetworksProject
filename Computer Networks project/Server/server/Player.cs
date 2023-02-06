using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace server
{
    public class Player
    {
        public decimal Score { get; set; }
        public string Name { get; set; }
        private Socket Socket { get; set; }

        public List<int> Answers { get; set; }

        public Player(string name, Socket socket)
        {
            Name = name;
            Socket = socket;
            Answers = new List<int>();
        }

        public string GetScoreMessage()
        {
            return "Player " + Name + " has " + Score + " points\n";
        }
        public void AddPoints(decimal points)
        {
            Score += points;
        }

        public void AddAnswer(int answer)
        {
            Answers.Add(answer);
        }

        public void SendGameStartingMessage(int numberOfQuestionsTobeAsked)
        {
            var message = "Game starting..." + numberOfQuestionsTobeAsked + " questions will be asked.";
            SendMessage(message);
        }

        public void SendWaitingForSecondPlayerMessage()
        {
            var message = "Server will start the game....";
            SendMessage(message);
        }

        public void AskQuestion(string question)
        {
            SendMessage(question);
        }

        public void SendMessage(string message)
        {
            try
            {
                var messageBuffer = Encoding.Default.GetBytes(message);
                Socket.Send(messageBuffer);
            }
            catch
            {

            }
        }
        
        public void SendWelcomeToWaitingRoomMessage()
        {
            SendMessage("Welcome to the game. You are in the waiting room now. Game will start soon");
        }


    }
}
