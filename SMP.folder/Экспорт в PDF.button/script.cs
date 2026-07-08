using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;
using Renga;

var project = RengaApp.Project;
if (string.IsNullOrEmpty(project.FilePath))
{
    MessageBox.Show("Проект не сохранен. Сохраните проект для экспорта PDF.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    return;
}

string projectDir = Path.GetDirectoryName(project.FilePath);
string projectName = Path.GetFileNameWithoutExtension(project.FilePath);
string saveDir = Path.Combine(projectDir, projectName);

// Gather topics
if (project.Drawings2 == null)
{
    MessageBox.Show("Модуль чертежей недоступен.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    return;
}
var drawingIdsObj = project.Drawings2.GetIds();
int[] drawingIds = drawingIdsObj as int[];
var topicIdToDrawings = new Dictionary<int, List<string>>();

if (drawingIds != null)
{
    for (int i = 0; i < drawingIds.Length; i++)
    {
        var id = drawingIds[i];
        var drawing = project.Drawings2.GetById(id);
        if (drawing == null) continue;

        var pCont = (IParameterContainer)drawing.GetInterfaceByName("IParameterContainer");
        int topicId = 1;
        if (pCont != null)
        {
            try { topicId = pCont.GetS("3B7FDF99-6C5E-4FED-8A3C-42149FE5D8B4").GetIntValue(); } catch {}
        }
        if (!topicIdToDrawings.ContainsKey(topicId)) topicIdToDrawings[topicId] = new List<string>();
        topicIdToDrawings[topicId].Add(drawing.UniqueIdS);
    }
}

var topics = new List<TopicItem>();
foreach (var tid in topicIdToDrawings.Keys)
{
    if (tid <= 1) 
    {
        topics.Add(new TopicItem { Id = tid, Name = "Раздел не назначен" });
    }
    else
    {
        var t = project.Topics?.GetById(tid);
        if (t != null)
        {
            topics.Add(new TopicItem { Id = tid, Name = t.Name });
        }
    }
}

if (topics.Count == 0)
{
    MessageBox.Show("Нет разделов с чертежами для экспорта.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
    return;
}

// Show selection form
Form form = new Form() { AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi,  
    Text = "Экспорт в PDF", 
    Width = 430, 
    Height = 450, 
    StartPosition = FormStartPosition.CenterScreen,
    FormBorderStyle = FormBorderStyle.FixedDialog,
    MaximizeBox = false,
    MinimizeBox = false,
    BackColor = System.Drawing.Color.White,
    Font = new System.Drawing.Font("Segoe UI", 9F)
};

var gbTopics = new GroupBox() { Text = "Выберите разделы", Location = new System.Drawing.Point(15, 15), Size = new System.Drawing.Size(385, 295) };
CheckedListBox clb = new CheckedListBox() { 
    Dock = DockStyle.Fill, 
    CheckOnClick = true,
    BorderStyle = BorderStyle.None,
    Font = new System.Drawing.Font("Segoe UI", 11F)
};
foreach (var t in topics) clb.Items.Add(t.Name, true);
gbTopics.Controls.Add(clb);
form.Controls.Add(gbTopics);

Button btnSelectAll = new Button() { Text = "Выделить все", Width = 130, Height = 32, Left = 15, Top = 320, FlatStyle = FlatStyle.Flat, BackColor = System.Drawing.Color.White, UseVisualStyleBackColor = false };
btnSelectAll.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(200, 208, 219);
Button btnDeselectAll = new Button() { Text = "Снять все", Width = 130, Height = 32, Left = 155, Top = 320, FlatStyle = FlatStyle.Flat, BackColor = System.Drawing.Color.White, UseVisualStyleBackColor = false };
btnDeselectAll.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(200, 208, 219);

btnSelectAll.Click += (s, e) => {
    for (int i = 0; i < clb.Items.Count; i++) clb.SetItemChecked(i, true);
};
btnDeselectAll.Click += (s, e) => {
    for (int i = 0; i < clb.Items.Count; i++) clb.SetItemChecked(i, false);
};

Button btnOk = new Button() { Text = "Экспорт", Width = 100, Height = 32, Left = 195, Top = 362, FlatStyle = FlatStyle.Flat, BackColor = System.Drawing.Color.FromArgb(26, 115, 232), ForeColor = System.Drawing.Color.White, UseVisualStyleBackColor = false };
btnOk.FlatAppearance.BorderSize = 0;
Button btnCancel = new Button() { Text = "Закрыть", Width = 100, Height = 32, Left = 300, Top = 362, FlatStyle = FlatStyle.Flat, BackColor = System.Drawing.Color.White, UseVisualStyleBackColor = false };
btnCancel.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(200, 208, 219);

btnOk.Click += (s, e) => form.DialogResult = DialogResult.OK;
btnCancel.Click += (s, e) => form.DialogResult = DialogResult.Cancel;

form.Controls.Add(btnSelectAll);
form.Controls.Add(btnDeselectAll);
form.Controls.Add(btnOk);
form.Controls.Add(btnCancel);
form.AcceptButton = btnOk;
form.CancelButton = btnCancel;

if (form.ShowDialog() == DialogResult.OK)
{
    if (clb.CheckedIndices.Count == 0) return;

    if (!Directory.Exists(saveDir)) Directory.CreateDirectory(saveDir);

    int count = 0;
    foreach (int idx in clb.CheckedIndices)
    {
        var t = topics[idx];
        string pdfName = Path.Combine(saveDir, $"{projectName}_{t.Name}.pdf");
        var uids = topicIdToDrawings[t.Id].ToArray();
        project.ExportDrawingsToPdfS(uids, pdfName, true /* Export to single file */);
        count++;
    }
    MessageBox.Show($"Экспорт {count} разделов успешно завершен!\nФайлы сохранены в:\n{saveDir}", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
}

public class TopicItem 
{ 
    public int Id; 
    public string Name; 
}
