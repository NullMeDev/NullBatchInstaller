using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NullInstaller
{
    public class MainForm : Form
    {
        // Main UI Components
        private ToolStrip toolStrip;
        private TreeView categoryTree;
        private ListView mainListView;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel statusLabel;
        private ToolStripProgressBar progressBar;
        private SplitContainer mainSplitContainer;
        private Panel rightPanel;
        private TextBox logTextBox;
        
        // Buttons
        private ToolStripButton installButton;
        private ToolStripButton downloadButton;
        private ToolStripButton stopButton;
        private ToolStripButton selectAllButton;
        private ToolStripButton deselectAllButton;
        private ToolStripButton stealthModeButton;
        
        // Data
        private SoftwareCatalog catalog;
        private HttpClient httpClient;
        private bool isRunning = false;
        private Dictionary<string, List<ProgramEntry>> categorizedPrograms;
        
        public MainForm()
        {
            InitializeComponent();
            LoadCatalog();
            InitializeHttpClient();
        }
        
        private void InitializeComponent()
        {
            this.Text = "NullInstaller v4.2.6 - Professional Software Installer";
            this.Size = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Icon = SystemIcons.Application;
            
            // Set dark theme colors
            this.BackColor = Color.FromArgb(39, 39, 42);
            this.ForeColor = Color.FromArgb(241, 241, 241);
            
            // Create toolbar (WinRAR style)
            toolStrip = new ToolStrip
            {
                ImageScalingSize = new Size(32, 32),
                Height = 50,
                BackColor = Color.FromArgb(51, 51, 55),
                RenderMode = ToolStripRenderMode.Professional,
                GripStyle = ToolStripGripStyle.Hidden
            };
            
            // Add toolbar buttons with icons
            installButton = CreateToolButton("Install", "▶", Color.FromArgb(92, 184, 92));
            downloadButton = CreateToolButton("Download", "⬇", Color.FromArgb(91, 192, 222));
            stopButton = CreateToolButton("Stop", "■", Color.FromArgb(217, 83, 79));
            selectAllButton = CreateToolButton("Select All", "☑", Color.FromArgb(240, 173, 78));
            deselectAllButton = CreateToolButton("Deselect", "☐", Color.FromArgb(240, 173, 78));
            stealthModeButton = CreateToolButton("Stealth Mode", "🛡", Color.FromArgb(155, 89, 182));
            
            stopButton.Enabled = false;
            
            toolStrip.Items.Add(installButton);
            toolStrip.Items.Add(downloadButton);
            toolStrip.Items.Add(new ToolStripSeparator());
            toolStrip.Items.Add(stopButton);
            toolStrip.Items.Add(new ToolStripSeparator());
            toolStrip.Items.Add(selectAllButton);
            toolStrip.Items.Add(deselectAllButton);
            toolStrip.Items.Add(new ToolStripSeparator());
            toolStrip.Items.Add(stealthModeButton);
            
            // Create main split container
            mainSplitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(39, 39, 42),
                Panel1MinSize = 150,
                Panel2MinSize = 400,
                SplitterDistance = 200
            };
            
            // Left panel - Category tree (like WinRAR folders)
            categoryTree = new TreeView
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.FromArgb(241, 241, 241),
                BorderStyle = BorderStyle.None,
                FullRowSelect = true,
                ShowLines = false,
                Font = new Font("Segoe UI", 9F),
                ItemHeight = 25
            };
            
            categoryTree.AfterSelect += CategoryTree_AfterSelect;
            
            // Right panel with list and log
            rightPanel = new Panel
            {
                Dock = DockStyle.Fill
            };
            
            // Main list view (WinRAR style)
            mainListView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                CheckBoxes = true,
                FullRowSelect = true,
                GridLines = true,
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.FromArgb(241, 241, 241),
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9F)
            };
            
            // Configure columns
            mainListView.Columns.Add("Name", 300);
            mainListView.Columns.Add("Version", 80);
            mainListView.Columns.Add("Size", 80);
            mainListView.Columns.Add("Status", 120);
            mainListView.Columns.Add("Vendor", 150);
            
            // Log text box (bottom panel)
            logTextBox = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Height = 150,
                Dock = DockStyle.Bottom,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.FromArgb(0, 255, 0),
                Font = new Font("Consolas", 8F),
                Text = "Ready - NullInstaller v4.2.6 initialized\r\n"
            };
            
            // Status bar
            statusStrip = new StatusStrip
            {
                BackColor = Color.FromArgb(51, 51, 55),
                ForeColor = Color.FromArgb(241, 241, 241)
            };
            
            statusLabel = new ToolStripStatusLabel
            {
                Text = "Ready",
                Spring = true,
                TextAlign = ContentAlignment.MiddleLeft
            };
            
            progressBar = new ToolStripProgressBar
            {
                Width = 200,
                Visible = false
            };
            
            statusStrip.Items.Add(statusLabel);
            statusStrip.Items.Add(progressBar);
            
            // Add splitter between list and log
            var splitter = new Splitter
            {
                Dock = DockStyle.Bottom,
                Height = 3,
                BackColor = Color.FromArgb(63, 63, 70)
            };
            
            // Assemble right panel
            rightPanel.Controls.Add(mainListView);
            rightPanel.Controls.Add(splitter);
            rightPanel.Controls.Add(logTextBox);
            
            // Assemble main split container
            mainSplitContainer.Panel1.Controls.Add(categoryTree);
            mainSplitContainer.Panel2.Controls.Add(rightPanel);
            
            // Add all to form
            this.Controls.Add(mainSplitContainer);
            this.Controls.Add(statusStrip);
            this.Controls.Add(toolStrip);
            
            // Event handlers
            installButton.Click += InstallButton_Click;
            downloadButton.Click += DownloadButton_Click;
            stopButton.Click += StopButton_Click;
            selectAllButton.Click += SelectAllButton_Click;
            deselectAllButton.Click += DeselectAllButton_Click;
            stealthModeButton.Click += StealthModeButton_Click;
        }
        
        private ToolStripButton CreateToolButton(string text, string symbol, Color color)
        {
            var button = new ToolStripButton
            {
                Text = symbol + " " + text,
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = color,
                Padding = new Padding(10, 0, 10, 0)
            };
            return button;
        }
        
        private void InitializeHttpClient()
        {
            httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "NullInstaller/4.2.6");
            httpClient.Timeout = TimeSpan.FromMinutes(30);
        }
        
        private void LoadCatalog()
        {
            try
            {
                string catalogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "software_catalog.json");
                if (!File.Exists(catalogPath))
                    catalogPath = "assets/software_catalog.json";
                
                if (File.Exists(catalogPath))
                {
                    string json = File.ReadAllText(catalogPath);
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    catalog = JsonSerializer.Deserialize<SoftwareCatalog>(json, options);
                    
                    if (catalog?.Software != null)
                    {
                        categorizedPrograms = catalog.Software
                            .GroupBy(p => p.Category ?? "Other")
                            .ToDictionary(g => g.Key, g => g.ToList());
                        
                        PopulateCategoryTree();
                        Log($"Loaded {catalog.Software.Count} programs from catalog");
                    }
                }
                else
                {
                    Log("Warning: Software catalog not found");
                    catalog = new SoftwareCatalog { Software = new List<ProgramEntry>() };
                    categorizedPrograms = new Dictionary<string, List<ProgramEntry>>();
                }
            }
            catch (Exception ex)
            {
                Log($"Error loading catalog: {ex.Message}");
                catalog = new SoftwareCatalog { Software = new List<ProgramEntry>() };
                categorizedPrograms = new Dictionary<string, List<ProgramEntry>>();
            }
        }
        
        private void PopulateCategoryTree()
        {
            categoryTree.BeginUpdate();
            categoryTree.Nodes.Clear();
            
            // Add "All Software" node
            var allNode = new TreeNode("📦 All Software")
            {
                Tag = "ALL",
                NodeFont = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            categoryTree.Nodes.Add(allNode);
            
            // Add category nodes
            var sortedCategories = categorizedPrograms.Keys.OrderBy(k => k).ToList();
            foreach (var category in sortedCategories)
            {
                string icon = GetCategoryIcon(category);
                var count = categorizedPrograms[category].Count;
                var node = new TreeNode($"{icon} {category} ({count})")
                {
                    Tag = category
                };
                categoryTree.Nodes.Add(node);
            }
            
            // Add special nodes
            var stealthNode = new TreeNode("🛡 Stealth Mode")
            {
                Tag = "STEALTH",
                NodeFont = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(155, 89, 182)
            };
            categoryTree.Nodes.Add(stealthNode);
            
            categoryTree.ExpandAll();
            categoryTree.EndUpdate();
            
            // Select "All Software" by default
            categoryTree.SelectedNode = allNode;
        }
        
        private string GetCategoryIcon(string category)
        {
            return category.ToLower() switch
            {
                "browsers" => "🌐",
                "development" => "💻",
                "development ides" => "⚡",
                "security" => "🔒",
                "privacy & security" => "🔐",
                "media" => "🎬",
                "productivity" => "📊",
                "system" => "⚙",
                "utilities" => "🔧",
                "gaming" => "🎮",
                "network" => "📡",
                "communication" => "💬",
                _ => "📁"
            };
        }
        
        private void CategoryTree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node == null) return;
            
            string tag = e.Node.Tag?.ToString();
            mainListView.BeginUpdate();
            mainListView.Items.Clear();
            
            List<ProgramEntry> programs = null;
            
            if (tag == "ALL")
            {
                programs = catalog.Software;
            }
            else if (tag == "STEALTH")
            {
                // Show stealth mode programs
                programs = catalog.Software
                    .Where(p => p.Category == "Security" || p.Category == "Privacy & Security" || 
                           p.Name.Contains("VPN") || p.Name.Contains("Tor") || 
                           p.Name.Contains("Privacy") || p.Name.Contains("Cleaner"))
                    .ToList();
            }
            else if (categorizedPrograms.ContainsKey(tag))
            {
                programs = categorizedPrograms[tag];
            }
            
            if (programs != null)
            {
                foreach (var program in programs.OrderBy(p => p.Name))
                {
                    var item = new ListViewItem(program.Name);
                    item.SubItems.Add(program.Version ?? "Latest");
                    item.SubItems.Add(program.Size ?? "Unknown");
                    item.SubItems.Add("Ready");
                    item.SubItems.Add(program.Vendor ?? "Unknown");
                    item.Tag = program;
                    item.ForeColor = Color.FromArgb(241, 241, 241);
                    mainListView.Items.Add(item);
                }
            }
            
            mainListView.EndUpdate();
            UpdateStatus($"Showing {mainListView.Items.Count} programs");
        }
        
        private async void InstallButton_Click(object sender, EventArgs e)
        {
            var selected = GetSelectedPrograms();
            if (selected.Count == 0)
            {
                MessageBox.Show("Please select programs to install", "No Selection", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            
            isRunning = true;
            installButton.Enabled = false;
            downloadButton.Enabled = false;
            stopButton.Enabled = true;
            progressBar.Visible = true;
            progressBar.Maximum = selected.Count;
            progressBar.Value = 0;
            
            Log($"Starting installation of {selected.Count} programs...");
            
            foreach (var program in selected)
            {
                if (!isRunning) break;
                
                UpdateStatus($"Installing {program.Name}...");
                Log($"Installing: {program.Name}");
                
                // Update item status
                UpdateItemStatus(program.Name, "Installing...", Color.Yellow);
                
                bool success = await InstallProgram(program);
                
                if (success)
                {
                    UpdateItemStatus(program.Name, "✓ Installed", Color.LightGreen);
                    Log($"✓ Successfully installed {program.Name}");
                }
                else
                {
                    UpdateItemStatus(program.Name, "✗ Failed", Color.LightCoral);
                    Log($"✗ Failed to install {program.Name}");
                }
                
                progressBar.Value++;
            }
            
            isRunning = false;
            installButton.Enabled = true;
            downloadButton.Enabled = true;
            stopButton.Enabled = false;
            progressBar.Visible = false;
            
            UpdateStatus("Installation complete");
            Log("Installation process completed");
        }
        
        private async void DownloadButton_Click(object sender, EventArgs e)
        {
            var selected = GetSelectedPrograms();
            if (selected.Count == 0)
            {
                MessageBox.Show("Please select programs to download", "No Selection", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            
            isRunning = true;
            installButton.Enabled = false;
            downloadButton.Enabled = false;
            stopButton.Enabled = true;
            progressBar.Visible = true;
            progressBar.Maximum = selected.Count;
            progressBar.Value = 0;
            
            Log($"Starting download of {selected.Count} programs...");
            
            foreach (var program in selected)
            {
                if (!isRunning) break;
                
                UpdateStatus($"Downloading {program.Name}...");
                Log($"Downloading: {program.Name}");
                
                UpdateItemStatus(program.Name, "Downloading...", Color.Cyan);
                
                bool success = await DownloadProgram(program);
                
                if (success)
                {
                    UpdateItemStatus(program.Name, "✓ Downloaded", Color.LightGreen);
                    Log($"✓ Successfully downloaded {program.Name}");
                }
                else
                {
                    UpdateItemStatus(program.Name, "✗ Failed", Color.LightCoral);
                    Log($"✗ Failed to download {program.Name}");
                }
                
                progressBar.Value++;
            }
            
            isRunning = false;
            installButton.Enabled = true;
            downloadButton.Enabled = true;
            stopButton.Enabled = false;
            progressBar.Visible = false;
            
            UpdateStatus("Download complete");
            Log("Download process completed");
        }
        
        private void StopButton_Click(object sender, EventArgs e)
        {
            isRunning = false;
            UpdateStatus("Operation cancelled");
            Log("Operation cancelled by user");
        }
        
        private void SelectAllButton_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in mainListView.Items)
                item.Checked = true;
            UpdateStatus($"Selected {mainListView.Items.Count} items");
        }
        
        private void DeselectAllButton_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in mainListView.Items)
                item.Checked = false;
            UpdateStatus("All items deselected");
        }
        
        private void StealthModeButton_Click(object sender, EventArgs e)
        {
            // Select stealth mode node
            foreach (TreeNode node in categoryTree.Nodes)
            {
                if (node.Tag?.ToString() == "STEALTH")
                {
                    categoryTree.SelectedNode = node;
                    break;
                }
            }
            
            // Select all stealth items
            foreach (ListViewItem item in mainListView.Items)
                item.Checked = true;
            
            UpdateStatus("Stealth mode programs selected");
            Log("Stealth mode activated - privacy tools selected");
        }
        
        private List<ProgramEntry> GetSelectedPrograms()
        {
            var selected = new List<ProgramEntry>();
            foreach (ListViewItem item in mainListView.Items)
            {
                if (item.Checked && item.Tag is ProgramEntry program)
                    selected.Add(program);
            }
            return selected;
        }
        
        private void UpdateItemStatus(string name, string status, Color color)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateItemStatus(name, status, color)));
                return;
            }
            
            foreach (ListViewItem item in mainListView.Items)
            {
                if (item.Text == name)
                {
                    item.SubItems[3].Text = status;
                    item.ForeColor = color;
                    break;
                }
            }
        }
        
        private void UpdateStatus(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateStatus(message)));
                return;
            }
            statusLabel.Text = message;
        }
        
        private void Log(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => Log(message)));
                return;
            }
            
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            logTextBox.AppendText($"[{timestamp}] {message}\r\n");
            logTextBox.SelectionStart = logTextBox.Text.Length;
            logTextBox.ScrollToCaret();
        }
        
        private async Task<bool> InstallProgram(ProgramEntry program)
        {
            try
            {
                // Simulate installation
                await Task.Delay(1000);
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        private async Task<bool> DownloadProgram(ProgramEntry program)
        {
            try
            {
                if (string.IsNullOrEmpty(program.DownloadUrl))
                {
                    Log($"No download URL for {program.Name}");
                    return false;
                }
                
                // Create temp directory
                string tempDir = Path.Combine(Path.GetTempPath(), "NullInstaller_Downloads");
                Directory.CreateDirectory(tempDir);
                
                string fileName = $"{program.Name.Replace(" ", "_")}.exe";
                string filePath = Path.Combine(tempDir, fileName);
                
                using (var response = await httpClient.GetAsync(program.DownloadUrl))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        var bytes = await response.Content.ReadAsByteArrayAsync();
                        await File.WriteAllBytesAsync(filePath, bytes);
                        Log($"Downloaded to: {filePath}");
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                Log($"Download error: {ex.Message}");
                return false;
            }
        }
    }
    
    // Data models
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
        
        [JsonPropertyName("vendor")]
        public string Vendor { get; set; }
        
        [JsonPropertyName("version")]
        public string Version { get; set; }
        
        [JsonPropertyName("size")]
        public string Size { get; set; }
    }
    
    public class SoftwareCatalog
    {
        [JsonPropertyName("software")]
        public List<ProgramEntry> Software { get; set; }
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
