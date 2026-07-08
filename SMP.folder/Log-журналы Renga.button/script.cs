using Renga;
using System;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;

try
{
    // Initialize and show the journal analyzer form as modeless (non-blocking) and TopMost
    var form = new LogAnalyzerForm(RengaApp);
    form.TopMost = true; // Floating on top of Renga window
    form.Show(); // Modeless window! User can interact with Renga freely
}
catch (Exception ex)
{
    Print("Ошибка во время выполнения плагина: " + ex.Message + "\n" + ex.StackTrace);
}

// ============================================================================
// ВСЕ КЛАССЫ И СТРУКТУРЫ ОПРЕДЕЛЯЮТСЯ В КОНЦЕ ФАЙЛА ДЛЯ ROSLYN CSX СОВМЕСТИМОСТИ
// ============================================================================

public class JournalEntry
{
    public string Timestamp { get; set; }
    public string User { get; set; }
    public string ObjectName { get; set; }
    public string ObjectGuid { get; set; }
    public string Action { get; set; }
    public string Status { get; set; }
    public string Message { get; set; }
    public string RawLine { get; set; }
}

public class AppLogEntry
{
    public string Timestamp { get; set; }
    public string Type { get; set; }
    public string Message { get; set; }
    public string RawLine { get; set; }
}

public class ServerLogEntry
{
    public string Timestamp { get; set; }
    public string Component { get; set; }
    public string User { get; set; }
    public string Status { get; set; }
    public string Type { get; set; }
    public string Message { get; set; }
    public string RawLine { get; set; }
}

public class LogFileItem
{
    public string FilePath { get; set; }
    public string DisplayName { get; set; }
    
    public override string ToString()
    {
        return DisplayName;
    }
}

public class LogAnalyzerForm : Form
{
    private IApplication rengaApp;
    private string currentLogFilePath = "";
    private List<JournalEntry> loadedJournalEntries = new List<JournalEntry>();
    private List<AppLogEntry> loadedAppLogEntries = new List<AppLogEntry>();
    private List<ServerLogEntry> loadedServerLogEntries = new List<ServerLogEntry>();
    private bool isShowInModelActionable = false;

    // UI Controls
    private ComboBox cmbLogType;
    private ComboBox cmbLogFiles;
    private Label lblLogFile;
    private Button btnBrowse;
    
    private GroupBox grpFilters;
    private TextBox txtSearch;
    private ComboBox cmbUser;
    private ComboBox cmbAction;
    private ComboBox cmbStatus;
    private Label lblUser;
    private Label lblAction;
    private Label lblStatus;
    private Button btnClearFilters;

    private DataGridView gridLogs;
    private Label lblLoading;
    private GroupBox grpDetails;
    private TextBox txtDetailText;
    private Button btnShowInModel;
    private Button btnClose;
    private string formBaseTitle;

    public LogAnalyzerForm(IApplication app)
    {
        this.rengaApp = app;
        InitializeComponents();
        
        // Default index
        cmbLogType.SelectedIndex = 0; // Trigger load
    }

    private void InitializeComponents()
    {
        // Get active project SessionId directly using SDK JournalPath property!
        string sessionId = "";
        try
        {
            if (rengaApp.Project != null && !string.IsNullOrEmpty(rengaApp.Project.JournalPath))
            {
                sessionId = Path.GetFileNameWithoutExtension(rengaApp.Project.JournalPath);
            }
        }
        catch {}

        // Enable DPI Scaling
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;

        // 1. Form styling (Resizable and Maximizable)
        this.Text = string.IsNullOrEmpty(sessionId) 
            ? "Анализатор журналов Renga | Текущий SessionId: не опубликован / не сохранен" 
            : $"Анализатор журналов Renga | Текущий SessionId: {sessionId}";
        formBaseTitle = this.Text;
            
        this.Size = new System.Drawing.Size(1200, 860);
        this.MinimumSize = new System.Drawing.Size(1200, 860); // Restricts resizing below layout-safe limits!
        this.BackColor = System.Drawing.Color.White;
        this.Font = new System.Drawing.Font("Segoe UI", 9);
        this.FormBorderStyle = FormBorderStyle.Sizable; // Resizable!
        this.MaximizeBox = true; // Maximizable!
        this.MinimizeBox = true;
        this.StartPosition = FormStartPosition.CenterScreen;

        // 2. Log Type and File Selection
        var lblLogType = new Label { Text = "Тип журнала:", Location = new System.Drawing.Point(15, 20), Size = new System.Drawing.Size(90, 20) };
        this.Controls.Add(lblLogType);

        cmbLogType = new ComboBox
        {
            Location = new System.Drawing.Point(110, 16),
            Size = new System.Drawing.Size(320, 25),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new System.Drawing.Font("Segoe UI", 9.5f)
        };
        cmbLogType.Items.Add("Журнал проекта (изменения объектов)");
        cmbLogType.Items.Add("Журнал приложения (AecApp.log)");
        cmbLogType.Items.Add("Журнал подключений сервера (RengaServer.log)");
        cmbLogType.SelectedIndexChanged += cmbLogType_SelectedIndexChanged;
        this.Controls.Add(cmbLogType);

        lblLogFile = new Label { Text = "Файл журнала:", Location = new System.Drawing.Point(435, 20), Size = new System.Drawing.Size(100, 20) };
        this.Controls.Add(lblLogFile);

        cmbLogFiles = new ComboBox
        {
            Location = new System.Drawing.Point(540, 16),
            Size = new System.Drawing.Size(380, 25),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new System.Drawing.Font("Segoe UI", 9.5f),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right // grows horizontally
        };
        cmbLogFiles.SelectedIndexChanged += cmbLogFiles_SelectedIndexChanged;
        this.Controls.Add(cmbLogFiles);

        btnBrowse = new Button
        {
            Text = "Обзор...",
            Location = new System.Drawing.Point(1015, 15),
            Size = new System.Drawing.Size(150, 28),
            FlatStyle = FlatStyle.Flat,
            Anchor = AnchorStyles.Top | AnchorStyles.Right // stays on right
        };
        btnBrowse.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(200, 208, 219);
        btnBrowse.ForeColor = System.Drawing.Color.FromArgb(74, 85, 104);
        btnBrowse.Click += btnBrowse_Click;
        this.Controls.Add(btnBrowse);

        // 3. Filters Section
        grpFilters = new GroupBox { Text = "Фильтры", Location = new System.Drawing.Point(15, 55), Size = new System.Drawing.Size(1155, 75), BackColor = System.Drawing.Color.White, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
        this.Controls.Add(grpFilters);

        var lblSearch = new Label { Text = "Поиск:", Location = new System.Drawing.Point(15, 22), Size = new System.Drawing.Size(50, 18) };
        grpFilters.Controls.Add(lblSearch);

        txtSearch = new TextBox
        {
            Location = new System.Drawing.Point(15, 42),
            Size = new System.Drawing.Size(240, 23),
            Font = new System.Drawing.Font("Segoe UI", 10)
        };
        txtSearch.TextChanged += (s, e) => ApplyFilters();
        grpFilters.Controls.Add(txtSearch);

        lblUser = new Label { Text = "Пользователь:", Location = new System.Drawing.Point(280, 22), Size = new System.Drawing.Size(100, 18) };
        grpFilters.Controls.Add(lblUser);

        cmbUser = new ComboBox
        {
            Location = new System.Drawing.Point(280, 42),
            Size = new System.Drawing.Size(180, 23),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new System.Drawing.Font("Segoe UI", 9.5f)
        };
        cmbUser.SelectedIndexChanged += (s, e) => ApplyFilters();
        grpFilters.Controls.Add(cmbUser);

        lblAction = new Label { Text = "Действие:", Location = new System.Drawing.Point(475, 22), Size = new System.Drawing.Size(100, 18) };
        grpFilters.Controls.Add(lblAction);

        cmbAction = new ComboBox
        {
            Location = new System.Drawing.Point(475, 42),
            Size = new System.Drawing.Size(180, 23),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new System.Drawing.Font("Segoe UI", 9.5f)
        };
        cmbAction.SelectedIndexChanged += (s, e) => ApplyFilters();
        grpFilters.Controls.Add(cmbAction);

        lblStatus = new Label { Text = "Статус:", Location = new System.Drawing.Point(670, 22), Size = new System.Drawing.Size(90, 18) };
        grpFilters.Controls.Add(lblStatus);

        cmbStatus = new ComboBox
        {
            Location = new System.Drawing.Point(670, 42),
            Size = new System.Drawing.Size(200, 23),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new System.Drawing.Font("Segoe UI", 9.5f)
        };
        cmbStatus.SelectedIndexChanged += (s, e) => ApplyFilters();
        grpFilters.Controls.Add(cmbStatus);

        btnClearFilters = new Button
        {
            Text = "Сбросить",
            Location = new System.Drawing.Point(965, 38),
            Size = new System.Drawing.Size(170, 28),
            FlatStyle = FlatStyle.Flat,
            Anchor = AnchorStyles.Top | AnchorStyles.Right // stays on right of filters
        };
        btnClearFilters.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(200, 208, 219);
        btnClearFilters.ForeColor = System.Drawing.Color.FromArgb(74, 85, 104);
        btnClearFilters.Click += btnClearFilters_Click;
        grpFilters.Controls.Add(btnClearFilters);

        // 4. DataGridView Logs
        gridLogs = new DataGridView
        {
            Location = new System.Drawing.Point(15, 140),
            Size = new System.Drawing.Size(1155, 410),
            BackgroundColor = System.Drawing.Color.White,
            GridColor = System.Drawing.Color.FromArgb(240, 240, 240),
            BorderStyle = BorderStyle.FixedSingle,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            ReadOnly = true,
            MultiSelect = false,
            EnableHeadersVisualStyles = false,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right // resizable grid!
        };
        
        // Header Default Styles
        gridLogs.ColumnHeadersDefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(248, 249, 250);
        gridLogs.ColumnHeadersDefaultCellStyle.ForeColor = System.Drawing.Color.FromArgb(74, 85, 104);
        gridLogs.ColumnHeadersDefaultCellStyle.Font = new System.Drawing.Font("Segoe UI", 9, System.Drawing.FontStyle.Bold);
        gridLogs.ColumnHeadersHeight = 30;
        gridLogs.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

        // PREVENT header selection blue highlight!
        gridLogs.ColumnHeadersDefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(248, 249, 250);
        gridLogs.ColumnHeadersDefaultCellStyle.SelectionForeColor = System.Drawing.Color.FromArgb(74, 85, 104);

        // Rows Default Styles
        gridLogs.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(230, 242, 255);
        gridLogs.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.Black;
        gridLogs.DefaultCellStyle.Font = new System.Drawing.Font("Segoe UI", 9.5f);

        gridLogs.SelectionChanged += gridLogs_SelectionChanged;
        gridLogs.DoubleClick += btnShowInModel_Click;
        this.Controls.Add(gridLogs);

        // Loading label (sits behind the grid, visible when grid is hidden)
        lblLoading = new Label
        {
            Text = "",
            Font = new System.Drawing.Font("Segoe UI", 10),
            ForeColor = System.Drawing.Color.FromArgb(120, 130, 150),
            BackColor = System.Drawing.Color.FromArgb(245, 247, 250),
            TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
            Location = gridLogs.Location,
            Size = gridLogs.Size,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };
        this.Controls.Add(lblLoading);
        lblLoading.SendToBack();

        // 5. Details Section (Anchored to the bottom)
        grpDetails = new GroupBox { Text = "Детали выбранной записи", Location = new System.Drawing.Point(15, 565), Size = new System.Drawing.Size(1155, 240), BackColor = System.Drawing.Color.White, Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };
        this.Controls.Add(grpDetails);

        txtDetailText = new TextBox
        {
            Location = new System.Drawing.Point(15, 20),
            Size = new System.Drawing.Size(890, 205),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            ReadOnly = true,
            BackColor = System.Drawing.Color.FromArgb(250, 252, 254),
            BorderStyle = BorderStyle.FixedSingle,
            Font = new System.Drawing.Font("Segoe UI", 11),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right // grows inside groupbox
        };
        grpDetails.Controls.Add(txtDetailText);

        btnShowInModel = new Button
        {
            Text = "Показать в модели",
            Location = new System.Drawing.Point(930, 120),
            Size = new System.Drawing.Size(210, 45),
            FlatStyle = FlatStyle.Flat,
            Font = new System.Drawing.Font("Segoe UI", 9, System.Drawing.FontStyle.Bold),
            Anchor = AnchorStyles.Top | AnchorStyles.Right // stays on top right inside details
        };
        btnShowInModel.FlatAppearance.BorderSize = 0;
        btnShowInModel.Click += btnShowInModel_Click;
        grpDetails.Controls.Add(btnShowInModel);
        
        // Initialize disabled appearance (gray on gray with text fully readable)
        SetShowInModelEnabled(false);

        btnClose = new Button
        {
            Text = "Закрыть",
            Location = new System.Drawing.Point(930, 180),
            Size = new System.Drawing.Size(210, 45),
            FlatStyle = FlatStyle.Flat,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right // stays on bottom right inside details
        };
        btnClose.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(200, 208, 219);
        btnClose.ForeColor = System.Drawing.Color.FromArgb(74, 85, 104);
        btnClose.Click += btnClose_Click;
        grpDetails.Controls.Add(btnClose);
    }

    private void ShowLoading(string text)
    {
        this.Text = formBaseTitle + "  ⏳ " + text;
        this.Cursor = Cursors.WaitCursor;
        lblLoading.Text = text;
        gridLogs.Visible = false;
        lblLoading.Refresh();
    }

    private void HideLoading()
    {
        this.Text = formBaseTitle;
        this.Cursor = Cursors.Default;
        gridLogs.Visible = true;
    }

    private void SetShowInModelEnabled(bool enabled)
    {
        isShowInModelActionable = enabled;
        if (enabled)
        {
            btnShowInModel.BackColor = System.Drawing.Color.FromArgb(26, 115, 232);
            btnShowInModel.ForeColor = System.Drawing.Color.White;
        }
        else
        {
            // Custom disabled appearance (avoids dark/ugly Windows override text colors)
            btnShowInModel.BackColor = System.Drawing.Color.FromArgb(240, 240, 240);
            btnShowInModel.ForeColor = System.Drawing.Color.FromArgb(160, 160, 160);
        }
    }

    private string FindLogDirectory()
    {
        try
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string pathPro = Path.Combine(localAppData, "Renga Software", "Renga Professional");
            string pathStd = Path.Combine(localAppData, "Renga Software", "Renga");
            
            if (Directory.Exists(pathPro)) return pathPro;
            if (Directory.Exists(pathStd)) return pathStd;
        }
        catch {}
        return null;
    }

    private void RefreshLogFilesList()
    {
        cmbLogFiles.Items.Clear();
        var dir = FindLogDirectory();
        if (string.IsNullOrEmpty(dir)) return;
        
        try
        {
            var files = Directory.GetFiles(dir, "*.log")
                                 .Select(f => new FileInfo(f))
                                 .Where(fi => !fi.Name.Equals("AecApp.log", StringComparison.OrdinalIgnoreCase))
								 //сортировка по дате .OrderByDescending(fi => fi.LastWriteTime)
								 //по имени:
                                 .OrderBy(fi => fi.Name)
                                 .ToList();
                                 
            foreach (var fi in files)
            {
                string sizeKb = (fi.Length / 1024.0).ToString("F1") + " КБ";
                string displayName = $"{fi.Name} ({fi.LastWriteTime:dd.MM.yyyy HH:mm}, {sizeKb})";
                
                cmbLogFiles.Items.Add(new LogFileItem { 
                    FilePath = fi.FullName, 
                    DisplayName = displayName 
                });
            }
            
            // Get active project SessionId to auto-select corresponding log file
            string sessionId = "";
            try
            {
                if (rengaApp.Project != null && !string.IsNullOrEmpty(rengaApp.Project.JournalPath))
                {
                    sessionId = Path.GetFileNameWithoutExtension(rengaApp.Project.JournalPath);
                }
            }
            catch {}
            
            int selectIndex = 0;
            if (!string.IsNullOrEmpty(sessionId))
            {
                for (int i = 0; i < cmbLogFiles.Items.Count; i++)
                {
                    if (cmbLogFiles.Items[i] is LogFileItem item)
                    {
                        string fileSessionId = Path.GetFileNameWithoutExtension(item.FilePath);
                        if (fileSessionId.Equals(sessionId, StringComparison.OrdinalIgnoreCase))
                        {
                            selectIndex = i;
                            break;
                        }
                    }
                }
            }
            
            if (cmbLogFiles.Items.Count > 0)
            {
                cmbLogFiles.SelectedIndex = selectIndex;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Ошибка сканирования папки логов: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void cmbLogType_SelectedIndexChanged(object sender, EventArgs e)
    {
        txtSearch.Clear();
        txtDetailText.Clear();
        SetShowInModelEnabled(false);
        
        if (cmbLogType.SelectedIndex == 0) // Project Journal
        {
            lblLogFile.Text = "Файл журнала:";
            lblLogFile.Visible = true;
            cmbLogFiles.Visible = true;
            btnBrowse.Text = "Обзор...";
            
            SetupGridForJournal();
            RefreshLogFilesList();
        }
        else if (cmbLogType.SelectedIndex == 1) // App Log (AecApp.log)
        {
            lblLogFile.Visible = false;
            cmbLogFiles.Visible = false;
            btnBrowse.Text = "Загрузить...";
            
            SetupGridForAppLog();
            
            var dir = FindLogDirectory();
            if (!string.IsNullOrEmpty(dir))
            {
                string path = Path.Combine(dir, "AecApp.log");
                if (File.Exists(path))
                {
                    currentLogFilePath = path;
                    LoadAppLogFile(path);
                    return;
                }
            }
            
            // If not found in default folder
            currentLogFilePath = "";
            loadedAppLogEntries.Clear();
            gridLogs.Rows.Clear();
            MessageBox.Show("Файл AecApp.log не найден в стандартной директории. Укажите файл вручную через 'Загрузить...'.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        else // Server Log (RengaServer.log)
        {
            lblLogFile.Visible = false;
            cmbLogFiles.Visible = false;
            btnBrowse.Text = "Загрузить...";
            
            SetupGridForServerLog();
            
            string defaultServerLogPath = @"C:\Program Files\Renga Collaboration Server\RengaServer.log";
            if (File.Exists(defaultServerLogPath))
            {
                currentLogFilePath = defaultServerLogPath;
                LoadServerLogFile(defaultServerLogPath);
            }
            else
            {
                currentLogFilePath = "";
                loadedServerLogEntries.Clear();
                gridLogs.Rows.Clear();
                MessageBox.Show("Файл RengaServer.log не найден по умолчанию на этом компьютере.\nПожалуйста, укажите файл RengaServer.log вручную с помощью кнопки 'Загрузить...'.", "Сетевой журнал не найден", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }

    private void cmbLogFiles_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (cmbLogFiles.SelectedItem is LogFileItem item)
        {
            currentLogFilePath = item.FilePath;
            LoadJournalFile(currentLogFilePath);
        }
    }

    private void btnBrowse_Click(object sender, EventArgs e)
    {
        using (var openFileDialog = new OpenFileDialog())
        {
            openFileDialog.Filter = "Файлы журналов (*.log)|*.log|Все файлы (*.*)|*.*";
            if (cmbLogType.SelectedIndex == 0)
                openFileDialog.Title = "Выбрать журнал проекта Renga";
            else if (cmbLogType.SelectedIndex == 1)
                openFileDialog.Title = "Выбрать файл AecApp.log";
            else
                openFileDialog.Title = "Выбрать файл RengaServer.log";
            
            var defaultDir = FindLogDirectory();
            if (cmbLogType.SelectedIndex == 2 && Directory.Exists(@"C:\Program Files\Renga Collaboration Server"))
            {
                openFileDialog.InitialDirectory = @"C:\Program Files\Renga Collaboration Server";
            }
            else if (!string.IsNullOrEmpty(defaultDir))
            {
                openFileDialog.InitialDirectory = defaultDir;
            }

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                currentLogFilePath = openFileDialog.FileName;
                
                // Show manually opened file in the label/combobox
                lblLogFile.Text = "Файл (внешний):";
                lblLogFile.Visible = true;
                cmbLogFiles.Visible = true;
                cmbLogFiles.Items.Clear();
                cmbLogFiles.Items.Add(Path.GetFileName(openFileDialog.FileName));
                cmbLogFiles.SelectedIndex = 0;
                
                if (cmbLogType.SelectedIndex == 0)
                    LoadJournalFile(currentLogFilePath);
                else if (cmbLogType.SelectedIndex == 1)
                    LoadAppLogFile(currentLogFilePath);
                else
                    LoadServerLogFile(currentLogFilePath);
            }
        }
    }

    private void SetupGridForJournal()
    {
        gridLogs.Columns.Clear();
        gridLogs.Columns.Add("Time", "Время");
        gridLogs.Columns.Add("User", "Пользователь");
        gridLogs.Columns.Add("Object", "Объект");
        gridLogs.Columns.Add("Guid", "GUID объекта");
        gridLogs.Columns.Add("Action", "Действие");
        gridLogs.Columns.Add("Status", "Статус");
        gridLogs.Columns.Add("Message", "Сообщение");
        
        gridLogs.Columns["Time"].Width = 140;
        gridLogs.Columns["User"].Width = 120;
        gridLogs.Columns["Object"].Width = 200;
        gridLogs.Columns["Guid"].Width = 150;
        gridLogs.Columns["Action"].Width = 95;
        gridLogs.Columns["Status"].Width = 90;
        gridLogs.Columns["Message"].Width = 200;
        gridLogs.Columns["Message"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill; // expands to fill remaining space
        
        cmbUser.Enabled = true;
        cmbStatus.Enabled = true;
        lblUser.Enabled = true;
        lblStatus.Enabled = true;
        lblAction.Text = "Действие:";
    }

    private void SetupGridForAppLog()
    {
        gridLogs.Columns.Clear();
        gridLogs.Columns.Add("Time", "Время");
        gridLogs.Columns.Add("Type", "Уровень");
        gridLogs.Columns.Add("Message", "Сообщение");
        
        gridLogs.Columns["Time"].Width = 150;
        gridLogs.Columns["Type"].Width = 90;
        gridLogs.Columns["Message"].Width = 790;
        gridLogs.Columns["Message"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill; // expands to fill remaining space
        
        cmbUser.Enabled = false;
        cmbStatus.Enabled = false;
        lblUser.Enabled = false;
        lblStatus.Enabled = false;
        lblAction.Text = "Уровень:";
    }

    private void SetupGridForServerLog()
    {
        gridLogs.Columns.Clear();
        gridLogs.Columns.Add("Time", "Время");
        gridLogs.Columns.Add("Component", "Компонент");
        gridLogs.Columns.Add("User", "Пользователь");
        gridLogs.Columns.Add("Status", "Статус");
        gridLogs.Columns.Add("Type", "Уровень");
        gridLogs.Columns.Add("Message", "Сообщение");
        
        gridLogs.Columns["Time"].Width = 140;
        gridLogs.Columns["Component"].Width = 100;
        gridLogs.Columns["User"].Width = 120;
        gridLogs.Columns["Status"].Width = 150;
        gridLogs.Columns["Type"].Width = 90;
        gridLogs.Columns["Message"].Width = 440;
        gridLogs.Columns["Message"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill; // expands to fill remaining space
        
        cmbUser.Enabled = true;
        cmbStatus.Enabled = true;
        lblUser.Enabled = true;
        lblStatus.Enabled = true;
        lblAction.Text = "Уровень:";
    }

    private void LoadJournalFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return;
        
        ShowLoading("Загрузка журнала проекта...");
        try
        {
            loadedJournalEntries.Clear();
            
            // Read lines safely handling sharing (Renga may be writing to it)
            string[] lines;
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
            {
                var content = streamReader.ReadToEnd();
                lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            }

            foreach (var line in lines)
            {
                var entry = ParseJournalLine(line);
                if (entry != null)
                {
                    loadedJournalEntries.Add(entry);
                }
            }
            
            // Populate filters
            var users = loadedJournalEntries.Select(e => e.User).Distinct().Where(u => !string.IsNullOrEmpty(u)).OrderBy(u => u).ToList();
            cmbUser.Items.Clear();
            cmbUser.Items.Add("Все");
            foreach (var u in users) cmbUser.Items.Add(u);
            cmbUser.SelectedIndex = 0;
            
            var actions = loadedJournalEntries.Select(e => e.Action).Distinct().Where(a => !string.IsNullOrEmpty(a)).OrderBy(a => a).ToList();
            cmbAction.Items.Clear();
            cmbAction.Items.Add("Все");
            foreach (var a in actions) cmbAction.Items.Add(a);
            cmbAction.SelectedIndex = 0;
            
            var statuses = loadedJournalEntries.Select(e => e.Status).Distinct().Where(s => !string.IsNullOrEmpty(s)).OrderBy(s => s).ToList();
            cmbStatus.Items.Clear();
            cmbStatus.Items.Add("Все");
            foreach (var s in statuses) cmbStatus.Items.Add(s);
            cmbStatus.SelectedIndex = 0;
            
            ApplyFilters();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Ошибка чтения файла журнала: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally { HideLoading(); }
    }

    private void LoadAppLogFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return;
        
        ShowLoading("Загрузка журнала приложения...");
        try
        {
            loadedAppLogEntries.Clear();
            
            string[] lines;
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
            {
                var content = streamReader.ReadToEnd();
                lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            }

            if (loadedAppLogEntries is List<AppLogEntry> appList) appList.Capacity = lines.Length;
            foreach (var line in lines)
            {
                var entry = ParseAppLogLine(line);
                if (entry != null)
                {
                    loadedAppLogEntries.Add(entry);
                }
            }
            
            // Populate Type/Severity filter (reused in Action combobox)
            var types = loadedAppLogEntries.Select(e => e.Type).Distinct().Where(t => !string.IsNullOrEmpty(t)).OrderBy(t => t).ToList();
            cmbAction.Items.Clear();
            cmbAction.Items.Add("Все");
            foreach (var t in types) cmbAction.Items.Add(t);
            cmbAction.SelectedIndex = 0;
            
            ApplyFilters();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Ошибка чтения файла AecApp.log: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally { HideLoading(); }
    }

    private void LoadServerLogFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return;
        
        ShowLoading("Загрузка журнала сервера...");
        try
        {
            loadedServerLogEntries.Clear();
            
            string[] lines;
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
            {
                var content = streamReader.ReadToEnd();
                lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            }

            if (loadedServerLogEntries is List<ServerLogEntry> srvList) srvList.Capacity = lines.Length;
            foreach (var line in lines)
            {
                var entry = ParseServerLogLine(line);
                if (entry != null)
                {
                    loadedServerLogEntries.Add(entry);
                }
            }
            
            // Populate User filter
            var users = loadedServerLogEntries.Select(e => e.User).Distinct().Where(u => !string.IsNullOrEmpty(u)).OrderBy(u => u).ToList();
            cmbUser.Items.Clear();
            cmbUser.Items.Add("Все");
            foreach (var u in users) cmbUser.Items.Add(u);
            cmbUser.SelectedIndex = 0;

            // Populate Status filter
            var statuses = loadedServerLogEntries.Select(e => e.Status).Distinct().Where(s => !string.IsNullOrEmpty(s)).OrderBy(s => s).ToList();
            cmbStatus.Items.Clear();
            cmbStatus.Items.Add("Все");
            foreach (var s in statuses) cmbStatus.Items.Add(s);
            cmbStatus.SelectedIndex = 0;

            // Populate Type/Severity filter (reused in Action combobox)
            var types = loadedServerLogEntries.Select(e => e.Type).Distinct().Where(t => !string.IsNullOrEmpty(t)).OrderBy(t => t).ToList();
            cmbAction.Items.Clear();
            cmbAction.Items.Add("Все");
            foreach (var t in types) cmbAction.Items.Add(t);
            cmbAction.SelectedIndex = 0;
            
            ApplyFilters();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Ошибка чтения файла RengaServer.log: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally { HideLoading(); }
    }

    private JournalEntry ParseJournalLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return null;
        
        var parts = line.Split('|');
        if (parts.Length < 6) return null;
        
        var entry = new JournalEntry();
        entry.Timestamp = parts[0].Trim('"');
        entry.User = parts[1].Trim('"');
        entry.ObjectName = parts[2].Trim('"');
        entry.ObjectGuid = parts[3].Trim('"');
        entry.Action = TranslateAction(parts[4].Trim('"'));
        entry.Status = TranslateStatus(parts[5].Trim('"'));
        entry.Message = parts.Length > 6 ? parts[6].Trim('"') : "";
        entry.RawLine = line;
        
        return entry;
    }

    private string TranslateAction(string action)
    {
        switch (action.Trim())
        {
            case "Creation": return "Создание";
            case "Editing": return "Редактирование";
            case "Removal": return "Удаление";
            default: return action;
        }
    }

    private string TranslateStatus(string status)
    {
        switch (status.Trim())
        {
            case "Success": return "Успешно";
            case "Conflict": return "Конфликт";
            case "Failure": return "Ошибка";
            default: return status;
        }
    }

    private string TranslateAppLogMessage(string msg)
    {
        if (string.IsNullOrEmpty(msg)) return msg;

        // Quick first-char dispatch to skip most regex calls
        char first = msg[0];

        if (first == 'L')
        {
            if (msg.StartsWith("Logging started"))
            {
                var logStartMatch = Regex.Match(msg, @"^Logging started\. Command line arguments are:\s*(?<args>.*)");
                if (logStartMatch.Success)
                    return $"Запуск логирования. Аргументы командной строки: {logStartMatch.Groups["args"].Value}";
            }
            if (msg.StartsWith("License:"))
                return $"Лицензия: {msg.Substring("License:".Length).TrimStart()}";
        }

        if (first == '[')
        {
            if (msg == "[CrashReporter] Initialization started.") return "[CrashReporter] Инициализация запущена.";
            if (msg == "[CrashReporter] Initialization completed.") return "[CrashReporter] Инициализация завершена.";
            if (msg == "[CrashReporter] Uninitialization started.") return "[CrashReporter] Деинициализация запущена.";
            if (msg == "[CrashReporter] Uninitialization completed.") return "[CrashReporter] Деинициализация завершена.";
        }

        if (first == 'D')
        {
            if (msg.StartsWith("D3D11 Device:"))
                return $"Устройство D3D11: {msg.Substring("D3D11 Device:".Length).TrimStart()}";
            if (msg.StartsWith("Device Vendor is"))
                return $"Производитель устройства: {msg.Substring("Device Vendor is".Length).TrimStart()}";
            if (msg.StartsWith("D3D Memory usage"))
            {
                return msg.Replace("D3D Memory usage :", "Использование памяти D3D:")
                          .Replace("DedicatedVideoMemory", "Выделенная видеопамять")
                          .Replace("DedicatedSystemMemory", "Выделенная системная память")
                          .Replace("SharedSystemMemory", "Общая системная память")
                          .Replace("kB", " КБ");
            }
        }

        if (first == 'I')
        {
            if (msg.StartsWith("ICLRRuntimeInfo:"))
            {
                return msg.Replace("ICLRRuntimeInfo: ver:", "Информация о среде CLR: версия:")
                          .Replace(", path:", ", путь:");
            }
        }

        if (first == 'P')
        {
            if (msg.StartsWith("PluginSystem:"))
            {
                var pluginMatch = Regex.Match(msg, @"^PluginSystem: Plugin '(?<name>[^']+)' is loaded successfully");
                if (pluginMatch.Success)
                    return $"Загрузка плагина: Плагин '{pluginMatch.Groups["name"].Value}' загружен успешно";
            }
            if (msg == "Project created") return "Проект создан.";
            if (msg == "Project closed") return "Проект закрыт";
            if (msg.StartsWith("Project opened from "))
                return $"Проект открыт из {msg.Substring("Project opened from ".Length)}";
        }

        if (first == 'S')
        {
            if (msg == "Start: Project loading") return "Начало: Загрузка проекта";
        }

        if (first == 'E')
        {
            if (msg.StartsWith("End: Project loading"))
            {
                var endProjectMatch = Regex.Match(msg, @"^End: Project loading\s*:\s*(?<guid>[a-fA-F0-9-]+)");
                if (endProjectMatch.Success)
                    return $"Окончание: Проект загружен: {endProjectMatch.Groups["guid"].Value}";
                return msg.Replace("End: Project loading", "Окончание: Проект загружен");
            }
            if (msg == "Exit application") return "Выход из приложения";
        }

        if (first == 'B')
        {
            if (msg.StartsWith("Backend finished loading"))
            {
                var backendMatch = Regex.Match(msg, @"^Backend finished loading\. Total size is (?<size>\d+) bytes\.");
                if (backendMatch.Success)
                    return $"Загрузка бэкенда завершена. Общий размер: {backendMatch.Groups["size"].Value} байт.";
            }
        }

        return msg;
    }

    private string TranslateServerLogMessage(string msg)
    {
        if (string.IsNullOrEmpty(msg)) return msg;

        char first = msg[0];

        if (first == 'U')
        {
            // All User messages start with "User "
            if (msg.StartsWith("User "))
            {
                // One regex to parse all User messages: "User ID(name) <action>"
                var userMatch = Regex.Match(msg, @"^User (?<id>\d+)\((?<name>[^\)]+)\)\s+(?<action>.*)");
                if (userMatch.Success)
                {
                    string name = userMatch.Groups["name"].Value;
                    string id = userMatch.Groups["id"].Value;
                    string action = userMatch.Groups["action"].Value;
                    string prefix = $"Пользователь: {name} (ID: {id})";

                    if (action.StartsWith("connected to server"))
                        return $"{prefix} подключился к серверу";
                    if (action.StartsWith("disconnected from server"))
                        return $"{prefix} отключился от сервера";
                    if (action.StartsWith("connecting to document"))
                    {
                        string docId = action.Substring("connecting to document ".Length).Trim();
                        return $"{prefix} подключается к документу {docId}";
                    }
                    if (action.StartsWith("connected to document"))
                    {
                        string docId = action.Substring("connected to document ".Length).Trim();
                        return $"{prefix} подключился к документу {docId}";
                    }
                    if (action.StartsWith("disconnected from document"))
                    {
                        string docId = action.Substring("disconnected from document ".Length).Trim();
                        return $"{prefix} отключился от документа {docId}";
                    }
                    if (action.StartsWith("was already authenticated on document"))
                    {
                        string docId = action.Substring("was already authenticated on document ".Length).Trim();
                        return $"{prefix} был авторизован в документе {docId}";
                    }
                    if (action.StartsWith("authenticated on document"))
                    {
                        string docId = action.Substring("authenticated on document ".Length).Trim();
                        return $"{prefix} авторизован в документе {docId}";
                    }
                }
            }
        }

        if (first == 'P')
        {
            if (msg.StartsWith("Project "))
            {
                var projPublishedMatch = Regex.Match(msg, @"^Project (?<projId>\d+) was published successfully");
                if (projPublishedMatch.Success)
                {
                    string projId = projPublishedMatch.Groups["projId"].Value;
                    return $"Проект {projId} опубликован успешно. (SessionID - {projId})";
                }
            }
        }

        if (first == 'D')
        {
            if (msg.StartsWith("Document "))
            {
                var docMatch = Regex.Match(msg, @"^Document (?<docId>\d+)\s+(?<action>\w+)");
                if (docMatch.Success)
                {
                    string docId = docMatch.Groups["docId"].Value;
                    string action = docMatch.Groups["action"].Value;
                    if (action == "initialized") return $"Документ {docId} инициализирован";
                    if (action == "written") return $"Документ {docId} записан";
                }
            }
        }

        if (first == 'C')
        {
            if (msg.StartsWith("Current number of opened documents:"))
            {
                string count = msg.Substring("Current number of opened documents:".Length).Trim();
                return $"Текущее количество открытых документов: {count}";
            }
        }

        if (first == 'S')
        {
            if (msg.StartsWith("Service status set to:"))
            {
                string status = msg.Substring("Service status set to:".Length).Trim();
                string statusRu = status;
                if (status == "SERVICE_STOP_PENDING") statusRu = "ожидание остановки";
                else if (status == "SERVICE_STOPPED") statusRu = "остановка завершена";
                else if (status == "SERVICE_START_PENDING") statusRu = "ожидание запуска";
                else if (status == "SERVICE_RUNNING") statusRu = "запущена";
                return $"Статус службы изменен на: {statusRu} ({status})";
            }
            if (msg.StartsWith("Starting Renga Server"))
            {
                var startingMatch = Regex.Match(msg, @"^Starting Renga Server (?<version>[\d\.]+)\.\.\.");
                if (startingMatch.Success)
                    return $"Запуск Renga Server {startingMatch.Groups["version"].Value}...";
            }
        }

        if (first == 'L')
        {
            if (msg.StartsWith("Listening port "))
                return $"Прослушивание порта {msg.Substring("Listening port ".Length).Trim()}";
        }

        if (first == 'E' && msg == "Exited gracefully")
            return "Успешное завершение работы";

        return msg;
    }

    private string GetServerConnectionStatus(string msg)
    {
        if (string.IsNullOrEmpty(msg)) return "";
        if (msg.Contains("disconnected from server"))
            return "Отключился от сервера";
        if (msg.Contains("disconnected from document"))
            return "Отключился от документа";
        if (msg.Contains("connected to server"))
            return "Подключился к серверу";
        if (msg.Contains("connected to document"))
            return "Подключился к документу";
        if (msg.Contains("connecting to document"))
            return "Подключается к документу";
        if (msg.Contains("was already authenticated on document"))
            return "Был авторизован в документе";
        if (msg.Contains("authenticated on document"))
            return "Авторизован в документе";
        return "";
    }

    private AppLogEntry ParseAppLogLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return null;
        
        // Format: "[(type) timestamp] message"
        // Manual parse instead of Regex for performance
        if (line.Length > 2 && line[0] == '[' && line[1] == '(')
        {
            int parenClose = line.IndexOf(')', 2);
            if (parenClose > 2)
            {
                string type = line.Substring(2, parenClose - 2);
                int bracketClose = line.IndexOf(']', parenClose);
                if (bracketClose > parenClose)
                {
                    string timestamp = line.Substring(parenClose + 1, bracketClose - parenClose - 1).Trim();
                    string msg = (bracketClose + 1 < line.Length) ? line.Substring(bracketClose + 1).TrimStart() : "";
                    return new AppLogEntry
                    {
                        Type = type,
                        Timestamp = timestamp,
                        Message = TranslateAppLogMessage(msg),
                        RawLine = line
                    };
                }
            }
        }
        
        return new AppLogEntry
        {
            Type = "info",
            Timestamp = "",
            Message = TranslateAppLogMessage(line.Trim()),
            RawLine = line
        };
    }

    private static string ExtractUserName(string msg)
    {
        // Manual parse of "User 12345(username)" pattern
        int userIdx = msg.IndexOf("User ");
        if (userIdx < 0) return "";
        int parenOpen = msg.IndexOf('(', userIdx + 5);
        if (parenOpen < 0) return "";
        int parenClose = msg.IndexOf(')', parenOpen + 1);
        if (parenClose < 0) return "";
        return msg.Substring(parenOpen + 1, parenClose - parenOpen - 1);
    }

    private ServerLogEntry ParseServerLogLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return null;
        
        // Format: "[2021-11-16 16:33:28.705] [RengaServer] [info] Listening"
        // Manual bracket parsing instead of Regex
        if (line.Length > 1 && line[0] == '[')
        {
            int b1Close = line.IndexOf(']', 1);
            if (b1Close > 1)
            {
                string timestamp = line.Substring(1, b1Close - 1);
                
                int b2Open = line.IndexOf('[', b1Close + 1);
                if (b2Open > b1Close)
                {
                    int b2Close = line.IndexOf(']', b2Open + 1);
                    if (b2Close > b2Open)
                    {
                        string component = line.Substring(b2Open + 1, b2Close - b2Open - 1);
                        
                        int b3Open = line.IndexOf('[', b2Close + 1);
                        if (b3Open > b2Close)
                        {
                            int b3Close = line.IndexOf(']', b3Open + 1);
                            if (b3Close > b3Open)
                            {
                                string type = line.Substring(b3Open + 1, b3Close - b3Open - 1);
                                string msg = (b3Close + 1 < line.Length) ? line.Substring(b3Close + 1).TrimStart() : "";
                                
                                return new ServerLogEntry
                                {
                                    Timestamp = timestamp,
                                    Component = component,
                                    User = ExtractUserName(msg),
                                    Status = GetServerConnectionStatus(msg),
                                    Type = type,
                                    Message = TranslateServerLogMessage(msg),
                                    RawLine = line
                                };
                            }
                        }
                    }
                }
            }
        }
        
        // Fallback for lines that don't match the bracket format
        string fallbackMsg = line.Trim();
        return new ServerLogEntry
        {
            Timestamp = "",
            Component = "RengaServer",
            User = ExtractUserName(fallbackMsg),
            Status = GetServerConnectionStatus(fallbackMsg),
            Type = "info",
            Message = TranslateServerLogMessage(fallbackMsg),
            RawLine = line
        };
    }

    private void ApplyFilters()
    {
        if (cmbLogType.SelectedIndex == 0) // Project Journal
        {
            if (loadedJournalEntries == null) return;
            
            var query = txtSearch.Text.Trim().ToLower();
            var selectedUser = cmbUser.SelectedItem?.ToString();
            var selectedAction = cmbAction.SelectedItem?.ToString();
            var selectedStatus = cmbStatus.SelectedItem?.ToString();
            
            var filtered = loadedJournalEntries.Where(e => {
                if (!string.IsNullOrEmpty(query))
                {
                    bool match = (e.ObjectName != null && e.ObjectName.ToLower().Contains(query)) ||
                                 (e.ObjectGuid != null && e.ObjectGuid.ToLower().Contains(query)) ||
                                 (e.Message != null && e.Message.ToLower().Contains(query)) ||
                                 (e.User != null && e.User.ToLower().Contains(query));
                    if (!match) return false;
                }
                
                if (selectedUser != null && selectedUser != "Все" && e.User != selectedUser) return false;
                if (selectedAction != null && selectedAction != "Все" && e.Action != selectedAction) return false;
                if (selectedStatus != null && selectedStatus != "Все" && e.Status != selectedStatus) return false;
                
                return true;
            }).ToList();
            
            DisplayJournalEntries(filtered);
        }
        else if (cmbLogType.SelectedIndex == 1) // App Log
        {
            if (loadedAppLogEntries == null) return;
            
            var query = txtSearch.Text.Trim().ToLower();
            var selectedType = cmbAction.SelectedItem?.ToString();
            
            var filtered = loadedAppLogEntries.Where(e => {
                if (!string.IsNullOrEmpty(query))
                {
                    bool match = (e.Message != null && e.Message.ToLower().Contains(query)) ||
                                 (e.Type != null && e.Type.ToLower().Contains(query)) ||
                                 (e.Timestamp != null && e.Timestamp.ToLower().Contains(query));
                    if (!match) return false;
                }
                
                if (selectedType != null && selectedType != "Все" && e.Type != selectedType) return false;
                
                return true;
            }).ToList();
            
            DisplayAppLogEntries(filtered);
        }
        else // Server Log
        {
            if (loadedServerLogEntries == null) return;
            
            var query = txtSearch.Text.Trim().ToLower();
            var selectedUser = cmbUser.SelectedItem?.ToString();
            var selectedStatus = cmbStatus.SelectedItem?.ToString();
            var selectedType = cmbAction.SelectedItem?.ToString();
            
            var filtered = loadedServerLogEntries.Where(e => {
                if (!string.IsNullOrEmpty(query))
                {
                    bool match = (e.Message != null && e.Message.ToLower().Contains(query)) ||
                                 (e.User != null && e.User.ToLower().Contains(query)) ||
                                 (e.Status != null && e.Status.ToLower().Contains(query)) ||
                                 (e.Type != null && e.Type.ToLower().Contains(query)) ||
                                 (e.Component != null && e.Component.ToLower().Contains(query)) ||
                                 (e.Timestamp != null && e.Timestamp.ToLower().Contains(query));
                    if (!match) return false;
                }
                
                if (selectedUser != null && selectedUser != "Все" && e.User != selectedUser) return false;
                if (selectedStatus != null && selectedStatus != "Все" && e.Status != selectedStatus) return false;
                if (selectedType != null && selectedType != "Все" && e.Type != selectedType) return false;
                
                return true;
            }).ToList();
            
            DisplayServerLogEntries(filtered);
        }
    }

    private void DisplayJournalEntries(List<JournalEntry> entries)
    {
        gridLogs.SuspendLayout();
        try
        {
        gridLogs.Rows.Clear();
        foreach (var e in entries)
        {
            int index = gridLogs.Rows.Add(
                e.Timestamp,
                e.User,
                e.ObjectName,
                e.ObjectGuid,
                e.Action,
                e.Status,
                e.Message
            );
            
            var lastRow = gridLogs.Rows[index];
            lastRow.Tag = e;
            
            if (e.Status == "Conflict" || e.Status == "Failure" || e.Status == "Конфликт" || e.Status == "Ошибка" || e.Status.Contains("Fail"))
            {
                lastRow.Cells["Status"].Style.ForeColor = System.Drawing.Color.Red;
                lastRow.Cells["Status"].Style.Font = new System.Drawing.Font(gridLogs.Font, System.Drawing.FontStyle.Bold);
            }
            else if (e.Status == "Успешно" || e.Status == "Success")
            {
                lastRow.Cells["Status"].Style.ForeColor = System.Drawing.Color.FromArgb(46, 125, 50);
            }
            
            if (e.Action == "Удаление" || e.Action == "Removal" || e.Action == "Delete")
            {
                lastRow.Cells["Action"].Style.ForeColor = System.Drawing.Color.FromArgb(192, 57, 43); // dark red
            }
            else if (e.Action == "Создание" || e.Action == "Creation" || e.Action == "Create")
            {
                lastRow.Cells["Action"].Style.ForeColor = System.Drawing.Color.FromArgb(46, 125, 50); // dark green
            }
            else if (e.Action == "Редактирование" || e.Action == "Editing" || e.Action == "Edit")
            {
                lastRow.Cells["Action"].Style.ForeColor = System.Drawing.Color.FromArgb(26, 115, 232); // blue
            }
        }
        }
        finally { gridLogs.ResumeLayout(); }
    }

    private void DisplayAppLogEntries(List<AppLogEntry> entries)
    {
        gridLogs.SuspendLayout();
        try
        {
        gridLogs.Rows.Clear();
        foreach (var e in entries)
        {
            int index = gridLogs.Rows.Add(
                e.Timestamp,
                e.Type,
                e.Message
            );
            
            var lastRow = gridLogs.Rows[index];
            lastRow.Tag = e;
            
            if (e.Type.Equals("error", StringComparison.OrdinalIgnoreCase) || 
                e.Type.Equals("critical", StringComparison.OrdinalIgnoreCase))
            {
                lastRow.Cells["Type"].Style.ForeColor = System.Drawing.Color.Red;
                lastRow.Cells["Type"].Style.Font = new System.Drawing.Font(gridLogs.Font, System.Drawing.FontStyle.Bold);
            }
            else if (e.Type.Equals("warning", StringComparison.OrdinalIgnoreCase))
            {
                lastRow.Cells["Type"].Style.ForeColor = System.Drawing.Color.FromArgb(183, 119, 13); // orange
                lastRow.Cells["Type"].Style.Font = new System.Drawing.Font(gridLogs.Font, System.Drawing.FontStyle.Bold);
            }
            else
            {
                lastRow.Cells["Type"].Style.ForeColor = System.Drawing.Color.FromArgb(74, 85, 104);
            }
        }
        }
        finally { gridLogs.ResumeLayout(); }
    }

    private void DisplayServerLogEntries(List<ServerLogEntry> entries)
    {
        gridLogs.SuspendLayout();
        try
        {
        gridLogs.Rows.Clear();
        foreach (var e in entries)
        {
            int index = gridLogs.Rows.Add(
                e.Timestamp,
                e.Component,
                e.User,
                e.Status,
                e.Type,
                e.Message
            );
            
            var lastRow = gridLogs.Rows[index];
            lastRow.Tag = e;
            
            if (e.Type.Equals("error", StringComparison.OrdinalIgnoreCase) || 
                e.Type.Equals("critical", StringComparison.OrdinalIgnoreCase) ||
                e.Type.Contains("fail") || e.Type.Contains("err"))
            {
                lastRow.Cells["Type"].Style.ForeColor = System.Drawing.Color.Red;
                lastRow.Cells["Type"].Style.Font = new System.Drawing.Font(gridLogs.Font, System.Drawing.FontStyle.Bold);
            }
            else if (e.Type.Equals("warning", StringComparison.OrdinalIgnoreCase))
            {
                lastRow.Cells["Type"].Style.ForeColor = System.Drawing.Color.FromArgb(183, 119, 13);
                lastRow.Cells["Type"].Style.Font = new System.Drawing.Font(gridLogs.Font, System.Drawing.FontStyle.Bold);
            }
            else
            {
                lastRow.Cells["Type"].Style.ForeColor = System.Drawing.Color.FromArgb(46, 125, 50); // green for info
            }

            if (!string.IsNullOrEmpty(e.Status))
            {
                if (e.Status.Contains("подключился") || e.Status.Contains("Подключился") || e.Status.Contains("авторизован") || e.Status.Contains("Авторизован"))
                {
                    lastRow.Cells["Status"].Style.ForeColor = System.Drawing.Color.FromArgb(46, 125, 50);
                }
                else if (e.Status.Contains("отключился") || e.Status.Contains("Отключился"))
                {
                    lastRow.Cells["Status"].Style.ForeColor = System.Drawing.Color.FromArgb(192, 57, 43);
                }
            }
        }
        }
        finally { gridLogs.ResumeLayout(); }
    }

    private void gridLogs_SelectionChanged(object sender, EventArgs e)
    {
        if (gridLogs.SelectedRows.Count == 0)
        {
            txtDetailText.Text = "";
            SetShowInModelEnabled(false);
            return;
        }
        
        var selectedRow = gridLogs.SelectedRows[0];
        var tag = selectedRow.Tag;
        
        if (tag is JournalEntry je)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Время: {je.Timestamp}");
            sb.AppendLine($"Пользователь: {je.User}");
            sb.AppendLine($"Действие: {je.Action}");
            sb.AppendLine($"Статус: {je.Status}");
            sb.AppendLine($"Объект: {je.ObjectName}");
            sb.AppendLine($"GUID: {je.ObjectGuid}");
            if (!string.IsNullOrEmpty(je.Message))
            {
                sb.AppendLine($"Сообщение: {je.Message}");
            }
            sb.AppendLine($"--------------------------------------------------------------------------------");
            sb.AppendLine($"Строка из Log-файла: {je.RawLine}");
            
            txtDetailText.Text = sb.ToString();
            
            bool canShow = !string.IsNullOrWhiteSpace(je.ObjectGuid) && je.Action != "Удаление" && je.Action != "Removal";
            SetShowInModelEnabled(canShow);
        }
        else if (tag is AppLogEntry ae)
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(ae.Timestamp))
            {
                sb.AppendLine($"Время: {ae.Timestamp}");
                sb.AppendLine($"Уровень: {ae.Type}");
            }
            sb.AppendLine($"Сообщение: {ae.Message}");
            sb.AppendLine($"--------------------------------------------------------------------------------");
            sb.AppendLine($"Строка из Log-файла: {ae.RawLine}");
            
            txtDetailText.Text = sb.ToString();
            SetShowInModelEnabled(false);
        }
        else if (tag is ServerLogEntry se)
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(se.Timestamp))
            {
                sb.AppendLine($"Время: {se.Timestamp}");
                sb.AppendLine($"Компонент: {se.Component}");
                sb.AppendLine($"Уровень: {se.Type}");
            }
            if (!string.IsNullOrEmpty(se.User))
            {
                sb.AppendLine($"Пользователь: {se.User}");
            }
            if (!string.IsNullOrEmpty(se.Status))
            {
                sb.AppendLine($"Статус: {se.Status}");
            }
            sb.AppendLine($"Сообщение: {se.Message}");
            sb.AppendLine($"--------------------------------------------------------------------------------");
            sb.AppendLine($"Строка из Log-файла: {se.RawLine}");
            
            txtDetailText.Text = sb.ToString();
            SetShowInModelEnabled(false);
        }
    }

    private void btnShowInModel_Click(object sender, EventArgs e)
    {
        if (!isShowInModelActionable) return; // Prevent clicking when inactive!
        if (gridLogs.SelectedRows.Count == 0) return;
        
        var selectedRow = gridLogs.SelectedRows[0];
        string guidStr = "";
        
        if (cmbLogType.SelectedIndex == 0 && selectedRow.Tag is JournalEntry je)
        {
            guidStr = je.ObjectGuid;
        }
        
        if (string.IsNullOrWhiteSpace(guidStr))
        {
            MessageBox.Show("У выбранного события нет GUID объекта или объект был удален.", "Выделение", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        
        try
        {
            Guid guid;
            if (!Guid.TryParse(guidStr, out guid))
            {
                MessageBox.Show("Некорректный формат GUID: " + guidStr, "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            var project = rengaApp.Project;
            if (project == null)
            {
                MessageBox.Show("В Renga не открыт проект.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            
            var model = project.Model;
            var objects = model.GetObjects();
            
            IModelObject modelObject = null;
            try
            {
                modelObject = objects.GetByUniqueId(guid);
            }
            catch {}
            
            if (modelObject == null)
            {
                MessageBox.Show($"Объект с GUID [{guidStr}] не найден в активном проекте.\n\nВозможные причины:\n1. Объект был окончательно удален.\n2. Вы просматриваете журнал другого проекта.\n3. Изменения еще не сохранены/не синхронизированы.", "Объект не найден", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            int id = modelObject.Id;
            
            // Set selection in Renga
            var selection = rengaApp.Selection;
            int[] selectIds = new int[] { id };
            selection.SetSelectedObjects(selectIds);
            
            // Show message confirming selection
            MessageBox.Show($"Объект '{modelObject.Name}' (ID: {id}) успешно выделен в модели Renga.", "Выделение выполнено", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Ошибка при поиске/выделении объекта в Renga: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void btnClearFilters_Click(object sender, EventArgs e)
    {
        txtSearch.Clear();
        if (cmbUser.Items.Count > 0) cmbUser.SelectedIndex = 0;
        if (cmbAction.Items.Count > 0) cmbAction.SelectedIndex = 0;
        if (cmbStatus.Items.Count > 0) cmbStatus.SelectedIndex = 0;
        ApplyFilters();
    }

    private void btnClose_Click(object sender, EventArgs e)
    {
        this.Close();
    }
}
