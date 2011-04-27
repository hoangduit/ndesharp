using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using NDESharp;
using System.Diagnostics;

namespace TestProject
{
    public partial class Form1 : Form
    {
        const string FILELOCATION = @"C:\Users\Josh\AppData\Roaming\Winamp\Plugins\ml\";

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {

            //just a bit of performance testing. it takes me about 4 to 5 seconds for 30,000+ songs.
            Stopwatch timer = new Stopwatch();
            timer.Start();
            NDEDatabase ndedb = new NDEDatabase(FILELOCATION + "main");
            timer.Stop();
            textBox1.Text = timer.Elapsed.ToString();

            //here we can access the dataset of music
            DataSet songs = ndedb.SongDS;

        }
    }
}
