using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Renga;

using Color = System.Drawing.Color;
using Font = System.Drawing.Font;
using FontStyle = System.Drawing.FontStyle;
using Form = System.Windows.Forms.Form;

namespace ScriptManagerPlugin
{
    public class FontCheckerCommand
    {
        public void Execute(IApplication rengaApp, Action<string> print)
        {
            try
            {
                if (rengaApp == null || rengaApp.Project == null)
                {
                    MessageBox.Show(
                        "Пожалуйста, откройте проект в Renga перед запуском проверки шрифтов.",
                        "Ошибка Шрифта",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                using (var form = new FontCheckerForm(rengaApp))
                {
                    form.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ошибка при запуске проверки шрифтов:\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }

    public class MissingFontLocation
    {
        public string FontName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string TargetName { get; set; } = string.Empty;
        public string CleanName { get; set; } = string.Empty;
        public Guid? Guid { get; set; }
        public int EntityId { get; set; } = -1;
    }

    public class FontCheckerForm : Form
    {
        private readonly IApplication _app;

        private Label _lblSummary;
        private Label _lblSubtitle;
        private DataGridView _gridDetails;
        private Button _btnOpenView;
        private Button _btnClose;

        private List<MissingFontLocation> _allLocations = new List<MissingFontLocation>();

        public FontCheckerForm(IApplication app)
        {
            _app = app;
            InitializeComponent();
            LoadFromRengaLog();
        }

        private void InitializeComponent()
        {
            this.Text = "Проверка отсутствующих шрифтов (по журналу Renga AecApp.log)";
            this.ClientSize = new Size(865, 510);
            this.BackColor = Color.White;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            this.AutoScaleMode = AutoScaleMode.Dpi;

            // 1. Верхний информационный блок
            var pnlTop = new Panel
            {
                Location = new Point(15, 12),
                Size = new Size(835, 54),
                BackColor = Color.FromArgb(248, 249, 250),
                BorderStyle = BorderStyle.FixedSingle
            };

            _lblSummary = new Label
            {
                Location = new Point(12, 8),
                Size = new Size(810, 20),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(32, 33, 36),
                Text = "Чтение журнала Renga..."
            };
            pnlTop.Controls.Add(_lblSummary);

            _lblSubtitle = new Label
            {
                Location = new Point(12, 29),
                Size = new Size(810, 18),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(95, 99, 104),
                Text = ""
            };
            pnlTop.Controls.Add(_lblSubtitle);

            this.Controls.Add(pnlTop);

            // 2. Группа: Места использования отсутствующего шрифта
            var gbDetails = new GroupBox
            {
                Text = "Места в проекте, где используется отсутствующий шрифт (двойной клик для перехода на вид)",
                Location = new Point(15, 74),
                Size = new Size(835, 375),
                Font = new Font("Segoe UI", 9F)
            };

            _gridDetails = new DataGridView
            {
                Location = new Point(15, 24),
                Size = new Size(805, 336),
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = true,
                AutoGenerateColumns = false
            };

            // Настройка заголовков: запрещаем синюю подсветку заголовка при фокусе
            _gridDetails.EnableHeadersVisualStyles = false;
            _gridDetails.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            _gridDetails.ColumnHeadersHeight = 32;
            _gridDetails.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(242, 244, 246);
            _gridDetails.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(32, 33, 36);
            _gridDetails.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(242, 244, 246);
            _gridDetails.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.FromArgb(32, 33, 36);
            _gridDetails.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);

            // Настройка строк
            _gridDetails.DefaultCellStyle.SelectionBackColor = Color.FromArgb(220, 230, 245);
            _gridDetails.DefaultCellStyle.SelectionForeColor = Color.Black;
            _gridDetails.DefaultCellStyle.Font = new Font("Segoe UI", 9.5F);
            _gridDetails.RowTemplate.Height = 28;

            _gridDetails.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Отсутствующий шрифт",
                Width = 200,
                DataPropertyName = "FontName"
            });
            _gridDetails.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Раздел",
                Width = 140,
                DataPropertyName = "Category"
            });
            _gridDetails.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Имя вида / Спецификации / Чертежа",
                Width = 310,
                DataPropertyName = "TargetName"
            });
            _gridDetails.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Статус перехода",
                Width = 155,
                DataPropertyName = "StatusText"
            });

            _gridDetails.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) BtnOpenView_Click(s, e); };

            gbDetails.Controls.Add(_gridDetails);
            this.Controls.Add(gbDetails);

            // 3. Кнопки действий
            _btnOpenView = new Button
            {
                Text = "Перейти",
                Location = new Point(605, 460),
                Size = new Size(125, 34),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(26, 115, 232),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnOpenView.FlatAppearance.BorderSize = 0;
            _btnOpenView.Click += BtnOpenView_Click;
            this.Controls.Add(_btnOpenView);

            _btnClose = new Button
            {
                Text = "Закрыть",
                Location = new Point(740, 460),
                Size = new Size(110, 34),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(32, 33, 36),
                Font = new Font("Segoe UI", 9F),
                Cursor = Cursors.Hand
            };
            _btnClose.FlatAppearance.BorderColor = Color.FromArgb(200, 208, 219);
            _btnClose.Click += (s, e) => this.Close();
            this.Controls.Add(_btnClose);
        }

        private void LoadFromRengaLog()
        {
            _allLocations.Clear();
            _gridDetails.Rows.Clear();

            string logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                @"Renga Software\Renga Professional\AecApp.log");

            if (!File.Exists(logPath))
            {
                _lblSummary.ForeColor = Color.FromArgb(190, 90, 0);
                _lblSummary.Text = "Файл журнала Renga не найден.";
                _lblSubtitle.Text = logPath;
                return;
            }

            try
            {
                _allLocations = ParseMissingFontsFromLog(logPath);

                // Сопоставление с EntityId открытого проекта
                ResolveEntityIds(_allLocations);

                if (_allLocations.Count > 0)
                {
                    var fontGroups = _allLocations.GroupBy(l => l.FontName).ToList();
                    var fontNames = fontGroups.Select(g => g.Key).ToList();
                    int totalPlaces = _allLocations.Count;

                    _lblSummary.ForeColor = Color.FromArgb(217, 48, 37);
                    if (fontGroups.Count == 1)
                    {
                        _lblSummary.Text = $"Отсутствует шрифт: {fontNames[0]}  —  всего найдено мест: {totalPlaces}";
                    }
                    else
                    {
                        var summaryParts = fontGroups.Select(g => $"{g.Key} ({g.Count()} мест)");
                        _lblSummary.Text = $"Отсутствуют шрифты: {string.Join(", ", summaryParts)}  —  всего мест: {totalPlaces}";
                    }

                    _lblSubtitle.Text = "Выберите строку и нажмите кнопку «Перейти» или дважды кликните по строке.";

                    foreach (var loc in _allLocations)
                    {
                        string openStatus = loc.EntityId >= 0 ? "Доступен для перехода" : "В проекте";
                        _gridDetails.Rows.Add(loc.FontName, loc.Category, loc.CleanName, openStatus);
                    }

                    if (_gridDetails.Rows.Count > 0)
                    {
                        _gridDetails.Rows[0].Selected = true;
                    }
                }
                else
                {
                    _lblSummary.ForeColor = Color.FromArgb(30, 142, 62);
                    _lblSummary.Text = "В журнале Renga ошибок отсутствия шрифтов не зафиксировано.";
                    _lblSubtitle.Text = "Все шрифты, используемые в проекте, установлены в системе.";
                }
            }
            catch (Exception ex)
            {
                _lblSummary.ForeColor = Color.FromArgb(217, 48, 37);
                _lblSummary.Text = "Ошибка чтения журнала Renga: " + ex.Message;
                _lblSubtitle.Text = "";
            }
        }

        private List<MissingFontLocation> ParseMissingFontsFromLog(string logPath)
        {
            var result = new List<MissingFontLocation>();
            string[] lines;

            using (var fs = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var sr = new StreamReader(fs, Encoding.UTF8))
            {
                var list = new List<string>();
                string l;
                while ((l = sr.ReadLine()) != null) list.Add(l);
                lines = list.ToArray();
            }

            int lastIdx = -1;
            for (int i = lines.Length - 1; i >= 0; i--)
            {
                if (lines[i].IndexOf("Missing fonts:", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    lastIdx = i;
                    break;
                }
            }

            if (lastIdx < 0) return result;

            string currentFont = null;
            string currentCategory = null;

            for (int i = lastIdx + 1; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                if (trimmed.StartsWith("[("))
                    break;

                if (trimmed.StartsWith("\"") || (trimmed.Contains("\"") && trimmed.EndsWith("\".")))
                {
                    // Извлечение всех подстрок внутри кавычек
                    int p = 0;
                    while (p < trimmed.Length)
                    {
                        int q1 = trimmed.IndexOf('"', p);
                        if (q1 < 0) break;
                        int q2 = trimmed.IndexOf('"', q1 + 1);
                        if (q2 < 0) break;

                        string raw = trimmed.Substring(q1 + 1, q2 - q1 - 1);
                        p = q2 + 1;

                        string clean = raw;
                        Guid? guid = null;

                        int open = raw.IndexOf('{');
                        int close = raw.IndexOf('}', open >= 0 ? open + 1 : 0);
                        if (open >= 0 && close > open && (close - open - 1) == 36)
                        {
                            string guidStr = raw.Substring(open + 1, 36);
                            if (Guid.TryParse(guidStr, out var g))
                            {
                                guid = g;
                                clean = raw.Substring(0, open).TrimEnd('.', ' ');
                            }
                        }

                        string catFriendly = currentCategory ?? "Объект";
                        if (catFriendly.Equals("Schedules", StringComparison.OrdinalIgnoreCase)) catFriendly = "Спецификация";
                        else if (catFriendly.Equals("DrawingTexts", StringComparison.OrdinalIgnoreCase) || catFriendly.Equals("Drawings", StringComparison.OrdinalIgnoreCase)) catFriendly = "Чертёж";
                        else if (catFriendly.Equals("Tables", StringComparison.OrdinalIgnoreCase)) catFriendly = "Таблица";

                        result.Add(new MissingFontLocation
                        {
                            FontName = currentFont ?? "Неизвестный шрифт",
                            Category = catFriendly,
                            TargetName = raw,
                            CleanName = clean,
                            Guid = guid
                        });
                    }
                }
                else if (trimmed.Equals("Schedules", StringComparison.OrdinalIgnoreCase) ||
                         trimmed.Equals("DrawingTexts", StringComparison.OrdinalIgnoreCase) ||
                         trimmed.Equals("Tables", StringComparison.OrdinalIgnoreCase) ||
                         trimmed.Equals("Drawings", StringComparison.OrdinalIgnoreCase))
                {
                    currentCategory = trimmed;
                }
                else if (!trimmed.StartsWith("["))
                {
                    currentFont = trimmed;
                    currentCategory = null;
                }
            }

            return result;
        }

        private void ResolveEntityIds(List<MissingFontLocation> locations)
        {
            try
            {
                var schedules = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                if (_app.Project.Schedules != null)
                {
                    for (int i = 0; i < _app.Project.Schedules.Count; i++)
                    {
                        var sc = _app.Project.Schedules.GetByIndex(i);
                        if (!string.IsNullOrEmpty(sc?.Name)) schedules[sc.Name] = sc.Id;
                    }
                }

                var drawings = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                if (_app.Project.Drawings2 != null)
                {
                    for (int i = 0; i < _app.Project.Drawings2.Count; i++)
                    {
                        var dr = _app.Project.Drawings2.GetByIndex(i);
                        if (!string.IsNullOrEmpty(dr?.Name)) drawings[dr.Name] = dr.Id;
                    }
                }

                var tables = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                if (_app.Project.Tables != null)
                {
                    for (int i = 0; i < _app.Project.Tables.Count; i++)
                    {
                        var tb = _app.Project.Tables.GetByIndex(i);
                        if (!string.IsNullOrEmpty(tb?.Name)) tables[tb.Name] = tb.Id;
                    }
                }

                foreach (var loc in locations)
                {
                    if (loc.Category == "Спецификация" && schedules.TryGetValue(loc.CleanName, out int scId))
                    {
                        loc.EntityId = scId;
                    }
                    else if (loc.Category == "Чертёж" && drawings.TryGetValue(loc.CleanName, out int drId))
                    {
                        loc.EntityId = drId;
                    }
                    else if (loc.Category == "Таблица" && tables.TryGetValue(loc.CleanName, out int tbId))
                    {
                        loc.EntityId = tbId;
                    }
                }
            }
            catch { }
        }

        private void BtnOpenView_Click(object sender, EventArgs e)
        {
            if (_gridDetails.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите строку в таблице для перехода.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int rowIdx = _gridDetails.SelectedRows[0].Index;
            if (rowIdx >= 0 && rowIdx < _allLocations.Count)
            {
                var loc = _allLocations[rowIdx];
                if (loc.EntityId >= 0)
                {
                    try
                    {
                        _app.OpenViewByEntity(loc.EntityId);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Не удалось открыть вид: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                else
                {
                    MessageBox.Show($"Вид «{loc.CleanName}» не удалось найти по идентификатору для автоматического перехода.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
    }
}
