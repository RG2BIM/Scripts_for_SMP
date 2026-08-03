using System;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Linq;

// Класс для хранения данных чертежа в списке
public class DrawingItem
{
    public int Id { get; set; }
    public string Name { get; set; }
    public override string ToString() { return Name; }
}

// Точка входа скрипта
public static class WinApi
{
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
    public static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);
}
try
{
    var project = RengaApp.Project;
    
    // Инициализация главной формы
    Form form = new Form() { AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi };
    form.Text = "Массовая замена шрифта на чертежах";
    form.BackColor = Color.White;
    form.Font = new Font("Segoe UI", 9);
    form.FormBorderStyle = FormBorderStyle.FixedDialog;
    form.MaximizeBox = false;
    form.MinimizeBox = false;
    form.StartPosition = FormStartPosition.CenterScreen;
    form.Width = 580;
    form.Height = 370;

    // --- ЛЕВАЯ ПАНЕЛЬ (Список чертежей) ---
    Label lblDrawings = new Label { Text = "Чертежи:", Left = 15, Top = 15, Width = 200, AutoSize = true };
    CheckedListBox clbDrawings = new CheckedListBox { Left = 15, Top = 40, Width = 250, Height = 220 };
    
    Button btnSelectAll = new Button { Text = "Выбрать все", Left = 15, Top = 275, Width = 120, Height = 35 };
    btnSelectAll.FlatStyle = FlatStyle.Flat;
    btnSelectAll.FlatAppearance.BorderSize = 1;
    btnSelectAll.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
    btnSelectAll.BackColor = Color.White;
    btnSelectAll.Click += (s, e) => { for (int i = 0; i < clbDrawings.Items.Count; i++) clbDrawings.SetItemChecked(i, true); };
    
    Button btnDeselectAll = new Button { Text = "Снять все", Left = 145, Top = 275, Width = 120, Height = 35 };
    btnDeselectAll.FlatStyle = FlatStyle.Flat;
    btnDeselectAll.FlatAppearance.BorderSize = 1;
    btnDeselectAll.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
    btnDeselectAll.BackColor = Color.White;
    btnDeselectAll.Click += (s, e) => { for (int i = 0; i < clbDrawings.Items.Count; i++) clbDrawings.SetItemChecked(i, false); };
    
    // Заполняем список чертежей
    var drawings = project.Drawings2;
    if (drawings != null)
    {
        for (int i = 0; i < drawings.Count; i++)
        {
            var entity = drawings.GetByIndex(i);
            if (entity != null)
            {
                clbDrawings.Items.Add(new DrawingItem { Id = entity.Id, Name = entity.Name });
            }
        }
    }
    else 
    {
        clbDrawings.Items.Add("(Чертежи не найдены)");
    }

    // Автоматическое выделение текущего чертежа (если он открыт на экране)
    var activeView = RengaApp.ActiveView;
    if (activeView != null && (int)activeView.Type == 6) // 6 = ViewType_Drawing
    {
        int viewId = activeView.Id;
        int repId = -1;
        string viewName = "";
        
        try { dynamic dv = activeView; repId = dv.RepresentedEntityId; } catch { }
        try { dynamic dv = activeView; viewName = dv.Name; } catch { }

        string windowTitle = "";
        try 
        {
            var sb = new System.Text.StringBuilder(256);
            WinApi.GetWindowText(WinApi.GetForegroundWindow(), sb, 256);
            windowTitle = sb.ToString();
        } 
        catch { }

        for (int i = 0; i < clbDrawings.Items.Count; i++)
        {
            var item = clbDrawings.Items[i] as DrawingItem;
            if (item != null)
            {
                if (item.Id == viewId || item.Id == repId || (!string.IsNullOrEmpty(viewName) && item.Name == viewName) || windowTitle.Contains(item.Name))
                {
                    clbDrawings.SetItemChecked(i, true);
                    clbDrawings.SelectedIndex = i;
                    break;
                }
            }
        }
    }

    // --- ПРАВАЯ ПАНЕЛЬ (Настройки шрифта) ---
    int rightX = 290;
    
    // Имя шрифта
    CheckBox chkChangeFont = new CheckBox { Text = "Заменить шрифт:", Left = rightX, Top = 42, Width = 130 };
    ComboBox cmbFont = new ComboBox { Left = rightX + 130, Top = 40, Width = 130, DropDownStyle = ComboBoxStyle.DropDownList };
    foreach (var ff in FontFamily.Families)
    {
        cmbFont.Items.Add(ff.Name);
    }
    int defaultFontIdx = cmbFont.Items.IndexOf("Arial");
    cmbFont.SelectedIndex = defaultFontIdx >= 0 ? defaultFontIdx : 0;
    cmbFont.Enabled = false;
    chkChangeFont.CheckedChanged += (s, e) => cmbFont.Enabled = chkChangeFont.Checked;

    // Размер шрифта
    CheckBox chkChangeSize = new CheckBox { Text = "Заменить размер:", Left = rightX, Top = 82, Width = 130 };
    NumericUpDown numSize = new NumericUpDown { Left = rightX + 130, Top = 80, Width = 130, DecimalPlaces = 1, Increment = 0.5M, Minimum = 1, Maximum = 100 };
    numSize.Value = 3.5M;
    numSize.Enabled = false;
    chkChangeSize.CheckedChanged += (s, e) => numSize.Enabled = chkChangeSize.Checked;

    // Стиль шрифта (жирный, курсив, подчеркнутый)
    CheckBox chkChangeStyle = new CheckBox { Text = "Заменить стиль:", Left = rightX, Top = 122, Width = 130 };
    Panel panelStyle = new Panel { Left = rightX + 130, Top = 120, Width = 130, Height = 80, Enabled = false };
    
    CheckBox chkBold = new CheckBox { Text = "Жирный", Left = 0, Top = 0, Width = 100 };
    CheckBox chkItalic = new CheckBox { Text = "Курсив", Left = 0, Top = 25, Width = 100 };
    CheckBox chkUnderline = new CheckBox { Text = "Подчеркнутый", Left = 0, Top = 50, Width = 120 };
    
    panelStyle.Controls.Add(chkBold);
    panelStyle.Controls.Add(chkItalic);
    panelStyle.Controls.Add(chkUnderline);
    
    chkChangeStyle.CheckedChanged += (s, e) => panelStyle.Enabled = chkChangeStyle.Checked;

    // Кнопка Применить
    Button btnApply = new Button { Text = "Применить к выбранным чертежам", Left = rightX, Top = 250, Width = 260, Height = 60 };
    btnApply.FlatStyle = FlatStyle.Flat;
    btnApply.BackColor = Color.FromArgb(26, 115, 232);
    btnApply.ForeColor = Color.White;
    btnApply.FlatAppearance.BorderSize = 0;

    btnApply.Click += (s, e) =>
    {
        if (clbDrawings.CheckedItems.Count == 0)
        {
            MessageBox.Show("Пожалуйста, выберите хотя бы один чертеж слева.");
            return;
        }
        
        if (!chkChangeFont.Checked && !chkChangeSize.Checked && !chkChangeStyle.Checked)
        {
            MessageBox.Show(form, "Пожалуйста, выберите хотя бы один параметр для замены (галочки справа).");
            return;
        }

        btnApply.Text = "Обработка...";
        btnApply.Enabled = false;
        Application.DoEvents();

        try 
        {
            int count = 0;
            var op = project.CreateOperation();
            op.Start();
            
            // Перебираем отмеченные чертежи
            foreach (var item in clbDrawings.CheckedItems)
            {
                DrawingItem drawingItem = item as DrawingItem;
                if (drawingItem == null) continue;

                // Получаем сущность чертежа по ID
                // (GetById возвращает объект по уникальному ID в коллекции чертежей)
                // Но нам нужно найти его перебором, так как GetById ждет ID, который мы сохранили
                Renga.IEntity drawingEntity = null;
                for (int i = 0; i < drawings.Count; i++)
                {
                    var ent = drawings.GetByIndex(i);
                    if (ent != null && ent.Id == drawingItem.Id)
                    {
                        drawingEntity = ent;
                        break;
                    }
                }

                if (drawingEntity == null) continue;

                // МАГИЯ Renga API:
                // Сущность чертежа (IEntity) также реализует интерфейс IModel!
                // Через него мы получаем объекты (тексты, виды и т.д.), размещенные именно на этом чертеже.
                Renga.IModel drawingModel = drawingEntity as Renga.IModel;
                if (drawingModel == null) continue;

                var objects = drawingModel.GetObjects();
                for (int i = 0; i < objects.Count; i++)
                {
                    var obj = objects.GetByIndex(i);
                    if (obj == null) continue;

                    Renga.ITextObject textObj = obj as Renga.ITextObject;
                    if (textObj == null) continue;

                    var doc = textObj.GetRichTextDocument();
                    bool modified = false;
                    
                    for (int p = 0; p < doc.ParagraphCount; p++)
                    {
                        var para = doc.GetParagraph(p);
                        for (int t = 0; t < para.TokenCount; t++)
                        {
                            var token = para.GetToken(t);
                            
                            if (chkChangeFont.Checked) token.FontFamily = cmbFont.SelectedItem.ToString();
                            if (chkChangeSize.Checked) token.FontCapSize = (float)numSize.Value;
                            if (chkChangeStyle.Checked)
                            {
                                var style = token.FontStyle;
                                style.Bold = (sbyte)(chkBold.Checked ? 1 : 0);
                                style.Italic = (sbyte)(chkItalic.Checked ? 1 : 0);
                                style.Underline = (sbyte)(chkUnderline.Checked ? 1 : 0);
                                token.FontStyle = style;
                            }

                            para.RemoveToken(t);
                            para.InsertToken(t, token);
                            modified = true;
                        }
                    }
                    
                    if (modified) count++;
                }
            }

            op.Apply();
            MessageBox.Show(form, $"Успешно!\nИзменено текстов на выбранных чертежах: {count}");
        }
        catch (Exception ex)
        {
            MessageBox.Show(form, "Ошибка: " + ex.Message);
        }
        finally 
        {
            form.Close();
        }
    };

    form.Controls.Add(lblDrawings);
    form.Controls.Add(clbDrawings);
    form.Controls.Add(btnSelectAll);
    form.Controls.Add(btnDeselectAll);
    
    form.Controls.Add(chkChangeFont);
    form.Controls.Add(cmbFont);
    
    form.Controls.Add(chkChangeSize);
    form.Controls.Add(numSize);
    
    form.Controls.Add(chkChangeStyle);
    form.Controls.Add(panelStyle);
    
    form.Controls.Add(btnApply);

    form.ShowDialog();
}
catch (Exception ex)
{
    MessageBox.Show("Фатальная ошибка: " + ex.Message);
}
