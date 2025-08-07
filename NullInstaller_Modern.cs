using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NullInstaller
{
    public partial class MainForm : Form
    {
        // UI Components
        private TabControl mainTabs;
        private ListView essentialList, privacyList, securityList, developmentList, localList;
        private Button downloadButton, installButton, clearButton;
        private ProgressBar progressBar;
        private Label statusLabel, selectedCountLabel;
        private TextBox logTextBox;
        private CheckBox verboseCheck;
        
        // Data
        private Dictionary<string, List<ProgramInfo>> programCategories;
        private List<InstallerFile> localInstallers = new List<InstallerFile>();
        private bool isRunning = false;
        private StreamWriter logWriter;

        public MainForm()
        {
            InitializeComponent();
            InitializePrograms();
            ScanLocalFiles();
            InitializeLogging();
        }

        private void InitializeComponent()
        {
            // Modern Form Setup
            this.Text = "NullInstaller - Modern Installer Tool";
            this.Size = new Size(1200, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(248, 249, 250); // Light gray background
            this.Font = new Font("Segoe UI", 9F);
            this.MinimumSize = new Size(1000, 600);

            // Create main layout panel
            var mainPanel = new TableLayoutPanel();
            mainPanel.Dock = DockStyle.Fill;
            mainPanel.ColumnCount = 2;
            mainPanel.RowCount = 1;
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 75F));
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            mainPanel.Padding = new Padding(10);

            // Left Panel - Program Selection
            var leftPanel = CreateLeftPanel();
            mainPanel.Controls.Add(leftPanel, 0, 0);

            // Right Panel - Controls and Status
            var rightPanel = CreateRightPanel();
            mainPanel.Controls.Add(rightPanel, 1, 0);

            this.Controls.Add(mainPanel);

            // Enable drag and drop
            this.AllowDrop = true;
            this.DragEnter += MainForm_DragEnter;
            this.DragDrop += MainForm_DragDrop;
        }

        private Panel CreateLeftPanel()
        {
            var panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.BackColor = Color.White;
            panel.Padding = new Padding(5);

            // Create tabs for program categories
            mainTabs = new TabControl();
            mainTabs.Dock = DockStyle.Fill;
            mainTabs.BackColor = Color.White;
            mainTabs.Font = new Font("Segoe UI", 9F);

            // Essential Programs Tab
            var essentialTab = new TabPage("Essential (20)");
            essentialTab.BackColor = Color.White;
            essentialList = CreateProgramListView();
            essentialTab.Controls.Add(essentialList);

            // Privacy Programs Tab  
            var privacyTab = new TabPage("Privacy (15)");
            privacyTab.BackColor = Color.White;
            privacyList = CreateProgramListView();
            privacyTab.Controls.Add(privacyList);

            // Security Programs Tab
            var securityTab = new TabPage("Security (15)");
            securityTab.BackColor = Color.White;
            securityList = CreateProgramListView();
            securityTab.Controls.Add(securityList);

            // Development Programs Tab
            var devTab = new TabPage("Development (10)");
            devTab.BackColor = Color.White;
            developmentList = CreateProgramListView();
            devTab.Controls.Add(developmentList);

            // Local Files Tab
            var localTab = new TabPage("Local Files");
            localTab.BackColor = Color.White;
            localList = CreateLocalListView();
            localTab.Controls.Add(localList);

            mainTabs.TabPages.Add(essentialTab);
            mainTabs.TabPages.Add(privacyTab);
            mainTabs.TabPages.Add(securityTab);
            mainTabs.TabPages.Add(devTab);
            mainTabs.TabPages.Add(localTab);

            panel.Controls.Add(mainTabs);
            return panel;
        }

        private ListView CreateProgramListView()
        {
            var listView = new ListView();
            listView.Dock = DockStyle.Fill;
            listView.View = View.Details;
            listView.CheckBoxes = true;
            listView.FullRowSelect = true;
            listView.GridLines = false;
            listView.HeaderStyle = ColumnHeaderStyle.None;
            listView.BackColor = Color.White;
            listView.BorderStyle = BorderStyle.None;
            listView.Font = new Font("Segoe UI", 9F);
            
            listView.Columns.Add("Program", 300);
            listView.Columns.Add("Description", 250);
            listView.ItemChecked += (s, e) => UpdateSelectedCount();
            
            return listView;
        }

        private ListView CreateLocalListView()
        {
            var listView = new ListView();
            listView.Dock = DockStyle.Fill;
            listView.View = View.Details;
            listView.CheckBoxes = true;
            listView.FullRowSelect = true;
            listView.GridLines = false;
            listView.HeaderStyle = ColumnHeaderStyle.Clickable;
            listView.BackColor = Color.White;
            listView.BorderStyle = BorderStyle.None;
            listView.Font = new Font("Segoe UI", 9F);
            
            listView.Columns.Add("File Name", 200);
            listView.Columns.Add("Size", 80);
            listView.Columns.Add("Status", 100);
            listView.ItemChecked += (s, e) => UpdateSelectedCount();

            // Drop zone label when empty
            var dropLabel = new Label();
            dropLabel.Text = "Drop .exe or .msi files here\nor they will be auto-detected";
            dropLabel.ForeColor = Color.Gray;
            dropLabel.Font = new Font("Segoe UI", 10F);
            dropLabel.TextAlign = ContentAlignment.MiddleCenter;
            dropLabel.Dock = DockStyle.Fill;
            dropLabel.Visible = localInstallers.Count == 0;
            listView.Controls.Add(dropLabel);
            
            return listView;
        }

        private Panel CreateRightPanel()
        {
            var panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.BackColor = Color.FromArgb(248, 249, 250);
            panel.Padding = new Padding(10);

            // Title
            var titleLabel = new Label();
            titleLabel.Text = "NullInstaller";
            titleLabel.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            titleLabel.ForeColor = Color.FromArgb(33, 37, 41);
            titleLabel.Location = new Point(10, 10);
            titleLabel.Size = new Size(200, 30);

            // Selected count
            selectedCountLabel = new Label();
            selectedCountLabel.Text = "0 programs selected";
            selectedCountLabel.Font = new Font("Segoe UI", 9F);
            selectedCountLabel.ForeColor = Color.FromArgb(108, 117, 125);
            selectedCountLabel.Location = new Point(10, 45);
            selectedCountLabel.Size = new Size(200, 20);

            // Download Button - Modern style
            downloadButton = new Button();
            downloadButton.Text = "Download Selected";
            downloadButton.Size = new Size(200, 35);
            downloadButton.Location = new Point(10, 80);
            downloadButton.BackColor = Color.FromArgb(0, 123, 255);
            downloadButton.ForeColor = Color.White;
            downloadButton.FlatStyle = FlatStyle.Flat;
            downloadButton.FlatAppearance.BorderSize = 0;
            downloadButton.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            downloadButton.Cursor = Cursors.Hand;
            downloadButton.Click += DownloadButton_Click;

            // Install Button
            installButton = new Button();
            installButton.Text = "Install Selected";
            installButton.Size = new Size(200, 35);
            installButton.Location = new Point(10, 125);
            installButton.BackColor = Color.FromArgb(40, 167, 69);
            installButton.ForeColor = Color.White;
            installButton.FlatStyle = FlatStyle.Flat;
            installButton.FlatAppearance.BorderSize = 0;
            installButton.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            installButton.Cursor = Cursors.Hand;
            installButton.Click += InstallButton_Click;

            // Clear Button
            clearButton = new Button();
            clearButton.Text = "Clear All";
            clearButton.Size = new Size(200, 30);
            clearButton.Location = new Point(10, 170);
            clearButton.BackColor = Color.FromArgb(108, 117, 125);
            clearButton.ForeColor = Color.White;
            clearButton.FlatStyle = FlatStyle.Flat;
            clearButton.FlatAppearance.BorderSize = 0;
            clearButton.Font = new Font("Segoe UI", 8F);
            clearButton.Cursor = Cursors.Hand;
            clearButton.Click += ClearButton_Click;

            // Progress Bar
            progressBar = new ProgressBar();
            progressBar.Size = new Size(200, 6);
            progressBar.Location = new Point(10, 215);
            progressBar.Style = ProgressBarStyle.Continuous;

            // Status Label
            statusLabel = new Label();
            statusLabel.Text = "Ready";
            statusLabel.Font = new Font("Segoe UI", 9F);
            statusLabel.ForeColor = Color.FromArgb(108, 117, 125);
            statusLabel.Location = new Point(10, 230);
            statusLabel.Size = new Size(200, 40);
            statusLabel.AutoSize = false;

            // Verbose Checkbox
            verboseCheck = new CheckBox();
            verboseCheck.Text = "Verbose Logging";
            verboseCheck.Font = new Font("Segoe UI", 8F);
            verboseCheck.ForeColor = Color.FromArgb(108, 117, 125);
            verboseCheck.Location = new Point(10, 280);
            verboseCheck.Size = new Size(120, 20);
            verboseCheck.Checked = true;

            // Log Display
            var logLabel = new Label();
            logLabel.Text = "Activity Log:";
            logLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            logLabel.ForeColor = Color.FromArgb(33, 37, 41);
            logLabel.Location = new Point(10, 310);
            logLabel.Size = new Size(100, 20);

            logTextBox = new TextBox();
            logTextBox.Multiline = true;
            logTextBox.ReadOnly = true;
            logTextBox.ScrollBars = ScrollBars.Vertical;
            logTextBox.BackColor = Color.FromArgb(248, 249, 250);
            logTextBox.BorderStyle = BorderStyle.FixedSingle;
            logTextBox.Font = new Font("Consolas", 8F);
            logTextBox.ForeColor = Color.FromArgb(73, 80, 87);
            logTextBox.Location = new Point(10, 335);
            logTextBox.Size = new Size(200, 200);

            panel.Controls.AddRange(new Control[] {
                titleLabel, selectedCountLabel, downloadButton, installButton, 
                clearButton, progressBar, statusLabel, verboseCheck, logLabel, logTextBox
            });

            return panel;
        }

        private void InitializePrograms()
        {
            programCategories = new Dictionary<string, List<ProgramInfo>>();

            // Essential Programs (20)
            programCategories["Essential"] = new List<ProgramInfo>
            {
                new ProgramInfo("Google Chrome", "Web browser", "https://dl.google.com/chrome/install/ChromeStandaloneSetup64.exe"),
                new ProgramInfo("Mozilla Firefox", "Web browser", "https://download.mozilla.org/?product=firefox-latest-ssl&os=win64&lang=en-US"),
                new ProgramInfo("Microsoft Edge", "Web browser", "https://msedge.sf.dl.delivery.mp.microsoft.com/filestreamingservice/files/MicrosoftEdgeEnterpriseX64.msi"),
                new ProgramInfo("VLC Media Player", "Media player", "https://get.videolan.org/vlc/last/win64/vlc-3.0.20-win64.exe"),
                new ProgramInfo("7-Zip", "File archiver", "https://www.7-zip.org/a/7z2301-x64.exe"),
                new ProgramInfo("WinRAR", "File archiver", "https://www.rarlab.com/rar/winrar-x64-623.exe"),
                new ProgramInfo("Notepad++", "Text editor", "https://github.com/notepad-plus-plus/notepad-plus-plus/releases/download/v8.5.8/npp.8.5.8.Installer.x64.exe"),
                new ProgramInfo("Adobe Acrobat Reader", "PDF reader", "https://ardownload2.adobe.com/pub/adobe/reader/win/AcrobatDC/2300820360/AcroRdrDC2300820360_en_US.exe"),
                new ProgramInfo("LibreOffice", "Office suite", "https://download.libreoffice.org/libreoffice/stable/7.5.6/win/x86_64/LibreOffice_7.5.6_Win_x64.msi"),
                new ProgramInfo("GIMP", "Image editor", "https://download.gimp.org/gimp/v2.10/windows/gimp-2.10.34-setup-3.exe"),
                new ProgramInfo("Discord", "Communication", "https://discord.com/api/downloads/distributions/app/installers/latest?channel=stable&platform=win&arch=x64"),
                new ProgramInfo("Spotify", "Music streaming", "https://download.scdn.co/SpotifySetup.exe"),
                new ProgramInfo("Steam", "Gaming platform", "https://cdn.akamai.steamstatic.com/client/installer/SteamSetup.exe"),
                new ProgramInfo("Zoom", "Video conferencing", "https://zoom.us/client/latest/ZoomInstallerFull.exe"),
                new ProgramInfo("TeamViewer", "Remote desktop", "https://download.teamviewer.com/download/TeamViewer_Setup.exe"),
                new ProgramInfo("CCleaner", "System cleaner", "https://download.ccleaner.com/ccsetup608.exe"),
                new ProgramInfo("BleachBit", "System cleaner", "https://download.bleachbit.org/BleachBit-4.6.0-setup.exe"),
                new ProgramInfo("HandBrake", "Video converter", "https://github.com/HandBrake/HandBrake/releases/download/1.6.1/HandBrake-1.6.1-x86_64-Win_GUI.exe"),
                new ProgramInfo("OBS Studio", "Screen recording", "https://cdn-fastly.obsproject.com/downloads/OBS-Studio-29.1.3-Windows-Installer.exe"),
                new ProgramInfo("Audacity", "Audio editor", "https://github.com/audacity/audacity/releases/download/Audacity-3.3.3/audacity-win-3.3.3-x64.exe")
            };

            // Privacy Programs (15)
            programCategories["Privacy"] = new List<ProgramInfo>
            {
                new ProgramInfo("Mullvad Browser", "Privacy browser", "https://cdn.mullvad.net/browser/13.0.1/mullvad-browser-windows-x86_64-13.0.1.exe"),
                new ProgramInfo("Mullvad VPN", "Privacy VPN", "https://github.com/mullvad/mullvadvpn-app/releases/download/2023.5/MullvadVPN-2023.5.exe"),
                new ProgramInfo("NordVPN", "VPN service", "https://downloads.nordcdn.com/apps/windows/NordVPN/latest/NordVPNSetup.exe"),
                new ProgramInfo("IPVanish VPN", "VPN service", "https://www.ipvanish.com/software/setup-prod-v2/ipvanish-setup.exe"),
                new ProgramInfo("Tor Browser", "Anonymous browser", "https://dist.torproject.org/torbrowser/12.5.6/torbrowser-install-win64-12.5.6_ALL.exe"),
                new ProgramInfo("Brave Browser", "Privacy browser", "https://laptop-updates.brave.com/latest/winx64"),
                new ProgramInfo("Signal Desktop", "Private messaging", "https://updates.signal.org/desktop/signal-desktop-win-6.32.0.exe"),
                new ProgramInfo("ProtonVPN", "Secure VPN", "https://protonvpn.com/download/ProtonVPN_win_v3.0.6.exe"),
                new ProgramInfo("VeraCrypt", "Disk encryption", "https://launchpad.net/veracrypt/trunk/1.25.9/+download/VeraCrypt%20Setup%201.25.9.exe"),
                new ProgramInfo("KeePassXC", "Password manager", "https://github.com/keepassxreboot/keepassxc/releases/download/2.7.6/KeePassXC-2.7.6-Win64.msi"),
                new ProgramInfo("Bitwarden", "Password manager", "https://vault.bitwarden.com/download/?app=desktop&platform=windows"),
                new ProgramInfo("qBittorrent", "Torrent client", "https://sourceforge.net/projects/qbittorrent/files/qbittorrent-win32/qbittorrent-4.5.4/qbittorrent_4.5.4_x64_setup.exe"),
                new ProgramInfo("I2P", "Anonymous network", "https://download.i2p2.de/releases/0.9.58/i2pinstall_0.9.58_windows.exe"),
                new ProgramInfo("Tails", "Anonymous OS", "https://tails.boum.org/install/win/usb/tails-amd64-5.16.img"),
                new ProgramInfo("GnuPG", "Encryption tool", "https://files.gpg4win.org/gpg4win-4.2.0.exe")
            };

            // Security Programs (15)
            programCategories["Security"] = new List<ProgramInfo>
            {
                new ProgramInfo("Kaspersky Security Cloud", "Antivirus", "https://dm.s.kaspersky-labs.com/download/ksc20/kaspersky_security_cloud.exe"),
                new ProgramInfo("Bitdefender Antivirus", "Antivirus", "https://download.bitdefender.com/windows/installer/en-us/bitdefender_tsecurity.exe"),
                new ProgramInfo("TotalAV Antivirus", "Antivirus", "https://downloads.totalav.com/installs/totalav-pc-app.exe"),
                new ProgramInfo("Malwarebytes", "Anti-malware", "https://data-cdn.mbamupdates.com/web/mb4-setup-consumer/MBSetup.exe"),
                new ProgramInfo("Windows Defender", "Built-in antivirus", ""),
                new ProgramInfo("ESET NOD32", "Antivirus", "https://download.eset.com/com/eset/apps/home/eav/windows/latest/eav_nt64.exe"),
                new ProgramInfo("Avast Free Antivirus", "Antivirus", "https://bits.avcdn.net/productfamily_ANTIVIRUS/insttype_FREE/platform_WIN/installertype_FULL/build_RELEASE/"),
                new ProgramInfo("AVG AntiVirus", "Antivirus", "https://bits.avcdn.net/productfamily_ANTIVIRUS/insttype_FREE/platform_WIN/installertype_ONLINE/build_RELEASE/"),
                new ProgramInfo("Spybot Search & Destroy", "Anti-spyware", "https://download.spybot.info/exe/SpybotSD2.exe"),
                new ProgramInfo("AdwCleaner", "Adware remover", "https://adwcleaner.malwarebytes.com/adwcleaner?channel=release"),
                new ProgramInfo("RootkitRevealer", "Rootkit scanner", "https://download.sysinternals.com/files/RootkitRevealer.zip"),
                new ProgramInfo("ClamWin Antivirus", "Open source AV", "https://downloads.sourceforge.net/clamwin/clamwin-0.103.9-setup.exe"),
                new ProgramInfo("HijackThis", "System scanner", "https://github.com/dragokas/hijackthis/releases/download/v2.8.1/HiJackThis.exe"),
                new ProgramInfo("Process Monitor", "System monitoring", "https://download.sysinternals.com/files/ProcessMonitor.zip"),
                new ProgramInfo("Wireshark", "Network analyzer", "https://2.na.dl.wireshark.org/win64/Wireshark-4.0.8-x64.exe")
            };

            // Development Programs (10)
            programCategories["Development"] = new List<ProgramInfo>
            {
                new ProgramInfo("Visual Studio Code", "Code editor", "https://code.visualstudio.com/sha/download?build=stable&os=win32-x64-user"),
                new ProgramInfo("Git for Windows", "Version control", "https://github.com/git-for-windows/git/releases/download/v2.42.0.windows.2/Git-2.42.0.2-64-bit.exe"),
                new ProgramInfo("Node.js", "JavaScript runtime", "https://nodejs.org/dist/v20.5.1/node-v20.5.1-x64.msi"),
                new ProgramInfo("Python", "Programming language", "https://www.python.org/ftp/python/3.11.5/python-3.11.5-amd64.exe"),
                new ProgramInfo("JetBrains Toolbox", "IDE manager", "https://download.jetbrains.com/toolbox/jetbrains-toolbox-1.28.1.15219.exe"),
                new ProgramInfo("Docker Desktop", "Containerization", "https://desktop.docker.com/win/main/amd64/Docker%20Desktop%20Installer.exe"),
                new ProgramInfo("Postman", "API testing", "https://dl.pstmn.io/download/latest/win64"),
                new ProgramInfo("FileZilla", "FTP client", "https://download.filezilla-project.org/client/FileZilla_3.65.0_win64-setup.exe"),
                new ProgramInfo("PuTTY", "SSH client", "https://the.earth.li/~sgtatham/putty/latest/w64/putty-64bit-0.78-installer.msi"),
                new ProgramInfo("WinSCP", "SCP client", "https://winscp.net/download/WinSCP-5.21.8-Setup.exe")
            };

            PopulateAllLists();
        }

        private void PopulateAllLists()
        {
            PopulateListView(essentialList, programCategories["Essential"]);
            PopulateListView(privacyList, programCategories["Privacy"]);
            PopulateListView(securityList, programCategories["Security"]);
            PopulateListView(developmentList, programCategories["Development"]);
        }

        private void PopulateListView(ListView listView, List<ProgramInfo> programs)
        {
            listView.Items.Clear();
            foreach (var program in programs)
            {
                var item = new ListViewItem(program.Name);
                item.SubItems.Add(program.Description);
                item.Tag = program;
                item.Font = new Font("Segoe UI", 9F);
                listView.Items.Add(item);
            }
        }

        private void ScanLocalFiles()
        {
            Task.Run(() =>
            {
                var paths = new[] { 
                    @"C:\Users\Administrator\Desktop\Down",
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Downloads"
                };

                foreach (var path in paths)
                {
                    if (Directory.Exists(path))
                    {
                        ScanDirectory(path);
                    }
                }

                this.Invoke(new Action(UpdateLocalFilesList));
            });
        }

        private void ScanDirectory(string path)
        {
            try
            {
                foreach (string file in Directory.GetFiles(path, "*.exe").Concat(Directory.GetFiles(path, "*.msi")))
                {
                    var fileInfo = new FileInfo(file);
                    var installer = new InstallerFile
                    {
                        FilePath = file,
                        FileName = fileInfo.Name,
                        Size = fileInfo.Length,
                        Status = "Ready"
                    };
                    localInstallers.Add(installer);
                }
            }
            catch { /* Ignore access errors */ }
        }

        private void UpdateLocalFilesList()
        {
            localList.Items.Clear();
            foreach (var installer in localInstallers)
            {
                var item = new ListViewItem(installer.FileName);
                item.SubItems.Add(FormatFileSize(installer.Size));
                item.SubItems.Add(installer.Status);
                item.Tag = installer;
                localList.Items.Add(item);
            }
        }

        private async void DownloadButton_Click(object sender, EventArgs e)
        {
            if (isRunning) return;

            var selectedPrograms = GetSelectedPrograms();
            if (selectedPrograms.Count == 0)
            {
                ShowMessage("No programs selected for download");
                return;
            }

            isRunning = true;
            downloadButton.Enabled = false;
            ShowMessage($"Downloading {selectedPrograms.Count} programs...");

            await Task.Run(() => DownloadPrograms(selectedPrograms));

            downloadButton.Enabled = true;
            isRunning = false;
            mainTabs.SelectedTab = mainTabs.TabPages[4]; // Switch to Local Files tab
        }

        private async void InstallButton_Click(object sender, EventArgs e)
        {
            if (isRunning) return;

            var selectedFiles = GetSelectedLocalFiles();
            if (selectedFiles.Count == 0)
            {
                ShowMessage("No files selected for installation");
                return;
            }

            isRunning = true;
            installButton.Enabled = false;
            
            await Task.Run(() => InstallFiles(selectedFiles));
            
            installButton.Enabled = true;
            isRunning = false;
        }

        private void ClearButton_Click(object sender, EventArgs e)
        {
            ClearAllSelections();
            ShowMessage("All selections cleared");
        }

        private List<ProgramInfo> GetSelectedPrograms()
        {
            var selected = new List<ProgramInfo>();
            
            foreach (ListView list in new[] { essentialList, privacyList, securityList, developmentList })
            {
                foreach (ListViewItem item in list.CheckedItems)
                {
                    if (item.Tag is ProgramInfo program)
                        selected.Add(program);
                }
            }
            
            return selected;
        }

        private List<InstallerFile> GetSelectedLocalFiles()
        {
            return localList.CheckedItems.Cast<ListViewItem>()
                .Where(item => item.Tag is InstallerFile)
                .Select(item => (InstallerFile)item.Tag)
                .ToList();
        }

        private void DownloadPrograms(List<ProgramInfo> programs)
        {
            var downloadDir = Path.Combine(Path.GetTempPath(), "NullInstaller_Downloads");
            Directory.CreateDirectory(downloadDir);

            int completed = 0;
            foreach (var program in programs)
            {
                try
                {
                    if (string.IsNullOrEmpty(program.DownloadURL)) continue;

                    this.Invoke(new Action(() => 
                    {
                        progressBar.Value = (completed * 100) / programs.Count;
                        ShowMessage($"Downloading {program.Name}...");
                    }));

                    var fileName = $"{program.Name.Replace(" ", "").Replace("/", "")}.exe";
                    var filePath = Path.Combine(downloadDir, fileName);

                    using (var client = new WebClient())
                    {
                        client.DownloadFile(program.DownloadURL, filePath);
                    }

                    var installer = new InstallerFile
                    {
                        FilePath = filePath,
                        FileName = Path.GetFileName(filePath),
                        Size = new FileInfo(filePath).Length,
                        Status = "Downloaded"
                    };

                    localInstallers.Add(installer);
                    completed++;

                    LogMessage($"Downloaded: {program.Name}");
                }
                catch (Exception ex)
                {
                    LogMessage($"Failed to download {program.Name}: {ex.Message}");
                }
            }

            this.Invoke(new Action(() =>
            {
                UpdateLocalFilesList();
                progressBar.Value = 100;
                ShowMessage($"Download complete: {completed}/{programs.Count} successful");
            }));
        }

        private void InstallFiles(List<InstallerFile> files)
        {
            int completed = 0;
            foreach (var file in files)
            {
                try
                {
                    this.Invoke(new Action(() =>
                    {
                        progressBar.Value = (completed * 100) / files.Count;
                        ShowMessage($"Installing {file.FileName}...");
                        UpdateFileStatus(file, "Installing");
                    }));

                    bool success = InstallFile(file);
                    
                    this.Invoke(new Action(() =>
                    {
                        UpdateFileStatus(file, success ? "✓ Installed" : "✗ Failed");
                    }));

                    completed++;
                    LogMessage($"{(success ? "Installed" : "Failed")}: {file.FileName}");
                }
                catch (Exception ex)
                {
                    LogMessage($"Error installing {file.FileName}: {ex.Message}");
                    this.Invoke(new Action(() => UpdateFileStatus(file, "✗ Error")));
                }
            }

            this.Invoke(new Action(() =>
            {
                progressBar.Value = 100;
                ShowMessage($"Installation complete: {completed}/{files.Count} successful");
            }));
        }

        private bool InstallFile(InstallerFile file)
        {
            try
            {
                string command, args;

                if (file.FilePath.EndsWith(".msi", StringComparison.OrdinalIgnoreCase))
                {
                    command = "msiexec";
                    args = $"/i \"{file.FilePath}\" /qn /norestart";
                }
                else
                {
                    command = file.FilePath;
                    args = "/S"; // Try common silent flag
                }

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = command,
                        Arguments = args,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = verboseCheck.Checked,
                        RedirectStandardError = verboseCheck.Checked
                    }
                };

                process.Start();
                process.WaitForExit();

                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        private void UpdateFileStatus(InstallerFile file, string status)
        {
            file.Status = status;
            foreach (ListViewItem item in localList.Items)
            {
                if (item.Tag == file)
                {
                    item.SubItems[2].Text = status;
                    break;
                }
            }
        }

        private void ClearAllSelections()
        {
            foreach (ListView list in new[] { essentialList, privacyList, securityList, developmentList, localList })
            {
                foreach (ListViewItem item in list.Items)
                    item.Checked = false;
            }
            UpdateSelectedCount();
        }

        private void UpdateSelectedCount()
        {
            int count = 0;
            foreach (ListView list in new[] { essentialList, privacyList, securityList, developmentList, localList })
            {
                count += list.CheckedItems.Count;
            }
            
            if (selectedCountLabel != null)
                selectedCountLabel.Text = $"{count} items selected";
        }

        private void ShowMessage(string message)
        {
            if (statusLabel != null)
                statusLabel.Text = message;
            LogMessage(message);
        }

        private void LogMessage(string message)
        {
            var logEntry = $"[{DateTime.Now:HH:mm:ss}] {message}";
            
            if (logTextBox != null)
            {
                logTextBox.Invoke(new Action(() =>
                {
                    logTextBox.AppendText(logEntry + Environment.NewLine);
                    logTextBox.SelectionStart = logTextBox.Text.Length;
                    logTextBox.ScrollToCaret();
                }));
            }

            try
            {
                logWriter?.WriteLine(logEntry);
                logWriter?.Flush();
            }
            catch { }
        }

        private void InitializeLogging()
        {
            try
            {
                logWriter = new StreamWriter("NullInstaller_Log.txt", true);
                logWriter.WriteLine($"\n=== NullInstaller Started: {DateTime.Now} ===");
                LogMessage("NullInstaller initialized successfully");
            }
            catch { }
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
            return $"{number:n1} {suffixes[counter]}";
        }

        private void MainForm_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }

        private void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(DataFormats.FileDrop) is string[] files)
            {
                int added = 0;
                foreach (string file in files)
                {
                    if (file.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ||
                        file.EndsWith(".msi", StringComparison.OrdinalIgnoreCase))
                    {
                        var fileInfo = new FileInfo(file);
                        var installer = new InstallerFile
                        {
                            FilePath = file,
                            FileName = fileInfo.Name,
                            Size = fileInfo.Length,
                            Status = "Ready"
                        };
                        localInstallers.Add(installer);
                        added++;
                    }
                }
                
                if (added > 0)
                {
                    UpdateLocalFilesList();
                    ShowMessage($"Added {added} file(s) via drag & drop");
                    mainTabs.SelectedTab = mainTabs.TabPages[4]; // Switch to Local Files tab
                }
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            logWriter?.Close();
            base.OnFormClosed(e);
        }
    }

    public class ProgramInfo
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string DownloadURL { get; set; }

        public ProgramInfo(string name, string description, string downloadUrl)
        {
            Name = name;
            Description = description;
            DownloadURL = downloadUrl;
        }
    }

    public class InstallerFile
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public long Size { get; set; }
        public string Status { get; set; }
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
