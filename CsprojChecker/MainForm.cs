using System.Xml.Linq;

namespace CsprojChecker;

public partial class MainForm : Form
{
    private TextBox folderPathTextBox = null!;
    private Button browseButton = null!;
    private Button checkCsprojButton = null!;
    private DataGridView projectsGridView = null!;
    private Label statusLabel = null!;
    private Button cancelButton = null!;
    private GroupBox frameworkOperationsGroupBox = null!;
    private GroupBox projectStyleGroupBox = null!;
    
    // Placeholder controls for Framework operations region
    private Label frameworkPlaceholderLabel = null!;
    
    // Placeholder controls for Project style conversions region
    private Label projectStylePlaceholderLabel = null!;
    
    // For cancellation support
    private CancellationTokenSource? _cancellationTokenSource;

    public MainForm()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.SuspendLayout();
        
        // Form settings
        this.Text = "Csproj Checker";
        this.Size = new Size(1000, 700);
        this.StartPosition = FormStartPosition.CenterScreen;
        
        // Folder path TextBox
        folderPathTextBox = new TextBox
        {
            Location = new Point(20, 20),
            Size = new Size(600, 23),
            Name = "folderPathTextBox"
        };
        
        // Browse Button (height increased by 25%)
        browseButton = new Button
        {
            Location = new Point(630, 19),
            Size = new Size(100, 31),
            Text = "Browse",
            Name = "browseButton"
        };
        browseButton.Click += BrowseButton_Click;
        
        // Check for csproj files Button (height increased by 25%)
        checkCsprojButton = new Button
        {
            Location = new Point(740, 19),
            Size = new Size(200, 31),
            Text = "Check for csproj files",
            Name = "checkCsprojButton"
        };
        checkCsprojButton.Click += CheckCsprojButton_Click;
        
        // DataGridView
        projectsGridView = new DataGridView
        {
            Location = new Point(20, 60),
            Size = new Size(940, 250),
            Name = "projectsGridView",
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect
        };
        
        // Add columns to DataGridView
        projectsGridView.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "FullPath",
            HeaderText = "Full Path",
            FillWeight = 40
        });
        projectsGridView.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Style",
            HeaderText = "Style",
            FillWeight = 15
        });
        projectsGridView.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "TargetFrameworks",
            HeaderText = "Target Framework(s)",
            FillWeight = 25
        });
        projectsGridView.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Changed",
            HeaderText = "Changed",
            FillWeight = 20
        });
        
        // Framework operations GroupBox
        frameworkOperationsGroupBox = new GroupBox
        {
            Location = new Point(20, 320),
            Size = new Size(460, 150),
            Text = "Framework Operations",
            Name = "frameworkOperationsGroupBox"
        };
        
        // Placeholder label for Framework operations
        frameworkPlaceholderLabel = new Label
        {
            Location = new Point(10, 25),
            Size = new Size(440, 20),
            Text = "Framework operations controls (placeholder)",
            Name = "frameworkPlaceholderLabel"
        };
        frameworkOperationsGroupBox.Controls.Add(frameworkPlaceholderLabel);
        
        // Project style conversions GroupBox
        projectStyleGroupBox = new GroupBox
        {
            Location = new Point(500, 320),
            Size = new Size(460, 150),
            Text = "Project Style Conversions",
            Name = "projectStyleGroupBox"
        };
        
        // Placeholder label for Project style conversions
        projectStylePlaceholderLabel = new Label
        {
            Location = new Point(10, 25),
            Size = new Size(440, 20),
            Text = "Project style conversion controls (placeholder)",
            Name = "projectStylePlaceholderLabel"
        };
        projectStyleGroupBox.Controls.Add(projectStylePlaceholderLabel);
        
        // Status Label
        statusLabel = new Label
        {
            Location = new Point(20, 480),
            Size = new Size(840, 23),
            Text = "Ready",
            Name = "statusLabel"
        };
        
        // Cancel Button (height increased by 25%)
        cancelButton = new Button
        {
            Location = new Point(860, 478),
            Size = new Size(100, 31),
            Text = "Cancel",
            Name = "cancelButton",
            Enabled = false
        };
        cancelButton.Click += CancelButton_Click;
        
        // Add controls to form
        this.Controls.Add(folderPathTextBox);
        this.Controls.Add(browseButton);
        this.Controls.Add(checkCsprojButton);
        this.Controls.Add(projectsGridView);
        this.Controls.Add(frameworkOperationsGroupBox);
        this.Controls.Add(projectStyleGroupBox);
        this.Controls.Add(statusLabel);
        this.Controls.Add(cancelButton);
        
        this.ResumeLayout(false);
    }
    
    // Event handlers - placeholders with no business logic yet
    
    private void BrowseButton_Click(object? sender, EventArgs e)
    {
        using var folderBrowserDialog = new FolderBrowserDialog
        {
            Description = "Select folder to scan for .csproj files",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false
        };
        
        if (!string.IsNullOrWhiteSpace(folderPathTextBox.Text) && Directory.Exists(folderPathTextBox.Text))
        {
            folderBrowserDialog.SelectedPath = folderPathTextBox.Text;
        }
        
        if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
        {
            folderPathTextBox.Text = folderBrowserDialog.SelectedPath;
        }
    }
    
    private async void CheckCsprojButton_Click(object? sender, EventArgs e)
    {
        var folderPath = folderPathTextBox.Text;
        
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            MessageBox.Show("Please select a folder to scan.", "No Folder Selected", 
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        
        if (!Directory.Exists(folderPath))
        {
            MessageBox.Show("The selected folder does not exist.", "Invalid Folder", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        
        // Clear existing data
        projectsGridView.Rows.Clear();
        
        // Setup cancellation token
        _cancellationTokenSource = new CancellationTokenSource();
        
        // Update UI state
        checkCsprojButton.Enabled = false;
        browseButton.Enabled = false;
        cancelButton.Enabled = true;
        statusLabel.Text = "Scanning...";
        
        try
        {
            await ScanForCsprojFilesAsync(folderPath, _cancellationTokenSource.Token);
            
            if (_cancellationTokenSource.Token.IsCancellationRequested)
            {
                statusLabel.Text = "Scan cancelled";
            }
            else
            {
                statusLabel.Text = $"Scan complete. Found {projectsGridView.Rows.Count} project(s)";
            }
        }
        catch (OperationCanceledException)
        {
            statusLabel.Text = "Scan cancelled";
        }
        catch (Exception ex)
        {
            statusLabel.Text = "Error during scan";
            MessageBox.Show($"An error occurred during scanning: {ex.Message}", "Scan Error", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            // Restore UI state
            checkCsprojButton.Enabled = true;
            browseButton.Enabled = true;
            cancelButton.Enabled = false;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }
    
    private void CancelButton_Click(object? sender, EventArgs e)
    {
        _cancellationTokenSource?.Cancel();
        cancelButton.Enabled = false;
        statusLabel.Text = "Cancelling...";
    }
    
    private async Task ScanForCsprojFilesAsync(string folderPath, CancellationToken cancellationToken)
    {
        await ScanDirectoryAsync(folderPath, cancellationToken);
    }
    
    private async Task ScanDirectoryAsync(string directoryPath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        // Search for .csproj files in current directory
        try
        {
            var csprojFiles = Directory.GetFiles(directoryPath, "*.csproj", SearchOption.TopDirectoryOnly);
            
            foreach (var csprojFile in csprojFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var projectInfo = await ParseCsprojFileAsync(csprojFile, cancellationToken);
                
                // Update UI on UI thread
                this.Invoke(() =>
                {
                    projectsGridView.Rows.Add(
                        projectInfo.FullPath,
                        projectInfo.Style,
                        projectInfo.TargetFrameworks,
                        projectInfo.Changed
                    );
                    
                    statusLabel.Text = $"Scanning... Found {projectsGridView.Rows.Count} project(s)";
                });
                
                // Small delay to allow UI to update smoothly
                await Task.Delay(10, cancellationToken);
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Skip directories we don't have access to
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        
        // Recursively scan subdirectories
        try
        {
            var subdirectories = Directory.GetDirectories(directoryPath);
            
            foreach (var subdirectory in subdirectories)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ScanDirectoryAsync(subdirectory, cancellationToken);
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Skip directories we don't have access to
        }
        catch (OperationCanceledException)
        {
            throw;
        }
    }
    
    private async Task<ProjectInfo> ParseCsprojFileAsync(string filePath, CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            try
            {
                var doc = XDocument.Load(filePath);
                var root = doc.Root;
                
                if (root == null)
                {
                    return new ProjectInfo
                    {
                        FullPath = filePath,
                        Style = "Unknown",
                        TargetFrameworks = "Unknown",
                        Changed = ""
                    };
                }
                
                // Determine if SDK-style or Old-style
                bool isSdkStyle = root.Attribute("Sdk") != null;
                string style = isSdkStyle ? "SDK" : "Old-style";
                
                // Parse target frameworks
                string targetFrameworks = ParseTargetFrameworks(root);
                
                return new ProjectInfo
                {
                    FullPath = filePath,
                    Style = style,
                    TargetFrameworks = targetFrameworks,
                    Changed = ""
                };
            }
            catch
            {
                return new ProjectInfo
                {
                    FullPath = filePath,
                    Style = "Error",
                    TargetFrameworks = "Error",
                    Changed = ""
                };
            }
        }, cancellationToken);
    }
    
    private string ParseTargetFrameworks(XElement root)
    {
        XNamespace ns = root.GetDefaultNamespace();
        
        // Look for TargetFrameworks (plural) first
        var targetFrameworksElement = root.Descendants(ns + "TargetFrameworks").FirstOrDefault();
        if (targetFrameworksElement != null)
        {
            var value = targetFrameworksElement.Value.Trim();
            if (value.StartsWith("$"))
            {
                // Variable token
                return value;
            }
            return value;
        }
        
        // Look for TargetFramework (singular)
        var targetFrameworkElement = root.Descendants(ns + "TargetFramework").FirstOrDefault();
        if (targetFrameworkElement != null)
        {
            var value = targetFrameworkElement.Value.Trim();
            if (value.StartsWith("$"))
            {
                // Variable token (e.g., $(TargetFramework))
                return value;
            }
            return value;
        }
        
        // Look for TargetFrameworkVersion (old-style projects)
        var targetFrameworkVersionElement = root.Descendants(ns + "TargetFrameworkVersion").FirstOrDefault();
        if (targetFrameworkVersionElement != null)
        {
            return targetFrameworkVersionElement.Value.Trim();
        }
        
        return "Not specified";
    }
    
    private class ProjectInfo
    {
        public string FullPath { get; set; } = "";
        public string Style { get; set; } = "";
        public string TargetFrameworks { get; set; } = "";
        public string Changed { get; set; } = "";
    }
}
