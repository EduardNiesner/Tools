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
        
        // Browse Button
        browseButton = new Button
        {
            Location = new Point(630, 19),
            Size = new Size(100, 25),
            Text = "Browse",
            Name = "browseButton"
        };
        browseButton.Click += BrowseButton_Click;
        
        // Check for csproj files Button
        checkCsprojButton = new Button
        {
            Location = new Point(740, 19),
            Size = new Size(200, 25),
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
        
        // Cancel Button
        cancelButton = new Button
        {
            Location = new Point(860, 478),
            Size = new Size(100, 25),
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
        // Placeholder - no business logic yet
    }
    
    private void CheckCsprojButton_Click(object? sender, EventArgs e)
    {
        // Placeholder - no business logic yet
    }
    
    private void CancelButton_Click(object? sender, EventArgs e)
    {
        // Placeholder - no business logic yet
    }
}
