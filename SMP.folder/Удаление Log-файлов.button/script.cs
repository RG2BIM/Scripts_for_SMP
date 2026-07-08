using System;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
using System.Collections.Generic;
using Color = System.Drawing.Color;
using Font = System.Drawing.Font;
using FontStyle = System.Drawing.FontStyle;
using Renga;

try
{
    // Run the UI
    System.Windows.Forms.Application.EnableVisualStyles();
    var rengaWindow = new WindowWrapper();
    using (var form = new CleanupForm(RengaApp, rengaWindow))
    {
        form.ShowDialog(rengaWindow);
    }
}
catch (Exception ex)
{
    var rengaWindow = new WindowWrapper();
    MessageBox.Show(rengaWindow, "Ошибка запуска плагина:\n" + ex.Message + "\n" + ex.StackTrace, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
}

// ============================================================================
// ВСЕ КЛАССЫ И СТРУКТУРЫ ОПРЕДЕЛЯЮТСЯ В КОНЦЕ ФАЙЛА ДЛЯ ROSLYN CSX СОВМЕСТИМОСТИ
// ============================================================================

public class CleanupForm : Form
{
    private IApplication rengaApp;
    
    // UI controls
    private GroupBox grpSettings;
    private Label lblCutoff;
    private ComboBox cmbCutoff;
    
    private GroupBox grpStats;
    private Label lblStatsLogs;
    private Label lblStatsPrefs;
    private Label lblStatsTotal;
    
    private Button btnClean;
    private Button btnCancel;
    
    // Paths
    private string logsDir;
    private string prefsDir;
    
    // Active IDs (Always skipped during cleanup)
    private string currentProjectId = "";
    private string currentSessionId = "";

    private IWin32Window ownerWindow;

    public CleanupForm(IApplication app, IWin32Window owner)
    {
        this.rengaApp = app;
        this.ownerWindow = owner;
        InitializePathsAndIds();
        InitializeComponents();
        UpdateStatistics();
    }

    private void InitializePathsAndIds()
    {
        try
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            logsDir = Path.Combine(localAppData, "Renga Software", "Renga Professional");
            if (!Directory.Exists(logsDir))
                logsDir = Path.Combine(localAppData, "Renga Software", "Renga");
            prefsDir = Path.Combine(logsDir, "ProjectPreferences");
        }
        catch {}

        try
        {
            if (rengaApp != null && rengaApp.Project != null)
            {
                if (rengaApp.Project.ProjectInfo != null)
                    currentProjectId = rengaApp.Project.ProjectInfo.UniqueIdS;
                if (!string.IsNullOrEmpty(rengaApp.Project.JournalPath))
                    currentSessionId = Path.GetFileNameWithoutExtension(rengaApp.Project.JournalPath);
            }
        }
        catch {}
    }

    private void InitializeComponents()
    {
        // Setup Form
        this.Text = "Удаление Log-файлов Renga";
        this.Font = new Font("Segoe UI", 9);
        this.AutoScaleMode = AutoScaleMode.Dpi;
        this.ClientSize = new Size(445, 252);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.BackColor = Color.White;

        // GroupBox Settings
        grpSettings = new GroupBox();
        grpSettings.Text = "Параметры очистки";
        grpSettings.Top = 15;
        grpSettings.Left = 15;
        grpSettings.Width = 415;
        grpSettings.Height = 65;
        this.Controls.Add(grpSettings);

        lblCutoff = new Label();
        lblCutoff.Text = "Удалить файлы старше:";
        lblCutoff.Top = 25;
        lblCutoff.Left = 15;
        lblCutoff.Width = 175;
        lblCutoff.Height = 20;
        grpSettings.Controls.Add(lblCutoff);

        cmbCutoff = new ComboBox();
        cmbCutoff.Top = 22;
        cmbCutoff.Left = 200;
        cmbCutoff.Width = 200;
        cmbCutoff.Height = 25;
        cmbCutoff.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbCutoff.Font = new Font("Segoe UI", 10);
        cmbCutoff.Items.AddRange(new object[] {
            "Все файлы (без ограничений)",
            "1 недели",
            "2 недель",
            "3 недель",
            "1 месяца",
            "3 месяцев",
            "6 месяцев",
            "1 года",
            "1.5 года"
        });
        cmbCutoff.SelectedIndex = 6; // Default: 6 months
        cmbCutoff.SelectedIndexChanged += (s, e) => UpdateStatistics();
        grpSettings.Controls.Add(cmbCutoff);

        // GroupBox Stats
        grpStats = new GroupBox();
        grpStats.Text = "Ожидаемый результат очистки";
        grpStats.Top = 95;
        grpStats.Left = 15;
        grpStats.Width = 415;
        grpStats.Height = 95;
        this.Controls.Add(grpStats);

        lblStatsLogs = new Label();
        lblStatsLogs.Text = "Лог-файлы сессий: 0 файлов (0 КБ)";
        lblStatsLogs.Top = 22;
        lblStatsLogs.Left = 15;
        lblStatsLogs.Width = 380;
        lblStatsLogs.Height = 20;
        grpStats.Controls.Add(lblStatsLogs);

        lblStatsPrefs = new Label();
        lblStatsPrefs.Text = "Настройки проектов: 0 файлов (0 КБ)";
        lblStatsPrefs.Top = 45;
        lblStatsPrefs.Left = 15;
        lblStatsPrefs.Width = 380;
        lblStatsPrefs.Height = 20;
        grpStats.Controls.Add(lblStatsPrefs);

        lblStatsTotal = new Label();
        lblStatsTotal.Text = "Итого к удалению: 0 файлов (0 КБ)";
        lblStatsTotal.Top = 68;
        lblStatsTotal.Left = 15;
        lblStatsTotal.Width = 380;
        lblStatsTotal.Height = 20;
        lblStatsTotal.Font = new Font("Segoe UI", 9, FontStyle.Bold);
        grpStats.Controls.Add(lblStatsTotal);

        // Buttons
        btnClean = new Button();
        btnClean.Text = "Очистить";
        btnClean.Top = 205;
        btnClean.Left = 230;
        btnClean.Width = 95;
        btnClean.Height = 32;
        btnClean.FlatStyle = FlatStyle.Flat;
        btnClean.FlatAppearance.BorderSize = 0;
        btnClean.Click += BtnClean_Click;
        this.Controls.Add(btnClean);

        btnCancel = new Button();
        btnCancel.Text = "Отмена";
        btnCancel.Top = 205;
        btnCancel.Left = 335;
        btnCancel.Width = 95;
        btnCancel.Height = 32;
        btnCancel.FlatStyle = FlatStyle.Flat;
        btnCancel.FlatAppearance.BorderColor = Color.FromArgb(200, 208, 219);
        btnCancel.Click += (s, e) => this.Close();
        this.Controls.Add(btnCancel);
        
        this.AcceptButton = btnClean;
        this.CancelButton = btnCancel;
    }

    private void UpdateStatistics()
    {
        List<string> logsToDelete, prefsToDelete;
        long logSize, prefSize;
        GetFilesToClean(out logsToDelete, out prefsToDelete, out logSize, out prefSize);

        lblStatsLogs.Text = string.Format("Лог-файлы сессий: {0} файлов ({1})", logsToDelete.Count, FormatSize(logSize));
        lblStatsPrefs.Text = string.Format("Настройки проектов: {0} файлов ({1})", prefsToDelete.Count, FormatSize(prefSize));
        lblStatsTotal.Text = string.Format("Итого к удалению: {0} файлов ({1})", logsToDelete.Count + prefsToDelete.Count, FormatSize(logSize + prefSize));

        bool canClean = (logsToDelete.Count + prefsToDelete.Count) > 0;
        btnClean.Enabled = canClean;
        if (canClean)
        {
            btnClean.BackColor = Color.FromArgb(26, 115, 232);
            btnClean.ForeColor = Color.White;
        }
        else
        {
            btnClean.BackColor = Color.FromArgb(240, 240, 240);
            btnClean.ForeColor = Color.FromArgb(160, 160, 160);
        }
    }

    private void GetFilesToClean(out List<string> logsList, out List<string> prefsList, out long logSize, out long prefSize)
    {
        logsList = new List<string>();
        prefsList = new List<string>();
        logSize = 0;
        prefSize = 0;

        DateTime cutoffDate = DateTime.Now;
        int idx = cmbCutoff.SelectedIndex;
        if (idx == 1) cutoffDate = DateTime.Now.AddDays(-7);
        else if (idx == 2) cutoffDate = DateTime.Now.AddDays(-14);
        else if (idx == 3) cutoffDate = DateTime.Now.AddDays(-21);
        else if (idx == 4) cutoffDate = DateTime.Now.AddMonths(-1);
        else if (idx == 5) cutoffDate = DateTime.Now.AddMonths(-3);
        else if (idx == 6) cutoffDate = DateTime.Now.AddMonths(-6);
        else if (idx == 7) cutoffDate = DateTime.Now.AddYears(-1);
        else if (idx == 8) cutoffDate = DateTime.Now.AddMonths(-18);

        // Scan logs
        if (Directory.Exists(logsDir))
        {
            try
            {
                var files = Directory.GetFiles(logsDir, "*.log");
                foreach (var file in files)
                {
                    string name = Path.GetFileName(file);
                    if (name.Equals("AecApp.log", StringComparison.OrdinalIgnoreCase))
                        continue;

                    string nameNoExt = Path.GetFileNameWithoutExtension(file);
                    
                    // Always skip the current active session log
                    if (!string.IsNullOrEmpty(currentSessionId) &&
                        nameNoExt.Equals(currentSessionId, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    FileInfo fi = new FileInfo(file);
                    if (idx == 0 || fi.LastWriteTime < cutoffDate)
                    {
                        logsList.Add(file);
                        logSize += fi.Length;
                    }
                }
            }
            catch {}
        }

        // Scan preferences
        if (Directory.Exists(prefsDir))
        {
            try
            {
                var files = Directory.GetFiles(prefsDir, "*.json");
                foreach (var file in files)
                {
                    string nameNoExt = Path.GetFileNameWithoutExtension(file);

                    // Always skip the current active project settings
                    if (!string.IsNullOrEmpty(currentProjectId) &&
                        nameNoExt.Equals(currentProjectId, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    FileInfo fi = new FileInfo(file);
                    if (idx == 0 || fi.LastWriteTime < cutoffDate)
                    {
                        prefsList.Add(file);
                        prefSize += fi.Length;
                    }
                }
            }
            catch {}
        }
    }

    private void BtnClean_Click(object sender, EventArgs e)
    {
        List<string> logsToDelete, prefsToDelete;
        long logSize, prefSize;
        GetFilesToClean(out logsToDelete, out prefsToDelete, out logSize, out prefSize);

        int totalCount = logsToDelete.Count + prefsToDelete.Count;
        if (totalCount == 0) return;

        DialogResult confirm = MessageBox.Show(
            this,
            string.Format("Вы уверены, что хотите безвозвратно удалить {0} файлов ({1})?", totalCount, FormatSize(logSize + prefSize)),
            "Подтверждение удаления",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning
        );
        if (confirm != DialogResult.Yes) return;

        int deletedLogs = 0;
        int deletedPrefs = 0;
        int skippedLogs = 0;
        int skippedPrefs = 0;

        // Delete logs
        foreach (var file in logsToDelete)
        {
            try
            {
                File.Delete(file);
                deletedLogs++;
            }
            catch
            {
                skippedLogs++;
            }
        }

        // Delete prefs
        foreach (var file in prefsToDelete)
        {
            try
            {
                File.Delete(file);
                deletedPrefs++;
            }
            catch
            {
                skippedPrefs++;
            }
        }

        // Hide form before final messagebox for better UX
        this.Hide();

        string summary = string.Format(
            "Очистка успешно завершена!\n\nУдалено логов сессий: {0}\nУдалено настроек проектов: {1}",
            deletedLogs,
            deletedPrefs
        );
        if (skippedLogs + skippedPrefs > 0)
        {
            summary += string.Format("\n\nПропущено занятых файлов: {0} (возможно, открыты в Renga)", skippedLogs + skippedPrefs);
        }

        MessageBox.Show(this.ownerWindow, summary, "Успешно", MessageBoxButtons.OK, MessageBoxIcon.Information);
        this.Close();
    }

    private string FormatSize(long bytes)
    {
        if (bytes >= 1024 * 1024)
            return string.Format("{0:F2} МБ", bytes / 1024.0 / 1024.0);
        else if (bytes >= 1024)
            return string.Format("{0:F1} КБ", bytes / 1024.0);
        else
            return string.Format("{0} Б", bytes);
    }
}

public class WindowWrapper : IWin32Window
{
    private IntPtr _hwnd;
    
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    public WindowWrapper()
    {
        IntPtr active = GetActiveWindow();
        if (active == IntPtr.Zero)
            active = GetForegroundWindow();
        _hwnd = active;
    }
    
    public WindowWrapper(IntPtr handle)
    {
        _hwnd = handle;
    }
    
    public IntPtr Handle
    {
        get { return _hwnd; }
    }
}
