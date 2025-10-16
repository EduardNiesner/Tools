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
    
    // Framework operations controls
    private Button changeTargetFrameworkButton = null!;
    private ComboBox targetFrameworkComboBox = null!;
    private Button appendTargetFrameworkButton = null!;
    private ComboBox appendTargetFrameworkComboBox = null!;
    
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
        this.MinimumSize = new Size(800, 600);
        
        // Folder path TextBox
        folderPathTextBox = new TextBox
        {
            Location = new Point(20, 20),
            Size = new Size(600, 23),
            Name = "folderPathTextBox",
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        
        // Browse Button (height increased by 25%)
        browseButton = new Button
        {
            Location = new Point(630, 19),
            Size = new Size(100, 31),
            Text = "Browse",
            Name = "browseButton",
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        browseButton.Click += BrowseButton_Click;
        
        // Check for csproj files Button (height increased by 25%)
        checkCsprojButton = new Button
        {
            Location = new Point(740, 19),
            Size = new Size(200, 31),
            Text = "Check for csproj files",
            Name = "checkCsprojButton",
            Anchor = AnchorStyles.Top | AnchorStyles.Right
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
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
        };
        projectsGridView.SelectionChanged += ProjectsGridView_SelectionChanged;
        
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
            Location = new Point(220, 95),
            Size = new Size(225, 23),
            Name = "appendTargetFrameworkComboBox",
            DropDownStyle = ComboBoxStyle.DropDown,
            Enabled = false
        };
        
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
            return targetFrameworksElement.Value.Trim();
        }
        
        // Look for TargetFramework (singular)
        var targetFrameworkElement = root.Descendants(ns + "TargetFramework").FirstOrDefault();
        if (targetFrameworkElement != null)
        {
            return targetFrameworkElement.Value.Trim();
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
    
    private void ProjectsGridView_SelectionChanged(object? sender, EventArgs e)
    {
        UpdateFrameworkOperationsState();
    }
    
    private void ChangeTargetFrameworkButton_Click(object? sender, EventArgs e)
    {
        // Placeholder for actual implementation
        MessageBox.Show("Change target framework functionality will be implemented in a future step.", 
            "Not Implemented", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                
                successCount++;
                results.Add($"✓ {Path.GetFileName(filePath)}: {currentTfms} → {newTfms}");
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
    
    private void UpdateFrameworkOperationsState()
    {
        if (projectsGridView.SelectedRows.Count == 0)
        {
            changeTargetFrameworkButton.Enabled = false;
            targetFrameworkComboBox.Enabled = false;
            targetFrameworkComboBox.Text = "";
            appendTargetFrameworkButton.Enabled = false;
            appendTargetFrameworkComboBox.Enabled = false;
            return;
        }
        
        // Collect TFM values from all selected rows
        List<string> tfmValues = new List<string>();
        bool allSdkStyle = true;
        
        foreach (DataGridViewRow row in projectsGridView.SelectedRows)
        {
            if (row.Cells["TargetFrameworks"].Value is string tfmValue)
            {
                tfmValues.Add(tfmValue);
            }
            
            if (row.Cells["Style"].Value is string styleValue && styleValue != "SDK")
            {
                allSdkStyle = false;
            }
        }
        
        if (tfmValues.Count == 0)
        {
            changeTargetFrameworkButton.Enabled = false;
            targetFrameworkComboBox.Enabled = false;
            targetFrameworkComboBox.Text = "";
            appendTargetFrameworkButton.Enabled = false;
            appendTargetFrameworkComboBox.Enabled = false;
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
            var doc = XDocument.Load(filePath);
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
                if (targetFrameworksElement != null)
                {
                    // Update existing TargetFrameworks
                    targetFrameworksElement.Value = newTfms;
                }
                else if (targetFrameworkElement != null)
                {
                    // Convert TargetFramework to TargetFrameworks
                    targetFrameworkElement.Name = ns + "TargetFrameworks";
                    targetFrameworkElement.Value = newTfms;
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
                // Single framework - use TargetFramework (singular) or keep TargetFrameworks if it already exists
                if (targetFrameworksElement != null)
                {
                    // Keep it as TargetFrameworks even for single value
                    targetFrameworksElement.Value = newTfms;
                }
                else if (targetFrameworkElement != null)
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
            
            doc.Save(filePath);
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
