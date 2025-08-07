using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Forms;

namespace NullInstaller
{
    public partial class MainForm : Form
    {
        private TabControl tabControl;
        private ListView localFilesList;
        private ListView universalProgramsList;
        private ProgressBar overallProgressBar;
        private Label statusLabel;
        private CheckBox verboseLoggingCheck;
        private Button startButton, stopButton, clearButton, downloadButton;
        private List<InstallerItem> installers = new List<InstallerItem>();
        private List<UniversalProgram> universalPrograms = new List<UniversalProgram>();
        private bool isRunning = false;
        private StreamWriter logWriter;

        public MainForm()
        {
            InitializeComponent();
            InitializeUniversalPrograms();
            ScanDefaultFolder();
            InitializeLogging();
        }

        private void InitializeComponent()
        {
            // Form properties
            this.Text = "NullInstaller v0.1.0 - Enhanced Windows Installer Tool";
            this.Size = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(45, 45, 48);
            this.ForeColor = Color.White;

            // Create tab control
            tabControl = new TabControl();
            tabControl.Dock = DockStyle.Left;
            tabControl.Width = 400;
            tabControl.BackColor = Color.FromArgb(37, 37, 38);
            tabControl.ForeColor = Color.White;

            // Local Files Tab
            var localTab = new TabPage("Local Files");
            localFilesList = new ListView();
            localFilesList.Dock = DockStyle.Fill;
            localFilesList.View = View.Details;
            localFilesList.CheckBoxes = true;
            localFilesList.BackColor = Color.FromArgb(30, 30, 30);
            localFilesList.ForeColor = Color.White;
            localFilesList.GridLines = true;
            localFilesList.Columns.Add("Installer", 200);
            localFilesList.Columns.Add("Size", 80);
            localFilesList.Columns.Add("Status", 100);
            localTab.Controls.Add(localFilesList);

            // Universal Programs Tab
            var universalTab = new TabPage("Universal Programs");
            universalProgramsList = new ListView();
            universalProgramsList.Dock = DockStyle.Fill;
            universalProgramsList.View = View.Details;
            universalProgramsList.CheckBoxes = true;
            universalProgramsList.BackColor = Color.FromArgb(30, 30, 30);
            universalProgramsList.ForeColor = Color.White;
            universalProgramsList.GridLines = true;
            universalProgramsList.Columns.Add("Program", 180);
            universalProgramsList.Columns.Add("Description", 200);
            universalTab.Controls.Add(universalProgramsList);

            tabControl.TabPages.Add(localTab);
            tabControl.TabPages.Add(universalTab);

            // Right panel for controls
            var rightPanel = new Panel();
            rightPanel.Dock = DockStyle.Fill;
            rightPanel.BackColor = Color.FromArgb(45, 45, 48);
            rightPanel.Padding = new Padding(10);

            // Buttons
            startButton = new Button();
            startButton.Text = "Start Installation";
            startButton.Size = new Size(120, 35);
            startButton.Location = new Point(10, 10);
            startButton.BackColor = Color.FromArgb(0, 122, 204);
            startButton.ForeColor = Color.White;
            startButton.FlatStyle = FlatStyle.Flat;
            startButton.Click += StartButton_Click;

            stopButton = new Button();
            stopButton.Text = "Stop";
            stopButton.Size = new Size(80, 35);
            stopButton.Location = new Point(140, 10);
            stopButton.BackColor = Color.FromArgb(196, 43, 28);
            stopButton.ForeColor = Color.White;
            stopButton.FlatStyle = FlatStyle.Flat;
            stopButton.Enabled = false;
            stopButton.Click += StopButton_Click;

            clearButton = new Button();
            clearButton.Text = "Clear All";
            clearButton.Size = new Size(80, 35);
            clearButton.Location = new Point(230, 10);
            clearButton.BackColor = Color.FromArgb(104, 104, 104);
            clearButton.ForeColor = Color.White;
            clearButton.FlatStyle = FlatStyle.Flat;
            clearButton.Click += ClearButton_Click;

            downloadButton = new Button();
            downloadButton.Text = "Download Selected";
            downloadButton.Size = new Size(140, 35);
            downloadButton.Location = new Point(10, 55);
            downloadButton.BackColor = Color.FromArgb(16, 124, 16);
            downloadButton.ForeColor = Color.White;
            downloadButton.FlatStyle = FlatStyle.Flat;
            downloadButton.Click += DownloadButton_Click;

            // Verbose logging checkbox
            verboseLoggingCheck = new CheckBox();
            verboseLoggingCheck.Text = "Verbose Logging";
            verboseLoggingCheck.Location = new Point(10, 100);
            verboseLoggingCheck.Size = new Size(120, 25);
            verboseLoggingCheck.Checked = true;
            verboseLoggingCheck.ForeColor = Color.White;

            // Progress bar
            overallProgressBar = new ProgressBar();
            overallProgressBar.Location = new Point(10, 140);
            overallProgressBar.Size = new Size(300, 25);
            overallProgressBar.Style = ProgressBarStyle.Continuous;

            // Status label
            statusLabel = new Label();
            statusLabel.Text = "Ready - Enhanced NullInstaller with 20 universal programs";
            statusLabel.Location = new Point(10, 175);
            statusLabel.Size = new Size(400, 60);
            statusLabel.ForeColor = Color.LightGray;
            statusLabel.AutoSize = false;

            rightPanel.Controls.AddRange(new Control[] {
                startButton, stopButton, clearButton, downloadButton,
                verboseLoggingCheck, overallProgressBar, statusLabel
            });

            // Add to form
            this.Controls.Add(rightPanel);
            this.Controls.Add(tabControl);

            // Enable drag and drop
            this.AllowDrop = true;
            this.DragEnter += MainForm_DragEnter;
            this.DragDrop += MainForm_DragDrop;
        }

        private void InitializeUniversalPrograms()
        {
            universalPrograms.AddRange(new UniversalProgram[]
            {
                new UniversalProgram("Google Chrome", "Popular web browser", "https://dl.google.com/chrome/install/ChromeStandaloneSetup64.exe"),
                new UniversalProgram("Mozilla Firefox", "Open-source web browser", "https://download.mozilla.org/?product=firefox-latest-ssl&os=win64&lang=en-US"),
                new UniversalProgram("VLC Media Player", "Multimedia player", "https://get.videolan.org/vlc/last/win64/vlc-3.0.20-win64.exe"),
                new UniversalProgram("7-Zip", "File archiver", "https://www.7-zip.org/a/7z2301-x64.exe"),
                new UniversalProgram("Notepad++", "Advanced text editor", "https://github.com/notepad-plus-plus/notepad-plus-plus/releases/download/v8.5.8/npp.8.5.8.Installer.x64.exe"),
                new UniversalProgram("Visual Studio Code", "Code editor", "https://code.visualstudio.com/sha/download?build=stable&os=win32-x64-user"),
                new UniversalProgram("Git for Windows", "Version control system", "https://github.com/git-for-windows/git/releases/download/v2.42.0.windows.2/Git-2.42.0.2-64-bit.exe"),
                new UniversalProgram("Discord", "Voice and text chat", "https://discord.com/api/downloads/distributions/app/installers/latest?channel=stable&platform=win&arch=x64"),
                new UniversalProgram("Spotify", "Music streaming", "https://download.scdn.co/SpotifySetup.exe"),
                new UniversalProgram("Steam", "Gaming platform", "https://cdn.akamai.steamstatic.com/client/installer/SteamSetup.exe"),
                new UniversalProgram("Zoom", "Video conferencing", "https://zoom.us/client/latest/ZoomInstallerFull.exe"),
                new UniversalProgram("TeamViewer", "Remote desktop", "https://download.teamviewer.com/download/TeamViewer_Setup.exe"),
                new UniversalProgram("WinRAR", "File archiver", "https://www.rarlab.com/rar/winrar-x64-623.exe"),
                new UniversalProgram("CCleaner", "System cleaner", "https://download.ccleaner.com/ccsetup608.exe"),
                new UniversalProgram("OBS Studio", "Streaming/recording", "https://cdn-fastly.obsproject.com/downloads/OBS-Studio-29.1.3-Windows-Installer.exe"),
                new UniversalProgram("LibreOffice", "Office suite", "https://download.libreoffice.org/libreoffice/stable/7.5.6/win/x86_64/LibreOffice_7.5.6_Win_x64.msi"),
                new UniversalProgram("GIMP", "Image editor", "https://download.gimp.org/gimp/v2.10/windows/gimp-2.10.34-setup-3.exe"),
                new UniversalProgram("Adobe Acrobat Reader", "PDF reader", "https://ardownload2.adobe.com/pub/adobe/reader/win/AcrobatDC/2300820360/AcroRdrDC2300820360_en_US.exe"),
                new UniversalProgram("Skype", "Voice and video calls", "https://get.skype.com/go/getskype-skypeforwindows"),
                new UniversalProgram("Malwarebytes", "Anti-malware", "https://data-cdn.mbamupdates.com/web/mb4-setup-consumer/MBSetup.exe")
            });

            PopulateUniversalProgramsList();
        }

        private void PopulateUniversalProgramsList()
        {
            universalProgramsList.Items.Clear();
            foreach (var program in universalPrograms)
            {
                var item = new ListViewItem(program.Name);
                item.SubItems.Add(program.Description);
                item.Tag = program;
                universalProgramsList.Items.Add(item);
            }
        }

        private void ScanDefaultFolder()
        {
            string defaultPath = @"C:\Users\Administrator\Desktop\Down";
            if (!Directory.Exists(defaultPath)) return;

            foreach (string file in Directory.GetFiles(defaultPath, "*.*", SearchOption.AllDirectories))
            {
                if (file.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ||
                    file.EndsWith(".msi", StringComparison.OrdinalIgnoreCase))
                {
                    AddInstallerFile(file);
                }
            }
            
            UpdateStatusLabel("Found " + installers.Count + " installer files");
        }

        private void AddInstallerFile(string filePath)
        {
            var fileInfo = new FileInfo(filePath);
            var installer = new InstallerItem();
            installer.FilePath = filePath;
            installer.FileName = fileInfo.Name;
            installer.Size = fileInfo.Length;
            installer.Status = "Ready";
            
            installers.Add(installer);
            
            var item = new ListViewItem(installer.FileName);
            item.SubItems.Add(FormatFileSize(installer.Size));
            item.SubItems.Add(installer.Status);
            item.Tag = installer;
            localFilesList.Items.Add(item);
        }

        private void StartButton_Click(object sender, EventArgs e)
        {
            if (isRunning) return;
            
            var selectedInstallers = GetSelectedInstallers();
            if (selectedInstallers.Count == 0)
            {
                UpdateStatusLabel("No installers selected");
                return;
            }

            isRunning = true;
            startButton.Enabled = false;
            stopButton.Enabled = true;
            overallProgressBar.Value = 0;
            
            // Run installations in background thread
            var bgWorker = new BackgroundWorker();
            bgWorker.DoWork += (s, args) => RunInstallations(selectedInstallers);
            bgWorker.RunWorkerCompleted += (s, args) => {
                isRunning = false;
                startButton.Enabled = true;
                stopButton.Enabled = false;
            };
            bgWorker.RunWorkerAsync();
        }

        private void RunInstallations(List<InstallerItem> selectedInstallers)
        {
            int completed = 0;
            foreach (var installer in selectedInstallers)
            {
                if (!isRunning) break;
                
                UpdateStatusLabel("Installing " + installer.FileName + "...");
                installer.Status = "Installing";
                this.Invoke(new Action(RefreshInstallerDisplay));
                
                bool success = RunInstaller(installer);
                installer.Status = success ? "✔ Completed" : "✖ Failed";
                
                completed++;
                var progress = (int)((double)completed / selectedInstallers.Count * 100);
                this.Invoke(new Action(() => overallProgressBar.Value = progress));
                this.Invoke(new Action(RefreshInstallerDisplay));
            }
            
            UpdateStatusLabel("Installation complete: " + completed + "/" + selectedInstallers.Count + " installers processed");
        }

        private bool RunInstaller(InstallerItem installer)
        {
            try
            {
                LogMessage("Starting installation: " + installer.FilePath);
                
                string command;
                string args;
                
                if (installer.FilePath.EndsWith(".msi", StringComparison.OrdinalIgnoreCase))
                {
                    command = "msiexec";
                    args = "/i \"" + installer.FilePath + "\" /qn /norestart";
                }
                else
                {
                    command = installer.FilePath;
                    args = "/S"; // Try NSIS silent flag first
                }
                
                var processInfo = new ProcessStartInfo();
                processInfo.FileName = command;
                processInfo.Arguments = args;
                processInfo.UseShellExecute = false;
                processInfo.RedirectStandardOutput = verboseLoggingCheck.Checked;
                processInfo.RedirectStandardError = verboseLoggingCheck.Checked;
                processInfo.CreateNoWindow = true;
                
                using (var process = Process.Start(processInfo))
                {
                    process.WaitForExit();
                    
                    if (verboseLoggingCheck.Checked && process.ExitCode != 0)
                    {
                        LogMessage("Installation failed with exit code: " + process.ExitCode);
                    }
                    
                    return process.ExitCode == 0;
                }
            }
            catch (Exception ex)
            {
                LogMessage("Error installing " + installer.FileName + ": " + ex.Message);
                return false;
            }
        }

        private void DownloadButton_Click(object sender, EventArgs e)
        {
            var selectedPrograms = GetSelectedUniversalPrograms();
            if (selectedPrograms.Count == 0)
            {
                UpdateStatusLabel("No universal programs selected for download");
                return;
            }

            downloadButton.Enabled = false;
            UpdateStatusLabel("Starting download of " + selectedPrograms.Count + " program(s)...");

            // Run downloads in background thread
            var bgWorker = new BackgroundWorker();
            bgWorker.DoWork += (s, args) => DownloadPrograms(selectedPrograms);
            bgWorker.RunWorkerCompleted += (s, args) => {
                downloadButton.Enabled = true;
                this.Invoke(new Action(() => tabControl.SelectedIndex = 0));
            };
            bgWorker.RunWorkerAsync();
        }

        private void DownloadPrograms(List<UniversalProgram> selectedPrograms)
        {
            string downloadsDir = Path.Combine(Path.GetTempPath(), "nullinstaller_downloads");
            Directory.CreateDirectory(downloadsDir);

            int downloaded = 0;
            foreach (var program in selectedPrograms)
            {
                try
                {
                    UpdateStatusLabel("Downloading " + program.Name + "...");
                    
                    string fileName = program.Name.Replace(" ", "") + ".exe";
                    string filePath = Path.Combine(downloadsDir, fileName);
                    
                    using (var client = new WebClient())
                    {
                        client.DownloadFile(program.DownloadURL, filePath);
                        this.Invoke(new Action(() => AddInstallerFile(filePath)));
                        downloaded++;
                    }
                }
                catch (Exception ex)
                {
                    LogMessage("Failed to download " + program.Name + ": " + ex.Message);
                }
            }

            UpdateStatusLabel("Download complete: " + downloaded + " succeeded");
        }

        private List<InstallerItem> GetSelectedInstallers()
        {
            var selected = new List<InstallerItem>();
            foreach (ListViewItem item in localFilesList.Items)
            {
                if (item.Checked && item.Tag is InstallerItem)
                    selected.Add((InstallerItem)item.Tag);
            }
            return selected;
        }

        private List<UniversalProgram> GetSelectedUniversalPrograms()
        {
            var selected = new List<UniversalProgram>();
            foreach (ListViewItem item in universalProgramsList.Items)
            {
                if (item.Checked && item.Tag is UniversalProgram)
                    selected.Add((UniversalProgram)item.Tag);
            }
            return selected;
        }

        private void RefreshInstallerDisplay()
        {
            for (int i = 0; i < localFilesList.Items.Count; i++)
            {
                if (localFilesList.Items[i].Tag is InstallerItem)
                {
                    var installer = (InstallerItem)localFilesList.Items[i].Tag;
                    localFilesList.Items[i].SubItems[2].Text = installer.Status;
                }
            }
        }

        private void StopButton_Click(object sender, EventArgs e)
        {
            isRunning = false;
            UpdateStatusLabel("Installation stopped by user");
        }

        private void ClearButton_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in localFilesList.Items)
            {
                item.Checked = false;
                if (item.Tag is InstallerItem)
                    ((InstallerItem)item.Tag).Status = "Ready";
            }
            RefreshInstallerDisplay();
            overallProgressBar.Value = 0;
            UpdateStatusLabel("All selections cleared");
        }

        private void MainForm_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }

        private void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(DataFormats.FileDrop) is string[])
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                int added = 0;
                foreach (string file in files)
                {
                    if (file.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ||
                        file.EndsWith(".msi", StringComparison.OrdinalIgnoreCase))
                    {
                        AddInstallerFile(file);
                        added++;
                    }
                }
                UpdateStatusLabel("Added " + added + " installer file(s) via drag & drop");
            }
        }

        private void UpdateStatusLabel(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(UpdateStatusLabel), message);
                return;
            }
            statusLabel.Text = message;
            LogMessage(message);
        }

        private void InitializeLogging()
        {
            try
            {
                logWriter = new StreamWriter("install_log.txt", true);
                logWriter.WriteLine("\n=== NullInstaller Started: " + DateTime.Now + " ===");
                logWriter.Flush();
            }
            catch { /* Ignore logging errors */ }
        }

        private void LogMessage(string message)
        {
            try
            {
                string logEntry = "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] " + message;
                if (logWriter != null)
                {
                    logWriter.WriteLine(logEntry);
                    logWriter.Flush();
                }
                
                if (verboseLoggingCheck.Checked)
                {
                    Console.WriteLine(logEntry);
                }
            }
            catch { /* Ignore logging errors */ }
        }

        private string FormatFileSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB" };
            int counter = 0;
            decimal number = bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }
            return number.ToString("n1") + " " + suffixes[counter];
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            isRunning = false;
            if (logWriter != null) logWriter.Close();
            base.OnFormClosed(e);
        }
    }

    public class InstallerItem
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public long Size { get; set; }
        public string Status { get; set; }
    }

    public class UniversalProgram
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string DownloadURL { get; set; }

        public UniversalProgram(string name, string description, string downloadUrl)
        {
            Name = name;
            Description = description;
            DownloadURL = downloadUrl;
        }
    }

    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
