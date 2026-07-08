using System;
using System.Linq;
using System.Windows.Forms;
using System.Text;
using System.Collections.Generic;
using Renga;

string B64_STD = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
string B64_IFC = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz_$";

byte[] DecodeIfcGuid(string ifc)
{
    StringBuilder sb = new StringBuilder();
    foreach (char ch in ifc)
    {
        int i = B64_IFC.IndexOf(ch);
        sb.Append(B64_STD[i]);
    }
    byte[] bytes18 = Convert.FromBase64String("AA" + sb.ToString());
    return bytes18.Skip(2).Take(16).ToArray();
}

Guid GetGuidStd(byte[] b)
{
    string hex = BitConverter.ToString(b).Replace("-", "").ToLower();
    return Guid.Parse($"{hex.Substring(0,8)}-{hex.Substring(8,4)}-{hex.Substring(12,4)}-{hex.Substring(16,4)}-{hex.Substring(20)}");
}

Guid GetGuidDn(byte[] b)
{
    var d1 = b.Take(4).Reverse().ToArray();
    var d2 = b.Skip(4).Take(2).Reverse().ToArray();
    var d3 = b.Skip(6).Take(2).Reverse().ToArray();
    var d4 = b.Skip(8).ToArray();
    var all = d1.Concat(d2).Concat(d3).Concat(d4).ToArray();
    string hex = BitConverter.ToString(all).Replace("-", "").ToLower();
    return Guid.Parse($"{hex.Substring(0,8)}-{hex.Substring(8,4)}-{hex.Substring(12,4)}-{hex.Substring(16,4)}-{hex.Substring(20)}");
}

Form f = new Form() { AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi,  
    Text = "Поиск по IfcGUID", 
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
Label l = new Label() { Text = "Введите IfcGUID (22 символа):", Top = 15, Left = 15, Width = 300, AutoSize = true };
TextBox t = new TextBox() { Top = 40, Left = 15, Width = 555, Font = new System.Drawing.Font("Segoe UI", 10) };

Action<int> performAction = (action) => {
    var inputs = t.Text.Split(new[] { ',', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries);
    if (inputs.Length == 0) return;
    
    var modelObjs = RengaApp.Project.Model.GetObjects();
    List<int> foundIds = new List<int>();
    List<string> notFound = new List<string>();

    var guidMap = new Dictionary<Guid, int>();
    for (int i = 0; i < modelObjs.Count; i++)
    {
        var obj = modelObjs.GetByIndex(i);
        if (obj != null) guidMap[obj.UniqueId] = obj.Id;
    }

    foreach (var ifc in inputs)
    {
        if (ifc.Length != 22) { notFound.Add(ifc); continue; }
        try {
            var b = DecodeIfcGuid(ifc);
            Guid g1 = GetGuidStd(b);
            Guid g2 = GetGuidDn(b);
            
            if (guidMap.ContainsKey(g1)) foundIds.Add(guidMap[g1]);
            else if (guidMap.ContainsKey(g2)) foundIds.Add(guidMap[g2]);
            else notFound.Add(ifc);
        } catch { notFound.Add(ifc); }
    }

    if (foundIds.Count > 0)
    {
        try {
            if (action == 0) {
                RengaApp.Selection.SetSelectedObjects(foundIds.ToArray());
            } else if (action == 1) {
                var view = RengaApp.ActiveView as Renga.IModelView;
                if (view != null) view.SetObjectsVisibility(foundIds.ToArray(), false);
                else MessageBox.Show("Активный вид не является 3D видом.", "Внимание");
            } else if (action == 2) {
                var view = RengaApp.ActiveView as Renga.IModelView;
                if (view != null) {
                    var vis = view.GetVisibleObjects();
                    if (vis != null) view.SetObjectsVisibility(vis, false);
                    view.SetObjectsVisibility(foundIds.ToArray(), true);
                } else MessageBox.Show("Активный вид не является 3D видом.", "Внимание");
            } else if (action == 3) {
                var view = RengaApp.ActiveView as Renga.IModelView;
                if (view != null) view.SetObjectsVisibility(foundIds.ToArray(), true);
                else MessageBox.Show("Активный вид не является 3D видом.", "Внимание");
            }
        } catch (Exception ex) {
            MessageBox.Show("Ошибка: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
    
    if (notFound.Count > 0)
    {
        MessageBox.Show($"Не удалось найти объекты для следующих IfcGUID:\n{string.Join(", ", notFound)}", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
