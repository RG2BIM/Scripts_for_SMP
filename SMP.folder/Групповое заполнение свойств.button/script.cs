using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;
using Renga;

var project = RengaApp.Project;
var modelObjs = project.Model.GetObjects();
var objects = new List<IModelObject>();

for (int i = 0; i < modelObjs.Count; i++)
{
    var obj = modelObjs.GetByIndex(i);
    var typeS = obj.ObjectTypeS.ToUpper();
    if (typeS != "{C3CE17FF-6F28-411F-B18D-74FE957B2BA8}" && 
        typeS != "{4B41CCF8-C969-4C55-A1F2-CCED9C164F07}" && 
        typeS != "{97675473-CA62-4EA4-BC6E-BB2CA57B7E67}" && 
        typeS != "{02BBEBE8-E28B-4EE5-8916-11B514A35DCA}")   
    {
        objects.Add(obj);
    }
}

if (objects.Count == 0) { Print("Объекты отсутствуют."); return; }

var categoryNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
    {"{4329112A-6B65-48D9-9DA8-ABF1F8F36327}", "Стена"},
    {"{D9EE2442-E807-42FB-8FE5-9DCFE543035D}", "Колонна"},
    {"{63478188-7C88-4A6D-B891-9725F04A5BC7}", "Балка"},
    {"{2B02B353-2CA5-4566-88BB-917EA8460174}", "Окно"},
    {"{1CFBA99C-01E7-4078-AE1A-3E2FF0673599}", "Дверь"},
    {"{F5BD8BD8-39C1-47F8-8499-F673C580DFBE}", "Перекрытие"},
    {"{6063816C-89FF-4C8F-A814-3BE6CB94128E}", "Фундамент"},
    {"{D7DD0293-DD65-4229-A64C-8B528D4E226F}", "Ленточный фундамент"},
    {"{3F522F49-AEE2-4D73-9866-9B07CF336A69}", "Лестница"},
    {"{BAC4470F-D560-4F57-A49E-FAA5F6E5A279}", "Крыша"},
    {"{DEBDE004-AFCC-4DA8-8DD0-4223FF836ACD}", "Пандус"},
    {"{F1A805FF-573D-F46B-FFBA-57F4BCCAA6ED}", "Помещение"},
    {"{B8C7155A-B462-4FF5-BC41-C9C17A9F48FA}", "Сантехническое оборудование"},
    {"{793D3F7C-905D-4D85-A351-B152241DD2E7}", "Осветительный прибор"},
    {"{96DA9155-43C1-42B8-BBA2-B4F61FA43ACC}", "Электрический щит"},
    {"{B00D5C25-92A8-4409-A3B7-7C37ED792C06}", "Электроустановочное изделие"},
    {"{83DE45E6-4793-49EC-8B9E-65A2438F36DE}", "Электрическая линия"},
    {"{838CC9F6-E3D8-4132-AF6F-C58DF0F8D037}", "Труба"},
    {"{06CC88EE-9A67-4626-9C34-DDE03C331A74}", "Воздуховод"},
    {"{41E2788A-49ED-487F-9AE1-55B6E09AE6E5}", "Арматура трубопровода"},
    {"{D31DC2E3-808E-4987-8481-7F86665A07FC}", "Деталь трубопровода"},
    {"{47D0D93F-3C7B-4269-BF8A-DE246E1724D0}", "Арматура воздуховода"},
    {"{77FFCA60-B20E-49F0-B42F-4FDC9B1C825B}", "Деталь воздуховода"},
    {"{DE4420CE-02B6-4B12-9CD7-9322118BE8FE}", "Вентиляционное оборудование"},
    {"{5D2F3734-5A49-4504-90B1-0676F0F25DA7}", "Оборудование"},
    {"{CB825BF3-15AE-4190-821C-8AD314951ADA}", "Сборка"}
};

string GetCategory(IModelObject obj) {
    var guid = obj.ObjectTypeS.ToUpper();
    if (categoryNames.TryGetValue(guid, out string name)) return name;
    var fallback = obj.Name.Split(new[] { '-', ':' }, 2)[0].Trim();
    return string.IsNullOrEmpty(fallback) ? "Прочее" : fallback;
}

var objectsByCategory = new Dictionary<string, List<IModelObject>>();
foreach(var o in objects) {
    string c = GetCategory(o);
    if (!objectsByCategory.ContainsKey(c)) objectsByCategory[c] = new List<IModelObject>();
    objectsByCategory[c].Add(o);
}

var categories = objectsByCategory.Keys.OrderBy(t => t).ToList();

Form form = new Form() { AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi,  
    Text = "Групповое заполнение свойств", 
    ClientSize = new System.Drawing.Size(460, 370), 
    StartPosition = FormStartPosition.CenterScreen,
    FormBorderStyle = FormBorderStyle.FixedDialog,
    MaximizeBox = false,
    MinimizeBox = false,
    Font = new System.Drawing.Font("Segoe UI", 9),
    BackColor = System.Drawing.Color.White
};

GroupBox grpParams = new GroupBox() { Text = "Настройки заполнения", Top = 15, Left = 15, Width = 430, Height = 280 };

Label lblCategory = new Label() { Text = "Выберите категорию:", Top = 30, Left = 15, Width = 400, Height = 20 };
ComboBox cbCategory = new ComboBox() { Top = 50, Left = 15, Width = 400, DropDownStyle = ComboBoxStyle.DropDownList, Font = new System.Drawing.Font("Segoe UI", 10) };
cbCategory.Items.AddRange(categories.ToArray());

Label lblType = new Label() { Text = "Выберите тип объекта:", Top = 90, Left = 15, Width = 400, Height = 20 };
ComboBox cbType = new ComboBox() { Top = 110, Left = 15, Width = 400, DropDownStyle = ComboBoxStyle.DropDownList, Font = new System.Drawing.Font("Segoe UI", 10) };

Label lblProp = new Label() { Text = "Выберите свойство:", Top = 150, Left = 15, Width = 400, Height = 20 };
ComboBox cbProp = new ComboBox() { Top = 170, Left = 15, Width = 400, DropDownStyle = ComboBoxStyle.DropDownList, Font = new System.Drawing.Font("Segoe UI", 10) };

Label lblVal = new Label() { Text = "Введите значение:", Top = 210, Left = 15, Width = 400, Height = 20 };
TextBox txtVal = new TextBox() { Top = 230, Left = 15, Width = 400, Font = new System.Drawing.Font("Segoe UI", 10) };

grpParams.Controls.Add(lblCategory); grpParams.Controls.Add(cbCategory);
grpParams.Controls.Add(lblType); grpParams.Controls.Add(cbType);
grpParams.Controls.Add(lblProp); grpParams.Controls.Add(cbProp);
grpParams.Controls.Add(lblVal);  grpParams.Controls.Add(txtVal);

Button btnOk = new Button() { Text = "Применить", Top = 320, Left = 240, Width = 100, Height = 32 };
btnOk.BackColor = System.Drawing.Color.FromArgb(26, 115, 232);
btnOk.ForeColor = System.Drawing.Color.White;
btnOk.FlatStyle = FlatStyle.Flat;
btnOk.FlatAppearance.BorderSize = 0;

Button btnCancel = new Button() { Text = "Отмена", Top = 320, Left = 350, Width = 95, Height = 32 };
btnCancel.FlatStyle = FlatStyle.Flat;
btnCancel.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(200, 208, 219);
btnCancel.Click += (s, e) => form.DialogResult = DialogResult.Cancel;

form.Controls.Add(grpParams);
form.Controls.Add(btnOk);
form.Controls.Add(btnCancel);

List<Guid> currentProps = new List<Guid>();

cbCategory.SelectedIndexChanged += (s, e) => {
    cbType.Items.Clear();
    cbProp.Items.Clear();
    currentProps.Clear();
    if (cbCategory.SelectedIndex < 0) return;
    
    string selCat = cbCategory.SelectedItem.ToString();
    var typesForCat = objectsByCategory[selCat].Select(o => o.Name).Distinct().OrderBy(t => t).ToList();
    
    cbType.Items.Add("< Все типы этой категории >");
    cbType.Items.AddRange(typesForCat.ToArray());
    cbType.SelectedIndex = 0;
};

cbType.SelectedIndexChanged += (s, e) => {
    cbProp.Items.Clear();
    currentProps.Clear();
    if (cbType.SelectedIndex < 0 || cbCategory.SelectedIndex < 0) return;
    
    string selCat = cbCategory.SelectedItem.ToString();
    bool allTypes = cbType.SelectedIndex == 0;
    string selType = cbType.SelectedItem.ToString();
    
    IModelObject firstObj = allTypes 
        ? objectsByCategory[selCat].FirstOrDefault()
        : objectsByCategory[selCat].FirstOrDefault(o => o.Name == selType);
        
    if (firstObj == null) return;

    string GetTypeName(int type) {
        switch (type) {
            case 1: return "Число";
            case 2: return "Строка";
            case 3: return "Угол";
            case 4: return "Площадь";
            case 5: return "Число";
            case 6: return "Перечисление";
            case 7: return "Целое число";
            case 8: return "Длина";
            case 9: return "Да/Нет";
            case 10: return "Масса";
            case 11: return "Объем";
            default: return "Тип " + type;
        }
    }

    var props = firstObj.GetProperties();
    if (props != null)
    {
        var ids = props.GetIds();
        var propsList = new List<dynamic>();
        for (int i = 0; i < ids.Count; i++)
        {
            var p = props.GetS(ids.GetS(i));
            propsList.Add(new { Id = ids.GetS(i), Name = p.Name, Type = (int)p.Type });
        }
        propsList = propsList.OrderBy(p => p.Name).ToList();
        foreach (var p in propsList)
        {
            cbProp.Items.Add($"{p.Name} ({GetTypeName(p.Type)})");
            currentProps.Add(Guid.Parse(p.Id));
        }
    }
    if (cbProp.Items.Count > 0) cbProp.SelectedIndex = 0;
};

btnOk.Click += (s, e) => {
    if (cbCategory.SelectedIndex < 0 || cbType.SelectedIndex < 0 || cbProp.SelectedIndex < 0) {
        MessageBox.Show("Выберите категорию, тип и свойство!"); return;
    }
    
    string selCat = cbCategory.SelectedItem.ToString();
    bool allTypes = cbType.SelectedIndex == 0;
    string selType = cbType.SelectedItem.ToString();
    Guid propId = currentProps[cbProp.SelectedIndex];
    string val = txtVal.Text;

    var targetObjs = allTypes 
        ? objectsByCategory[selCat].ToList()
        : objectsByCategory[selCat].Where(o => o.Name == selType).ToList();
    
    var op = project.CreateOperation();
    op.Start();
    int successCount = 0;
    foreach (var obj in targetObjs)
    {
        var props = obj.GetProperties();
        if (props == null) continue;
        var p = props.Get(propId);
        if (p == null) continue;

        try {
            switch ((int)p.Type)
            {
                case 1: p.SetDoubleValue(double.Parse(val)); break;
                case 2: p.SetStringValue(val); break;
                case 3: p.SetAngleValue(double.Parse(val), (Renga.AngleUnit)1); break;
                case 4: p.SetAreaValue(double.Parse(val), (Renga.AreaUnit)3); break;
                case 5: p.SetDoubleValue(double.Parse(val)); break; 
                case 6: p.SetEnumerationValue(val); break;
                case 7: p.SetIntegerValue(int.Parse(val)); break;
                case 8: p.SetLengthValue(double.Parse(val), (Renga.LengthUnit)1); break;
                case 9: 
                    if (int.TryParse(val, out int lv)) p.SetLogicalValue((Renga.Logical)lv);
                    else if (bool.TryParse(val, out bool bv)) p.SetLogicalValue(bv ? Renga.Logical.Logical_True : Renga.Logical.Logical_False);
                    break;
                case 10: p.SetMassValue(double.Parse(val), (Renga.MassUnit)2); break;
                case 11: p.SetVolumeValue(double.Parse(val), (Renga.VolumeUnit)3); break;
            }
            successCount++;
        } catch { /* ignore parse errors */ }
    }
    op.Apply();
    MessageBox.Show($"Успешно применено к {successCount} из {targetObjs.Count} объектов.");
    form.Close();
};

try {
    var selIds = RengaApp.Selection.GetSelectedObjects() as Array;
    if (selIds != null && selIds.Length > 0) {
        int firstId = Convert.ToInt32(selIds.GetValue(0));
        var firstSel = objects.FirstOrDefault(o => o.Id == firstId);
        if (firstSel != null) {
            string catName = GetCategory(firstSel);
            int catIdx = cbCategory.Items.IndexOf(catName);
            if (catIdx >= 0) {
                cbCategory.SelectedIndex = catIdx;
                int typeIdx = cbType.Items.IndexOf(firstSel.Name);
                if (typeIdx >= 0) {
                    cbType.SelectedIndex = typeIdx;
                }
            }
        }
    }
} catch { /* ignore selection errors */ }

form.ShowDialog();
