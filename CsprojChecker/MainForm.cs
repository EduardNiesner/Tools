using System.Xml.Linq;
using System.Xml;
using System.Text.Json;

namespace CsprojChecker;

public partial class MainForm : Form
{
    private ComboBox folderPathComboBox = null!;
    private Button browseButton = null!;
    private Button checkCsprojButton = null!;
    private Button exportCsvButton = null!;
    private TextBox fileFilterTextBox = null!;
    private DataGridView projectsGridView = null!;
    private Label statusLabel = null!;
    private Button cancelButton = null!;
    private GroupBox frameworkOperationsGroupBox = null!;
    private GroupBox projectStyleGroupBox = null!;

    // Framework operations controls
    private Button changeTargetFrameworkButton = null!;
    private ComboBox targetFrameworkComboBox = null!;
    private Button appendTargetFrameworkButton = null!;
    private ComboBox appendTargetFrameworkComboBox = null!;

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
        this.Size = new Size(1000, 700);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.MinimumSize = new Size(800, 600);

        // File filter TextBox
        fileFilterTextBox = new TextBox
        {
            Location = new Point(20, 20),
            Size = new Size(940, 23),
            Name = "fileFilterTextBox",
            PlaceholderText = "Filter files by path...",
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        fileFilterTextBox.TextChanged += FileFilterTextBox_TextChanged;

        // Folder path ComboBox
        folderPathComboBox = new ComboBox
        {
            Location = new Point(20, 53),
            Size = new Size(600, 23),
            Name = "folderPathComboBox",
            DropDownStyle = ComboBoxStyle.DropDown,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        // Browse Button (height increased by 25%)
        browseButton = new Button
        {
            Location = new Point(630, 52),
            Size = new Size(100, 31),
            Text = "Browse",
            Name = "browseButton",
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        browseButton.Click += BrowseButton_Click;

        // Check for csproj files Button (height increased by 25%)
        checkCsprojButton = new Button
        {
            Location = new Point(740, 52),
            Size = new Size(140, 31),
            Text = "Check for csproj files",
            Name = "checkCsprojButton",
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        checkCsprojButton.Click += CheckCsprojButton_Click;

        // Export to CSV Button
        exportCsvButton = new Button
        {
            Location = new Point(890, 52),
            Size = new Size(70, 31),
            Text = "Export CSV",
            Name = "exportCsvButton",
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        exportCsvButton.Click += ExportCsvButton_Click;

        // DataGridView
        projectsGridView = new DataGridView
        {
            Location = new Point(20, 93),
            Size = new Size(940, 217),
            Name = "projectsGridView",
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
        };
        projectsGridView.SelectionChanged += ProjectsGridView_SelectionChanged;

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

        // Framework operations GroupBox
        frameworkOperationsGroupBox = new GroupBox
        {
            Location = new Point(20, 320),
            Size = new Size(460, 160),
            Text = "Framework Operations",
            Name = "frameworkOperationsGroupBox",
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left
        };

        // Change target framework button
        changeTargetFrameworkButton = new Button
        {
            Location = new Point(10, 25),
            Size = new Size(200, 31),
            Text = "Change target framework",
            Name = "changeTargetFrameworkButton",
            Enabled = false
        };
        changeTargetFrameworkButton.Click += ChangeTargetFrameworkButton_Click;

        // Target framework ComboBox
        targetFrameworkComboBox = new ComboBox
        {
            Location = new Point(10, 65),
            Size = new Size(435, 23),
            Name = "targetFrameworkComboBox",
            DropDownStyle = ComboBoxStyle.DropDown,
            Enabled = false
        };

        // Append target framework button
        appendTargetFrameworkButton = new Button
        {
            Location = new Point(10, 95),
            Size = new Size(200, 31),
            Text = "Append target framework",
            Name = "appendTargetFrameworkButton",
            Enabled = false
        };
        appendTargetFrameworkButton.Click += AppendTargetFrameworkButton_Click;

        // Append target framework ComboBox
        appendTargetFrameworkComboBox = new ComboBox
        {
            Location = new Point(220, 99),
            Size = new Size(225, 23),
            Name = "appendTargetFrameworkComboBox",
            DropDownStyle = ComboBoxStyle.DropDown,
            Enabled = false
        };

        // Initialize ComboBox suggestions
        InitializeComboBoxSuggestions();

        frameworkOperationsGroupBox.Controls.Add(changeTargetFrameworkButton);
        frameworkOperationsGroupBox.Controls.Add(targetFrameworkComboBox);
        frameworkOperationsGroupBox.Controls.Add(appendTargetFrameworkButton);
        frameworkOperationsGroupBox.Controls.Add(appendTargetFrameworkComboBox);

        // Project style conversions GroupBox
        projectStyleGroupBox = new GroupBox
        {
            Location = new Point(500, 320),
            Size = new Size(460, 160),
            Text = "Project Style Conversions",
            Name = "projectStyleGroupBox",
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };

        // Convert to SDK-style button
        convertToSdkButton = new Button
        {
            Location = new Point(10, 25),
            Size = new Size(250, 31),
            Text = "Convert Old-style → SDK",
            Name = "convertToSdkButton",
            Enabled = false
        };
        convertToSdkButton.Click += ConvertToSdkButton_Click;
        projectStyleGroupBox.Controls.Add(convertToSdkButton);

        // Convert to Old-style button
        convertToOldStyleButton = new Button
        {
            Location = new Point(10, 65),
            Size = new Size(250, 31),
            Text = "Convert SDK → Old-style",
            Name = "convertToOldStyleButton",
            Enabled = false
        };
        convertToOldStyleButton.Click += ConvertToOldStyleButton_Click;
        projectStyleGroupBox.Controls.Add(convertToOldStyleButton);

        // Status Label
        statusLabel = new Label
        {
            Location = new Point(20, 490),
            Size = new Size(840, 23),
            Text = "Ready",
            Name = "statusLabel",
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };

        // Cancel Button (height increased by 25%)
        cancelButton = new Button
        {
            Location = new Point(860, 488),
            Size = new Size(100, 31),
            Text = "Cancel",
            Name = "cancelButton",
            Enabled = false,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right
        };
        cancelButton.Click += CancelButton_Click;

        // Add controls to form
        this.Controls.Add(fileFilterTextBox);
        this.Controls.Add(folderPathComboBox);
        this.Controls.Add(browseButton);
        this.Controls.Add(checkCsprojButton);
        this.Controls.Add(exportCsvButton);
        this.Controls.Add(projectsGridView);
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

            // Detect if it's a WinForms project
            bool isWinForms = DetectWinFormsProject(root, ns);

            // Convert framework version
            string newTfm = ConvertFrameworkVersion(oldTfm, isWinForms);

            // Create new SDK-style project
            var newRoot = new XElement("Project");

            // Determine SDK attribute
            if (isWinForms)
            {
                newRoot.Add(new XAttribute("Sdk", "Microsoft.NET.Sdk"));
            }
            else
            {
                newRoot.Add(new XAttribute("Sdk", "Microsoft.NET.Sdk"));
            }

            // Create PropertyGroup with essential properties
            var propertyGroup = new XElement("PropertyGroup");

            // Add TargetFramework
            propertyGroup.Add(new XElement("TargetFramework", newTfm));

            // Add OutputType if needed
            var outputType = root.Descendants(ns + "OutputType").FirstOrDefault();
            if (outputType != null)
            {
                propertyGroup.Add(new XElement("OutputType", outputType.Value));
            }

            // Add WinForms specific properties if needed
            if (isWinForms)
            {
                propertyGroup.Add(new XElement("UseWindowsForms", "true"));
            }

            // Add common properties
            propertyGroup.Add(new XElement("ImplicitUsings", "enable"));
            propertyGroup.Add(new XElement("Nullable", "enable"));

            // Copy over RootNamespace if it exists
            var rootNamespace = root.Descendants(ns + "RootNamespace").FirstOrDefault();
            if (rootNamespace != null)
            {
                propertyGroup.Add(new XElement("RootNamespace", rootNamespace.Value));
            }

            // Copy over AssemblyName if it exists
            var assemblyName = root.Descendants(ns + "AssemblyName").FirstOrDefault();
            if (assemblyName != null)
            {
                propertyGroup.Add(new XElement("AssemblyName", assemblyName.Value));
            }

            newRoot.Add(propertyGroup);

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

    private string ConvertFrameworkVersion(string oldTfm, bool isWinForms)
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

            // For .NET Framework 4.x versions on WinForms, add -windows suffix
            if (isWinForms && version.StartsWith("4"))
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

            // Detect if it's a WinForms project
            bool isWinForms = IsWinFormsInSdkProject(root, ns);

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
            propertyGroup.Add(new XElement(msbuildNs + "ProjectGuid", "{" + Guid.NewGuid().ToString().ToUpper() + "}"));

            // Add OutputType
            var outputType = root.Descendants(ns + "OutputType").FirstOrDefault();
            if (outputType != null)
            {
                propertyGroup.Add(new XElement(msbuildNs + "OutputType", outputType.Value));
            }
            else
            {
                propertyGroup.Add(new XElement(msbuildNs + "OutputType", "Library"));
            }

            // Add RootNamespace
            var rootNamespace = root.Descendants(ns + "RootNamespace").FirstOrDefault();
            if (rootNamespace != null)
            {
                propertyGroup.Add(new XElement(msbuildNs + "RootNamespace", rootNamespace.Value));
            }
            else
            {
                // Use file name without extension
                propertyGroup.Add(new XElement(msbuildNs + "RootNamespace", Path.GetFileNameWithoutExtension(filePath)));
            }

            // Add AssemblyName
            var assemblyName = root.Descendants(ns + "AssemblyName").FirstOrDefault();
            if (assemblyName != null)
            {
                propertyGroup.Add(new XElement(msbuildNs + "AssemblyName", assemblyName.Value));
            }
            else
            {
                // Use file name without extension
                propertyGroup.Add(new XElement(msbuildNs + "AssemblyName", Path.GetFileNameWithoutExtension(filePath)));
            }

            // Add TargetFrameworkVersion
            propertyGroup.Add(new XElement(msbuildNs + "TargetFrameworkVersion", newTfm));

            // Add FileAlignment
            propertyGroup.Add(new XElement(msbuildNs + "FileAlignment", "512"));

            // Add Deterministic
            propertyGroup.Add(new XElement(msbuildNs + "Deterministic", "true"));

            newRoot.Add(propertyGroup);

            // Add Debug PropertyGroup
            var debugPropertyGroup = new XElement(msbuildNs + "PropertyGroup");
            debugPropertyGroup.Add(new XAttribute("Condition", " '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "));
            debugPropertyGroup.Add(new XElement(msbuildNs + "DebugSymbols", "true"));
            debugPropertyGroup.Add(new XElement(msbuildNs + "DebugType", "full"));
            debugPropertyGroup.Add(new XElement(msbuildNs + "Optimize", "false"));
            debugPropertyGroup.Add(new XElement(msbuildNs + "OutputPath", @"bin\Debug\"));
            debugPropertyGroup.Add(new XElement(msbuildNs + "DefineConstants", "DEBUG;TRACE"));
            debugPropertyGroup.Add(new XElement(msbuildNs + "ErrorReport", "prompt"));
            debugPropertyGroup.Add(new XElement(msbuildNs + "WarningLevel", "4"));
            newRoot.Add(debugPropertyGroup);

            // Add Release PropertyGroup
            var releasePropertyGroup = new XElement(msbuildNs + "PropertyGroup");
            releasePropertyGroup.Add(new XAttribute("Condition", " '$(Configuration)|$(Platform)' == 'Release|AnyCPU' "));
            releasePropertyGroup.Add(new XElement(msbuildNs + "DebugType", "pdbonly"));
            releasePropertyGroup.Add(new XElement(msbuildNs + "Optimize", "true"));
            releasePropertyGroup.Add(new XElement(msbuildNs + "OutputPath", @"bin\Release\"));
            releasePropertyGroup.Add(new XElement(msbuildNs + "DefineConstants", "TRACE"));
            releasePropertyGroup.Add(new XElement(msbuildNs + "ErrorReport", "prompt"));
            releasePropertyGroup.Add(new XElement(msbuildNs + "WarningLevel", "4"));
            newRoot.Add(releasePropertyGroup);

            // Add References ItemGroup
            var referencesGroup = new XElement(msbuildNs + "ItemGroup");
            referencesGroup.Add(new XElement(msbuildNs + "Reference", new XAttribute("Include", "System")));
            referencesGroup.Add(new XElement(msbuildNs + "Reference", new XAttribute("Include", "System.Core")));
            referencesGroup.Add(new XElement(msbuildNs + "Reference", new XAttribute("Include", "System.Xml.Linq")));
            referencesGroup.Add(new XElement(msbuildNs + "Reference", new XAttribute("Include", "System.Data.DataSetExtensions")));
            referencesGroup.Add(new XElement(msbuildNs + "Reference", new XAttribute("Include", "Microsoft.CSharp")));
            referencesGroup.Add(new XElement(msbuildNs + "Reference", new XAttribute("Include", "System.Data")));
            referencesGroup.Add(new XElement(msbuildNs + "Reference", new XAttribute("Include", "System.Xml")));

            // Add WinForms references if needed
            if (isWinForms)
            {
                referencesGroup.Add(new XElement(msbuildNs + "Reference", new XAttribute("Include", "System.Drawing")));
                referencesGroup.Add(new XElement(msbuildNs + "Reference", new XAttribute("Include", "System.Windows.Forms")));
            }

            newRoot.Add(referencesGroup);

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
