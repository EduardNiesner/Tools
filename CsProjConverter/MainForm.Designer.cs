namespace CsProjConverter;

partial class MainForm
{
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        this.components = new System.ComponentModel.Container();
        this.pathSelectionGroupBox = new GroupBox();
        this.pathLabel = new Label();
        this.folderPathComboBox = new ComboBox();
        this.browseButton = new Button();
        this.checkCsprojButton = new Button();
        this.exportCsvButton = new Button();
        this.filterResultsGroupBox = new GroupBox();
        this.filterLabel = new Label();
        this.fileFilterTextBox = new TextBox();
        this.projectsGridView = new DataGridView();
        this.frameworkOperationsGroupBox = new GroupBox();
        this.targetFrameworkLabel = new Label();
        this.targetFrameworkComboBox = new ComboBox();
        this.changeTargetFrameworkButton = new Button();
        this.appendTargetFrameworkLabel = new Label();
        this.appendTargetFrameworkComboBox = new ComboBox();
        this.appendTargetFrameworkButton = new Button();
        this.projectStyleGroupBox = new GroupBox();
        this.ConvertToSdkRoundTripButton = new Button();
        this.ConvertToSdkOneWayButton = new Button();
        this.ConvertToSdkCustomModernButton = new Button();
        this.convertToOldStyleButton = new Button();
        this.statusLabel = new Label();
        this.cancelButton = new Button();
        this.toolTip = new ToolTip(this.components);
        this.pathSelectionGroupBox.SuspendLayout();
        this.filterResultsGroupBox.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this.projectsGridView)).BeginInit();
        this.frameworkOperationsGroupBox.SuspendLayout();
        this.projectStyleGroupBox.SuspendLayout();
        this.SuspendLayout();

        // Form settings
        this.Text = "CsProjConverter";
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

        // Convert to SDK-style (round-trip) button
        ConvertToSdkRoundTripButton = new Button
        {
            Location = new Point(10, 30),
            Size = new Size(340, 27),
            Text = "Convert Old-style → SDK (round-trip)",
            Name = "convertToSdkRoundTripButton",
            Enabled = false,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            TabIndex = 0
        };
        ConvertToSdkRoundTripButton.Click += ConvertToSdkRoundTripButton_Click;
        ConvertToSdkRoundTripButton.AccessibleName = "Convert to SDK Style (Round-trip)";
        ConvertToSdkRoundTripButton.AccessibleDescription = "Convert selected old-style projects to SDK-style format with round-trip compatibility";
        toolTip.SetToolTip(ConvertToSdkRoundTripButton, "Converts to SDK format while keeping compatibility for converting back to old-style later.");
        projectStyleGroupBox.Controls.Add(ConvertToSdkRoundTripButton);

        // Convert to SDK-style (one-way modern) button
        ConvertToSdkOneWayButton = new Button
        {
            Location = new Point(10, 65),
            Size = new Size(340, 27),
            Text = "Convert Old-style → SDK (one-way modern)",
            Name = "convertToSdkOneWayButton",
            Enabled = false,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            TabIndex = 1
        };
        ConvertToSdkOneWayButton.Click += ConvertToSdkOneWayButton_Click;
        ConvertToSdkOneWayButton.AccessibleName = "Convert to Modern SDK Style";
        ConvertToSdkOneWayButton.AccessibleDescription = "Convert selected old-style projects to modern SDK-style format";
        toolTip.SetToolTip(ConvertToSdkOneWayButton, "Creates a clean modern SDK project. Not intended to be converted back.");
        projectStyleGroupBox.Controls.Add(ConvertToSdkOneWayButton);

        // Convert to SDK-style (custom one-way modern) button
        ConvertToSdkCustomModernButton = new Button
        {
            Location = new Point(10, 100),
            Size = new Size(340, 27),
            Text = "Custom One-Way (Modern)",
            Name = "convertToSdkCustomModernButton",
            Enabled = false,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            TabIndex = 2
        };
        ConvertToSdkCustomModernButton.Click += ConvertToSdkCustomModernButton_Click;
        ConvertToSdkCustomModernButton.AccessibleName = "Convert to Custom Modern SDK Style";
        ConvertToSdkCustomModernButton.AccessibleDescription = "Convert selected old-style projects to custom modern SDK-style format with explicit includes";
        toolTip.SetToolTip(ConvertToSdkCustomModernButton, "Converts to modern SDK format with explicit file includes and preserved build settings.");
        projectStyleGroupBox.Controls.Add(ConvertToSdkCustomModernButton);

        // Convert to Old-style button
        convertToOldStyleButton = new Button
        {
            Location = new Point(10, 135),
            Size = new Size(340, 27),
            Text = "Convert SDK → Old-style",
            Name = "convertToOldStyleButton",
            Enabled = false,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            TabIndex = 3
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

        this.pathSelectionGroupBox.ResumeLayout(false);
        this.filterResultsGroupBox.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)(this.projectsGridView)).EndInit();
        this.frameworkOperationsGroupBox.ResumeLayout(false);
        this.projectStyleGroupBox.ResumeLayout(false);
        this.ResumeLayout(false);
    }

    #endregion

    private GroupBox pathSelectionGroupBox;
    private Label pathLabel;
    private ComboBox folderPathComboBox;
    private Button browseButton;
    private Button checkCsprojButton;
    private Button exportCsvButton;
    private GroupBox filterResultsGroupBox;
    private Label filterLabel;
    private TextBox fileFilterTextBox;
    private DataGridView projectsGridView;
    private Label statusLabel;
    private Button cancelButton;
    private GroupBox frameworkOperationsGroupBox;
    private GroupBox projectStyleGroupBox;
    private Label targetFrameworkLabel;
    private ComboBox targetFrameworkComboBox;
    private Button changeTargetFrameworkButton;
    private Label appendTargetFrameworkLabel;
    private ComboBox appendTargetFrameworkComboBox;
    private Button appendTargetFrameworkButton;
    private Button ConvertToSdkRoundTripButton;
    private Button ConvertToSdkOneWayButton;
    private Button ConvertToSdkCustomModernButton;
    private Button convertToOldStyleButton;
    private ToolTip toolTip;
}
