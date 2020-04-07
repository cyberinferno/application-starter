using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.Xml;

namespace ApplicationStarter
{
    public partial class ApplicationStarterForm : Form
    {
        private string _configFile;

        private Timer _timer;

        public ApplicationStarterForm()
        {
            InitializeComponent();
            _configFile = getWorkingDirectory() + System.IO.Path.DirectorySeparatorChar + "Config.xml";
        }

        private void ApplicationStarterForm_Load(object sender, EventArgs e)
        {
            if (File.Exists(_configFile))
            {
                var document = new XmlDocument();
                document.Load(_configFile);
                foreach (XmlNode item in document.GetElementsByTagName("Application"))
                {
                    if (item.Attributes["path"].Value != null && File.Exists(item.Attributes["path"].Value))
                    {
                        var enabled = false;
                        var status = "Stopped";

                        if (item.Attributes["enabled"].Value == "1")
                        {
                            enabled = true;
                        }

                        foreach (var process in Process.GetProcesses())
                        {
                            try
                            {
                                if (System.IO.Path.GetFullPath(process.MainModule.FileName) == System.IO.Path.GetFullPath(item.Attributes["path"].Value))
                                {
                                    status = "Running";
                                    break;
                                }
                            }
                            catch { }
                        }

                        var row = (DataGridViewRow)dataGridView.RowTemplate.Clone();
                        row.CreateCells(dataGridView, item.Attributes["path"].Value, enabled, status);
                        dataGridView.Rows.Add(row);
                    }
                }

                statusCheckWorker.RunWorkerAsync();
            }

            _timer = new Timer
            {
                Interval = 10000
            };
            _timer.Tick += new EventHandler((o, ea) =>
            {
                if (!statusCheckWorker.IsBusy)
                {
                    statusCheckWorker.RunWorkerAsync();
                }
            });
            _timer.Start();
        }

        private void addButton_Click(object sender, EventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                DefaultExt = "exe",
                Filter = "exe files (*.exe)|*.exe"
            };
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                var row = (DataGridViewRow)dataGridView.RowTemplate.Clone();
                row.CreateCells(dataGridView, openFileDialog.FileName, false, true, "Stopped");
                dataGridView.Rows.Add(row);
                if (!statusCheckWorker.IsBusy)
                {
                    statusCheckWorker.RunWorkerAsync();
                }
            }
        }

        private void startButton_Click(object sender, EventArgs e)
        {
            if (dataGridView.RowCount == 0)
            {
                _ = MessageBox.Show("There are no applications to start", "Application Starter", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            disableAllButtons();
            var worker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };

            worker.DoWork += startButton_DoWork;
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler((o, rwcea) =>
            {
                bringMeToForeground();
                if (!statusCheckWorker.IsBusy)
                {
                    statusCheckWorker.RunWorkerAsync();
                }
                enableAllButtons();
                _ = MessageBox.Show("Applications started", "Application Starter", MessageBoxButtons.OK, MessageBoxIcon.Information);
            });
            worker.RunWorkerAsync();
        }

        private void startButton_DoWork(object sender, DoWorkEventArgs e)
        {
            startApplications();
        }

        private void stopButton_Click(object sender, EventArgs e)
        {
            if (dataGridView.RowCount == 0)
            {
                _ = MessageBox.Show("There are no applications to stop", "Application Starter", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            disableAllButtons();
            var worker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };

            worker.DoWork += stopButton_DoWork;
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler((o, rwcea) =>
            {
                bringMeToForeground();
                if (!statusCheckWorker.IsBusy)
                {
                    statusCheckWorker.RunWorkerAsync();
                }
                enableAllButtons();
                _ = MessageBox.Show("Applications stopped", "Application Starter", MessageBoxButtons.OK, MessageBoxIcon.Information);
            });
            worker.RunWorkerAsync();
        }

        private void stopButton_DoWork(object sender, DoWorkEventArgs e)
        {
            stopApplications();
        }

        private void restartButton_Click(object sender, EventArgs e)
        {
            if (dataGridView.RowCount == 0)
            {
                _ = MessageBox.Show("There are no applications to restart", "Application Starter", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            disableAllButtons();
            var worker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };

            worker.DoWork += restartButton_DoWork;
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler((o, rwcea) =>
            {
                bringMeToForeground();
                if (!statusCheckWorker.IsBusy)
                {
                    statusCheckWorker.RunWorkerAsync();
                }
                enableAllButtons();
                _ = MessageBox.Show("Applications restarted", "Application Starter", MessageBoxButtons.OK, MessageBoxIcon.Information);
            });
            worker.RunWorkerAsync();
        }

        private void restartButton_DoWork(object sender, DoWorkEventArgs e)
        {
            stopApplications();
            startApplications();
        }

        private void statusCheckWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var processes = Process.GetProcesses();
            foreach (DataGridViewRow row in dataGridView.Rows)
            {
                var status = "Stopped";
                foreach (DataGridViewCell cell in row.Cells)
                {
                    if (cell.OwningColumn.Name == "Path")
                    {
                        foreach (var process in processes)
                        {
                            try
                            {
                                if (System.IO.Path.GetFullPath(process.MainModule.FileName) == System.IO.Path.GetFullPath(cell.Value.ToString()))
                                {
                                    status = "Running";
                                    break;
                                }
                            }
                            catch { }
                        }
                        break;
                    }
                }

                foreach (DataGridViewCell cell in row.Cells)
                {
                    if (cell.OwningColumn.Name == "Status")
                    {
                        cell.Value = status;
                    }
                }
            }
        }

        private void removeButton_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow item in dataGridView.SelectedRows)
            {
                dataGridView.Rows.RemoveAt(item.Index);
            }
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            if (dataGridView.RowCount == 0)
            {
                _ = MessageBox.Show("There are no applications to save config", "Application Starter", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            saveButton.Enabled = false;
            addButton.Enabled = false;
            removeButton.Enabled = false;
            if (File.Exists(_configFile))
            {
                File.Move(_configFile, getWorkingDirectory() + System.IO.Path.DirectorySeparatorChar + string.Format(@"{0}_Config.xml", DateTime.Now.Ticks));
            }

            var settings = new XmlWriterSettings
            {
                Indent = true
            };
            using (var writer = XmlWriter.Create(_configFile, settings))
            {
                writer.WriteStartElement("Applications");
                foreach (DataGridViewRow row in dataGridView.Rows)
                {
                    writer.WriteStartElement("Application");
                    foreach (DataGridViewCell cell in row.Cells)
                    {
                        switch (cell.OwningColumn.Name)
                        {
                            case "Path":
                                writer.WriteAttributeString("path", cell.Value.ToString());
                                break;

                            case "Enabled":
                                writer.WriteAttributeString("enabled", Convert.ToBoolean(cell.Value) ? "1" : "0");
                                break;
                        }
                    }

                    writer.WriteEndElement();
                    writer.Flush();
                }
                writer.WriteEndElement();
            }

            _ = MessageBox.Show("Config was saved successfully", "Application Starter", MessageBoxButtons.OK, MessageBoxIcon.Information);
            saveButton.Enabled = true;
            addButton.Enabled = true;
            removeButton.Enabled = true;
        }

        private string getWorkingDirectory()
        {
            return System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
        }

        private void enableAllButtons()
        {
            setAllButtonStates(true);
        }

        private void disableAllButtons()
        {
            setAllButtonStates(false);
        }

        private void setAllButtonStates(bool state)
        {
            addButton.Enabled = state;
            removeButton.Enabled = state;
            startButton.Enabled = state;
            stopButton.Enabled = state;
            restartButton.Enabled = state;
            saveButton.Enabled = state;
        }

        private void startApplications()
        {
            var processes = Process.GetProcesses();
            foreach (DataGridViewRow row in dataGridView.Rows)
            {
                var isRunning = false;
                string path = null;
                var shouldStart = false;
                foreach (DataGridViewCell cell in row.Cells)
                {
                    if (cell.OwningColumn.Name == "Path")
                    {
                        path = cell.Value.ToString();
                        foreach (var process in processes)
                        {
                            try
                            {
                                if (System.IO.Path.GetFullPath(process.MainModule.FileName) == System.IO.Path.GetFullPath(cell.Value.ToString()))
                                {
                                    isRunning = true;
                                    break;
                                }
                            }
                            catch { }
                        }
                    }

                    if (cell.OwningColumn.Name == "Enabled")
                    {
                        shouldStart = Convert.ToBoolean(cell.Value);
                    }
                }

                if (!isRunning && shouldStart)
                {
                    startProcess(path);
                }
            }
        }

        private void startProcess(string filePath)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = filePath
                }
            };
            process.Start();
            while (true)
            {
                try
                {
                    var time = process.StartTime;
                    break;
                }
                catch { }
            }
        }

        private void stopApplications()
        {
            var processes = Process.GetProcesses();
            Process currentProcess = null;
            var shouldSop = false;
            foreach (DataGridViewRow row in dataGridView.Rows)
            {
                foreach (DataGridViewCell cell in row.Cells)
                {
                    if (cell.OwningColumn.Name == "Path")
                    {
                        foreach (var process in processes)
                        {
                            try
                            {
                                if (System.IO.Path.GetFullPath(process.MainModule.FileName) == System.IO.Path.GetFullPath(cell.Value.ToString()))
                                {
                                    currentProcess = process;
                                    break;
                                }
                            }
                            catch { }
                        }
                    }

                    if (cell.OwningColumn.Name == "Enabled")
                    {
                        shouldSop = Convert.ToBoolean(cell.Value);
                    }
                }

                if (currentProcess != null && shouldSop)
                {
                    currentProcess.Kill();
                }
            }
        }

        private void bringMeToForeground()
        {
            var currentProcess = Process.GetCurrentProcess();
            var hWnd = currentProcess.MainWindowHandle;
            if (hWnd != User32.InvalidHandleValue)
            {
                User32.SetForegroundWindow(hWnd);
                User32.ShowWindow(hWnd, User32.SW_RESTORE);
            }
        }
    }
}
