using System;
using System.Windows.Forms;
using System.Drawing;
using Renga;
using Color = System.Drawing.Color;

var project = RengaApp.Project;
string uniqueId = project.ProjectInfo.UniqueIdS;
string journalPath = project.JournalPath;
string sessionId = string.IsNullOrEmpty(journalPath) ? "Не определен" : System.IO.Path.GetFileNameWithoutExtension(journalPath);

System.Windows.Forms.Application.EnableVisualStyles();
Form form = new Form();
form.SuspendLayout();
form.Text = "Уникальные ID проекта и сессии";
form.Font = new Font("Segoe UI", 9);
form.AutoScaleDimensions = new SizeF(96F, 96F);
form.AutoScaleMode = AutoScaleMode.Dpi;
form.ClientSize = new Size(400, 162);
form.StartPosition = FormStartPosition.CenterScreen;
form.FormBorderStyle = FormBorderStyle.FixedDialog;
form.MaximizeBox = false;
form.MinimizeBox = false;
form.BackColor = Color.White;

GroupBox grp = new GroupBox();
grp.Text = "Сведения о файле";
grp.Top = 15;
grp.Left = 15;
grp.Width = 370;
grp.Height = 95;
form.Controls.Add(grp);

Font txtFont = new Font("Segoe UI", 10);
ToolTip tip = new ToolTip();

Bitmap copyIcon = new Bitmap(12, 12);
Graphics g = Graphics.FromImage(copyIcon);
g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
Pen p = new Pen(Color.FromArgb(100, 100, 100), 1);
g.DrawRectangle(p, 1, 4, 6, 7);
g.DrawLine(p, 3, 0, 9, 0);
g.DrawLine(p, 9, 0, 9, 7);
g.DrawLine(p, 7, 7, 9, 7);
g.DrawLine(p, 3, 0, 3, 3);
p.Dispose();
g.Dispose();

Label lblProj = new Label() { Text = "ID проекта:", Top = 25, Left = 15, AutoSize = true };
TextBox txtProj = new TextBox() { Text = uniqueId, Top = 22, Left = 95, Width = 230, ReadOnly = true, Font = txtFont };
Button btnCopyProj = new Button() { Image = copyIcon, Top = 22, FlatStyle = FlatStyle.Flat };
btnCopyProj.FlatAppearance.BorderColor = Color.FromArgb(200, 208, 219);
tip.SetToolTip(btnCopyProj, "Копировать ID проекта");
btnCopyProj.Click += (s, e) => {
    if (!string.IsNullOrEmpty(txtProj.Text)) {
        Clipboard.SetText(txtProj.Text);
        MessageBox.Show("ID проекта скопирован в буфер обмена!", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
};

Label lblSess = new Label() { Text = "ID сессии:", Top = 58, Left = 15, AutoSize = true };
TextBox txtSess = new TextBox() { Text = sessionId, Top = 55, Left = 95, Width = 230, ReadOnly = true, Font = txtFont };
Button btnCopySess = new Button() { Image = copyIcon, Top = 55, FlatStyle = FlatStyle.Flat };
btnCopySess.FlatAppearance.BorderColor = Color.FromArgb(200, 208, 219);
tip.SetToolTip(btnCopySess, "Копировать ID сессии");
btnCopySess.Click += (s, e) => {
    if (!string.IsNullOrEmpty(txtSess.Text)) {
        Clipboard.SetText(txtSess.Text);
        MessageBox.Show("ID сессии скопирован в буфер обмена!", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
};

grp.Controls.AddRange(new Control[] { lblProj, txtProj, btnCopyProj, lblSess, txtSess, btnCopySess });

Button btnClose = new Button();
btnClose.Text = "Закрыть";
btnClose.Top = 120;
btnClose.Left = 300;
btnClose.Width = 85;
btnClose.Height = 30;
btnClose.FlatStyle = FlatStyle.Flat;
btnClose.FlatAppearance.BorderColor = Color.FromArgb(200, 208, 219);
btnClose.Click += (s, e) => form.Close();
form.Controls.Add(btnClose);

form.AcceptButton = btnClose;
form.CancelButton = btnClose;

form.Load += (s, e) => {
    btnCopyProj.Height = txtProj.Height;
    btnCopyProj.Width = txtProj.Height;
    btnCopyProj.Top = txtProj.Top;
    btnCopyProj.Left = grp.Width - 15 - btnCopyProj.Width;
    txtProj.Width = btnCopyProj.Left - txtProj.Left - 5;

    btnCopySess.Height = txtSess.Height;
    btnCopySess.Width = txtSess.Height;
    btnCopySess.Top = txtSess.Top;
    btnCopySess.Left = grp.Width - 15 - btnCopySess.Width;
    txtSess.Width = btnCopySess.Left - txtSess.Left - 5;
};

form.ResumeLayout(false);
form.PerformLayout();

form.ShowDialog();
form.Dispose();
copyIcon.Dispose();
