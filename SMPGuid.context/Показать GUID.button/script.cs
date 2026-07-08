using System;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using Renga;

string EncodeIfcGuid(Guid guid)
{
    byte[] bytes = guid.ToByteArray();
    byte[] b = new byte[16];
    b[0] = bytes[3]; b[1] = bytes[2]; b[2] = bytes[1]; b[3] = bytes[0];
    b[4] = bytes[5]; b[5] = bytes[4]; b[6] = bytes[7]; b[7] = bytes[6];
    Array.Copy(bytes, 8, b, 8, 8);
    byte[] bytes18 = new byte[18];
    Array.Copy(b, 0, bytes18, 2, 16);
    string stdBase64 = Convert.ToBase64String(bytes18);
    string b64std_str = stdBase64.Substring(2);
    string B64_STD = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
    string B64_IFC = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz_$";
    StringBuilder sb = new StringBuilder(22);
    foreach (char ch in b64std_str) {
        int i = B64_STD.IndexOf(ch);
        sb.Append(B64_IFC[i]);
    }
    return sb.ToString();
}

var project = RengaApp.Project;
if (project == null) {
    MessageBox.Show("Нет открытого проекта.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
    return;
}

var selections = RengaApp.Selection.GetSelectedObjects() as Array;
if (selections == null || selections.Length == 0) {
    MessageBox.Show("Пожалуйста, выделите элемент в модели.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    return;
}

int id = (int)selections.GetValue(0);
var mo = project.Model.GetObjects().GetById(id);
if (mo == null) return;

string rengaIdStr = mo.Id.ToString();
string uniqueIdStr = mo.UniqueId.ToString();
string ifcGuidStr = EncodeIfcGuid(mo.UniqueId);

System.Windows.Forms.Application.EnableVisualStyles();
Form form = new Form();
form.SuspendLayout();
form.Text = "Информация об элементе";
form.Font = new Font("Segoe UI", 9);
form.AutoScaleDimensions = new SizeF(96F, 96F);
form.AutoScaleMode = AutoScaleMode.Dpi;
form.ClientSize = new Size(400, 192);
form.StartPosition = FormStartPosition.CenterScreen;
form.FormBorderStyle = FormBorderStyle.FixedDialog;
form.MaximizeBox = false;
form.MinimizeBox = false;
form.BackColor = System.Drawing.Color.White;

GroupBox grp = new GroupBox();
grp.Text = "Идентификаторы выделенного элемента";
grp.Top = 15;
grp.Left = 15;
grp.Width = 370;
grp.Height = 125;
form.Controls.Add(grp);

Font txtFont = new Font("Segoe UI", 10);
ToolTip tip = new ToolTip();

Bitmap copyIcon = new Bitmap(12, 12);
Graphics g = Graphics.FromImage(copyIcon);
g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
Pen p = new Pen(System.Drawing.Color.FromArgb(100, 100, 100), 1);
g.DrawRectangle(p, 1, 4, 6, 7);
g.DrawLine(p, 3, 0, 9, 0);
g.DrawLine(p, 9, 0, 9, 7);
g.DrawLine(p, 7, 7, 9, 7);
g.DrawLine(p, 3, 0, 3, 3);
p.Dispose();
g.Dispose();

Label lbl1 = new Label() { Text = "Renga ID:", Top = 25, Left = 15, AutoSize = true };
TextBox txt1 = new TextBox() { Text = rengaIdStr, Top = 22, Left = 120, Width = 200, ReadOnly = true, Font = txtFont };
Button btnCopy1 = new Button() { Image = copyIcon, Top = 22, FlatStyle = FlatStyle.Flat };
btnCopy1.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(200, 208, 219);
tip.SetToolTip(btnCopy1, "Копировать Renga ID");
btnCopy1.Click += (s, e) => {
    if (!string.IsNullOrEmpty(txt1.Text)) {
        Clipboard.SetText(txt1.Text);
        MessageBox.Show("Renga ID скопирован в буфер обмена!", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
};

Label lbl2 = new Label() { Text = "Уникальный\nидентификатор:", Top = 50, Left = 15, AutoSize = true };
TextBox txt2 = new TextBox() { Text = uniqueIdStr, Top = 55, Left = 120, Width = 200, ReadOnly = true, Font = txtFont };
Button btnCopy2 = new Button() { Image = copyIcon, Top = 55, FlatStyle = FlatStyle.Flat };
btnCopy2.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(200, 208, 219);
tip.SetToolTip(btnCopy2, "Копировать уникальный идентификатор");
btnCopy2.Click += (s, e) => {
    if (!string.IsNullOrEmpty(txt2.Text)) {
        Clipboard.SetText(txt2.Text);
        MessageBox.Show("Уникальный идентификатор скопирован в буфер обмена!", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
};

Label lbl3 = new Label() { Text = "IfcGUID:", Top = 88, Left = 15, AutoSize = true };
TextBox txt3 = new TextBox() { Text = ifcGuidStr, Top = 85, Left = 120, Width = 200, ReadOnly = true, Font = txtFont };
Button btnCopy3 = new Button() { Image = copyIcon, Top = 85, FlatStyle = FlatStyle.Flat };
btnCopy3.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(200, 208, 219);
tip.SetToolTip(btnCopy3, "Копировать IfcGUID");
btnCopy3.Click += (s, e) => {
    if (!string.IsNullOrEmpty(txt3.Text)) {
        Clipboard.SetText(txt3.Text);
        MessageBox.Show("IfcGUID скопирован в буфер обмена!", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
};

grp.Controls.AddRange(new Control[] { lbl1, txt1, btnCopy1, lbl2, txt2, btnCopy2, lbl3, txt3, btnCopy3 });

Button btnClose = new Button();
btnClose.Text = "Закрыть";
btnClose.Top = 150;
btnClose.Left = 300;
btnClose.Width = 85;
btnClose.Height = 30;
btnClose.FlatStyle = FlatStyle.Flat;
btnClose.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(200, 208, 219);
btnClose.Click += (s, e) => form.Close();
form.Controls.Add(btnClose);

form.AcceptButton = btnClose;
form.CancelButton = btnClose;

form.Load += (s, e) => {
    btnCopy1.Height = txt1.Height;
    btnCopy1.Width = txt1.Height;
    btnCopy1.Top = txt1.Top;
    btnCopy1.Left = grp.Width - 15 - btnCopy1.Width;
    txt1.Width = btnCopy1.Left - txt1.Left - 5;

    btnCopy2.Height = txt2.Height;
    btnCopy2.Width = txt2.Height;
    btnCopy2.Top = txt2.Top;
    btnCopy2.Left = grp.Width - 15 - btnCopy2.Width;
    txt2.Width = btnCopy2.Left - txt2.Left - 5;

    btnCopy3.Height = txt3.Height;
    btnCopy3.Width = txt3.Height;
    btnCopy3.Top = txt3.Top;
    btnCopy3.Left = grp.Width - 15 - btnCopy3.Width;
    txt3.Width = btnCopy3.Left - txt3.Left - 5;
};

form.ResumeLayout(false);
form.PerformLayout();

form.ShowDialog();
form.Dispose();
copyIcon.Dispose();
