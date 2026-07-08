using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Renga;

var project = RengaApp.Project;
if (project == null)
{
    MessageBox.Show("Откройте проект перед запуском плагина.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
    return;
}

var form = new AxisNamingForm();
if (form.ShowDialog() != DialogResult.OK)
    return;

bool onlySelected = form.rbSel.Checked;
string vStart = form.txtNum.Text.Trim();
string hStart = form.txtLet.Text.Trim();
var axes = GetAxes(project, RengaApp.Selection, onlySelected);
if (axes.Count == 0)
{
    MessageBox.Show("Не найдено осей для обработки.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Information);
    return;
}

// 1. Сохраняем оригинальные имена и временно переименовываем оси в их ID
var opRename = project.CreateOperation();
opRename.Start();
var originalNames = new Dictionary<int, string>();
foreach (var axis in axes)
{
    originalNames[axis.Id] = GetName(axis);
    SetName(axis, axis.Id.ToString());
}
opRename.Apply();

// 2. Экспорт в IFC
string tempIfc = Path.Combine(Path.GetTempPath(), "renga_axes_" + Guid.NewGuid().ToString() + ".ifc");
var axisCoords = new Dictionary<int, AxisData>();

try
{
    var ifcSettings = RengaApp.CreateIfcExportSettings();
    project.ExportObjectsToIFC(tempIfc, axes.Select(a => a.Id).ToArray(), true, ifcSettings);

    // 3. Парсинг координат из IFC
    var pointLists = new Dictionary<string, string>();
    var is3DList = new Dictionary<string, bool>(); 
    var polyCurves = new Dictionary<string, string>();

    foreach (var line in File.ReadAllLines(tempIfc))
    {
        if (line.Contains("IFCCARTESIANPOINTLIST"))
        {
            var parts = line.Split(new[] { '=' }, 2);
            string id = parts[0].Trim();
            string content = parts[1].Trim();
            int start = content.IndexOf("(((");
            if (start != -1) {
                int end = content.IndexOf(")))", start);
                string coordsStr = content.Substring(start + 3, end - start - 3).Replace("),(", ",");
                pointLists[id] = coordsStr;
                is3DList[id] = content.Contains("3D");
            }
        }
        else if (line.Contains("IFCINDEXEDPOLYCURVE"))
        {
            var parts = line.Split(new[] { '=' }, 2);
            string id = parts[0].Trim();
            string content = parts[1].Trim();
            string refId = content.Split('(')[1].Split(',')[0].Trim();
            polyCurves[id] = refId;
        }
        else if (line.Contains("IFCGRIDAXIS"))
        {
            var parts = line.Split(new[] { '=' }, 2);
            string content = parts[1].Trim();
            string args = content.Substring(content.IndexOf('(') + 1);
            string nameParam = args.Split(',')[0].Trim('\'');
            string refId = args.Split(',')[1].Trim();
            
            if (int.TryParse(nameParam, out int rengaId))
            {
                if (polyCurves.TryGetValue(refId, out string ptId) && pointLists.TryGetValue(ptId, out string coordsStr))
                {
                    var cc = coordsStr.Split(',');
                    bool is3d = is3DList.ContainsKey(ptId) && is3DList[ptId];
                    if (!is3d && cc.Length >= 4)
                    {
                        axisCoords[rengaId] = new AxisData { 
                            X1 = double.Parse(cc[0], System.Globalization.CultureInfo.InvariantCulture), 
                            Y1 = double.Parse(cc[1], System.Globalization.CultureInfo.InvariantCulture), 
                            X2 = double.Parse(cc[2], System.Globalization.CultureInfo.InvariantCulture), 
                            Y2 = double.Parse(cc[3], System.Globalization.CultureInfo.InvariantCulture) 
                        };
                    }
                    else if (is3d && cc.Length >= 6)
                    {
                        axisCoords[rengaId] = new AxisData { 
                            X1 = double.Parse(cc[0], System.Globalization.CultureInfo.InvariantCulture), 
                            Y1 = double.Parse(cc[1], System.Globalization.CultureInfo.InvariantCulture), 
                            X2 = double.Parse(cc[3], System.Globalization.CultureInfo.InvariantCulture), 
                            Y2 = double.Parse(cc[4], System.Globalization.CultureInfo.InvariantCulture) 
                        };
                    }
                }
            }
        }
    }
}
catch (Exception ex)
{
    MessageBox.Show("Ошибка экспорта/чтения IFC: " + ex.Message);
}
finally
{
    if (File.Exists(tempIfc)) File.Delete(tempIfc);
}

// 4. Восстанавливаем оригинальные имена на случай если что-то не отработает дальше
var opRevert = project.CreateOperation();
opRevert.Start();
foreach (var axis in axes) {
    if (originalNames.ContainsKey(axis.Id)) SetName(axis, originalNames[axis.Id]);
}
opRevert.Apply();

if (axisCoords.Count == 0)
{
    MessageBox.Show("Не удалось получить координаты осей.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
    return;
}

// 5. Разделение и сортировка
var sortedAxes = new List<AxisData>();

foreach (var axis in axes)
{
    if (axisCoords.TryGetValue(axis.Id, out var data))
    {
        data.Axis = axis;
        sortedAxes.Add(data);
    }
}

// Сначала вертикальные (по X), затем горизонтальные (по Y)
var verticalAxes = sortedAxes.Where(a => a.Dy > a.Dx).OrderBy(a => a.MidX).ToList();
var horizontalAxes = sortedAxes.Where(a => a.Dx >= a.Dy).OrderBy(a => a.MidY).ToList();

var opFinish = project.CreateOperation();
opFinish.Start();

int vCounter = 0;
int hCounter = 0;
for (int i = 0; i < verticalAxes.Count; i++)
{
    SetName(verticalAxes[i].Axis, GetNextName(vStart, vCounter++));
}
for (int i = 0; i < horizontalAxes.Count; i++)
{
    SetName(horizontalAxes[i].Axis, GetNextName(hStart, hCounter++));
}

opFinish.Apply();

MessageBox.Show(string.Format("Успешно переименовано осей: {0}", sortedAxes.Count), 
    "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);


// ==============================================================================================
// Вспомогательные функции
// ==============================================================================================

static readonly string[] RussianAlphabet = new string[] 
{
    "А", "Б", "В", "Г", "Д", "Е", "Ж", "И", "К", "Л", "М", "Н", "П", "Р", "С", "Т", "У", "Ф", "Х", "Ц", "Ш", "Щ", "Э", "Ю", "Я"
};

string GetLetterName(int index)
{
    if (index < RussianAlphabet.Length)
        return RussianAlphabet[index];
    
    int firstIndex = (index / RussianAlphabet.Length) - 1;
    int secondIndex = index % RussianAlphabet.Length;
    return string.Format("{0}{1}", RussianAlphabet[firstIndex], RussianAlphabet[secondIndex]);
}

int GetLetterNameIndex(string letter)
{
    if (string.IsNullOrEmpty(letter)) return 0;
    letter = letter.ToUpper();
    
    if (letter.Length == 2)
    {
        int i1 = Array.IndexOf(RussianAlphabet, letter[0].ToString());
        int i2 = Array.IndexOf(RussianAlphabet, letter[1].ToString());
        if (i1 != -1 && i2 != -1)
            return (i1 + 1) * RussianAlphabet.Length + i2;
    }
    else
    {
        int i1 = Array.IndexOf(RussianAlphabet, letter);
        if (i1 != -1) return i1;
    }
    return 0;
}

string GetNextName(string startValue, int offset)
{
    if (int.TryParse(startValue, out int startNum))
    {
        return (startNum + offset).ToString();
    }
    else
    {
        int startIndex = GetLetterNameIndex(startValue);
        return GetLetterName(startIndex + offset);
    }
}

string GetName(IModelObject axis)
{
    var properties = axis.GetParameters();
    if (properties == null) return "";
    Guid nameParamId = new Guid("DA64F3A9-4BB1-4FF8-BD50-706A2BDB81AD");
    if (properties.Contains(nameParamId)) {
        var param = properties.Get(nameParamId);
        if (param.HasValue) return param.GetStringValue();
    }
    return "";
}

void SetName(IModelObject axis, string name)
{
    var properties = axis.GetParameters();
    if (properties == null) return;
    
    Guid nameParamId = new Guid("DA64F3A9-4BB1-4FF8-BD50-706A2BDB81AD"); // GridLineName
    if (properties.Contains(nameParamId))
    {
        var param = properties.Get(nameParamId);
        param.SetStringValue(name);
    }
}

List<IModelObject> GetAxes(IProject proj, ISelection selection, bool onlySel)
{
    var result = new List<IModelObject>();
    var model = proj.Model;
    var collection = model.GetObjects();
    var axisGuid = new Guid("4B41CCF8-C969-4C55-A1F2-CCED9C164F07");

    if (onlySel && selection != null)
    {
        var idsObj = selection.GetSelectedObjects();
        var array = idsObj as Array;
        if (array != null)
        {
            foreach (var item in array)
            {
                int id = Convert.ToInt32(item);
                var obj = collection.GetById(id);
                if (obj != null && obj.ObjectType == axisGuid)
                {
                    result.Add(obj);
                }
            }
        }
    }
    else
    {
        for (int i = 0; i < collection.Count; i++)
        {
            var obj = collection.GetByIndex(i);
            if (obj.ObjectType == axisGuid)
            {
                result.Add(obj);
            }
        }
    }
    return result;
}

class AxisData
{
    public IModelObject Axis { get; set; }
    public double X1 { get; set; }
    public double Y1 { get; set; }
    public double X2 { get; set; }
    public double Y2 { get; set; }
    
    public double MidX { get { return (X1 + X2) / 2.0; } }
    public double MidY { get { return (Y1 + Y2) / 2.0; } }
    public double Dx { get { return Math.Abs(X2 - X1); } }
    public double Dy { get { return Math.Abs(Y2 - Y1); } }
}

class AxisNamingForm : Form
{
    public RadioButton rbAll;
    public RadioButton rbSel;
    public TextBox txtNum;
    public TextBox txtLet;
    private Button btnOk;
    private Button btnCancel;

    public AxisNamingForm()
    {
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
        this.Text = "Авто-именование осей";
        this.Size = new System.Drawing.Size(350, 290);
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.StartPosition = FormStartPosition.CenterScreen;
        this.MaximizeBox = false;
        this.MinimizeBox = false;

        this.BackColor = System.Drawing.Color.White;
        this.Font = new System.Drawing.Font("Segoe UI", 9F);

        var gbScope = new GroupBox() { Text = "Область применения", Location = new System.Drawing.Point(15, 15), Size = new System.Drawing.Size(305, 80) };
        rbAll = new RadioButton() { Text = "Все оси в проекте", Location = new System.Drawing.Point(20, 25), AutoSize = true, Checked = true };
        rbSel = new RadioButton() { Text = "Только выделенные оси", Location = new System.Drawing.Point(20, 50), AutoSize = true };
        gbScope.Controls.Add(rbAll);
        gbScope.Controls.Add(rbSel);
        this.Controls.Add(gbScope);

        var gbStart = new GroupBox() { Text = "Начальные значения", Location = new System.Drawing.Point(15, 105), Size = new System.Drawing.Size(305, 85) };
        var lblNum = new Label() { Text = "Вертикальные (цифры):", Location = new System.Drawing.Point(15, 25), AutoSize = true };
        txtNum = new TextBox() { Text = "1", Location = new System.Drawing.Point(210, 22), Size = new System.Drawing.Size(80, 25), Font = new System.Drawing.Font("Segoe UI", 10F) };
        var lblLet = new Label() { Text = "Горизонтальные (буквы):", Location = new System.Drawing.Point(15, 55), AutoSize = true };
        txtLet = new TextBox() { Text = "А", Location = new System.Drawing.Point(210, 52), Size = new System.Drawing.Size(80, 25), Font = new System.Drawing.Font("Segoe UI", 10F) };
        gbStart.Controls.Add(lblNum);
        gbStart.Controls.Add(txtNum);
        gbStart.Controls.Add(lblLet);
        gbStart.Controls.Add(txtLet);
        this.Controls.Add(gbStart);

        btnOk = new Button() { Text = "ОК", DialogResult = DialogResult.OK, Location = new System.Drawing.Point(165, 205), Size = new System.Drawing.Size(75, 32), FlatStyle = FlatStyle.Flat, BackColor = System.Drawing.Color.FromArgb(26, 115, 232), ForeColor = System.Drawing.Color.White, UseVisualStyleBackColor = false };
        btnOk.FlatAppearance.BorderSize = 0;
        this.Controls.Add(btnOk);

        btnCancel = new Button() { Text = "Отмена", DialogResult = DialogResult.Cancel, Location = new System.Drawing.Point(245, 205), Size = new System.Drawing.Size(75, 32), FlatStyle = FlatStyle.Flat, BackColor = System.Drawing.Color.White, UseVisualStyleBackColor = false };
        btnCancel.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(200, 208, 219);
        this.Controls.Add(btnCancel);

        this.AcceptButton = btnOk;
        this.CancelButton = btnCancel;
    }
}
