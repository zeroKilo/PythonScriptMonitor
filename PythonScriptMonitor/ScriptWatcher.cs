using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Text;
using System.Threading;

namespace PythonScriptMonitor
{
    public class ScriptWatcher
    {
        public string pythonPath;
        public string arguments;
        private StringBuilder outputBuffer = new StringBuilder();
        private Process process;
        private bool _isRunning = false;
        private bool _shouldExit = false;
        private static readonly object _sync = new object();
        private static readonly int limit = 1024 * 1024; //1MB

        public bool IsRunning
        {
            set
            {
                lock (_sync)
                {
                    _isRunning = value;
                }
            }
            get
            {
                bool result = false;
                lock (_sync)
                {
                    result = _isRunning;
                }
                return result;
            }
        }

        public bool ShouldExit
        {
            set
            {
                lock (_sync)
                {
                    _shouldExit = value;
                }
            }
            get
            {
                bool result = false;
                lock (_sync)
                {
                    result = _shouldExit;
                }
                return result;
            }
        }

        public string GetStatus()
        {
            StringBuilder sb = new StringBuilder();
            if (IsRunning)
                sb.Append("[* RUNNING] ");
            else
                sb.Append("[X STOPPED] ");
            sb.Append(arguments);
            return sb.ToString();
        }

        public string GetLatestOutput()
        {
            string result;
            lock (_sync)
            {
                result = outputBuffer.ToString();
            }
            return result;
        }

        public void Start()
        {
            if (IsRunning)
                return;
            ShouldExit = false;
            lock (_sync)
            {
                outputBuffer.Clear();
            }
            new Thread(tMain).Start();
        }

        public void Stop()
        {
            if (!IsRunning)
                return;
            ShouldExit = true;
        }

        public void tMain(object obj)
        {
            IsRunning = true;
            process = new Process();
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.FileName = pythonPath;
            process.StartInfo.Arguments = "-u " + arguments;
            process.StartInfo.WorkingDirectory = Path.GetDirectoryName(arguments) + "\\";
            process.OutputDataReceived += new DataReceivedEventHandler(OutputHandler);
            process.ErrorDataReceived += new DataReceivedEventHandler(OutputHandler);
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            while (!process.HasExited)
            {
                Thread.Sleep(100);
                if (ShouldExit)
                {
                    KillProcessAndChildrens(process.Id);
                    break;
                }
            }
            IsRunning = false;
        }
        void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            try
            {

                if (outLine.Data != null)
                    lock (_sync)
                    {
                        outputBuffer.AppendLine(DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString() + " " + outLine.Data);
                        if (outputBuffer.Length > limit)
                        {
                            string s = outputBuffer.ToString().Substring(outputBuffer.Length - limit - 0x1000);
                            outputBuffer.Clear();
                            outputBuffer.Append(s);
                        }
                    }
            }
            catch { }
        }

        private static void KillProcessAndChildrens(int pid)
        {
            ManagementObjectSearcher processSearcher = new ManagementObjectSearcher
              ("Select * From Win32_Process Where ParentProcessID=" + pid);
            ManagementObjectCollection processCollection = processSearcher.Get();
            try
            {
                Process proc = Process.GetProcessById(pid);
                if (!proc.HasExited) proc.Kill();
            }
            catch { }
            if (processCollection != null)
                foreach (ManagementObject mo in processCollection)
                    KillProcessAndChildrens(Convert.ToInt32(mo["ProcessID"]));
        }
    }
}
