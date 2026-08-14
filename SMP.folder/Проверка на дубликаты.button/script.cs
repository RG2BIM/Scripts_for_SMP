using System;
using System.Text;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.Linq;
using Renga;

var project = RengaApp.Project;

// Структура для 3D Габарита (Bounding Box)
class BBox {
    public float MinX = float.MaxValue, MinY = float.MaxValue, MinZ = float.MaxValue;
    public float MaxX = float.MinValue, MaxY = float.MinValue, MaxZ = float.MinValue;

    public void Add(float x, float y, float z) {
        if (x < MinX) MinX = x;
        if (y < MinY) MinY = y;
        if (z < MinZ) MinZ = z;
        if (x > MaxX) MaxX = x;
        if (y > MaxY) MaxY = y;
        if (z > MaxZ) MaxZ = z;
    }
}

// Извлечение BBox из 3D-геометрии
BBox GetBBox(IExportedObject3D obj3D) {
    BBox box = new BBox();
    for (int mi = 0; mi < obj3D.MeshCount; mi++) {
        var mesh = obj3D.GetMesh(mi);
        for (int gi = 0; gi < mesh.GridCount; gi++) {
            var grid = mesh.GetGrid(gi);
            for (int vi = 0; vi < grid.VertexCount; vi++) {
                float x = 0, y = 0, z = 0;
                grid.GetVertexComponents(vi, out x, out y, out z);
                box.Add(x, y, z);
            }
        }
    }
    return box;
}

// Генератор IfcGUID
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

class DuplicateItem {
    public int Id;
    public string IfcGuid;
    public string Name;
}

// 1. Извлекаем объекты модели
var modelObjects = new Dictionary<int, IModelObject>();
var objectCollection = project.Model.GetObjects();
for (int i = 0; i < objectCollection.Count; i++) {
    var mo = objectCollection.GetByIndex(i);
    modelObjects[mo.Id] = mo;
}

// 2. Окно загрузки и Экспорт геометрии
Form progressForm = new Form() { AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi, 
    Text = "Поиск дубликатов...",
    Size = new Size(400, 120),
    StartPosition = FormStartPosition.CenterScreen,
    FormBorderStyle = FormBorderStyle.FixedToolWindow,
    TopMost = true
};
Label progressLabel = new Label() { Text = "Экспорт 3D-геометрии...", Top = 10, Left = 10, Width = 360 };
ProgressBar progressBar = new ProgressBar() { Top = 40, Left = 10, Width = 360 };
progressForm.Controls.Add(progressLabel);
progressForm.Controls.Add(progressBar);
progressForm.Show();
System.Windows.Forms.Application.DoEvents();

// (Это нужно только для получения 3D-координат элементов. Никакие файлы на диск не сохраняются!)
var exportedCollection = project.DataExporter.GetObjects3D();

// 3. Анализируем дубликаты
var dict = new Dictionary<string, List<DuplicateItem>>();

// Создаем словарь экспортированной 3D геометрии для быстрого доступа
var exportedDict = new Dictionary<int, IExportedObject3D>();
for (int i = 0; i < exportedCollection.Count; i++) {
    var obj3D = exportedCollection.Get(i);
    exportedDict[obj3D.ModelObjectId] = obj3D;
}

progressBar.Maximum = modelObjects.Count;
int currentIndex = 0;

foreach (var kvp in modelObjects) {
    currentIndex++;
    if (currentIndex % 25 == 0) {
        progressBar.Value = currentIndex;
        progressLabel.Text = $"Анализ объектов: {currentIndex} из {modelObjects.Count}";
        System.Windows.Forms.Application.DoEvents();
    }
    
    int id = kvp.Key;
    var mo = kvp.Value;
    
    // Пропускаем помещения, сборки, оси, фасады, разрезы
    string objType = mo.ObjectTypeS.ToUpper();
    if (objType == "{F1A805FF-573D-F46B-FFBA-57F4BCCAA6ED}" || // Помещение
        objType == "{818A2346-60DB-4011-8575-B943A6605DCA}" || // Сборка
        objType == "{4B41CCF8-C969-4C55-A1F2-CCED9C164F07}" || // Ось
        objType == "{0A7B06CF-0D2B-4EE0-BB7A-5AE1B088F5E9}" || // Фасад
        objType == "{B50ED483-E138-4A17-AEBD-A8DBE79F053E}" || // Разрез
        mo.Name == "Соединение")   // Автоматические соединения трасс
        continue;
        
    string hash = "";
    
    if (exportedDict.TryGetValue(id, out var obj3D) && obj3D.MeshCount > 0) {
        var bbox = GetBBox(obj3D);
        hash = $"BBOX_{objType}_{Math.Round(bbox.MinX, 3)}_{Math.Round(bbox.MinY, 3)}_{Math.Round(bbox.MinZ, 3)}_{Math.Round(bbox.MaxX, 3)}_{Math.Round(bbox.MaxY, 3)}_{Math.Round(bbox.MaxZ, 3)}";
    } else {
        // Пропускаем объекты без 3D геометрии (2D-размеры, тексты, выноски, пустые объекты), 
        // так как без координат их нельзя проверить на пространственные дубликаты.
        continue;
    }
    
    if (!dict.ContainsKey(hash)) {
        dict[hash] = new List<DuplicateItem>();
    }
    dict[hash].Add(new DuplicateItem { 
        Id = id, 
        IfcGuid = EncodeIfcGuid(mo.UniqueId),
        Name = mo.Name
    });
}

progressForm.Close();

// 4. Оставляем только те группы, где больше 1 элемента
var duplicates = dict.Values.Where(v => v.Count > 1).ToList();

if (duplicates.Count == 0) {
    MessageBox.Show("Дубликатов не найдено! Модель чистая.", "Отличные новости", MessageBoxButtons.OK, MessageBoxIcon.Information);
    return;
}

// 5. Создаем интерфейс
try { System.Windows.Forms.Application.EnableVisualStyles(); } catch { }
Form form = new Form() { AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi };
form.Text = "Найденные дубликаты";
form.Width = 700;
form.Height = 600;
form.StartPosition = FormStartPosition.CenterScreen;
form.FormBorderStyle = FormBorderStyle.FixedDialog;
form.MaximizeBox = false;
form.MinimizeBox = false;
form.BackColor = System.Drawing.Color.White;
form.Font = new System.Drawing.Font("Segoe UI", 9);

GroupBox grpList = new GroupBox() { Text = "Список дубликатов", Top = 15, Left = 15, Width = 655, Height = 470 };
form.Controls.Add(grpList);

Label lblInfo = new Label() { Text = $"Обнаружено групп дубликатов: {duplicates.Count}", Top = 25, Left = 15, Width = 550, Font = new System.Drawing.Font("Segoe UI", 9, System.Drawing.FontStyle.Bold) };
grpList.Controls.Add(lblInfo);

DataGridView grid = new DataGridView() {
    Top = 50, Left = 15, Width = 625, Height = 400,
    AllowUserToAddRows = false, AllowUserToDeleteRows = false, AllowUserToResizeRows = false,
    RowHeadersVisible = false, ColumnHeadersVisible = false,
    BackgroundColor = System.Drawing.Color.White,
    CellBorderStyle = DataGridViewCellBorderStyle.None,
    SelectionMode = DataGridViewSelectionMode.FullRowSelect,
    Font = new System.Drawing.Font("Segoe UI", 10),
    ReadOnly = false
};
grid.Columns.Add(new DataGridViewCheckBoxColumn() { Width = 30, ReadOnly = false });
grid.Columns.Add(new DataGridViewTextBoxColumn() { AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, ReadOnly = true });
grpList.Controls.Add(grid);

// Тогл галочки при клике на текст
grid.CellClick += (s, e) => {
    if (e.RowIndex >= 0 && e.ColumnIndex == 1) { // Если кликнули по столбцу с текстом
        if (grid.Rows[e.RowIndex].Cells[0] is DataGridViewCheckBoxCell chkCell) {
            bool currentValue = chkCell.Value != null && (bool)chkCell.Value;
            chkCell.Value = !currentValue;
        }
    }
};

// Заполняем список
for (int i = 0; i < duplicates.Count; i++) {
    var groupList = duplicates[i];
    string elemName = string.IsNullOrEmpty(groupList[0].Name) ? "Элемент" : groupList[0].Name;
    
    int headerRow = grid.Rows.Add(false, $"=== Группа {i + 1} ({elemName}): {groupList.Count} шт. ===");
    grid.Rows[headerRow].Cells[0] = new DataGridViewTextBoxCell(); // Удаляем квадратик!
    grid.Rows[headerRow].Cells[0].Value = "";
    grid.Rows[headerRow].ReadOnly = true;
    
    foreach (var item in groupList) {
        grid.Rows.Add(false, $"    RengaID: {item.Id} | IfcGUID: {item.IfcGuid}");
    }
}

Button btnSelect = new Button() { Text = "Выделить элемент", Top = 505, Left = 15, Width = 180, Height = 32 };
btnSelect.FlatStyle = FlatStyle.Flat;
btnSelect.BackColor = System.Drawing.Color.FromArgb(26, 115, 232);
btnSelect.ForeColor = System.Drawing.Color.White;
btnSelect.FlatAppearance.BorderSize = 0;

Button btnDelete = new Button() { Text = "Удалить элемент", Top = 505, Left = 210, Width = 180, Height = 32 };
btnDelete.FlatStyle = FlatStyle.Flat;
btnDelete.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(200, 208, 219);
// Внимание: API Renga не позволяет удалять объекты напрямую через скрипт. 
// Кнопка будет выделять элемент и просить нажать Delete на клавиатуре.

Button btnClose = new Button() { Text = "Закрыть", Top = 505, Left = 485, Width = 180, Height = 32 };
btnClose.FlatStyle = FlatStyle.Flat;
btnClose.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(200, 208, 219);

Action<bool> handleItemAction = (isDelete) => {
    grid.EndEdit(); // Применяем галочки
    
    var idsToProcess = new List<int>();
    var rowsToRemove = new List<DataGridViewRow>();
    
    foreach (DataGridViewRow row in grid.Rows) {
        if (row.Cells[0] is DataGridViewCheckBoxCell chkCell && chkCell.Value != null && (bool)chkCell.Value == true) {
            try {
                string text = row.Cells[1].Value.ToString();
                string idPart = text.Split('|')[0].Replace("RengaID:", "").Trim();
                if (int.TryParse(idPart, out int id)) {
                    idsToProcess.Add(id);
                    rowsToRemove.Add(row);
                }
            } catch {}
        }
    }
    
    if (idsToProcess.Count == 0) {
        MessageBox.Show("Сперва отметьте элементы галочками в списке.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return;
    }
    
    if (idsToProcess.Count == 0) {
        MessageBox.Show("Вы выбрали только заголовки групп. Пожалуйста, выберите сами элементы.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return;
    }

    if (isDelete) {
        var op = project.StartOperationWithUndo(project.Model.Id);
        foreach (int id in idsToProcess) {
            try { project.Model.DeleteObjectById(id); } catch { }
        }
        op.Apply();
        
        // Удаляем выбранные строчки
        foreach (var row in rowsToRemove) {
            grid.Rows.Remove(row);
        }
        
        // Удаляем пустые группы или те, где остался только 1 элемент
        var rowsToDeleteClean = new List<DataGridViewRow>();
        for (int i = 0; i < grid.Rows.Count; i++) {
            if (grid.Rows[i].Cells[0] is DataGridViewTextBoxCell) { // Это заголовок
                int count = 0;
                int j = i + 1;
                while (j < grid.Rows.Count && !(grid.Rows[j].Cells[0] is DataGridViewTextBoxCell)) {
                    count++;
                    j++;
                }
                if (count <= 1) { // 1 или 0 элементов осталось
                    for (int k = i; k < i + 1 + count; k++) {
                        if (k < grid.Rows.Count) rowsToDeleteClean.Add(grid.Rows[k]);
                    }
                }
            }
        }
        
        foreach (var row in rowsToDeleteClean) {
            if (grid.Rows.Contains(row)) grid.Rows.Remove(row);
        }
        
        MessageBox.Show($"Элементов удалено: {idsToProcess.Count}.\nОставшиеся уникальные элементы убраны из списка дубликатов.", "Удаление завершено", MessageBoxButtons.OK, MessageBoxIcon.Information);
    } else {
        RengaApp.Selection.SetSelectedObjects(idsToProcess.ToArray());
        form.Close();
    }
};

btnSelect.Click += (s, e) => { handleItemAction(false); };
btnDelete.Click += (s, e) => { handleItemAction(true); };
btnClose.Click += (s, e) => { form.Close(); };

form.Controls.Add(btnSelect);
form.Controls.Add(btnDelete);
form.Controls.Add(btnClose);
form.ShowDialog();
