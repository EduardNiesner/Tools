using System.Security.Cryptography;
using System.Xml.Linq;
using System.Xml;
using System.Text.Json;

namespace CsprojChecker;

public partial class MainForm : Form
{
    private GroupBox pathSelectionGroupBox = null!;
    private Label pathLabel = null!;
    private ComboBox folderPathComboBox = null!;
    private Button browseButton = null!;
    private Button checkCsprojButton = null!;
    private Button exportCsvButton = null!;
    private GroupBox filterResultsGroupBox = null!;
    private Label filterLabel = null!;
    private TextBox fileFilterTextBox = null!;
    private DataGridView projectsGridView = null!;
    private Label statusLabel = null!;
    private Button cancelButton = null!;
    private GroupBox frameworkOperationsGroupBox = null!;
    private GroupBox projectStyleGroupBox = null!;

    // Framework operations controls
    private Label targetFrameworkLabel = null!;
    private ComboBox targetFrameworkComboBox = null!;
    private Button changeTargetFrameworkButton = null!;
    private Label appendTargetFrameworkLabel = null!;
    private ComboBox appendTargetFrameworkComboBox = null!;
    private Button appendTargetFrameworkButton = null!;

    // Project style conversion controls
    private Button convertToSdkButton = null!;
    private Button convertToOldStyleButton = null!;

    // For cancellation support
    private CancellationTokenSource? _cancellationTokenSource;

    // Track discovered variables
    private HashSet<string> _discoveredVariables = new HashSet<string>();

    // Store all scanned projects for filtering
    private List<ProjectInfo> _allProjects = new List<ProjectInfo>();

    // Settings file path
    private static readonly string SettingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "CsprojChecker");
    private static readonly string SettingsFilePath = Path.Combine(SettingsDirectory, "settings.json");

    // Common TFMs
    private static readonly string[] CommonTfms = new[]
    {
        "net9.0", "net9.0-windows",
        "net8.0", "net8.0-windows",
        "net7.0", "net7.0-windows",
        "net6.0", "net6.0-windows",
        "net5.0", "net5.0-windows",
        "netcoreapp3.1",
        "netstandard2.1", "netstandard2.0",
        "net48", "net48-windows",
        "net472", "net472-windows",
        "net471", "net471-windows",
        "net47", "net47-windows",
        "net462", "net462-windows",
        "net461", "net461-windows",
        "net46", "net46-windows",
        "net452", "net452-windows",
        "net451", "net451-windows",
        "net45", "net45-windows"
    };

    public MainForm()
    {
        InitializeComponent();
        LoadSettings();
    }

    private void InitializeComponent()
    {
        this.SuspendLayout();

        // Form settings
        this.Text = "Csproj Checker";
        this.Size = new Size(1000, 750);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.MinimumSize = new Size(900, 650);

        // ===== TOP REGION: Path Selection =====
        pathSelectionGroupBox = new GroupBox
        {
            Location = new Point(10, 10),
            Size = new Size(970, 80),
            Text = "Path Selection",
            Name = "pathSelectionGroupBox",
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            TabIndex = 0
        };

        // Path label
        pathLabel = new Label
        {
            Location = new Point(10, 22),
            Size = new Size(100, 20),
            Text = "Project/Solution:",
            Name = "pathLabel",
            TextAlign = ContentAlignment.MiddleLeft,
            TabIndex = 0
        };
        pathLabel.AccessibleName = "Path Selection Label";

        // Folder path ComboBox
        folderPathComboBox = new ComboBox
        {
            Location = new Point(10, 45),
            Size = new Size(545, 23),
            Name = "folderPathComboBox",
            DropDownStyle = ComboBoxStyle.DropDown,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            TabIndex = 1
        };
        folderPathComboBox.AccessibleName = "Project or Solution Path";
        folderPathComboBox.AccessibleDescription = "Select or enter the path to scan for project files";

        // Browse Button
        browseButton = new Button
        {
            Location = new Point(565, 43),
            Size = new Size(100, 27),
            Text = "Browse",
            Name = "browseButton",
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            TabIndex = 2
        };
        browseButton.Click += BrowseButton_Click;
        browseButton.AccessibleName = "Browse for Folder";
        browseButton.AccessibleDescription = "Open folder browser dialog";

        // Check for csproj files Button
        checkCsprojButton = new Button
        {
            Location = new Point(675, 43),
            Size = new Size(140, 27),
            Text = "Check for .csproj",
            Name = "checkCsprojButton",
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            TabIndex = 3
        };
        checkCsprojButton.Click += CheckCsprojButton_Click;
        checkCsprojButton.AccessibleName = "Check for Project Files";
        checkCsprojButton.AccessibleDescription = "Scan the selected path for .csproj files";

        // Export to CSV Button
        exportCsvButton = new Button
        {
            Location = new Point(825, 43),
            Size = new Size(125, 27),
            Text = "Export CSV",
            Name = "exportCsvButton",
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            TabIndex = 4
        };
        exportCsvButton.Click += ExportCsvButton_Click;
        exportCsvButton.AccessibleName = "Export to CSV";
        exportCsvButton.AccessibleDescription = "Export the results to a CSV file";

        pathSelectionGroupBox.Controls.Add(pathLabel);
        pathSelectionGroupBox.Controls.Add(folderPathComboBox);
        pathSelectionGroupBox.Controls.Add(browseButton);
        pathSelectionGroupBox.Controls.Add(checkCsprojButton);
        pathSelectionGroupBox.Controls.Add(exportCsvButton);

        // ===== MIDDLE REGION: Filter and Results =====
        filterResultsGroupBox = new GroupBox
        {
            Location = new Point(10, 100),
            Size = new Size(970, 320),
            Text = "Filter and Results",
            Name = "filterResultsGroupBox",
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
            TabIndex = 5
        };

        // Filter label
        filterLabel = new Label
        {
            Location = new Point(10, 22),
            Size = new Size(100, 20),
            Text = "Filter by path:",
            Name = "filterLabel",
            TextAlign = ContentAlignment.MiddleLeft,
            TabIndex = 0
        };
        filterLabel.AccessibleName = "Filter Label";

        // File filter TextBox
        fileFilterTextBox = new TextBox
        {
            Location = new Point(10, 45),
            Size = new Size(940, 23),
            Name = "fileFilterTextBox",
            PlaceholderText = "Filter files by path...",
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            TabIndex = 1
        };
        fileFilterTextBox.TextChanged += FileFilterTextBox_TextChanged;
        fileFilterTextBox.AccessibleName = "Filter Textbox";
        fileFilterTextBox.AccessibleDescription = "Enter text to filter the project list by file path";

        // DataGridView
        projectsGridView = new DataGridView
        {
            Location = new Point(10, 75),
            Size = new Size(940, 230),
            Name = "projectsGridView",
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
            TabIndex = 2
        };
        projectsGridView.SelectionChanged += ProjectsGridView_SelectionChanged;
        projectsGridView.AccessibleName = "Project Results Grid";
        projectsGridView.AccessibleDescription = "List of discovered project files with their properties";

        // Add context menu for grid
        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("Open containing folder", null, OpenContainingFolder_Click);
        contextMenu.Items.Add("Copy path", null, CopyPath_Click);
        projectsGridView.ContextMenuStrip = contextMenu;

        // Add double-click handler
        projectsGridView.CellDoubleClick += ProjectsGridView_CellDoubleClick;

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

        filterResultsGroupBox.Controls.Add(filterLabel);
        filterResultsGroupBox.Controls.Add(fileFilterTextBox);
        filterResultsGroupBox.Controls.Add(projectsGridView);

        // ===== BOTTOM REGION: Framework Operations (Left - Larger) =====
        frameworkOperationsGroupBox = new GroupBox
        {
            Location = new Point(10, 430),
            Size = new Size(590, 230),
            Text = "Framework Operations",
            Name = "frameworkOperationsGroupBox",
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            TabIndex = 6
        };

        // Target framework label
        targetFrameworkLabel = new Label
        {
            Location = new Point(10, 25),
            Size = new Size(150, 20),
            Text = "Target framework:",
            Name = "targetFrameworkLabel",
            TextAlign = ContentAlignment.MiddleLeft,
            TabIndex = 0
        };
        targetFrameworkLabel.AccessibleName = "Target Framework Label";

        // Target framework ComboBox
        targetFrameworkComboBox = new ComboBox
        {
            Location = new Point(10, 48),
            Size = new Size(380, 23),
            Name = "targetFrameworkComboBox",
            DropDownStyle = ComboBoxStyle.DropDown,
            Enabled = false,
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
            TabIndex = 1
        };
        targetFrameworkComboBox.AccessibleName = "Target Framework Selection";
        targetFrameworkComboBox.AccessibleDescription = "Select or enter the target framework to apply";

        // Change target framework button
        changeTargetFrameworkButton = new Button
        {
            Location = new Point(400, 45),
            Size = new Size(180, 27),
            Text = "Change target framework",
            Name = "changeTargetFrameworkButton",
            Enabled = false,
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
            TabIndex = 2
        };
        changeTargetFrameworkButton.Click += ChangeTargetFrameworkButton_Click;
        changeTargetFrameworkButton.AccessibleName = "Change Target Framework";
        changeTargetFrameworkButton.AccessibleDescription = "Replace the target framework for selected projects";

        // Append target framework label
        appendTargetFrameworkLabel = new Label
        {
            Location = new Point(10, 85),
            Size = new Size(150, 20),
            Text = "Append framework:",
            Name = "appendTargetFrameworkLabel",
            TextAlign = ContentAlignment.MiddleLeft,
            TabIndex = 3
        };
        appendTargetFrameworkLabel.AccessibleName = "Append Framework Label";

        // Append target framework ComboBox
        appendTargetFrameworkComboBox = new ComboBox
        {
            Location = new Point(10, 108),
            Size = new Size(380, 23),
            Name = "appendTargetFrameworkComboBox",
            DropDownStyle = ComboBoxStyle.DropDown,
            Enabled = false,
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
            TabIndex = 4
        };
        appendTargetFrameworkComboBox.AccessibleName = "Append Framework Selection";
        appendTargetFrameworkComboBox.AccessibleDescription = "Select or enter the framework to append to existing targets";

        // Append target framework button
        appendTargetFrameworkButton = new Button
        {
            Location = new Point(400, 105),
            Size = new Size(180, 27),
            Text = "Append target framework",
            Name = "appendTargetFrameworkButton",
            Enabled = false,
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
            TabIndex = 5
        };
        appendTargetFrameworkButton.Click += AppendTargetFrameworkButton_Click;
        appendTargetFrameworkButton.AccessibleName = "Append Target Framework";
        appendTargetFrameworkButton.AccessibleDescription = "Add the framework to the existing target frameworks for selected projects";

        // Initialize ComboBox suggestions
        InitializeComboBoxSuggestions();

        frameworkOperationsGroupBox.Controls.Add(targetFrameworkLabel);
        frameworkOperationsGroupBox.Controls.Add(targetFrameworkComboBox);
        frameworkOperationsGroupBox.Controls.Add(changeTargetFrameworkButton);
        frameworkOperationsGroupBox.Controls.Add(appendTargetFrameworkLabel);
        frameworkOperationsGroupBox.Controls.Add(appendTargetFrameworkComboBox);
        frameworkOperationsGroupBox.Controls.Add(appendTargetFrameworkButton);

        // ===== BOTTOM REGION: Project Style Conversions (Right - Smaller) =====
        projectStyleGroupBox = new GroupBox
        {
            Location = new Point(610, 430),
            Size = new Size(370, 230),
            Text = "Project Style Conversions",
            Name = "projectStyleGroupBox",
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            TabIndex = 7
        };

        // Convert to SDK-style button
        convertToSdkButton = new Button
        {
            Location = new Point(10, 30),
            Size = new Size(340, 27),
            Text = "Convert Old-style → SDK",
            Name = "convertToSdkButton",
            Enabled = false,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            TabIndex = 0
        };
        convertToSdkButton.Click += ConvertToSdkButton_Click;
        convertToSdkButton.AccessibleName = "Convert to SDK Style";
        convertToSdkButton.AccessibleDescription = "Convert selected old-style projects to SDK-style format";
        projectStyleGroupBox.Controls.Add(convertToSdkButton);

        // Convert to Old-style button
        convertToOldStyleButton = new Button
        {
            Location = new Point(10, 70),
            Size = new Size(340, 27),
            Text = "Convert SDK → Old-style",
            Name = "convertToOldStyleButton",
            Enabled = false,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            TabIndex = 1
        };
        convertToOldStyleButton.Click += ConvertToOldStyleButton_Click;
        convertToOldStyleButton.AccessibleName = "Convert to Old Style";
        convertToOldStyleButton.AccessibleDescription = "Convert selected SDK-style projects to old-style format";
        projectStyleGroupBox.Controls.Add(convertToOldStyleButton);

        // ===== BOTTOM STATUS BAR =====
        // Status Label
        statusLabel = new Label
        {
            Location = new Point(10, 670),
            Size = new Size(850, 23),
            Text = "Ready",
            Name = "statusLabel",
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            TabIndex = 8
        };
        statusLabel.AccessibleName = "Status";
        statusLabel.AccessibleDescription = "Current operation status";

        // Cancel Button
        cancelButton = new Button
        {
            Location = new Point(870, 668),
            Size = new Size(100, 27),
            Text = "Cancel",
            Name = "cancelButton",
            Enabled = false,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            TabIndex = 9
        };
        cancelButton.Click += CancelButton_Click;
        cancelButton.AccessibleName = "Cancel Operation";
        cancelButton.AccessibleDescription = "Cancel the current scanning operation";

        // Add controls to form
        this.Controls.Add(pathSelectionGroupBox);
        this.Controls.Add(filterResultsGroupBox);
        this.Controls.Add(frameworkOperationsGroupBox);
        this.Controls.Add(projectStyleGroupBox);
        this.Controls.Add(statusLabel);
        this.Controls.Add(cancelButton);

        this.ResumeLayout(false);
    }

    // Event handlers - placeholders with no business logic yet

    private void FileFilterTextBox_TextChanged(object? sender, EventArgs e)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var filterText = fileFilterTextBox.Text.Trim();

        // Clear the grid
        projectsGridView.Rows.Clear();

        if (string.IsNullOrWhiteSpace(filterText))
        {
            // No filter, show all projects
            foreach (var project in _allProjects)
            {
                var rowIndex = projectsGridView.Rows.Add(
                    project.FullPath,
                    project.Style,
                    project.TargetFrameworks,
                    project.Changed
                );
                
                // Restore background color if changed
                if (project.Changed == "✓")
                {
                    projectsGridView.Rows[rowIndex].DefaultCellStyle.BackColor = Color.LightGreen;
                }
            }
        }
        else
        {
            // Apply filter - match any part of the full path (case-insensitive)
            var filteredProjects = _allProjects.Where(p =>
                p.FullPath.Contains(filterText, StringComparison.OrdinalIgnoreCase)
            ).ToList();

            foreach (var project in filteredProjects)
            {
                var rowIndex = projectsGridView.Rows.Add(
                    project.FullPath,
                    project.Style,
                    project.TargetFrameworks,
                    project.Changed
                );
                
                // Restore background color if changed
                if (project.Changed == "✓")
                {
                    projectsGridView.Rows[rowIndex].DefaultCellStyle.BackColor = Color.LightGreen;
                }
            }

            // Update status to show filtered count
            if (_allProjects.Count > 0)
            {
                statusLabel.Text = $"Showing {filteredProjects.Count} of {_allProjects.Count} project(s)";
            }
        }
    }

    private void BrowseButton_Click(object? sender, EventArgs e)
    {
        using var folderBrowserDialog = new FolderBrowserDialog
        {
            Description = "Select folder to scan for .csproj files",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false
        };

        if (!string.IsNullOrWhiteSpace(folderPathComboBox.Text) && Directory.Exists(folderPathComboBox.Text))
        {
            folderBrowserDialog.SelectedPath = folderPathComboBox.Text;
        }

        if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
        {
            folderPathComboBox.Text = folderBrowserDialog.SelectedPath;
        }
    }

    private async void CheckCsprojButton_Click(object? sender, EventArgs e)
    {
        var folderPath = folderPathComboBox.Text;

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

        // Save the folder path to settings
        SaveSettings(folderPath);

        // Clear existing data
        projectsGridView.Rows.Clear();
        _discoveredVariables.Clear();
        _allProjects.Clear();

        // Setup cancellation token
        _cancellationTokenSource = new CancellationTokenSource();

        // Update UI state
        checkCsprojButton.Enabled = false;
        browseButton.Enabled = false;
        exportCsvButton.Enabled = false;
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
                statusLabel.Text = $"Scan complete. Found {_allProjects.Count} project(s)";

                // Update ComboBox suggestions with discovered variables
                UpdateComboBoxSuggestions();

                // Apply current filter
                ApplyFilter();
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
            exportCsvButton.Enabled = true;
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

                // Store in the list for filtering
                _allProjects.Add(projectInfo);

                // Update UI on UI thread
                this.Invoke(() =>
                {
                    projectsGridView.Rows.Add(
                        projectInfo.FullPath,
                        projectInfo.Style,
                        projectInfo.TargetFrameworks,
                        projectInfo.Changed
                    );

                    statusLabel.Text = $"Scanning... Found {_allProjects.Count} project(s)";
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
            TrackVariablesInTfm(value);
            return value;
        }

        // Look for TargetFramework (singular)
        var targetFrameworkElement = root.Descendants(ns + "TargetFramework").FirstOrDefault();
        if (targetFrameworkElement != null)
        {
            var value = targetFrameworkElement.Value.Trim();
            TrackVariablesInTfm(value);
            return value;
        }

        // Look for TargetFrameworkVersion (old-style projects)
        var targetFrameworkVersionElement = root.Descendants(ns + "TargetFrameworkVersion").FirstOrDefault();
        if (targetFrameworkVersionElement != null)
        {
            return targetFrameworkVersionElement.Value.Trim();
        }

        // If neither exists, return empty string
        return "";
    }

    private void TrackVariablesInTfm(string tfmValue)
    {
        // Track any variables (tokens starting with $) for ComboBox suggestions
        if (string.IsNullOrWhiteSpace(tfmValue))
            return;

        var parts = tfmValue.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                           .Select(p => p.Trim())
                           .Where(p => !string.IsNullOrEmpty(p) && p.StartsWith("$"));

        foreach (var variable in parts)
        {
            _discoveredVariables.Add(variable);
        }
    }

    private void ProjectsGridView_SelectionChanged(object? sender, EventArgs e)
    {
        UpdateFrameworkOperationsState();
    }

    private async void ChangeTargetFrameworkButton_Click(object? sender, EventArgs e)
    {
        var newValue = targetFrameworkComboBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(newValue))
        {
            MessageBox.Show("Please enter a target framework.", "No Framework Specified",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // Get selected projects
        var selectedProjects = new List<(int rowIndex, string filePath, string currentTfms)>();

        foreach (DataGridViewRow row in projectsGridView.SelectedRows)
        {
            var filePath = row.Cells["FullPath"].Value?.ToString() ?? "";
            var currentTfms = row.Cells["TargetFrameworks"].Value?.ToString() ?? "";

            selectedProjects.Add((row.Index, filePath, currentTfms));
        }

        if (selectedProjects.Count == 0)
        {
            return;
        }

        // Show confirmation dialog
        var confirmMessage = $"Are you sure you want to change the target framework to '{newValue}' for {selectedProjects.Count} project(s)?";
        var confirmResult = MessageBox.Show(confirmMessage, "Confirm Change",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question);

        if (confirmResult != DialogResult.Yes)
        {
            return;
        }

        // Apply changes
        int successCount = 0;
        int errorCount = 0;
        var results = new List<string>();

        foreach (var (rowIndex, filePath, currentTfms) in selectedProjects)
        {
            try
            {
                // Write to file
                await WriteTfmToFileAsync(filePath, newValue);

                // Update grid
                projectsGridView.Rows[rowIndex].Cells["TargetFrameworks"].Value = newValue;
                projectsGridView.Rows[rowIndex].Cells["Changed"].Value = "✓";
                projectsGridView.Rows[rowIndex].DefaultCellStyle.BackColor = Color.LightGreen;

                // Update the stored project info
                var project = _allProjects.FirstOrDefault(p => p.FullPath == filePath);
                if (project != null)
                {
                    project.TargetFrameworks = newValue;
                    project.Changed = "✓";
                }

                successCount++;
                results.Add($"✓ {Path.GetFileName(filePath)}: {currentTfms} → {newValue}");
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("read-only") || ex.Message.Contains("locked"))
            {
                errorCount++;
                results.Add($"✗ {Path.GetFileName(filePath)}: {ex.Message}");
                projectsGridView.Rows[rowIndex].Cells["Changed"].Value = "Error";
            }
            catch (Exception ex)
            {
                errorCount++;
                results.Add($"✗ {Path.GetFileName(filePath)}: Error - {ex.Message}");
                projectsGridView.Rows[rowIndex].Cells["Changed"].Value = "Error";
            }
        }

        // Show results
        var resultMessage = $"Change completed:\n\n" +
                          $"Successful: {successCount}\n" +
                          $"Errors: {errorCount}\n\n" +
                          string.Join("\n", results.Take(10));

        if (results.Count > 10)
        {
            resultMessage += $"\n\n... and {results.Count - 10} more";
        }

        MessageBox.Show(resultMessage, "Change Results",
            MessageBoxButtons.OK, errorCount > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);

        // Refresh selection state
        UpdateFrameworkOperationsState();
    }

    private async void AppendTargetFrameworkButton_Click(object? sender, EventArgs e)
    {
        var appendValue = appendTargetFrameworkComboBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(appendValue))
        {
            MessageBox.Show("Please enter a target framework to append.", "No Framework Specified",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // Get selected SDK-style projects
        var selectedProjects = new List<(int rowIndex, string filePath, string currentTfms)>();

        foreach (DataGridViewRow row in projectsGridView.SelectedRows)
        {
            var filePath = row.Cells["FullPath"].Value?.ToString() ?? "";
            var style = row.Cells["Style"].Value?.ToString() ?? "";
            var currentTfms = row.Cells["TargetFrameworks"].Value?.ToString() ?? "";

            if (style == "SDK")
            {
                selectedProjects.Add((row.Index, filePath, currentTfms));
            }
        }

        if (selectedProjects.Count == 0)
        {
            return;
        }

        // Show confirmation dialog
        var confirmMessage = $"Are you sure you want to append '{appendValue}' to {selectedProjects.Count} project(s)?";
        var confirmResult = MessageBox.Show(confirmMessage, "Confirm Append",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question);

        if (confirmResult != DialogResult.Yes)
        {
            return;
        }

        // Apply changes
        int successCount = 0;
        int errorCount = 0;
        var results = new List<string>();

        foreach (var (rowIndex, filePath, currentTfms) in selectedProjects)
        {
            try
            {
                var newTfms = AppendTfmValue(currentTfms, appendValue);

                // Write to file
                await WriteTfmToFileAsync(filePath, newTfms);

                // Update grid
                projectsGridView.Rows[rowIndex].Cells["TargetFrameworks"].Value = newTfms;
                projectsGridView.Rows[rowIndex].Cells["Changed"].Value = "✓";
                projectsGridView.Rows[rowIndex].DefaultCellStyle.BackColor = Color.LightGreen;

                // Update the stored project info
                var project = _allProjects.FirstOrDefault(p => p.FullPath == filePath);
                if (project != null)
                {
                    project.TargetFrameworks = newTfms;
                    project.Changed = "✓";
                }

                successCount++;
                results.Add($"✓ {Path.GetFileName(filePath)}: {currentTfms} → {newTfms}");
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("read-only") || ex.Message.Contains("locked"))
            {
                errorCount++;
                results.Add($"✗ {Path.GetFileName(filePath)}: {ex.Message}");
                projectsGridView.Rows[rowIndex].Cells["Changed"].Value = "Error";
            }
            catch (Exception ex)
            {
                errorCount++;
                results.Add($"✗ {Path.GetFileName(filePath)}: Error - {ex.Message}");
                projectsGridView.Rows[rowIndex].Cells["Changed"].Value = "Error";
            }
        }

        // Show results
        var resultMessage = $"Append completed:\n\n" +
                          $"Successful: {successCount}\n" +
                          $"Errors: {errorCount}\n\n" +
                          string.Join("\n", results.Take(10));

        if (results.Count > 10)
        {
            resultMessage += $"\n\n... and {results.Count - 10} more";
        }

        MessageBox.Show(resultMessage, "Append Results",
            MessageBoxButtons.OK, errorCount > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);

        // Refresh selection state
        UpdateFrameworkOperationsState();
    }

    private async void ConvertToSdkButton_Click(object? sender, EventArgs e)
    {
        // Get selected Old-style projects
        var selectedProjects = new List<(int rowIndex, string filePath, string currentTfms)>();

        foreach (DataGridViewRow row in projectsGridView.SelectedRows)
        {
            var filePath = row.Cells["FullPath"].Value?.ToString() ?? "";
            var style = row.Cells["Style"].Value?.ToString() ?? "";
            var currentTfms = row.Cells["TargetFrameworks"].Value?.ToString() ?? "";

            if (style == "Old-style")
            {
                selectedProjects.Add((row.Index, filePath, currentTfms));
            }
        }

        if (selectedProjects.Count == 0)
        {
            return;
        }

        // Show confirmation dialog
        var confirmMessage = $"Are you sure you want to convert {selectedProjects.Count} Old-style project(s) to SDK-style?\n\n" +
                           "This will:\n" +
                           "- Convert the project file to SDK-style format\n" +
                           "- Map old framework versions (v4.x) to SDK-style (net4x)\n" +
                           "- Add -windows suffix for WinForms projects\n" +
                           "- Remove most legacy project elements";
        var confirmResult = MessageBox.Show(confirmMessage, "Confirm Conversion",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question);

        if (confirmResult != DialogResult.Yes)
        {
            return;
        }

        // Apply changes
        int successCount = 0;
        int errorCount = 0;
        var results = new List<string>();

        foreach (var (rowIndex, filePath, currentTfms) in selectedProjects)
        {
            try
            {
                // Convert the project file
                var newTfms = await ConvertOldStyleToSdkAsync(filePath, currentTfms);

                // Update grid
                projectsGridView.Rows[rowIndex].Cells["Style"].Value = "SDK";
                projectsGridView.Rows[rowIndex].Cells["TargetFrameworks"].Value = newTfms;
                projectsGridView.Rows[rowIndex].Cells["Changed"].Value = "✓";
                projectsGridView.Rows[rowIndex].DefaultCellStyle.BackColor = Color.LightGreen;

                // Update the stored project info
                var project = _allProjects.FirstOrDefault(p => p.FullPath == filePath);
                if (project != null)
                {
                    project.Style = "SDK";
                    project.TargetFrameworks = newTfms;
                    project.Changed = "✓";
                }

                successCount++;
                results.Add($"✓ {Path.GetFileName(filePath)}: {currentTfms} → {newTfms}");
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("read-only") || ex.Message.Contains("locked"))
            {
                errorCount++;
                results.Add($"✗ {Path.GetFileName(filePath)}: {ex.Message}");
                projectsGridView.Rows[rowIndex].Cells["Changed"].Value = "Error";
            }
            catch (Exception ex)
            {
                errorCount++;
                results.Add($"✗ {Path.GetFileName(filePath)}: Error - {ex.Message}");
                projectsGridView.Rows[rowIndex].Cells["Changed"].Value = "Error";
            }
        }

        // Show results
        var resultMessage = $"Conversion completed:\n\n" +
                          $"Successful: {successCount}\n" +
                          $"Errors: {errorCount}\n\n" +
                          string.Join("\n", results.Take(10));

        if (results.Count > 10)
        {
            resultMessage += $"\n\n... and {results.Count - 10} more";
        }

        MessageBox.Show(resultMessage, "Conversion Results",
            MessageBoxButtons.OK, errorCount > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);

        // Refresh selection state
        UpdateFrameworkOperationsState();
    }

    private async void ConvertToOldStyleButton_Click(object? sender, EventArgs e)
    {
        // Get selected SDK-style projects
        var selectedProjects = new List<(int rowIndex, string filePath, string currentTfms, string currentStyle)>();

        foreach (DataGridViewRow row in projectsGridView.SelectedRows)
        {
            var filePath = row.Cells["FullPath"].Value?.ToString() ?? "";
            var style = row.Cells["Style"].Value?.ToString() ?? "";
            var currentTfms = row.Cells["TargetFrameworks"].Value?.ToString() ?? "";

            if (style == "SDK")
            {
                selectedProjects.Add((row.Index, filePath, currentTfms, style));
            }
        }

        if (selectedProjects.Count == 0)
        {
            return;
        }

        // Validate constraints and identify projects that can be converted
        var validProjects = new List<(int rowIndex, string filePath, string currentTfms)>();
        var skippedProjects = new List<(string fileName, string reason)>();

        foreach (var (rowIndex, filePath, currentTfms, currentStyle) in selectedProjects)
        {
            // Check if already Old-style (shouldn't happen due to button enablement, but check anyway)
            if (currentStyle == "Old-style")
            {
                skippedProjects.Add((Path.GetFileName(filePath), "Already Old-style"));
                continue;
            }

            // Check constraints
            var validationResult = ValidateSdkToOldStyleConstraints(filePath, currentTfms);

            if (validationResult.IsValid)
            {
                validProjects.Add((rowIndex, filePath, currentTfms));
            }
            else
            {
                skippedProjects.Add((Path.GetFileName(filePath), validationResult.Reason));
            }
        }

        if (validProjects.Count == 0)
        {
            // All projects were skipped
            var skipMessage = "No projects can be converted. Reasons:\n\n" +
                            string.Join("\n", skippedProjects.Take(10).Select(p => $"✗ {p.fileName}: {p.reason}"));

            if (skippedProjects.Count > 10)
            {
                skipMessage += $"\n\n... and {skippedProjects.Count - 10} more";
            }

            MessageBox.Show(skipMessage, "Conversion Blocked",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // Show confirmation dialog
        var confirmMessage = $"Are you sure you want to convert {validProjects.Count} SDK-style project(s) to Old-style?\n\n" +
                           "This will:\n" +
                           "- Convert the project file to Old-style format\n" +
                           "- Map SDK-style framework versions (net4x) to old-style (v4.x)\n" +
                           "- Preserve variable tokens verbatim\n" +
                           "- Restore legacy project structure";

        if (skippedProjects.Count > 0)
        {
            confirmMessage += $"\n\nNote: {skippedProjects.Count} project(s) will be skipped due to constraint violations.";
        }

        var confirmResult = MessageBox.Show(confirmMessage, "Confirm Conversion",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question);

        if (confirmResult != DialogResult.Yes)
        {
            return;
        }

        // Apply changes
        int successCount = 0;
        int errorCount = 0;
        var results = new List<string>();

        foreach (var (rowIndex, filePath, currentTfms) in validProjects)
        {
            try
            {
                // Convert the project file
                var newTfms = await ConvertSdkToOldStyleAsync(filePath, currentTfms);

                // Update grid
                projectsGridView.Rows[rowIndex].Cells["Style"].Value = "Old-style";
                projectsGridView.Rows[rowIndex].Cells["TargetFrameworks"].Value = newTfms;
                projectsGridView.Rows[rowIndex].Cells["Changed"].Value = "✓";
                projectsGridView.Rows[rowIndex].DefaultCellStyle.BackColor = Color.LightGreen;

                // Update the stored project info
                var project = _allProjects.FirstOrDefault(p => p.FullPath == filePath);
                if (project != null)
                {
                    project.Style = "Old-style";
                    project.TargetFrameworks = newTfms;
                    project.Changed = "✓";
                }

                successCount++;
                results.Add($"✓ {Path.GetFileName(filePath)}: {currentTfms} → {newTfms}");
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("read-only") || ex.Message.Contains("locked"))
            {
                errorCount++;
                results.Add($"✗ {Path.GetFileName(filePath)}: {ex.Message}");
                projectsGridView.Rows[rowIndex].Cells["Changed"].Value = "Error";
            }
            catch (Exception ex)
            {
                errorCount++;
                results.Add($"✗ {Path.GetFileName(filePath)}: Error - {ex.Message}");
                projectsGridView.Rows[rowIndex].Cells["Changed"].Value = "Error";
            }
        }

        // Add skipped projects to results
        foreach (var (fileName, reason) in skippedProjects)
        {
            results.Add($"⊘ {fileName}: Skipped - {reason}");
        }

        // Show results
        var resultMessage = $"Conversion completed:\n\n" +
                          $"Successful: {successCount}\n" +
                          $"Errors: {errorCount}\n" +
                          $"Skipped: {skippedProjects.Count}\n\n" +
                          string.Join("\n", results.Take(10));

        if (results.Count > 10)
        {
            resultMessage += $"\n\n... and {results.Count - 10} more";
        }

        MessageBox.Show(resultMessage, "Conversion Results",
            MessageBoxButtons.OK, errorCount > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);

        // Refresh selection state
        UpdateFrameworkOperationsState();
    }

    private void UpdateFrameworkOperationsState()
    {
        if (projectsGridView.SelectedRows.Count == 0)
        {
            changeTargetFrameworkButton.Enabled = false;
            targetFrameworkComboBox.Enabled = false;
            targetFrameworkComboBox.Text = "";
            appendTargetFrameworkButton.Enabled = false;
            appendTargetFrameworkComboBox.Enabled = false;
            convertToSdkButton.Enabled = false;
            convertToOldStyleButton.Enabled = false;
            return;
        }

        // Collect TFM values from all selected rows
        List<string> tfmValues = new List<string>();
        bool allSdkStyle = true;
        bool allOldStyle = true;

        foreach (DataGridViewRow row in projectsGridView.SelectedRows)
        {
            if (row.Cells["TargetFrameworks"].Value is string tfmValue)
            {
                tfmValues.Add(tfmValue);
            }

            if (row.Cells["Style"].Value is string styleValue)
            {
                if (styleValue != "SDK")
                {
                    allSdkStyle = false;
                }
                if (styleValue != "Old-style")
                {
                    allOldStyle = false;
                }
            }
        }

        if (tfmValues.Count == 0)
        {
            changeTargetFrameworkButton.Enabled = false;
            targetFrameworkComboBox.Enabled = false;
            targetFrameworkComboBox.Text = "";
            appendTargetFrameworkButton.Enabled = false;
            appendTargetFrameworkComboBox.Enabled = false;
            convertToSdkButton.Enabled = false;
            convertToOldStyleButton.Enabled = false;
            return;
        }

        // Check if all TFM sets are identical (order-insensitive, case-insensitive for literals, exact for variables)
        bool allSetsEqual = true;
        var firstNormalized = NormalizeTfmSet(tfmValues[0]);

        for (int i = 1; i < tfmValues.Count; i++)
        {
            var currentNormalized = NormalizeTfmSet(tfmValues[i]);
            if (!firstNormalized.Equals(currentNormalized))
            {
                allSetsEqual = false;
                break;
            }
        }

        if (allSetsEqual)
        {
            changeTargetFrameworkButton.Enabled = true;
            targetFrameworkComboBox.Enabled = true;

            // Prefill the ComboBox with the exact joined TFMs from the first selection
            // (since all are identical, we can use the first one)
            targetFrameworkComboBox.Text = tfmValues[0];
        }
        else
        {
            changeTargetFrameworkButton.Enabled = false;
            targetFrameworkComboBox.Enabled = false;
            targetFrameworkComboBox.Text = "";
        }

        // Enable Append button only if all selected rows are SDK-style
        if (allSdkStyle)
        {
            appendTargetFrameworkButton.Enabled = true;
            appendTargetFrameworkComboBox.Enabled = true;
        }
        else
        {
            appendTargetFrameworkButton.Enabled = false;
            appendTargetFrameworkComboBox.Enabled = false;
        }

        // Enable Convert to SDK button only if all selected rows are Old-style
        if (allOldStyle)
        {
            convertToSdkButton.Enabled = true;
        }
        else
        {
            convertToSdkButton.Enabled = false;
        }

        // Enable Convert to Old-style button only if all selected rows are SDK-style
        if (allSdkStyle)
        {
            convertToOldStyleButton.Enabled = true;
        }
        else
        {
            convertToOldStyleButton.Enabled = false;
        }
    }

    private string AppendTfmValue(string currentTfms, string appendValue)
    {
        // Parse the append value to extract individual TFMs
        var appendTokens = ParseTfmTokens(appendValue);

        // Parse existing TFMs to extract individual tokens
        var existingTokens = ParseTfmTokens(currentTfms);

        // Combine and deduplicate
        var allTokens = new List<TfmToken>();
        allTokens.AddRange(existingTokens);

        foreach (var appendToken in appendTokens)
        {
            bool isDuplicate = false;

            foreach (var existingToken in allTokens)
            {
                if (AreTfmTokensEqual(appendToken, existingToken))
                {
                    isDuplicate = true;
                    break;
                }
            }

            if (!isDuplicate)
            {
                allTokens.Add(appendToken);
            }
        }

        // Sort tokens: variables first (in original order), then literals (sorted)
        var variables = allTokens.Where(t => t.IsVariable).ToList();
        var literals = allTokens.Where(t => !t.IsVariable)
                                .OrderBy(t => GetSortOrder(t.Value))
                                .ThenBy(t => t.Value.ToLowerInvariant())
                                .ToList();

        var sortedTokens = variables.Concat(literals).ToList();

        // Join tokens back into a semicolon-separated string
        return string.Join(";", sortedTokens.Select(t => t.Value));
    }

    private List<TfmToken> ParseTfmTokens(string tfmValue)
    {
        var tokens = new List<TfmToken>();

        if (string.IsNullOrWhiteSpace(tfmValue))
        {
            return tokens;
        }

        var parts = tfmValue.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                           .Select(p => p.Trim())
                           .Where(p => !string.IsNullOrEmpty(p));

        foreach (var part in parts)
        {
            var token = new TfmToken
            {
                Value = part,
                IsVariable = part.StartsWith("$")
            };

            // Apply WinForms autocorrection for literal nets
            if (!token.IsVariable && IsNetFrameworkToken(part))
            {
                token.Value = ApplyWinFormsAutocorrection(part);
            }

            tokens.Add(token);
        }

        return tokens;
    }

    private bool IsNetFrameworkToken(string value)
    {
        var lower = value.ToLowerInvariant();
        // Check if it looks like a .NET Framework version (net followed by digits and optional decimals)
        return lower.StartsWith("net") &&
               lower.Length > 3 &&
               char.IsDigit(lower[3]) &&
               !lower.Contains("-"); // Exclude net6.0-windows style
    }

    private string ApplyWinFormsAutocorrection(string tfm)
    {
        var lower = tfm.ToLowerInvariant();

        // Common .NET Framework versions that should have -windows suffix for WinForms
        // We'll add -windows if it's a modern .NET version (net5.0+) without existing platform suffix
        if (lower.StartsWith("net") && !lower.Contains("-"))
        {
            // Extract version number
            var versionPart = lower.Substring(3);

            // Check if it's a modern .NET version (5.0 or higher)
            if (versionPart.StartsWith("5") || versionPart.StartsWith("6") ||
                versionPart.StartsWith("7") || versionPart.StartsWith("8") ||
                versionPart.StartsWith("9"))
            {
                // Add -windows suffix for WinForms projects
                return tfm + "-windows";
            }
        }

        return tfm;
    }

    private bool AreTfmTokensEqual(TfmToken token1, TfmToken token2)
    {
        // Variables require exact match (case-sensitive)
        if (token1.IsVariable && token2.IsVariable)
        {
            return token1.Value == token2.Value;
        }

        // If one is variable and other is not, they're not equal
        if (token1.IsVariable != token2.IsVariable)
        {
            return false;
        }

        // Literals are case-insensitive
        return string.Equals(token1.Value, token2.Value, StringComparison.OrdinalIgnoreCase);
    }

    private int GetSortOrder(string tfm)
    {
        var lower = tfm.ToLowerInvariant();

        // Extract version number for sorting
        // Priority: newer versions first
        if (lower.StartsWith("net"))
        {
            var versionPart = lower.Substring(3);

            // Try to parse version number
            if (versionPart.Length > 0 && char.IsDigit(versionPart[0]))
            {
                // Extract numeric part
                var numericPart = "";
                foreach (var ch in versionPart)
                {
                    if (char.IsDigit(ch) || ch == '.')
                    {
                        numericPart += ch;
                    }
                    else
                    {
                        break;
                    }
                }

                if (double.TryParse(numericPart, out var version))
                {
                    // Return negative to sort descending (newer first)
                    return -(int)(version * 10);
                }
            }
        }

        return 0;
    }

    private async Task WriteTfmToFileAsync(string filePath, string newTfms)
    {
        await Task.Run(() =>
        {
            // Check if file is read-only
            if (File.Exists(filePath))
            {
                var fileInfo = new FileInfo(filePath);
                if (fileInfo.IsReadOnly)
                {
                    throw new InvalidOperationException($"File is read-only: {filePath}");
                }
            }

            // Load document with encoding preservation
            XDocument doc;
            System.Text.Encoding? encoding = null;

            try
            {
                // Detect encoding from file
                using (var reader = new StreamReader(filePath, true))
                {
                    reader.Peek(); // Force encoding detection
                    encoding = reader.CurrentEncoding;
                }

                doc = XDocument.Load(filePath, LoadOptions.PreserveWhitespace);
            }
            catch (IOException ex)
            {
                throw new InvalidOperationException($"File is locked or inaccessible: {ex.Message}");
            }

            var root = doc.Root;

            if (root == null)
            {
                throw new InvalidOperationException("Invalid project file");
            }

            XNamespace ns = root.GetDefaultNamespace();

            // Look for existing TargetFramework or TargetFrameworks element
            var targetFrameworkElement = root.Descendants(ns + "TargetFramework").FirstOrDefault();
            var targetFrameworksElement = root.Descendants(ns + "TargetFrameworks").FirstOrDefault();

            // Determine if we need singular or plural based on the new value
            bool isMultiple = newTfms.Contains(';');

            if (isMultiple)
            {
                // We need TargetFrameworks (plural)
                // Remove conflicting TargetFramework (singular) if it exists
                if (targetFrameworkElement != null)
                {
                    targetFrameworkElement.Remove();
                }

                if (targetFrameworksElement != null)
                {
                    // Update existing TargetFrameworks
                    targetFrameworksElement.Value = newTfms;
                }
                else
                {
                    // Create new TargetFrameworks element in the first PropertyGroup
                    var propertyGroup = root.Descendants(ns + "PropertyGroup").FirstOrDefault();
                    if (propertyGroup == null)
                    {
                        propertyGroup = new XElement(ns + "PropertyGroup");
                        root.Add(propertyGroup);
                    }
                    propertyGroup.Add(new XElement(ns + "TargetFrameworks", newTfms));
                }
            }
            else
            {
                // Single framework
                // Remove conflicting TargetFrameworks (plural) if it exists
                if (targetFrameworksElement != null)
                {
                    targetFrameworksElement.Remove();
                }

                if (targetFrameworkElement != null)
                {
                    // Update existing TargetFramework
                    targetFrameworkElement.Value = newTfms;
                }
                else
                {
                    // Create new TargetFramework element in the first PropertyGroup
                    var propertyGroup = root.Descendants(ns + "PropertyGroup").FirstOrDefault();
                    if (propertyGroup == null)
                    {
                        propertyGroup = new XElement(ns + "PropertyGroup");
                        root.Add(propertyGroup);
                    }
                    propertyGroup.Add(new XElement(ns + "TargetFramework", newTfms));
                }
            }

            // Save with original encoding
            try
            {
                var settings = new XmlWriterSettings
                {
                    Encoding = encoding ?? System.Text.Encoding.UTF8,
                    Indent = true,
                    IndentChars = "  ",
                    OmitXmlDeclaration = doc.Declaration == null
                };

                using (var writer = XmlWriter.Create(filePath, settings))
                {
                    doc.Save(writer);
                }
            }
            catch (IOException ex)
            {
                throw new InvalidOperationException($"Failed to write to file (may be locked): {ex.Message}");
            }
        });
    }

    private NormalizedTfmSet NormalizeTfmSet(string tfmValue)
    {
        var normalizedSet = new NormalizedTfmSet();

        // Check if it's a variable token (starts with $)
        if (tfmValue.StartsWith("$"))
        {
            // It's a variable - exact match required
            normalizedSet.IsVariable = true;
            normalizedSet.VariableToken = tfmValue;
            normalizedSet.Tfms = new List<string> { tfmValue };
            return normalizedSet;
        }

        // It's a literal or semicolon-separated list
        var tfms = tfmValue.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                          .Select(t => t.Trim())
                          .Where(t => !string.IsNullOrEmpty(t))
                          .ToList();

        // Check if the list contains any variables
        bool hasVariables = tfms.Any(t => t.StartsWith("$"));

        if (hasVariables)
        {
            // If any TFM is a variable, treat the entire set as requiring exact match
            // This handles cases like "net8.0;$(TargetFrameworks)"
            normalizedSet.IsVariable = true;
            normalizedSet.Tfms = tfms; // Keep original order and case
            return normalizedSet;
        }

        // All are literals - normalize: order-insensitive, case-insensitive
        var normalizedTfms = tfms.Select(t => t.ToLowerInvariant())
                                 .OrderBy(t => t)
                                 .ToList();

        normalizedSet.IsVariable = false;
        normalizedSet.Tfms = normalizedTfms;

        return normalizedSet;
    }

    private async Task<string> ConvertOldStyleToSdkAsync(string filePath, string oldTfm)
    {
        return await Task.Run(() =>
        {
            // Check if file is read-only
            if (File.Exists(filePath))
            {
                var fileInfo = new FileInfo(filePath);
                if (fileInfo.IsReadOnly)
                {
                    throw new InvalidOperationException($"File is read-only: {filePath}");
                }
            }

            XDocument doc;
            System.Text.Encoding? encoding = null;

            try
            {
                // Detect encoding from file
                using (var reader = new StreamReader(filePath, true))
                {
                    reader.Peek(); // Force encoding detection
                    encoding = reader.CurrentEncoding;
                }

                doc = XDocument.Load(filePath, LoadOptions.PreserveWhitespace);
            }
            catch (IOException ex)
            {
                throw new InvalidOperationException($"File is locked or inaccessible: {ex.Message}");
            }

            var root = doc.Root;

            if (root == null)
            {
                throw new InvalidOperationException("Invalid project file");
            }

            // Check if already SDK-style
            if (root.Attribute("Sdk") != null)
            {
                // Already SDK-style, skip
                return ParseTargetFrameworks(root);
            }

            XNamespace ns = root.GetDefaultNamespace();

            // Detect project characteristics
            bool isWinForms = DetectWinFormsProject(root, ns);
            bool isWpf = DetectWpfProject(root, ns);
            bool requiresWindowsDesktop = isWinForms || isWpf;

            // Convert framework version
            string newTfm = ConvertFrameworkVersion(oldTfm, requiresWindowsDesktop);

            // Create new SDK-style project
            var newRoot = new XElement("Project");

            var sdkAttribute = requiresWindowsDesktop
                ? "Microsoft.NET.Sdk.WindowsDesktop"
                : "Microsoft.NET.Sdk";
            newRoot.Add(new XAttribute("Sdk", sdkAttribute));

            var newElements = new List<XElement>();
            var propertyGroups = new List<XElement>();

            foreach (var child in root.Elements())
            {
                if (child.Name == ns + "PropertyGroup")
                {
                    var newGroup = ClonePropertyGroupWithoutTargets(child);
                    if (newGroup != null)
                    {
                        newElements.Add(newGroup);
                        propertyGroups.Add(newGroup);
                    }
                }
                else if (child.Name == ns + "ItemGroup")
                {
                    var newGroup = CloneElementWithoutNamespace(child);
                    if (newGroup.HasElements)
                    {
                        newElements.Add(newGroup);
                    }
                }
                else if (child.Name == ns + "Import")
                {
                    // SDK-style projects do not generally require explicit imports for common props/targets
                    continue;
                }
                else
                {
                    var clone = CloneElementWithoutNamespace(child);
                    if (clone != null)
                    {
                        newElements.Add(clone);
                    }
                }
            }

            // Ensure we have a PropertyGroup to host core properties
            var globalPropertyGroup = propertyGroups.FirstOrDefault(pg => pg.Attribute("Condition") == null);
            if (globalPropertyGroup == null)
            {
                globalPropertyGroup = new XElement("PropertyGroup");
                newElements.Insert(0, globalPropertyGroup);
                propertyGroups.Insert(0, globalPropertyGroup);
            }

            EnsureElementValue(globalPropertyGroup, "TargetFramework", newTfm);

            if (requiresWindowsDesktop)
            {
                if (isWinForms && !globalPropertyGroup.Elements("UseWindowsForms").Any())
                {
                    globalPropertyGroup.Add(new XElement("UseWindowsForms", "true"));
                }

                if (isWpf && !globalPropertyGroup.Elements("UseWPF").Any())
                {
                    globalPropertyGroup.Add(new XElement("UseWPF", "true"));
                }
            }

            // Copy OutputType, RootNamespace, AssemblyName if they were not preserved
            EnsureElementCopied(root, ns, globalPropertyGroup, "OutputType");
            EnsureElementCopied(root, ns, globalPropertyGroup, "RootNamespace");
            EnsureElementCopied(root, ns, globalPropertyGroup, "AssemblyName");

            foreach (var element in newElements)
            {
                newRoot.Add(element);
            }

            // Create new document
            var newDoc = new XDocument(new XDeclaration("1.0", "utf-8", null), newRoot);

            // Save the new document with encoding preservation
            try
            {
                var settings = new XmlWriterSettings
                {
                    Encoding = encoding ?? System.Text.Encoding.UTF8,
                    Indent = true,
                    IndentChars = "  ",
                    OmitXmlDeclaration = false
                };

                using (var writer = XmlWriter.Create(filePath, settings))
                {
                    newDoc.Save(writer);
                }
            }
            catch (IOException ex)
            {
                throw new InvalidOperationException($"Failed to write to file (may be locked): {ex.Message}");
            }

            return newTfm;
        });
    }

    private bool DetectWinFormsProject(XElement root, XNamespace ns)
    {
        // Check for WinForms indicators
        var references = root.Descendants(ns + "Reference")
                            .Select(r => r.Attribute("Include")?.Value ?? "")
                            .ToList();

        bool hasWinFormsRef = references.Any(r =>
            r.Contains("System.Windows.Forms") ||
            r.Contains("System.Drawing"));

        // Check for UseWindowsForms property
        var useWindowsForms = root.Descendants(ns + "UseWindowsForms").FirstOrDefault();
        if (useWindowsForms != null && useWindowsForms.Value.Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Check for form files
        var compiles = root.Descendants(ns + "Compile")
                          .Select(c => c.Attribute("Include")?.Value ?? "")
                          .ToList();

        bool hasFormFiles = compiles.Any(c => c.EndsWith(".Designer.cs"));

        return hasWinFormsRef || hasFormFiles;
    }

    private string ConvertFrameworkVersion(string oldTfm, bool requiresWindowsDesktop)
    {
        // Handle variable tokens - preserve them verbatim
        if (oldTfm.StartsWith("$"))
        {
            return oldTfm;
        }

        // Map old-style versions to SDK-style
        // Examples: v4.5 → net45, v4.7.2 → net472
        var trimmed = oldTfm.Trim();

        if (trimmed.StartsWith("v", StringComparison.OrdinalIgnoreCase))
        {
            // Remove 'v' prefix and dots
            var version = trimmed.Substring(1).Replace(".", "");
            var newTfm = "net" + version;

            // For .NET Framework 4.x versions that require Windows desktop assets, add -windows suffix
            if (requiresWindowsDesktop && version.StartsWith("4"))
            {
                newTfm += "-windows";
            }

            return newTfm;
        }

        // If it doesn't start with 'v', return as-is
        return trimmed;
    }

    private (bool IsValid, string Reason) ValidateSdkToOldStyleConstraints(string filePath, string currentTfms)
    {
        try
        {
            var doc = XDocument.Load(filePath);
            var root = doc.Root;

            if (root == null)
            {
                return (false, "Invalid project file");
            }

            // Skip if already Old-style
            if (root.Attribute("Sdk") == null)
            {
                return (false, "Already Old-style");
            }

            XNamespace ns = root.GetDefaultNamespace();

            // Check for PackageReference items
            var packageReferences = root.Descendants(ns + "PackageReference").ToList();
            if (packageReferences.Count > 0)
            {
                return (false, $"Has {packageReferences.Count} PackageReference(s)");
            }

            // Check if it's a single .NET Framework target
            // Variable tokens are allowed and preserved
            if (currentTfms.StartsWith("$"))
            {
                // It's a variable token - allow it (will be preserved verbatim)
                return (true, "");
            }

            // Check if multiple targets
            if (currentTfms.Contains(";"))
            {
                return (false, "Multiple target frameworks");
            }

            // Check if it's a .NET Framework target (net40–net48 or net40-windows–net48-windows)
            var tfmLower = currentTfms.ToLowerInvariant();

            if (!IsNetFrameworkTarget(tfmLower))
            {
                return (false, "Not a .NET Framework target");
            }

            return (true, "");
        }
        catch (Exception ex)
        {
            return (false, $"Error: {ex.Message}");
        }
    }

    private bool IsNetFrameworkTarget(string tfm)
    {
        // Remove -windows suffix if present
        var baseTfm = tfm.Replace("-windows", "");

        // Check if it's in the range net40–net48
        var validTargets = new[]
        {
            "net40", "net403",
            "net45", "net451", "net452",
            "net46", "net461", "net462",
            "net47", "net471", "net472",
            "net48", "net481"
        };

        return validTargets.Contains(baseTfm);
    }

    private async Task<string> ConvertSdkToOldStyleAsync(string filePath, string currentTfm)
    {
        return await Task.Run(() =>
        {
            // Check if file is read-only
            if (File.Exists(filePath))
            {
                var fileInfo = new FileInfo(filePath);
                if (fileInfo.IsReadOnly)
                {
                    throw new InvalidOperationException($"File is read-only: {filePath}");
                }
            }

            XDocument doc;
            System.Text.Encoding? encoding = null;

            try
            {
                // Detect encoding from file
                using (var reader = new StreamReader(filePath, true))
                {
                    reader.Peek(); // Force encoding detection
                    encoding = reader.CurrentEncoding;
                }

                doc = XDocument.Load(filePath, LoadOptions.PreserveWhitespace);
            }
            catch (IOException ex)
            {
                throw new InvalidOperationException($"File is locked or inaccessible: {ex.Message}");
            }

            var root = doc.Root;

            if (root == null)
            {
                throw new InvalidOperationException("Invalid project file");
            }

            // Check if already Old-style
            if (root.Attribute("Sdk") == null)
            {
                // Already Old-style, skip
                return ParseTargetFrameworks(root);
            }

            XNamespace ns = root.GetDefaultNamespace();
            XNamespace msbuildNs = "http://schemas.microsoft.com/developer/msbuild/2003";

            // Convert framework version from SDK-style to Old-style
            string newTfm = ConvertSdkToOldStyleFrameworkVersion(currentTfm);

            // Detect desktop requirements
            bool isWinForms = IsWinFormsInSdkProject(root, ns);
            bool isWpf = IsWpfInSdkProject(root, ns);

            // Create new Old-style project
            var newRoot = new XElement(msbuildNs + "Project");
            newRoot.Add(new XAttribute("ToolsVersion", "15.0"));

            // Default xmlns attribute
            newRoot.Add(new XAttribute("xmlns", "http://schemas.microsoft.com/developer/msbuild/2003"));

            // Import Microsoft.CSharp.targets at the beginning
            var importGroup1 = new XElement(msbuildNs + "Import");
            importGroup1.Add(new XAttribute("Project", @"$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props"));
            importGroup1.Add(new XAttribute("Condition", @"Exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props')"));
            newRoot.Add(importGroup1);

            // Create PropertyGroup with essential properties
            var propertyGroup = new XElement(msbuildNs + "PropertyGroup");

            // Copy Configuration and Platform
            propertyGroup.Add(new XElement(msbuildNs + "Configuration", new XAttribute("Condition", " '$(Configuration)' == '' "), "Debug"));
            propertyGroup.Add(new XElement(msbuildNs + "Platform", new XAttribute("Condition", " '$(Platform)' == '' "), "AnyCPU"));

            // Add ProjectGuid
            var existingGuid = root.Descendants(ns + "ProjectGuid").FirstOrDefault()?.Value;
            var projectGuid = string.IsNullOrWhiteSpace(existingGuid)
                ? CreateDeterministicProjectGuid(filePath)
                : NormalizeProjectGuid(existingGuid);
            propertyGroup.Add(new XElement(msbuildNs + "ProjectGuid", projectGuid));

            // Add OutputType
            var outputType = root.Descendants(ns + "OutputType").FirstOrDefault();
            propertyGroup.Add(new XElement(msbuildNs + "OutputType", outputType?.Value ?? "Library"));

            // Add RootNamespace
            var rootNamespace = root.Descendants(ns + "RootNamespace").FirstOrDefault()?.Value;
            if (!string.IsNullOrWhiteSpace(rootNamespace))
            {
                propertyGroup.Add(new XElement(msbuildNs + "RootNamespace", rootNamespace));
            }
            else
            {
                propertyGroup.Add(new XElement(msbuildNs + "RootNamespace", Path.GetFileNameWithoutExtension(filePath)));
            }

            // Add AssemblyName
            var assemblyName = root.Descendants(ns + "AssemblyName").FirstOrDefault()?.Value;
            if (!string.IsNullOrWhiteSpace(assemblyName))
            {
                propertyGroup.Add(new XElement(msbuildNs + "AssemblyName", assemblyName));
            }
            else
            {
                propertyGroup.Add(new XElement(msbuildNs + "AssemblyName", Path.GetFileNameWithoutExtension(filePath)));
            }

            // Add TargetFrameworkVersion
            propertyGroup.Add(new XElement(msbuildNs + "TargetFrameworkVersion", newTfm));

            // Add FileAlignment
            propertyGroup.Add(new XElement(msbuildNs + "FileAlignment", "512"));

            // Add Deterministic
            propertyGroup.Add(new XElement(msbuildNs + "Deterministic", "true"));

            CopyGlobalPropertiesForOldStyle(root, ns, propertyGroup, msbuildNs);

            newRoot.Add(propertyGroup);

            // Add Debug PropertyGroup
            var debugPropertyGroup = CreateDefaultConfigurationGroup(msbuildNs, "Debug|AnyCPU", isDebug: true);
            MergeConfigurationProperties(root, ns, debugPropertyGroup, "Debug|AnyCPU", msbuildNs);
            newRoot.Add(debugPropertyGroup);

            // Add Release PropertyGroup
            var releasePropertyGroup = CreateDefaultConfigurationGroup(msbuildNs, "Release|AnyCPU", isDebug: false);
            MergeConfigurationProperties(root, ns, releasePropertyGroup, "Release|AnyCPU", msbuildNs);
            newRoot.Add(releasePropertyGroup);

            // Copy other conditional property groups
            foreach (var pg in root.Elements(ns + "PropertyGroup"))
            {
                var condition = pg.Attribute("Condition")?.Value;
                if (string.IsNullOrWhiteSpace(condition))
                {
                    continue;
                }

                if (condition.Contains("Debug|AnyCPU", StringComparison.OrdinalIgnoreCase) ||
                    condition.Contains("Release|AnyCPU", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var clone = CloneElementWithNamespace(pg, msbuildNs);
                RemoveTargetFrameworkProperties(clone, msbuildNs);
                if (clone.HasElements)
                {
                    newRoot.Add(clone);
                }
            }

            // Copy ItemGroups
            foreach (var ig in root.Elements(ns + "ItemGroup"))
            {
                var clone = CloneItemGroupForOldStyle(ig, msbuildNs);
                if (clone != null && clone.HasElements)
                {
                    newRoot.Add(clone);
                }
            }

            EnsureDefaultReferenceItems(newRoot, msbuildNs, isWinForms, isWpf);
            EnsureFileItems(newRoot, filePath, msbuildNs, "Compile", ".cs");
            EnsureFileItems(newRoot, filePath, msbuildNs, "EmbeddedResource", ".resx");
            EnsureFileItems(newRoot, filePath, msbuildNs, "None", ".config", ".settings");
            if (isWpf)
            {
                EnsureWpfItems(newRoot, filePath, msbuildNs);
            }

            // Import Microsoft.CSharp.targets at the end
            var importGroup2 = new XElement(msbuildNs + "Import");
            importGroup2.Add(new XAttribute("Project", @"$(MSBuildToolsPath)\Microsoft.CSharp.targets"));
            newRoot.Add(importGroup2);

            // Create new document
            var newDoc = new XDocument(new XDeclaration("1.0", "utf-8", null), newRoot);

            // Save the new document with encoding preservation
            try
            {
                var settings = new XmlWriterSettings
                {
                    Encoding = encoding ?? System.Text.Encoding.UTF8,
                    Indent = true,
                    IndentChars = "  ",
                    OmitXmlDeclaration = false
                };

                using (var writer = XmlWriter.Create(filePath, settings))
                {
                    newDoc.Save(writer);
                }
            }
            catch (IOException ex)
            {
                throw new InvalidOperationException($"Failed to write to file (may be locked): {ex.Message}");
            }

            return newTfm;
        });
    }

    private XElement? ClonePropertyGroupWithoutTargets(XElement propertyGroup)
    {
        var newGroup = new XElement("PropertyGroup");

        foreach (var attribute in propertyGroup.Attributes())
        {
            if (attribute.IsNamespaceDeclaration)
            {
                continue;
            }

            newGroup.Add(new XAttribute(attribute.Name.LocalName, attribute.Value));
        }

        foreach (var child in propertyGroup.Elements())
        {
            if (IsTargetFrameworkElementName(child.Name.LocalName))
            {
                continue;
            }

            var clone = CloneElementWithoutNamespace(child);
            if (clone != null)
            {
                newGroup.Add(clone);
            }
        }

        if (!newGroup.HasElements && !newGroup.Attributes().Any())
        {
            return null;
        }

        return newGroup;
    }

    private XElement CloneElementWithoutNamespace(XElement element)
    {
        var clone = new XElement(element.Name.LocalName);

        foreach (var attribute in element.Attributes())
        {
            if (attribute.IsNamespaceDeclaration)
            {
                continue;
            }

            XName attributeName = attribute.Name.Namespace == XNamespace.None
                ? attribute.Name.LocalName
                : attribute.Name;

            clone.Add(new XAttribute(attributeName, attribute.Value));
        }

        foreach (var node in element.Nodes())
        {
            switch (node)
            {
                case XElement childElement:
                    clone.Add(CloneElementWithoutNamespace(childElement));
                    break;
                case XCData cdata:
                    clone.Add(new XCData(cdata.Value));
                    break;
                case XText text:
                    clone.Add(new XText(text.Value));
                    break;
                case XComment comment:
                    clone.Add(new XComment(comment.Value));
                    break;
                case XProcessingInstruction instruction:
                    clone.Add(new XProcessingInstruction(instruction.Target, instruction.Data));
                    break;
                case XDocumentType:
                    break;
            }
        }

        return clone;
    }

    private XElement CloneElementWithNamespace(XElement element, XNamespace targetNamespace)
    {
        var clone = new XElement(targetNamespace + element.Name.LocalName);

        foreach (var attribute in element.Attributes())
        {
            if (attribute.IsNamespaceDeclaration)
            {
                continue;
            }

            XName attributeName = attribute.Name.Namespace == XNamespace.None
                ? attribute.Name.LocalName
                : attribute.Name;

            clone.Add(new XAttribute(attributeName, attribute.Value));
        }

        foreach (var node in element.Nodes())
        {
            switch (node)
            {
                case XElement childElement:
                    clone.Add(CloneElementWithNamespace(childElement, targetNamespace));
                    break;
                case XCData cdata:
                    clone.Add(new XCData(cdata.Value));
                    break;
                case XText text:
                    clone.Add(new XText(text.Value));
                    break;
                case XComment comment:
                    clone.Add(new XComment(comment.Value));
                    break;
                case XProcessingInstruction instruction:
                    clone.Add(new XProcessingInstruction(instruction.Target, instruction.Data));
                    break;
                case XDocumentType:
                    break;
            }
        }

        return clone;
    }

    private bool IsTargetFrameworkElementName(string name)
    {
        return name.Equals("TargetFramework", StringComparison.OrdinalIgnoreCase)
            || name.Equals("TargetFrameworks", StringComparison.OrdinalIgnoreCase)
            || name.Equals("TargetFrameworkVersion", StringComparison.OrdinalIgnoreCase);
    }

    private void EnsureElementValue(XElement parent, string elementName, string value)
    {
        var element = parent.Elements(elementName).FirstOrDefault();
        if (element == null)
        {
            parent.AddFirst(new XElement(elementName, value));
        }
        else
        {
            element.Value = value;
        }
    }

    private void EnsureElementCopied(XElement originalRoot, XNamespace originalNamespace, XElement targetGroup, string elementName)
    {
        if (targetGroup.Elements(elementName).Any())
        {
            return;
        }

        var sourceElement = originalRoot.Descendants(originalNamespace + elementName).FirstOrDefault();
        if (sourceElement != null)
        {
            targetGroup.Add(new XElement(elementName, sourceElement.Value));
        }
    }

    private bool DetectWpfProject(XElement root, XNamespace ns)
    {
        var useWpf = root.Descendants(ns + "UseWPF").FirstOrDefault();
        if (useWpf != null && useWpf.Value.Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var projectTypeGuids = root.Descendants(ns + "ProjectTypeGuids").FirstOrDefault()?.Value ?? string.Empty;
        if (projectTypeGuids.IndexOf("60DC8134-BAA2-11D1-AF9E-00A0C90F26F4", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }

        var references = root.Descendants(ns + "Reference")
            .Select(r => r.Attribute("Include")?.Value ?? string.Empty)
            .ToList();

        if (references.Any(r => r.Contains("PresentationFramework", StringComparison.OrdinalIgnoreCase)
            || r.Contains("PresentationCore", StringComparison.OrdinalIgnoreCase)
            || r.Contains("WindowsBase", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        var hasXamlPages = root.Descendants(ns + "Page").Any()
            || root.Descendants(ns + "ApplicationDefinition").Any();

        return hasXamlPages;
    }

    private bool IsWpfInSdkProject(XElement root, XNamespace ns)
    {
        var useWpf = root.Descendants(ns + "UseWPF").FirstOrDefault();
        if (useWpf != null && useWpf.Value.Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (root.Descendants(ns + "Page").Any() || root.Descendants(ns + "ApplicationDefinition").Any())
        {
            return true;
        }

        var references = root.Descendants(ns + "Reference")
            .Select(r => r.Attribute("Include")?.Value ?? string.Empty)
            .ToList();

        return references.Any(r => r.Contains("PresentationFramework", StringComparison.OrdinalIgnoreCase)
            || r.Contains("PresentationCore", StringComparison.OrdinalIgnoreCase)
            || r.Contains("WindowsBase", StringComparison.OrdinalIgnoreCase));
    }

    private string CreateDeterministicProjectGuid(string filePath)
    {
        var fullPath = Path.GetFullPath(filePath).ToLowerInvariant();
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(fullPath));
        var guid = new Guid(hash);
        return $"{{{guid.ToString().ToUpperInvariant()}}}";
    }

    private string NormalizeProjectGuid(string guidValue)
    {
        var trimmed = guidValue.Trim();

        if (!trimmed.StartsWith("{", StringComparison.Ordinal))
        {
            trimmed = "{" + trimmed;
        }

        if (!trimmed.EndsWith("}", StringComparison.Ordinal))
        {
            trimmed += "}";
        }

        return trimmed.ToUpperInvariant();
    }

    private void CopyGlobalPropertiesForOldStyle(XElement sourceRoot, XNamespace sourceNs, XElement targetGroup, XNamespace targetNs)
    {
        var skipProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Configuration",
            "Platform",
            "ProjectGuid",
            "OutputType",
            "RootNamespace",
            "AssemblyName",
            "TargetFramework",
            "TargetFrameworks",
            "TargetFrameworkVersion",
            "FileAlignment",
            "Deterministic"
        };

        foreach (var propertyGroup in sourceRoot.Elements(sourceNs + "PropertyGroup"))
        {
            if (propertyGroup.Attribute("Condition") != null)
            {
                continue;
            }

            foreach (var element in propertyGroup.Elements())
            {
                var localName = element.Name.LocalName;
                if (skipProperties.Contains(localName))
                {
                    continue;
                }

                var existing = targetGroup.Elements(targetNs + localName).FirstOrDefault()
                    ?? targetGroup.Elements(localName).FirstOrDefault();
                if (existing != null)
                {
                    continue;
                }

                var clone = CloneElementWithNamespace(element, targetNs);
                if (clone != null)
                {
                    targetGroup.Add(clone);
                }
            }
        }
    }

    private XElement CreateDefaultConfigurationGroup(XNamespace targetNs, string configuration, bool isDebug)
    {
        var group = new XElement(targetNs + "PropertyGroup");
        group.Add(new XAttribute("Condition", $" '$(Configuration)|$(Platform)' == '{configuration}' "));

        group.Add(new XElement(targetNs + "DebugSymbols", isDebug ? "true" : "false"));
        group.Add(new XElement(targetNs + "DebugType", isDebug ? "full" : "pdbonly"));
        group.Add(new XElement(targetNs + "Optimize", isDebug ? "false" : "true"));

        var configName = configuration.Split('|')[0];
        group.Add(new XElement(targetNs + "OutputPath", $"bin\\{configName}\\"));

        group.Add(new XElement(targetNs + "DefineConstants", isDebug ? "DEBUG;TRACE" : "TRACE"));
        group.Add(new XElement(targetNs + "ErrorReport", "prompt"));
        group.Add(new XElement(targetNs + "WarningLevel", "4"));

        return group;
    }

    private void MergeConfigurationProperties(XElement sourceRoot, XNamespace sourceNs, XElement targetGroup, string configurationKey, XNamespace targetNs)
    {
        foreach (var propertyGroup in sourceRoot.Elements(sourceNs + "PropertyGroup"))
        {
            var condition = propertyGroup.Attribute("Condition")?.Value;
            if (string.IsNullOrWhiteSpace(condition) || !condition.Contains(configurationKey, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (var element in propertyGroup.Elements())
            {
                var localName = element.Name.LocalName;
                if (IsTargetFrameworkElementName(localName))
                {
                    continue;
                }

                var existing = targetGroup.Elements(targetNs + localName).FirstOrDefault();
                if (existing != null)
                {
                    existing.Value = element.Value;
                    continue;
                }

                var clone = CloneElementWithNamespace(element, targetNs);
                if (clone != null)
                {
                    targetGroup.Add(clone);
                }
            }
        }
    }

    private void RemoveTargetFrameworkProperties(XElement propertyGroup, XNamespace targetNs)
    {
        var targets = propertyGroup.Elements()
            .Where(e => IsTargetFrameworkElementName(e.Name.LocalName))
            .ToList();

        foreach (var element in targets)
        {
            element.Remove();
        }
    }

    private XElement? CloneItemGroupForOldStyle(XElement itemGroup, XNamespace targetNs)
    {
        var clone = new XElement(targetNs + "ItemGroup");

        foreach (var attribute in itemGroup.Attributes())
        {
            if (attribute.IsNamespaceDeclaration)
            {
                continue;
            }

            clone.Add(new XAttribute(attribute.Name.LocalName, attribute.Value));
        }

        foreach (var element in itemGroup.Elements())
        {
            var localName = element.Name.LocalName;

            if (localName.Equals("PackageReference", StringComparison.OrdinalIgnoreCase)
                || localName.Equals("FrameworkReference", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var clonedElement = CloneElementWithNamespace(element, targetNs);
            if (clonedElement != null)
            {
                clone.Add(clonedElement);
            }
        }

        return clone.HasElements ? clone : null;
    }

    private void EnsureDefaultReferenceItems(XElement projectRoot, XNamespace targetNs, bool isWinForms, bool isWpf)
    {
        var existing = new HashSet<string>(projectRoot.Descendants(targetNs + "Reference")
            .Select(r => r.Attribute("Include")?.Value ?? string.Empty), StringComparer.OrdinalIgnoreCase);

        var defaults = new List<string>
        {
            "System",
            "System.Core",
            "System.Xml.Linq",
            "System.Data.DataSetExtensions",
            "Microsoft.CSharp",
            "System.Data",
            "System.Xml"
        };

        if (isWinForms)
        {
            defaults.Add("System.Windows.Forms");
            defaults.Add("System.Drawing");
        }

        if (isWpf)
        {
            defaults.Add("System.Xaml");
            defaults.Add("PresentationFramework");
            defaults.Add("PresentationCore");
            defaults.Add("WindowsBase");
        }

        var missing = defaults
            .Where(d => !string.IsNullOrWhiteSpace(d) && !existing.Contains(d))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (missing.Count == 0)
        {
            return;
        }

        var itemGroup = new XElement(targetNs + "ItemGroup");
        foreach (var reference in missing)
        {
            itemGroup.Add(new XElement(targetNs + "Reference", new XAttribute("Include", reference)));
        }

        projectRoot.Add(itemGroup);
    }

    private void EnsureFileItems(XElement projectRoot, string projectPath, XNamespace targetNs, string itemName, params string[] extensions)
    {
        if (extensions.Length == 0)
        {
            return;
        }

        var normalizedExtensions = new HashSet<string>(extensions.Select(ext =>
            ext.StartsWith('.') ? ext : $".{ext}"), StringComparer.OrdinalIgnoreCase);

        var existing = new HashSet<string>(projectRoot.Descendants(targetNs + itemName)
            .Select(e =>
            {
                var includeValue = e.Attribute("Include")?.Value;
                if (string.IsNullOrWhiteSpace(includeValue))
                {
                    includeValue = e.Attribute("Update")?.Value ?? string.Empty;
                }

                return NormalizePathForMsbuild(includeValue ?? string.Empty);
            }), StringComparer.OrdinalIgnoreCase);

        var projectDirectory = Path.GetDirectoryName(projectPath);
        if (string.IsNullOrEmpty(projectDirectory) || !Directory.Exists(projectDirectory))
        {
            return;
        }

        var files = Directory.EnumerateFiles(projectDirectory, "*", SearchOption.AllDirectories)
            .Where(path => normalizedExtensions.Contains(Path.GetExtension(path)))
            .Where(path => !IsInIgnoredDirectory(projectDirectory, path));

        var newItems = new List<XElement>();

        foreach (var file in files)
        {
            if (itemName.Equals("Compile", StringComparison.OrdinalIgnoreCase))
            {
                var fileName = Path.GetFileName(file);
                if (fileName.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase)
                    || fileName.EndsWith(".g.i.cs", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
            }

            var relative = Path.GetRelativePath(projectDirectory, file);
            var normalized = NormalizePathForMsbuild(relative);

            if (existing.Contains(normalized))
            {
                continue;
            }

            var element = new XElement(targetNs + itemName, new XAttribute("Include", normalized));
            newItems.Add(element);
        }

        if (newItems.Count == 0)
        {
            return;
        }

        var itemGroup = new XElement(targetNs + "ItemGroup");
        foreach (var element in newItems)
        {
            itemGroup.Add(element);
        }

        projectRoot.Add(itemGroup);
    }

    private void EnsureWpfItems(XElement projectRoot, string projectPath, XNamespace targetNs)
    {
        var projectDirectory = Path.GetDirectoryName(projectPath);
        if (string.IsNullOrEmpty(projectDirectory) || !Directory.Exists(projectDirectory))
        {
            return;
        }

        var existingPages = new HashSet<string>(projectRoot.Descendants(targetNs + "Page")
            .Select(e =>
            {
                var includeValue = e.Attribute("Include")?.Value;
                if (string.IsNullOrWhiteSpace(includeValue))
                {
                    includeValue = e.Attribute("Update")?.Value ?? string.Empty;
                }

                return NormalizePathForMsbuild(includeValue ?? string.Empty);
            }), StringComparer.OrdinalIgnoreCase);

        var existingAppDefs = new HashSet<string>(projectRoot.Descendants(targetNs + "ApplicationDefinition")
            .Select(e =>
            {
                var includeValue = e.Attribute("Include")?.Value;
                if (string.IsNullOrWhiteSpace(includeValue))
                {
                    includeValue = e.Attribute("Update")?.Value ?? string.Empty;
                }

                return NormalizePathForMsbuild(includeValue ?? string.Empty);
            }), StringComparer.OrdinalIgnoreCase);

        var xamlFiles = Directory.EnumerateFiles(projectDirectory, "*.xaml", SearchOption.AllDirectories)
            .Where(path => !IsInIgnoredDirectory(projectDirectory, path));

        var pageGroup = new XElement(targetNs + "ItemGroup");
        var appGroup = new XElement(targetNs + "ItemGroup");

        foreach (var file in xamlFiles)
        {
            var relative = NormalizePathForMsbuild(Path.GetRelativePath(projectDirectory, file));

            if (relative.EndsWith("App.xaml", StringComparison.OrdinalIgnoreCase))
            {
                if (!existingAppDefs.Contains(relative))
                {
                    var appDefinition = new XElement(targetNs + "ApplicationDefinition", new XAttribute("Include", relative));
                    appDefinition.Add(new XElement(targetNs + "Generator", "MSBuild:Compile"));
                    appDefinition.Add(new XElement(targetNs + "SubType", "Designer"));
                    appGroup.Add(appDefinition);
                }
            }
            else
            {
                if (!existingPages.Contains(relative))
                {
                    var pageElement = new XElement(targetNs + "Page", new XAttribute("Include", relative));
                    pageElement.Add(new XElement(targetNs + "Generator", "MSBuild:Compile"));
                    pageElement.Add(new XElement(targetNs + "SubType", "Designer"));
                    pageGroup.Add(pageElement);
                }
            }
        }

        if (appGroup.HasElements)
        {
            projectRoot.Add(appGroup);
        }

        if (pageGroup.HasElements)
        {
            projectRoot.Add(pageGroup);
        }
    }

    private bool IsInIgnoredDirectory(string projectDirectory, string filePath)
    {
        var relative = Path.GetRelativePath(projectDirectory, filePath);
        var segments = relative.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var segment in segments)
        {
            if (segment.Equals("bin", StringComparison.OrdinalIgnoreCase)
                || segment.Equals("obj", StringComparison.OrdinalIgnoreCase)
                || segment.Equals(".git", StringComparison.OrdinalIgnoreCase)
                || segment.Equals(".vs", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private string NormalizePathForMsbuild(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return path;
        }

        var normalized = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        normalized = normalized.Replace(Path.DirectorySeparatorChar, '\\');

        if (normalized.StartsWith(".\\", StringComparison.Ordinal))
        {
            normalized = normalized.Substring(2);
        }

        return normalized;
    }

    private bool IsWinFormsInSdkProject(XElement root, XNamespace ns)
    {
        // Check for UseWindowsForms property
        var useWindowsForms = root.Descendants(ns + "UseWindowsForms").FirstOrDefault();
        if (useWindowsForms != null && useWindowsForms.Value.Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private string ConvertSdkToOldStyleFrameworkVersion(string sdkTfm)
    {
        // Handle variable tokens - preserve them verbatim
        if (sdkTfm.StartsWith("$"))
        {
            return sdkTfm;
        }

        // Map SDK-style versions to Old-style
        // Examples: net45 → v4.5, net472 → v4.7.2, net48-windows → v4.8
        var trimmed = sdkTfm.Trim().ToLowerInvariant();

        // Remove -windows suffix if present
        trimmed = trimmed.Replace("-windows", "");

        if (trimmed.StartsWith("net"))
        {
            // Remove 'net' prefix
            var version = trimmed.Substring(3);

            // Add dots back
            // net45 → 4.5, net472 → 4.7.2
            if (version.Length == 2)
            {
                // net40 → v4.0, net45 → v4.5
                return $"v{version[0]}.{version[1]}";
            }
            else if (version.Length == 3)
            {
                // net403 → v4.0.3, net451 → v4.5.1, net462 → v4.6.2, net472 → v4.7.2, net481 → v4.8.1
                return $"v{version[0]}.{version[1]}.{version[2]}";
            }
            else
            {
                // Fallback - return as-is
                return sdkTfm;
            }
        }

        // If it doesn't start with 'net', return as-is
        return trimmed;
    }

    private void InitializeComboBoxSuggestions()
    {
        // Populate both ComboBoxes with common TFMs
        targetFrameworkComboBox.Items.Clear();
        appendTargetFrameworkComboBox.Items.Clear();

        foreach (var tfm in CommonTfms)
        {
            targetFrameworkComboBox.Items.Add(tfm);
            appendTargetFrameworkComboBox.Items.Add(tfm);
        }
    }

    private void UpdateComboBoxSuggestions()
    {
        // Add discovered variables to ComboBoxes
        var currentTargetItems = targetFrameworkComboBox.Items.Cast<string>().ToHashSet();
        var currentAppendItems = appendTargetFrameworkComboBox.Items.Cast<string>().ToHashSet();

        foreach (var variable in _discoveredVariables)
        {
            if (!currentTargetItems.Contains(variable))
            {
                targetFrameworkComboBox.Items.Add(variable);
            }
            if (!currentAppendItems.Contains(variable))
            {
                appendTargetFrameworkComboBox.Items.Add(variable);
            }
        }
    }

    private void OpenContainingFolder_Click(object? sender, EventArgs e)
    {
        if (projectsGridView.SelectedRows.Count == 0)
            return;

        var row = projectsGridView.SelectedRows[0];
        var filePath = row.Cells["FullPath"].Value?.ToString();

        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            MessageBox.Show("File not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        try
        {
            var folder = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(folder))
            {
                System.Diagnostics.Process.Start("explorer.exe", folder);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to open folder: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void CopyPath_Click(object? sender, EventArgs e)
    {
        if (projectsGridView.SelectedRows.Count == 0)
            return;

        var paths = new List<string>();
        foreach (DataGridViewRow row in projectsGridView.SelectedRows)
        {
            var filePath = row.Cells["FullPath"].Value?.ToString();
            if (!string.IsNullOrEmpty(filePath))
            {
                paths.Add(filePath);
            }
        }

        if (paths.Count > 0)
        {
            try
            {
                Clipboard.SetText(string.Join(Environment.NewLine, paths));
                statusLabel.Text = $"Copied {paths.Count} path(s) to clipboard";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to copy to clipboard: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void ProjectsGridView_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0)
            return;

        var row = projectsGridView.Rows[e.RowIndex];
        var filePath = row.Cells["FullPath"].Value?.ToString();

        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            MessageBox.Show("File not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to open file: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ExportCsvButton_Click(object? sender, EventArgs e)
    {
        if (projectsGridView.Rows.Count == 0)
        {
            MessageBox.Show("No data to export.", "Export CSV",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var saveFileDialog = new SaveFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            DefaultExt = "csv",
            FileName = $"csproj_report_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
        };

        if (saveFileDialog.ShowDialog() == DialogResult.OK)
        {
            try
            {
                ExportToCsv(saveFileDialog.FileName);
                statusLabel.Text = $"Exported {projectsGridView.Rows.Count} row(s) to {Path.GetFileName(saveFileDialog.FileName)}";
                MessageBox.Show($"Data exported successfully to:\n{saveFileDialog.FileName}",
                    "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to export CSV: {ex.Message}", "Export Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void ExportToCsv(string filePath)
    {
        using var writer = new StreamWriter(filePath, false, System.Text.Encoding.UTF8);

        // Write header
        var headers = new List<string>();
        foreach (DataGridViewColumn column in projectsGridView.Columns)
        {
            headers.Add(EscapeCsvField(column.HeaderText));
        }
        writer.WriteLine(string.Join(",", headers));

        // Write data
        foreach (DataGridViewRow row in projectsGridView.Rows)
        {
            var values = new List<string>();
            foreach (DataGridViewCell cell in row.Cells)
            {
                values.Add(EscapeCsvField(cell.Value?.ToString() ?? ""));
            }
            writer.WriteLine(string.Join(",", values));
        }
    }

    private string EscapeCsvField(string field)
    {
        // Escape double quotes and wrap in quotes if needed
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
        return field;
    }

    private void LoadSettings()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                var json = File.ReadAllText(SettingsFilePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);

                if (settings?.RecentDirectories != null && settings.RecentDirectories.Count > 0)
                {
                    // Populate the combobox with recent directories
                    folderPathComboBox.Items.Clear();
                    foreach (var dir in settings.RecentDirectories)
                    {
                        if (Directory.Exists(dir))
                        {
                            folderPathComboBox.Items.Add(dir);
                        }
                    }

                    // Select the most recent directory (first in list)
                    if (folderPathComboBox.Items.Count > 0)
                    {
                        folderPathComboBox.SelectedIndex = 0;
                    }
                }
            }
        }
        catch
        {
            // If settings file is corrupted or unreadable, just ignore it
            // The app will work with an empty path
        }
    }

    private void SaveSettings(string folderPath)
    {
        try
        {
            // Ensure settings directory exists
            if (!Directory.Exists(SettingsDirectory))
            {
                Directory.CreateDirectory(SettingsDirectory);
            }

            // Load existing settings or create new
            AppSettings settings;
            if (File.Exists(SettingsFilePath))
            {
                var json = File.ReadAllText(SettingsFilePath);
                settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            else
            {
                settings = new AppSettings();
            }

            // Remove the folder path if it already exists (to avoid duplicates)
            settings.RecentDirectories.RemoveAll(d => 
                string.Equals(d, folderPath, StringComparison.OrdinalIgnoreCase));

            // Add the new path at the beginning (most recent)
            settings.RecentDirectories.Insert(0, folderPath);

            // Keep only the last 10 directories
            if (settings.RecentDirectories.Count > 10)
            {
                settings.RecentDirectories = settings.RecentDirectories.Take(10).ToList();
            }

            var jsonToSave = JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(SettingsFilePath, jsonToSave);

            // Update the combobox
            folderPathComboBox.Items.Clear();
            foreach (var dir in settings.RecentDirectories)
            {
                folderPathComboBox.Items.Add(dir);
            }
            folderPathComboBox.SelectedIndex = 0;
        }
        catch
        {
            // If we can't save settings, don't fail the operation
            // Just continue without persisting
        }
    }

    private class AppSettings
    {
        public List<string> RecentDirectories { get; set; } = new List<string>();
    }

    private class NormalizedTfmSet
    {
        public bool IsVariable { get; set; }
        public string? VariableToken { get; set; }
        public List<string> Tfms { get; set; } = new List<string>();

        public override bool Equals(object? obj)
        {
            if (obj is not NormalizedTfmSet other)
                return false;

            // If one is variable and the other is not, they're not equal
            if (IsVariable != other.IsVariable)
                return false;

            // If both contain variables (IsVariable == true), compare exact lists
            if (IsVariable)
            {
                // For variables, exact match required (case-sensitive, order-sensitive)
                if (Tfms.Count != other.Tfms.Count)
                    return false;

                return Tfms.SequenceEqual(other.Tfms);
            }

            // If both are literals, compare normalized TFM lists (already sorted and lowercased)
            if (Tfms.Count != other.Tfms.Count)
                return false;

            return Tfms.SequenceEqual(other.Tfms);
        }

        public override int GetHashCode()
        {
            if (IsVariable)
            {
                int hash = IsVariable.GetHashCode();
                foreach (var tfm in Tfms)
                {
                    hash = HashCode.Combine(hash, tfm);
                }
                return hash;
            }

            int hashCode = IsVariable.GetHashCode();
            foreach (var tfm in Tfms)
            {
                hashCode = HashCode.Combine(hashCode, tfm);
            }
            return hashCode;
        }
    }

    private class TfmToken
    {
        public string Value { get; set; } = "";
        public bool IsVariable { get; set; }
    }

    private class ProjectInfo
    {
        public string FullPath { get; set; } = "";
        public string Style { get; set; } = "";
        public string TargetFrameworks { get; set; } = "";
        public string Changed { get; set; } = "";
    }
}
