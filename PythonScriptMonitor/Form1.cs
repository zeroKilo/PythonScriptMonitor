using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace PythonScriptMonitor
{
    public partial class Form1 : Form
    {
        public string pythonPath;
        public List<ScriptWatcher> scriptWatcherList;
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            pythonPath = File.ReadAllText("python_path.txt").Trim();
            string[] lines = File.ReadAllLines("command_list.txt");
            scriptWatcherList = new List<ScriptWatcher>();
            for (int i = 0; i < lines.Length; i++)
            {
                ScriptWatcher sw = new ScriptWatcher();
                sw.pythonPath = pythonPath;
                sw.arguments = lines[i];
                scriptWatcherList.Add(sw);
            }
            RefreshList();
        }

        public void RefreshList()
        {
            listBox1.Items.Clear(); 
            foreach(ScriptWatcher sw in scriptWatcherList)
                listBox1.Items.Add(sw.GetStatus());
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            int n = listBox1.SelectedIndex;
            if (n == -1)
                return;
            rtb1.Text = scriptWatcherList[n].GetLatestOutput();
            rtb1.SelectionStart = rtb1.Text.Length;
            rtb1.ScrollToCaret();
            bool isRunning = scriptWatcherList[n].IsRunning;
            startToolStripMenuItem.Enabled = !isRunning;
            stopToolStripMenuItem.Enabled = isRunning;
            listBox1.Focus();
        }

        private void startToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int n = listBox1.SelectedIndex;
            if (n == -1)
                return;
            scriptWatcherList[n].Start();
            Thread.Sleep(100);
            startToolStripMenuItem.Enabled =
            stopToolStripMenuItem.Enabled = false;
            RefreshList();
        }

        private void stopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int n = listBox1.SelectedIndex;
            if (n == -1)
                return;
            scriptWatcherList[n].Stop();
            Thread.Sleep(100);
            startToolStripMenuItem.Enabled =
            stopToolStripMenuItem.Enabled = false;
            RefreshList();
        }

        private void refreshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RefreshList();
        }
    }
}
