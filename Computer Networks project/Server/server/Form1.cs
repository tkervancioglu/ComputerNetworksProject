using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;



namespace server
{


    public partial class Form1 : Form
    {
        Game game;
        public Form1()
        {
            Control.CheckForIllegalCrossThreadCalls = false;
            this.FormClosing += new FormClosingEventHandler(Form1_FormClosing);
            InitializeComponent();

            quetext.Text = "";
            textBox_port.Text = "";
            startgamebut.Enabled = false;

        }
        private void button_listen_Click(object sender, EventArgs e)
        {
            var isPortEntered = Int32.TryParse(textBox_port.Text, out int serverPort);
            RemoveLogs();

            if (isPortEntered)
            {
                game = new Game(logs,startgamebut, quetext);
                if (game.StartServer(serverPort))
                {
                    button1.Enabled = true;
                    logs.AppendText("Started listening on port: " + serverPort + "\n");
                }
                else
                {
                    PrintCantStartListening(isPortEntered);

                }

                button_listen.Enabled = false;
                textBox_message.Enabled = true;
                button_send.Enabled = true;
            }
            else
            {
                PrintCantStartListening(isPortEntered);
            }
        }

        private void PrintCantStartListening(bool isPortEntered)
        {
            logs.AppendText("Couldn't start listening\n");

            if (!isPortEntered)
            {
                logs.AppendText("Port number box is empty!\n");
            }
        }

        private void RemoveLogs()
        {
            logs.ResetText();
        }

   
        private void Form1_FormClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Environment.Exit(0);
        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void queText_TextChanged(object sender, EventArgs e)
        {

            game.HandleStartGameButton(quetext);
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void label3_Click_1(object sender, EventArgs e)
        {

        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            logs.Clear();
            game.ResetGame();   
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void textBox_port_TextChanged(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            var fileName = "questions.txt";

            var isNumberOfQuestionsEntered = Int32.TryParse(quetext.Text, out int numberOfQuestions);
            if(isNumberOfQuestionsEntered)
            {
                var isGameStarted = game.TryToStartGame(fileName,numberOfQuestions);
                if (isGameStarted)
                {
                    startgamebut.Enabled = false;
                }
                else
                {
                    logs.AppendText("There is less than 2 players or a game is already started. Can't start game.");
                }
            }

        }

        private void logs_TextChanged(object sender, EventArgs e)
        {
            logs.SelectionStart = logs.Text.Length;
            logs.ScrollToCaret();
        }

        private void button2_Click_1(object sender, EventArgs e)
        {

        }
    }
}

