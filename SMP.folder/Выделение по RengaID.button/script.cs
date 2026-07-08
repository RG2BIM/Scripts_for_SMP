using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;
using Renga;

Form f = new Form() { AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi,  
    Text = "Выделение элементов", 
    Width = 600, 
    Height = 208, 
    TopMost = true,
    StartPosition = FormStartPosition.CenterScreen,
    FormBorderStyle = FormBorderStyle.FixedDialog,
    MaximizeBox = false,
    MinimizeBox = false,
    Font = new System.Drawing.Font("Segoe UI", 9),
    BackColor = System.Drawing.Color.White
};
Label l = new Label() { Text = "Введите ID (через запятую или пробел):", Top = 15, Left = 15, Width = 300, AutoSize = true };
TextBox t = new TextBox() { Top = 40, Left = 15, Width = 555, Font = new System.Drawing.Font("Segoe UI", 10) };

Action<int> performAction = (action) => {
    var strIds = t.Text.Split(new[] { ',', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries);
    var ids = strIds.Select(s => { int r; return int.TryParse(s, out r) ? r : -1; }).Where(id => id > 0).ToArray();
    if (ids.Length == 0) return;
    
    var modelObjs = RengaApp.Project.Model.GetObjects();
    List<int> validIds = new List<int>();
    List<int> notFound = new List<int>();

    foreach (var id in ids)
    {
        try {
            var obj = modelObjs.GetById(id);
            if (obj != null) validIds.Add(id);
            else notFound.Add(id);
        } catch { notFound.Add(id); }
    }

    if (validIds.Count > 0)
    {
        try {
            if (action == 0) {
                RengaApp.Selection.SetSelectedObjects(validIds.ToArray());
            } else if (action == 1) {
                var view = RengaApp.ActiveView as Renga.IModelView;
                if (view != null) view.SetObjectsVisibility(validIds.ToArray(), false);
                else MessageBox.Show("Активный вид не является 3D видом.", "Внимание");
            } else if (action == 2) {
                var view = RengaApp.ActiveView as Renga.IModelView;
                if (view != null) {
                    var vis = view.GetVisibleObjects();
                    if (vis != null) view.SetObjectsVisibility(vis, false);
                    view.SetObjectsVisibility(validIds.ToArray(), true);
                } else MessageBox.Show("Активный вид не является 3D видом.", "Внимание");
            } else if (action == 3) {
                var view = RengaApp.ActiveView as Renga.IModelView;
                if (view != null) view.SetObjectsVisibility(validIds.ToArray(), true);
                else MessageBox.Show("Активный вид не является 3D видом.", "Внимание");
            }
        } catch (Exception ex) {
            MessageBox.Show("Ошибка: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
    
    if (notFound.Count > 0)
    {
        MessageBox.Show($"Не удалось найти объекты со следующими ID:\n{string.Join(", ", notFound)}", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
};

Button btnSel = new Button() { Text = "Выделить", Top = 75, Left = 15, Width = 135, Height = 32, FlatStyle = FlatStyle.Flat, BackColor = System.Drawing.Color.FromArgb(26, 115, 232), ForeColor = System.Drawing.Color.White };
btnSel.FlatAppearance.BorderSize = 0;
btnSel.Click += (s, e) => performAction(0);

Button btnHide = new Button() { Text = "Скрыть", Top = 75, Left = 155, Width = 135, Height = 32, FlatStyle = FlatStyle.Flat, BackColor = System.Drawing.Color.FromArgb(52, 168, 83), ForeColor = System.Drawing.Color.White };
btnHide.FlatAppearance.BorderSize = 0;
btnHide.Click += (s, e) => performAction(1);

Button btnIso = new Button() { Text = "Изолировать", Top = 75, Left = 295, Width = 135, Height = 32, FlatStyle = FlatStyle.Flat, BackColor = System.Drawing.Color.FromArgb(142, 36, 170), ForeColor = System.Drawing.Color.White };
btnIso.FlatAppearance.BorderSize = 0;
btnIso.Click += (s, e) => performAction(2);

Button btnShow = new Button() { Text = "Показать", Top = 75, Left = 435, Width = 135, Height = 32, FlatStyle = FlatStyle.Flat, BackColor = System.Drawing.Color.FromArgb(251, 188, 5), ForeColor = System.Drawing.Color.Black };
btnShow.FlatAppearance.BorderSize = 0;
btnShow.Click += (s, e) => performAction(3);

Button btnClose = new Button() { Text = "Закрыть", Top = 115, Left = 435, Width = 135, Height = 32, FlatStyle = FlatStyle.Flat };
btnClose.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(200, 208, 219);
btnClose.Click += (s, e) => f.Close();

f.Controls.Add(l); f.Controls.Add(t); f.Controls.Add(btnSel); f.Controls.Add(btnHide); f.Controls.Add(btnIso); f.Controls.Add(btnShow); f.Controls.Add(btnClose);
f.AcceptButton = btnSel;
f.CancelButton = btnClose;

f.Show();
