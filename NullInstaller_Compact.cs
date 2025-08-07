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
        private ListView installerListView;
        private Button installButton, stealthButton, clearButton, verifyButton;
        private ProgressBar progressBar;
        private Label titleLabel, countLabel, statusLabel;
        private CheckBox verboseCheck, showOutputCheck;
        private Panel headerPanel, listPanel, buttonPanel, progressPanel;
        
        // Data
        private List<InstallerFile> localInstallers = new List<InstallerFile>();
        private List<ProgramInfo> stealthPrograms = new List<ProgramInfo>();
        private bool isRunning = false;
        private StreamWriter logWriter;
        private int currentProgress = 0;

        public MainForm()
        {
            InitializeComponent();
            InitializeStealthPrograms();
            ScanLocalFiles();
            InitializeLogging();
        }

        private void InitializeComponent()
        {
            // Compact Form Setup
            this.Text = "NullInstaller v0.1.0";
            this.Size = new Size(580, 420);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(32, 32, 32);
            this.Font = new Font("Segoe UI", 9F);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            
            CreateLayout();
            
            // Enable drag and drop
            this.AllowDrop = true;
            this.DragEnter += MainForm_DragEnter;
            this.DragDrop += MainForm_DragDrop;
        }

        private void CreateLayout()
        {
            // Header Panel
            headerPanel = new Panel();
            headerPanel.Size = new Size(580, 60);
            headerPanel.Location = new Point(0, 0);
            headerPanel.BackColor = Color.FromArgb(28, 28, 28);
            headerPanel.Dock = DockStyle.Top;

            // Title
            titleLabel = new Label();
            titleLabel.Text = "NullInstaller v0.1.0";
            titleLabel.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            titleLabel.ForeColor = Color.White;
            titleLabel.Location = new Point(15, 10);
            titleLabel.AutoSize = true;

            // Count
            countLabel = new Label();
            countLabel.Text = "0 installers detected";
            countLabel.Font = new Font("Segoe UI", 9F);
            countLabel.ForeColor = Color.FromArgb(160, 160, 160);
            countLabel.Location = new Point(400, 18);
            countLabel.AutoSize = true;

            headerPanel.Controls.AddRange(new Control[] { titleLabel, countLabel });

            // List Panel
            listPanel = new Panel();
            listPanel.Location = new Point(0, 60);
            listPanel.Size = new Size(580, 240);
            listPanel.BackColor = Color.FromArgb(38, 38, 38);

            // Section Label
            var sectionLabel = new Label();
            sectionLabel.Text = "INSTALLER FILES";
            sectionLabel.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            sectionLabel.ForeColor = Color.FromArgb(140, 140, 140);
            sectionLabel.Location = new Point(15, 8);
            sectionLabel.AutoSize = true;

            // Installer ListView
            installerListView = new ListView();
            installerListView.Location = new Point(15, 25);
            installerListView.Size = new Size(550, 200);
            installerListView.View = View.Details;
            installerListView.CheckBoxes = true;
            installerListView.FullRowSelect = true;
            installerListView.GridLines = false;
            installerListView.HeaderStyle = ColumnHeaderStyle.None;
            installerListView.BackColor = Color.FromArgb(45, 45, 45);
            installerListView.ForeColor = Color.White;
            installerListView.BorderStyle = BorderStyle.FixedSingle;
            installerListView.Font = new Font("Segoe UI", 9F);
            
            installerListView.Columns.Add("Program", 280);
            installerListView.Columns.Add("Size", 80);
            installerListView.Columns.Add("Status", 120);
            installerListView.ItemChecked += (s, e) => UpdateSelectedCount();

            listPanel.Controls.AddRange(new Control[] { sectionLabel, installerListView });

            // Button Panel
            buttonPanel = new Panel();
            buttonPanel.Location = new Point(0, 300);
            buttonPanel.Size = new Size(580, 50);
            buttonPanel.BackColor = Color.FromArgb(28, 28, 28);

            // Install Button
            installButton = CreateButton("↓ Install All", Color.FromArgb(0, 120, 215), new Point(15, 10));
            installButton.Click += InstallButton_Click;

            // Stealth Setup Button
            stealthButton = CreateButton("🔒 Stealth Setup", Color.FromArgb(108, 117, 125), new Point(135, 10));
            stealthButton.Click += StealthButton_Click;

            // Verify Button
            verifyButton = CreateButton("✓ Verify", Color.FromArgb(40, 167, 69), new Point(255, 10));
            verifyButton.Size = new Size(80, 30);
            verifyButton.Click += VerifyButton_Click;

            // Clear Button
            clearButton = CreateButton("Clear", Color.FromArgb(220, 53, 69), new Point(340, 10));
            clearButton.Size = new Size(60, 30);
            clearButton.Click += ClearButton_Click;

            // Verbose Checkbox
            verboseCheck = new CheckBox();
            verboseCheck.Text = "Verbose";
            verboseCheck.Font = new Font("Segoe UI", 7F);
            verboseCheck.ForeColor = Color.FromArgb(160, 160, 160);
            verboseCheck.Location = new Point(410, 12);
            verboseCheck.AutoSize = true;

            // Show Output Checkbox
            showOutputCheck = new CheckBox();
            showOutputCheck.Text = "Show Output";
            showOutputCheck.Font = new Font("Segoe UI", 7F);
            showOutputCheck.ForeColor = Color.FromArgb(160, 160, 160);
            showOutputCheck.Location = new Point(410, 25);
            showOutputCheck.AutoSize = true;

            buttonPanel.Controls.AddRange(new Control[] { installButton, stealthButton, verifyButton, clearButton, verboseCheck, showOutputCheck });

            // Progress Panel
            progressPanel = new Panel();
            progressPanel.Location = new Point(0, 350);
            progressPanel.Size = new Size(580, 50);
            progressPanel.BackColor = Color.FromArgb(32, 32, 32);
            progressPanel.Dock = DockStyle.Bottom;

            // Progress Bar
            progressBar = new ProgressBar();
            progressBar.Location = new Point(15, 8);
            progressBar.Size = new Size(550, 8);
            progressBar.Style = ProgressBarStyle.Continuous;
            progressBar.BackColor = Color.FromArgb(60, 60, 60);
            progressBar.ForeColor = Color.FromArgb(0, 120, 215);

            // Status Label
            statusLabel = new Label();
            statusLabel.Text = "Ready - Drag .exe/.msi files here or use Stealth Setup";
            statusLabel.Font = new Font("Segoe UI", 8F);
            statusLabel.ForeColor = Color.FromArgb(140, 140, 140);
            statusLabel.Location = new Point(15, 22);
            statusLabel.Size = new Size(550, 20);

            progressPanel.Controls.AddRange(new Control[] { progressBar, statusLabel });

            // Add all panels to form
            this.Controls.AddRange(new Control[] { headerPanel, listPanel, buttonPanel, progressPanel });
        }

        private Button CreateButton(string text, Color backColor, Point location)
        {
            var button = new Button();
            button.Text = text;
            button.Size = new Size(120, 30);
            button.Location = location;
            button.BackColor = backColor;
            button.ForeColor = Color.White;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            button.Cursor = Cursors.Hand;
            
            // Hover effects
            var originalColor = backColor;
            button.MouseEnter += (s, e) => button.BackColor = ControlPaint.Light(originalColor, 0.2f);
            button.MouseLeave += (s, e) => button.BackColor = originalColor;
            
            return button;
        }

        private void InitializeStealthPrograms()
        {
            stealthPrograms = new List<ProgramInfo>
            {
                // Privacy Browsers
                new ProgramInfo("Tor Browser", "Anonymous browsing", "https://dist.torproject.org/torbrowser/13.0.1/torbrowser-install-win64-13.0.1_ALL.exe"),
                new ProgramInfo("Brave Browser", "Privacy-focused browser", "https://laptop-updates.brave.com/latest/winx64"),
                new ProgramInfo("Mullvad Browser", "Tor Browser fork", "https://cdn.mullvad.net/browser/13.0.1/mullvad-browser-windows-x86_64-13.0.1.exe"),
                
                // VPNs
                new ProgramInfo("Mullvad VPN", "Privacy VPN", "https://github.com/mullvad/mullvadvpn-app/releases/latest/download/MullvadVPN-2023.6.exe"),
                new ProgramInfo("ProtonVPN", "Secure VPN", "https://protonvpn.com/download/ProtonVPN_win_v3.2.0.exe"),
                new ProgramInfo("Windscribe", "VPN service", "https://windscribe.com/install/desktop/windows"),
                
                // Security & Privacy Tools
                new ProgramInfo("VeraCrypt", "Disk encryption", "https://launchpad.net/veracrypt/trunk/1.25.9/+download/VeraCrypt%20Setup%201.25.9.exe"),
                new ProgramInfo("KeePassXC", "Password manager", "https://github.com/keepassxreboot/keepassxc/releases/latest/download/KeePassXC-2.7.6-Win64.msi"),
                new ProgramInfo("Signal Desktop", "Secure messaging", "https://updates.signal.org/desktop/signal-desktop-win-6.39.0.exe"),
                new ProgramInfo("qBittorrent", "Private torrent client", "https://sourceforge.net/projects/qbittorrent/files/latest/download"),
                
                // System Cleaning
                new ProgramInfo("BleachBit", "Privacy cleaner", "https://download.bleachbit.org/BleachBit-4.6.0-setup.exe"),
                new ProgramInfo("Eraser", "Secure file deletion", "https://eraser.heidi.ie/download/beta/Eraser%206.2.2.2993.exe")
            };
        }

        private void ScanLocalFiles()
        {
            Task.Run(() =>
            {
                var paths = new[] { 
                    @"C:\Users\Administrator\Desktop\Down",
                    @"C:\Users\Administrator\Desktop\Projects\NullInstaller\dist",
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"),
                    Path.GetTempPath() + "NullInstaller_Downloads"
                };

                foreach (var path in paths)
                {
                    if (Directory.Exists(path))
                    {
                        ScanDirectory(path);
                    }
                }

                this.Invoke(new Action(UpdateInstallerList));
            });
        }

        private void ScanDirectory(string path)
        {
            try
            {
                var extensions = new[] { "*.exe", "*.msi" };
                foreach (var extension in extensions)
                {
                    foreach (string file in Directory.GetFiles(path, extension))
                    {
                        var fileInfo = new FileInfo(file);
                        var installer = new InstallerFile
                        {
                            FilePath = file,
                            FileName = fileInfo.Name,
                            Size = fileInfo.Length,
                            Status = "Ready"
                        };
                        
                        if (!localInstallers.Any(i => i.FileName.Equals(installer.FileName, StringComparison.OrdinalIgnoreCase)))
                        {
                            localInstallers.Add(installer);
                        }
                    }
                }
            }
            catch { /* Ignore access errors */ }
        }

        private void UpdateInstallerList()
        {
            installerListView.Items.Clear();
            
            foreach (var installer in localInstallers.OrderBy(i => i.FileName))
            {
                var item = new ListViewItem(installer.FileName);
                item.SubItems.Add(FormatFileSize(installer.Size));
                item.SubItems.Add(installer.Status);
                item.Tag = installer;
                
                // Color coding for status
                if (installer.Status.Contains("Ready"))
                    item.BackColor = Color.FromArgb(45, 45, 45);
                else if (installer.Status.Contains("Installing"))
                    item.BackColor = Color.FromArgb(70, 50, 20);
                else if (installer.Status.Contains("✓"))
                    item.BackColor = Color.FromArgb(20, 70, 20);
                else if (installer.Status.Contains("✗"))
                    item.BackColor = Color.FromArgb(70, 20, 20);
                
                installerListView.Items.Add(item);
            }
            
            UpdateSelectedCount();
        }

        private void UpdateSelectedCount()
        {
            int total = localInstallers.Count;
            int selected = installerListView.CheckedItems.Count;
            
            countLabel.Text = $"{total} installers detected";
            
            if (selected > 0)
            {
                statusLabel.Text = $"{selected} selected for installation";
            }
            else if (total == 0)
            {
                statusLabel.Text = "Ready - Drag .exe/.msi files here or use Stealth Setup";
            }
            else
            {
                statusLabel.Text = "Ready - Select installers to proceed";
            }
        }

        private async void InstallButton_Click(object sender, EventArgs e)
        {
            var selectedFiles = installerListView.CheckedItems.Cast<ListViewItem>()
                .Where(item => item.Tag is InstallerFile)
                .Select(item => (InstallerFile)item.Tag)
                .ToList();
                
            if (selectedFiles.Count == 0)
            {
                MessageBox.Show("No installers selected.", "Selection Required", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            
            if (MessageBox.Show($"Install {selectedFiles.Count} selected programs?\\n\\nThis will run installations silently in the background.", 
                "Confirm Installation", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            
            await RunInstallation(selectedFiles);
        }

        private async void StealthButton_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "Stealth Setup will download and install privacy tools:\\n\\n" +
                "• Tor Browser (Anonymous browsing)\\n" +
                "• Brave Browser (Privacy-focused)\\n" +
                "• Mullvad VPN (If you have account)\\n" +
                "• VeraCrypt (Disk encryption)\\n" +
                "• Signal Desktop (Secure messaging)\\n" +
                "• KeePassXC (Password manager)\\n" +
                "• BleachBit (Privacy cleaner)\\n\\n" +
                "Continue with stealth setup?",
                "Stealth Privacy Setup",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
                
            if (result != DialogResult.Yes) return;
            
            await RunStealthSetup();
        }

        private void ClearButton_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in installerListView.Items)
                item.Checked = false;
            UpdateSelectedCount();
        }

        private async void VerifyButton_Click(object sender, EventArgs e)
        {
            verifyButton.Enabled = false;
            progressBar.Value = 0;
            statusLabel.Text = "Verifying installed programs...";

            await Task.Run(() =>
            {
                var verificationResults = new List<string>();
                var installedPrograms = GetInstalledPrograms();
                
                this.Invoke(new Action(() =>
                {
                    progressBar.Value = 30;
                    statusLabel.Text = "Checking installation status...";
                }));

                int verified = 0;
                foreach (var installer in localInstallers)
                {
                    string programName = Path.GetFileNameWithoutExtension(installer.FileName);
                    bool isInstalled = IsInstalled(programName, installedPrograms);
                    
                    this.Invoke(new Action(() =>
                    {
                        UpdateFileStatus(installer, isInstalled ? "✓ Verified" : "? Unknown");
                    }));
                    
                    if (isInstalled)
                    {
                        verified++;
                        verificationResults.Add($"✓ {programName} - Installed");
                    }
                    else
                    {
                        verificationResults.Add($"? {programName} - Not detected");
                    }
                }

                // Check for stealth programs
                var stealthInstalled = 0;
                foreach (var program in stealthPrograms)
                {
                    if (IsInstalled(program.Name, installedPrograms))
                    {
                        stealthInstalled++;
                        verificationResults.Add($"✓ {program.Name} - Stealth installed");
                    }
                }

                this.Invoke(new Action(() =>
                {
                    progressBar.Value = 100;
                    statusLabel.Text = $"Verification complete: {verified} verified, {stealthInstalled} stealth programs found";
                    
                    if (verboseCheck.Checked || showOutputCheck.Checked)
                    {
                        ShowVerificationResults(verificationResults);
                    }
                    
                    verifyButton.Enabled = true;
                }));
            });
        }

        private List<string> GetInstalledPrograms()
        {
            var programs = new List<string>();
            
            try
            {
                // Check Programs and Features (Control Panel)
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"))
                {
                    if (key != null)
                    {
                        foreach (string subkeyName in key.GetSubKeyNames())
                        {
                            using (var subkey = key.OpenSubKey(subkeyName))
                            {
                                var displayName = subkey?.GetValue("DisplayName")?.ToString();
                                if (!string.IsNullOrEmpty(displayName))
                                {
                                    programs.Add(displayName);
                                }
                            }
                        }
                    }
                }

                // Check 32-bit programs on 64-bit systems
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"))
                {
                    if (key != null)
                    {
                        foreach (string subkeyName in key.GetSubKeyNames())
                        {
                            using (var subkey = key.OpenSubKey(subkeyName))
                            {
                                var displayName = subkey?.GetValue("DisplayName")?.ToString();
                                if (!string.IsNullOrEmpty(displayName))
                                {
                                    programs.Add(displayName);
                                }
                            }
                        }
                    }
                }
                
                // Check common installation paths
                var commonPaths = new[]
                {
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs")
                };
                
                foreach (var path in commonPaths)
                {
                    if (Directory.Exists(path))
                    {
                        foreach (var dir in Directory.GetDirectories(path))
                        {
                            programs.Add(Path.GetFileName(dir));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error scanning installed programs: {ex.Message}");
            }
            
            return programs.Distinct().ToList();
        }

        private bool IsInstalled(string programName, List<string> installedPrograms)
        {
            var searchTerms = new[]
            {
                programName,
                programName.Replace(" ", ""),
                programName.Replace("Installer", "").Trim(),
                programName.Replace("Setup", "").Trim(),
                programName.Replace(".exe", "").Replace(".msi", "")
            };

            foreach (var term in searchTerms.Where(t => !string.IsNullOrEmpty(t)))
            {
                if (installedPrograms.Any(p => p.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    return true;
                }
            }

            return false;
        }

        private void ShowVerificationResults(List<string> results)
        {
            var resultText = string.Join("\n", results);
            var form = new Form
            {
                Text = "Verification Results",
                Size = new Size(500, 400),
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.FromArgb(32, 32, 32),
                ForeColor = Color.White
            };

            var textBox = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Dock = DockStyle.Fill,
                Text = resultText,
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White,
                Font = new Font("Consolas", 9F),
                BorderStyle = BorderStyle.None
            };

            form.Controls.Add(textBox);
            form.ShowDialog(this);
        }

        private async Task RunInstallation(List<InstallerFile> files)
        {
            isRunning = true;
            installButton.Enabled = false;
            stealthButton.Enabled = false;
            progressBar.Value = 0;
            currentProgress = 0;
            
            await Task.Run(() =>
            {
                int completed = 0;
                foreach (var file in files)
                {
                    if (!isRunning) break;
                    
                    try
                    {
                        this.Invoke(new Action(() =>
                        {
                            progressBar.Value = (completed * 100) / files.Count;
                            statusLabel.Text = $"Installing {file.FileName}... ({completed + 1}/{files.Count})";
                            UpdateFileStatus(file, "Installing...");
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
                    statusLabel.Text = $"Installation complete: {completed}/{files.Count} successful";
                    installButton.Enabled = true;
                    stealthButton.Enabled = true;
                    isRunning = false;
                }));
            });
        }

        private async Task RunStealthSetup()
        {
            isRunning = true;
            installButton.Enabled = false;
            stealthButton.Enabled = false;
            progressBar.Value = 0;
            
            var downloadDir = Path.Combine(Path.GetTempPath(), "NullInstaller_Stealth");
            Directory.CreateDirectory(downloadDir);
            
            await Task.Run(() =>
            {
                int completed = 0;
                var selectedPrograms = stealthPrograms.Take(7).ToList(); // Install core privacy tools
                
                foreach (var program in selectedPrograms)
                {
                    if (!isRunning) break;
                    
                    try
                    {
                        this.Invoke(new Action(() =>
                        {
                            progressBar.Value = (completed * 100) / selectedPrograms.Count;
                            statusLabel.Text = $"Downloading {program.Name}... ({completed + 1}/{selectedPrograms.Count})";
                        }));

                        // Download
                        var fileName = $"{program.Name.Replace(" ", "").Replace(".", "")}.exe";
                        var filePath = Path.Combine(downloadDir, fileName);
                        
                        using (var client = new WebClient())
                        {
                            client.DownloadFile(program.DownloadURL, filePath);
                        }
                        
                        this.Invoke(new Action(() =>
                        {
                            statusLabel.Text = $"Installing {program.Name}...";
                        }));
                        
                        // Install
                        var installer = new InstallerFile
                        {
                            FilePath = filePath,
                            FileName = fileName,
                            Size = new FileInfo(filePath).Length,
                            Status = "Installing..."
                        };
                        
                        bool success = InstallFile(installer);
                        completed++;
                        
                        LogMessage($"{(success ? "Stealth installed" : "Failed stealth install")}: {program.Name}");
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"Stealth setup error for {program.Name}: {ex.Message}");
                    }
                }

                this.Invoke(new Action(() =>
                {
                    progressBar.Value = 100;
                    statusLabel.Text = $"Stealth setup complete: {completed}/{selectedPrograms.Count} installed";
                    installButton.Enabled = true;
                    stealthButton.Enabled = true;
                    isRunning = false;
                    
                    // Refresh local file list
                    ScanLocalFiles();
                }));
            });
        }

        private bool InstallFile(InstallerFile file)
        {
            try
            {
                if (string.IsNullOrEmpty(file.FilePath) || !File.Exists(file.FilePath))
                {
                    LogMessage($"File not found: {file.FilePath}");
                    return false;
                }
                    
                string command, args;
                var fileName = file.FileName.ToLower();

                if (file.FilePath.EndsWith(".msi", StringComparison.OrdinalIgnoreCase))
                {
                    command = "msiexec";
                    args = $"/i \"{file.FilePath}\" /qn /norestart";
                    LogMessage($"Installing MSI: {file.FileName} with msiexec");
                }
                else
                {
                    command = file.FilePath;
                    
                    // Try different silent installation flags based on common installers
                    if (fileName.Contains("setup") || fileName.Contains("install"))
                    {
                        // Try common silent flags in order of preference
                        var silentFlags = new[] { "/S", "/SILENT", "/QUIET", "/s", "/silent", "/q", "-q", "-silent", "/passive" };
                        args = silentFlags[0]; // Start with most common
                    }
                    else
                    {
                        args = "/S"; // Default fallback
                    }
                    
                    LogMessage($"Installing EXE: {file.FileName} with args: {args}");
                }

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = command,
                        Arguments = args,
                        UseShellExecute = false,
                        CreateNoWindow = !showOutputCheck.Checked,
                        RedirectStandardOutput = verboseCheck.Checked,
                        RedirectStandardError = verboseCheck.Checked,
                        WindowStyle = showOutputCheck.Checked ? ProcessWindowStyle.Normal : ProcessWindowStyle.Hidden
                    }
                };

                if (verboseCheck.Checked)
                {
                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                            LogMessage($"[{file.FileName}] OUT: {e.Data}");
                    };
                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                            LogMessage($"[{file.FileName}] ERR: {e.Data}");
                    };
                }

                LogMessage($"Starting installation: {file.FileName}");
                process.Start();
                
                if (verboseCheck.Checked)
                {
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                }
                
                bool finished = process.WaitForExit(120000); // 2 minute timeout
                
                if (!finished)
                {
                    LogMessage($"Installation timeout for {file.FileName}, killing process");
                    process.Kill();
                    return false;
                }

                int exitCode = process.ExitCode;
                LogMessage($"Installation finished: {file.FileName}, Exit code: {exitCode}");
                
                // Many installers return different exit codes for success
                // 0 = success, 3010 = success but reboot required, etc.
                return exitCode == 0 || exitCode == 3010;
            }
            catch (Exception ex)
            {
                LogMessage($"Exception installing {file.FileName}: {ex.Message}");
                return false;
            }
        }

        private void UpdateFileStatus(InstallerFile file, string status)
        {
            file.Status = status;
            foreach (ListViewItem item in installerListView.Items)
            {
                if (item.Tag == file)
                {
                    item.SubItems[2].Text = status;
                    
                    // Update colors
                    if (status.Contains("Installing"))
                        item.BackColor = Color.FromArgb(70, 50, 20);
                    else if (status.Contains("✓"))
                        item.BackColor = Color.FromArgb(20, 70, 20);
                    else if (status.Contains("✗"))
                        item.BackColor = Color.FromArgb(70, 20, 20);
                    
                    break;
                }
            }
        }

        private void LogMessage(string message)
        {
            var logEntry = $"[{DateTime.Now:HH:mm:ss}] {message}";
            
            try
            {
                logWriter?.WriteLine(logEntry);
                logWriter?.Flush();
            }
            catch { }
            
            if (verboseCheck.Checked)
            {
                Console.WriteLine(logEntry);
            }
        }

        private void InitializeLogging()
        {
            try
            {
                logWriter = new StreamWriter("NullInstaller_Log.txt", true);
                logWriter.WriteLine($"\\n=== NullInstaller Started: {DateTime.Now} ===");
            }
            catch { }
        }

        private string FormatFileSize(long bytes)
        {
            if (bytes == 0) return "0 B";
            
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
                        
                        if (!localInstallers.Any(i => i.FileName.Equals(installer.FileName, StringComparison.OrdinalIgnoreCase)))
                        {
                            localInstallers.Add(installer);
                            added++;
                        }
                    }
                }
                
                if (added > 0)
                {
                    UpdateInstallerList();
                    statusLabel.Text = $"Added {added} file(s) via drag & drop";
                }
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            logWriter?.Close();
            base.OnFormClosed(e);
        }
    }

    public class InstallerFile
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public long Size { get; set; }
        public string Status { get; set; }
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
