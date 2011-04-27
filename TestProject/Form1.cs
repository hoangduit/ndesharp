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
        private DataTable songs;
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //just a bit of performance testing. it takes me about 4 to 5 seconds for 30,000+ songs.
            Stopwatch timer = new Stopwatch();
            timer.Start();
            NDEDatabase ndedb = new NDEDatabase(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Winamp\Plugins\ml\main");
            timer.Stop();
            textBox1.Text = "Time to process: " + timer.Elapsed.ToString();

            //here we can access the dataset of music
            songs = ndedb.SongDS.Tables[0];
            songs.TableName = "songs";
            songs.CaseSensitive = false;
            dataGridView1.DataSource = songs;
            dataGridView1.Refresh();

        }

        private void filterBtn_Click(object sender, EventArgs e)
        {
            var data = from o in songs.AsEnumerable()
                       where o.Field<string>("artist").Contains(artistTB.Text) && o.Field<string>("title").Contains(titleTB.Text)
                       select o;

            dataGridView1.DataSource = data.CopyToDataTable();
            dataGridView1.Refresh();
        }
    }
}
