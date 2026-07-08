using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Windows.Forms;
using Renga;

var project = RengaApp.Project;
var propManager = project.PropertyManager;

var propTypesDict = new Dictionary<int, string>() {
    {1, "Действительное число"},
    {2, "Строка"},
    {3, "Угол"},
    {4, "Площадь"},
    {5, "Булево"},
    {6, "Перечисление"},
    {7, "Целое число"},
    {8, "Длина"},
    {9, "Логическое"},
    {10, "Масса"},
    {11, "Объем"}
};

var properties = new List<Tuple<string, string, int, string>>();

Action loadProperties = () => {
    properties.Clear();
    for (int i = 0; i < propManager.PropertyCount; i++) {
        var pid = propManager.GetPropertyId(i);
        int type = 0;
        try { type = (int)propManager.GetPropertyType(pid); } catch {}
        properties.Add(new Tuple<string, string, int, string>(
            pid.ToString("B").ToUpper(),
            propManager.GetPropertyName(pid),
            type,
            propTypesDict.ContainsKey(type) ? propTypesDict[type] : "Тип " + type
        ));
    }
    properties = properties.OrderBy(p => p.Item2).ToList();
};

loadProperties();

var categoryNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
    {"{67A0B42C-8C1E-47E8-B46E-78D8BB260DE0}", "3D-модели"},
    {"{6485AC11-5B26-4D77-9788-7936AF87C85F}", "IFC-модели"},
    {"{47D0D93F-3C7B-4269-BF8A-DE246E1724D0}", "Аксессуары воздуховода"},
    {"{41E2788A-49ED-487F-9AE1-55B6E09AE6E5}", "Аксессуары трубопровода"},
    {"{9FABC932-590F-4068-89A8-EE6EE3D7CBBF}", "Арматурные стержни"},
    {"{63478188-7C88-4A6D-B891-9725F04A5BC7}", "Балки"},
    {"{DE4420CE-02B6-4B12-9CD7-9322118BE8FE}", "Вентиляционное оборудование"},
    {"{06CC88EE-9A67-4626-9C34-DDE03C331A74}", "Воздуховоды"},
    {"{F9C7F77A-5644-4ED3-85CE-9EA21881D76A}", "Группы"},
    {"{1CFBA99C-01E7-4078-AE1A-3E2FF0673599}", "Двери"},
    {"{77FFCA60-B20E-49F0-B42F-4FDC9B1C825B}", "Детали воздуховода"},
    {"{D31DC2E3-808E-4987-8481-7F86665A07FC}", "Детали трубопровода"},
    {"{2AABE3A4-A29E-4534-A9F5-0F070FEE240C}", "Диаметральные размеры"},
    {"{165D15BC-FD8D-4BBB-B73C-56956D7CEBF1}", "Здания"},
    {"{857A042D-7D3C-4715-9EBF-95E2E9648ADF}", "Изображения"},
    {"{517A337A-58D5-46FF-81B8-65CF0389A191}", "Изоляция"},
    {"{D9EE2442-E807-42FB-8FE5-9DCFE543035D}", "Колонны"},
    {"{BAC4470F-D560-4F57-A49E-FAA5F6E5A279}", "Крыши"},
    {"{D7DD0293-DD65-4229-A64C-8B528D4E226F}", "Ленточные фундаменты"},
    {"{3F522F49-AEE2-4D73-9866-9B07CF336A69}", "Лестницы"},
    {"{DC82CA1A-A0C3-4A1A-AEFB-A7D720DD3A09}", "Линейные размеры"},
    {"{02BBEBE8-E28B-4EE5-8916-11B514A35DCA}", "Линии модели"},
    {"{0ABCB18F-0AAF-4509-BF89-5C5FAD9D5D8B}", "Материалы"},
    {"{0F0ADBA0-5C06-46C0-9C8A-B9D69EF1251F}", "Многослойные материалы"},
    {"{5D2F3734-5A49-4504-90B1-0676F0F25DA7}", "Оборудование"},
    {"{A1ACA786-78A4-4015-B412-9150BAAD71A9}", "Ограждения"},
    {"{2B02B353-2CA5-4566-88BB-917EA8460174}", "Окна"},
    {"{F6647DC9-CFAE-4C6B-9312-CD6D8010C340}", "Опорные чертежи"},
    {"{793D3F7C-905D-4D85-A351-B152241DD2E7}", "Осветительные приборы"},
    {"{4B41CCF8-C969-4C55-A1F2-CCED9C164F07}", "Оси"},
    {"{ECEF8F90-0CF9-4494-98DE-91242A2A9F5C}", "Отверстия"},
    {"{DEBDE004-AFCC-4DA8-8DD0-4223FF836ACD}", "Пандусы"},
    {"{F5BD8BD8-39C1-47F8-8499-F673C580DFBE}", "Перекрытия"},
    {"{62CF086E-5A39-4484-840C-FFA6A1C6E2B7}", "Пластины"},
    {"{F1A805FF-573D-F46B-FFBA-57F4BCCAA6ED}", "Помещения"},
    {"{9BD80F5A-9448-48DE-A9AB-935A946DAB65}", "Проекты"},
    {"{FC443D5A-B76C-45E5-B91C-520EF0896109}", "Проёмы"},
    {"{377C2FDA-9411-43AC-A6C6-0E3B520BE721}", "Радиальные размеры"},
    {"{4166FD59-64C0-45EE-AE3B-49FAE1257EF1}", "Разрезы"},
    {"{B8C7155A-B462-4FF5-BC41-C9C17A9F48FA}", "Санитарно-техническое оборудование"},
    {"{CB825BF3-15AE-4190-821C-8AD314951ADA}", "Сборки"},
    {"{CA526024-04A1-40C7-87FD-2E95C722CC50}", "Слои"},
    {"{5CBF0016-32BC-4630-99EA-C7CC94DDA8E3}", "Спецификации"},
    {"{4329112A-6B65-48D9-9DA8-ABF1F8F36327}", "Стены"},
    {"{6C671391-BFEA-4E92-9753-8855C05640A0}", "Стили аксессуара воздуховода"},
    {"{A31CF7CA-F17B-422A-886A-7A8C362CD49A}", "Стили аксессуара трубопровода"},
    {"{608EDB78-96F3-40A6-A0EC-71000105581B}", "Стили арматурного стержня"},
    {"{7EE13BD6-7C0A-47D3-ADCE-35B8E0DAE28A}", "Стили арматурных изделий"},
    {"{CF2B8B04-F595-4432-98F4-8234C95ADBDD}", "Стили балки"},
    {"{D43C7509-A92C-4E32-BD2D-BA6DD8F5B7A1}", "Стили вентиляционного оборудования"},
    {"{A999F05A-D730-42E7-BFC8-E4433EBACE78}", "Стили воздуховода"},
    {"{19D0649F-582A-488E-A52B-585C1151A5E4}", "Стили двери"},
    {"{6C6821A0-EBB9-445B-84A2-ED9EB0938E4F}", "Стили детали воздуховода"},
    {"{B1359BDC-F7FF-43A4-BCA0-8D09BC974537}", "Стили детали трубопровода"},
    {"{BE49A354-19B7-435A-8957-9EF8782630C2}", "Стили колонны"},
    {"{A369AD70-C1FE-41DD-AF3D-BD659EA5B360}", "Стили оборудования"},
    {"{FAC43446-031C-413E-9993-6E9CF9F2306A}", "Стили окна"},
    {"{1F85F676-BB99-4A6F-9F72-1789F2F7B362}", "Стили осветительного прибора"},
    {"{83085C7B-16C4-473E-85BC-9AAFA504FF7D}", "Стили отверстия"},
    {"{9B60D6AD-3468-478E-94DF-A535C5AEAA3E}", "Стили пластины"},
    {"{FA7F1AE9-F4F4-4F95-B108-FEEA4D7EFEB7}", "Стили проводника"},
    {"{344299F5-7D7F-43E2-B0A2-1DB8E06E8AC8}", "Стили санитарно-технического оборудования"},
    {"{9D6DFFB9-4828-40D8-8529-BF5CD2B58C4E}", "Стили трубы"},
    {"{861C0037-7797-43A9-96E7-833A7A2C6EA4}", "Стили электрического распределительного щита"},
    {"{33FB4B37-83F9-422A-81D4-640A152C619E}", "Стили электрической линии"},
    {"{A6E0BA72-ACBD-4423-9AFC-04D84A09211A}", "Стили электроустановочного изделия"},
    {"{514A3AE7-F551-4D0F-B5BA-5D4F0ECF4E7A}", "Стили элемента"},
    {"{6063816C-89FF-4C8F-A814-3BE6CB94128E}", "Столбчатые фундаменты"},
    {"{ED1F87A1-5C9C-4994-969D-6D3854571193}", "Таблицы"},
    {"{DA557027-F243-4331-BB5B-853ABC437CD7}", "Тексты модели"},
    {"{CE93E320-7167-4CD1-92A8-5E42D546066B}", "Точки трассировки"},
    {"{8B323BEE-3882-4744-8838-24F45DF714A9}", "Трассы"},
    {"{838CC9F6-E3D8-4132-AF6F-C58DF0F8D037}", "Трубы"},
    {"{96788994-B7FC-41D7-8A99-D674543E9237}", "Угловые размеры"},
    {"{C3CE17FF-6F28-411F-B18D-74FE957B2BA8}", "Уровни"},
    {"{56652D5B-536E-4EF6-A1CD-5AD69BB025AB}", "Участки"},
    {"{8A49A9A8-A401-4AB1-8038-92093503C97A}", "Фасады"},
    {"{A7DFE1E1-BF2C-4C4A-BA74-3F156B1BBF8F}", "Чертежи"},
    {"{84B43087-D4A4-4CCE-B34D-40E283D9E691}", "Штриховки модели"},
    {"{83DE45E6-4793-49EC-8B9E-65A2438F36DE}", "Электрические линии"},
    {"{96DA9155-43C1-42B8-BBA2-B4F61FA43ACC}", "Электрические распределительные щиты"},
    {"{B00D5C25-92A8-4409-A3B7-7C37ED792C06}", "Электроустановочные изделия"},
    {"{E1E3BD66-2E13-4FA4-A9EB-677E03067C2F}", "Элементы"}
};

var reverseCategories = categoryNames.ToDictionary(x => x.Value, x => x.Key);
var categoryList = categoryNames.Values.OrderBy(v => v).ToList();

Form form = new Form() { AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi,  
    Text = "Назначение свойств категориям", 
    ClientSize = new System.Drawing.Size(1100, 650), 
    MinimumSize = new System.Drawing.Size(1000, 650),
    StartPosition = FormStartPosition.CenterScreen,
    FormBorderStyle = FormBorderStyle.Sizable,
    Font = new System.Drawing.Font("Segoe UI", 8.25f),
    BackColor = System.Drawing.Color.White
};

GroupBox grpProps = new GroupBox() { 
    Text = "Свойства проекта", 
    Top = 15, Left = 15, Width = 450, Height = 570, 
    Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left 
};

DataGridView dgvProps = new DataGridView() {
    Top = 25, Left = 15, Width = 420, Height = 495,
    Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
    AllowUserToAddRows = false,
    AllowUserToDeleteRows = false,
    AllowUserToResizeRows = false,
    RowHeadersVisible = false,
    ColumnHeadersVisible = true,
    BackgroundColor = System.Drawing.Color.White,
    SelectionMode = DataGridViewSelectionMode.FullRowSelect,
    MultiSelect = false,
    CellBorderStyle = DataGridViewCellBorderStyle.None,
    BorderStyle = BorderStyle.FixedSingle,
    EnableHeadersVisualStyles = false,
    ScrollBars = ScrollBars.Vertical
};

dgvProps.ColumnHeadersDefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(240, 240, 240);
dgvProps.ColumnHeadersDefaultCellStyle.ForeColor = System.Drawing.Color.Black;
dgvProps.ColumnHeadersDefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(240, 240, 240);
dgvProps.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
dgvProps.ColumnHeadersHeight = 32;

dgvProps.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(220, 220, 220);
dgvProps.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.Black;

var colPropName = new DataGridViewTextBoxColumn() { HeaderText = "Свойство", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, ReadOnly = true, Name = "Property" };
var colPropType = new DataGridViewTextBoxColumn() { HeaderText = "Тип", Width = 140, ReadOnly = true, Name = "Type" };
dgvProps.Columns.AddRange(colPropName, colPropType);

Action renderPropsList = () => {
    dgvProps.Rows.Clear();
    foreach(var p in properties) dgvProps.Rows.Add(p.Item2, p.Item4);
};
renderPropsList();
grpProps.Controls.Add(dgvProps);

GroupBox grpCats = new GroupBox() { 
    Text = "Категории Renga", 
    Top = 15, Left = 480, Width = 600, Height = 570, 
    Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right 
};

DataGridView dgvCats = new DataGridView() {
    Top = 25, Left = 15, Width = 570, Height = 480,
    Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
    AllowUserToAddRows = false,
    AllowUserToDeleteRows = false,
    AllowUserToResizeRows = false,
    RowHeadersVisible = false,
    BackgroundColor = System.Drawing.Color.White,
    SelectionMode = DataGridViewSelectionMode.FullRowSelect,
    MultiSelect = false,
    CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
    GridColor = System.Drawing.Color.FromArgb(230, 230, 230),
    BorderStyle = BorderStyle.FixedSingle,
    EnableHeadersVisualStyles = false
};

dgvCats.ColumnHeadersDefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(240, 240, 240);
dgvCats.ColumnHeadersDefaultCellStyle.ForeColor = System.Drawing.Color.Black;
dgvCats.ColumnHeadersDefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(240, 240, 240);
dgvCats.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
dgvCats.ColumnHeadersHeight = 32;

dgvCats.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(220, 220, 220);
dgvCats.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.Black;

var colAssign = new DataGridViewCheckBoxColumn() { HeaderText = "Назначить", Width = 75, Name = "Assign" };
var colCsv = new DataGridViewCheckBoxColumn() { HeaderText = "В CSV", Width = 55, Name = "Csv" };
var colFormula = new DataGridViewCheckBoxColumn() { HeaderText = "Формула", Width = 65, Name = "Formula", ReadOnly = true };
var colName = new DataGridViewTextBoxColumn() { HeaderText = "Категория", Width = 150, ReadOnly = true, Name = "Category" };
var colExpr = new DataGridViewTextBoxColumn() { HeaderText = "Выражение", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, Name = "Expression" };
dgvCats.Columns.AddRange(colAssign, colCsv, colFormula, colName, colExpr);

foreach(var c in categoryList) dgvCats.Rows.Add(false, false, false, c, "");
grpCats.Controls.Add(dgvCats);

Button btnSelectAll = new Button() { 
    Text = "Все", 
    Top = 520, Left = 15, Width = 80, Height = 30, 
    Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
    FlatStyle = FlatStyle.Flat 
};
btnSelectAll.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(200, 208, 219);
btnSelectAll.Click += (s, e) => {
    for (int i = 0; i < dgvCats.Rows.Count; i++) dgvCats.Rows[i].Cells["Assign"].Value = true;
};

Button btnDeselectAll = new Button() { 
    Text = "Снять", 
    Top = 520, Left = 105, Width = 80, Height = 30, 
    Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
    FlatStyle = FlatStyle.Flat 
};
btnDeselectAll.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(200, 208, 219);
btnDeselectAll.Click += (s, e) => {
    for (int i = 0; i < dgvCats.Rows.Count; i++) {
        dgvCats.Rows[i].Cells["Assign"].Value = false;
        dgvCats.Rows[i].Cells["Csv"].Value = false;
        dgvCats.Rows[i].Cells["Formula"].Value = false;
    }
};

grpCats.Controls.Add(btnSelectAll);
grpCats.Controls.Add(btnDeselectAll);

Button btnCreateProp = new Button() { 
    Text = "Создать...", 
    Top = 530, Left = 15, Width = 135, Height = 30, 
    Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
    FlatStyle = FlatStyle.Flat 
};
btnCreateProp.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(200, 208, 219);

Button btnRenameProp = new Button() { 
    Text = "Переименовать", 
    Top = 530, Left = 155, Width = 135, Height = 30, 
    Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
    FlatStyle = FlatStyle.Flat 
};
btnRenameProp.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(200, 208, 219);

Button btnDeleteProp = new Button() { 
    Text = "Удалить", 
    Top = 530, Left = 295, Width = 140, Height = 30, 
    Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
    FlatStyle = FlatStyle.Flat 
};
btnDeleteProp.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(200, 208, 219);

grpProps.Controls.Add(btnCreateProp);
grpProps.Controls.Add(btnRenameProp);
grpProps.Controls.Add(btnDeleteProp);

Button btnExport = new Button() { 
    Text = "Экспорт в CSV", 
    Top = 600, Left = 15, Width = 135, Height = 30, 
    Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
    FlatStyle = FlatStyle.Flat 
};
btnExport.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(200, 208, 219);

Button btnImport = new Button() { 
    Text = "Импорт из CSV", 
    Top = 600, Left = 155, Width = 135, Height = 30, 
    Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
    FlatStyle = FlatStyle.Flat 
};
btnImport.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(200, 208, 219);

Button btnApply = new Button() { 
    Text = "Применить", 
    Top = 595, Left = 845, Width = 110, Height = 35, 
    Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
    FlatStyle = FlatStyle.Flat 
};
btnApply.BackColor = System.Drawing.Color.FromArgb(26, 115, 232);
btnApply.ForeColor = System.Drawing.Color.White;
btnApply.FlatAppearance.BorderSize = 0;

Button btnClose = new Button() { 
    Text = "Закрыть", 
    Top = 595, Left = 970, Width = 110, Height = 35, 
    Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
    FlatStyle = FlatStyle.Flat 
};
btnClose.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(200, 208, 219);
btnClose.Click += (s, e) => form.Close();

form.Controls.Add(grpProps);
form.Controls.Add(grpCats);
form.Controls.Add(btnExport);
form.Controls.Add(btnImport);
form.Controls.Add(btnApply);
form.Controls.Add(btnClose);

bool isUpdatingCats = false;

dgvProps.SelectionChanged += (s, e) => {
    if (dgvProps.SelectedRows.Count == 0) return;
    isUpdatingCats = true;
    int selectedIndex = dgvProps.SelectedRows[0].Index;
    string propId = properties[selectedIndex].Item1;
    for (int i = 0; i < dgvCats.Rows.Count; i++) {
        string catName = dgvCats.Rows[i].Cells["Category"].Value.ToString();
        string catId = reverseCategories[catName];
        
        bool isAssigned = false;
        try { isAssigned = propManager.IsPropertyAssignedToTypeS(propId, catId); } catch {}
        dgvCats.Rows[i].Cells["Assign"].Value = isAssigned;
        
        bool isCsv = false;
        string expr = null;
        bool hasFormula = false;
        if (isAssigned) {
            try { isCsv = propManager.GetCSVExportFlagS(propId, catId); } catch {}
            try { 
                expr = propManager.GetExpressionS(propId, catId); 
                hasFormula = (expr != null); 
            } catch {
                hasFormula = false;
            }
        }
        dgvCats.Rows[i].Cells["Csv"].Value = isCsv;
        dgvCats.Rows[i].Cells["Formula"].Value = hasFormula;
        dgvCats.Rows[i].Cells["Expression"].Value = expr ?? "";
    }
    isUpdatingCats = false;
};

btnApply.Click += (s, e) => {
    if (dgvProps.SelectedRows.Count == 0) {
        MessageBox.Show("Сначала выберите свойство слева.", "Внимание");
        return;
    }
    if (dgvCats.IsCurrentCellInEditMode) {
        dgvCats.CommitEdit(DataGridViewDataErrorContexts.Commit);
    }
    string propId = properties[dgvProps.SelectedRows[0].Index].Item1;
    int changes = 0;
    
    var op = project.CreateOperation();
    op.Start();
    for (int i = 0; i < dgvCats.Rows.Count; i++) {
        string catName = dgvCats.Rows[i].Cells["Category"].Value.ToString();
        string catId = reverseCategories[catName];
        
        bool isCurrentlyAssigned = false;
        try { isCurrentlyAssigned = propManager.IsPropertyAssignedToTypeS(propId, catId); } catch {}
        bool shouldBeAssigned = Convert.ToBoolean(dgvCats.Rows[i].Cells["Assign"].Value);
        
        bool currentlyCsv = false;
        string currentlyExpr = "";
        if (isCurrentlyAssigned) {
            try { currentlyCsv = propManager.GetCSVExportFlagS(propId, catId); } catch {}
            try { currentlyExpr = propManager.GetExpressionS(propId, catId); } catch {}
        }
        bool shouldBeCsv = Convert.ToBoolean(dgvCats.Rows[i].Cells["Csv"].Value);
        string shouldBeExpr = dgvCats.Rows[i].Cells["Expression"].Value?.ToString() ?? "";

        if (shouldBeAssigned && !isCurrentlyAssigned) {
            try {
                propManager.AssignPropertyToTypeS(propId, catId);
                if (shouldBeCsv) propManager.SetCSVExportFlagS(propId, catId, true);
                if (!string.IsNullOrEmpty(shouldBeExpr)) propManager.SetExpressionS(propId, catId, shouldBeExpr);
                changes++;
            } catch {}
        } else if (!shouldBeAssigned && isCurrentlyAssigned) {
            try {
                propManager.UnassignPropertyFromTypeS(propId, catId);
                changes++;
            } catch {}
        } else if (shouldBeAssigned && isCurrentlyAssigned) {
            if (currentlyCsv != shouldBeCsv) {
                try { propManager.SetCSVExportFlagS(propId, catId, shouldBeCsv); changes++; } catch {}
            }
            if (currentlyExpr != shouldBeExpr) {
                try { propManager.SetExpressionS(propId, catId, shouldBeExpr); changes++; } catch {}
            }
        }
    }
    op.Apply();
    if (changes > 0) {
        MessageBox.Show($"Успешно применено изменений: {changes}", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
};

dgvCats.CurrentCellDirtyStateChanged += (s, e) => {
    if (dgvCats.IsCurrentCellDirty && !isUpdatingCats) {
        if (dgvCats.CurrentCell is DataGridViewCheckBoxCell) {
            dgvCats.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }
    }
};

dgvCats.CellValueChanged += (s, e) => {
    if (e.RowIndex >= 0 && !isUpdatingCats) {
        if (e.ColumnIndex == dgvCats.Columns["Expression"].Index) {
            string expr = dgvCats.Rows[e.RowIndex].Cells["Expression"].Value?.ToString() ?? "";
            if (!string.IsNullOrWhiteSpace(expr)) {
                isUpdatingCats = true;
                dgvCats.Rows[e.RowIndex].Cells["Formula"].Value = true;
                isUpdatingCats = false;
            }
        }
    }
};

btnCreateProp.Click += (s, e) => {
    Form fNew = new Form() { AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi,  Text = "Новое свойство", Width = 350, Height = 200, StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
    Label lblName = new Label() { Text = "Имя:", Left = 15, Top = 20, Width = 50 };
    TextBox txtName = new TextBox() { Left = 70, Top = 18, Width = 240 };
    Label lblType = new Label() { Text = "Тип:", Left = 15, Top = 60, Width = 50 };
    ComboBox cmbType = new ComboBox() { Left = 70, Top = 58, Width = 240, DropDownStyle = ComboBoxStyle.DropDownList };
    
    var typeItems = propTypesDict.Select(kvp => new { Id = kvp.Key, Name = kvp.Value }).ToArray();
    cmbType.DisplayMember = "Name";
    cmbType.ValueMember = "Id";
    foreach(var item in typeItems) cmbType.Items.Add(item);
    cmbType.SelectedIndex = 1; // String by default
    
    Button btnOk = new Button() { Text = "Создать", Left = 130, Top = 110, Width = 80, DialogResult = DialogResult.OK };
    Button btnCancel = new Button() { Text = "Отмена", Left = 230, Top = 110, Width = 80, DialogResult = DialogResult.Cancel };
    fNew.Controls.AddRange(new Control[] { lblName, txtName, lblType, cmbType, btnOk, btnCancel });
    fNew.AcceptButton = btnOk;
    fNew.CancelButton = btnCancel;
    
    if (fNew.ShowDialog(form) == DialogResult.OK) {
        if (string.IsNullOrWhiteSpace(txtName.Text)) {
            MessageBox.Show("Имя не может быть пустым.");
            return;
        }
        string newName = txtName.Text.Trim();
        dynamic selItem = cmbType.SelectedItem;
        int newType = selItem.Id;
        
        Guid newGuid = Guid.NewGuid();
        string newGuidStr = newGuid.ToString("B").ToUpper();
        
        try {
            var op = project.CreateOperation();
            op.Start();
            propManager.RegisterPropertyS(newGuidStr, newName, (Renga.PropertyType)newType);
            op.Apply();
            
            loadProperties();
            renderPropsList();
            
            // Select the newly created property
            for(int i = 0; i < dgvProps.Rows.Count; i++) {
                if (properties[i].Item1 == newGuidStr) {
                    dgvProps.Rows[i].Selected = true;
                    dgvProps.FirstDisplayedScrollingRowIndex = i;
                    break;
                }
            }
        } catch (Exception ex) {
            MessageBox.Show("Ошибка при создании свойства: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
};

btnRenameProp.Click += (s, e) => {
    if (dgvProps.SelectedRows.Count == 0) return;
    int selectedIndex = dgvProps.SelectedRows[0].Index;
    string propId = properties[selectedIndex].Item1;
    string oldName = properties[selectedIndex].Item2;
    
    Form fRen = new Form() { AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi,  Text = "Переименовать", Width = 350, Height = 130, StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
    Label lblName = new Label() { Text = "Имя:", Left = 15, Top = 20, Width = 50 };
    TextBox txtName = new TextBox() { Left = 70, Top = 18, Width = 240, Text = oldName };
    Button btnOk = new Button() { Text = "ОК", Left = 130, Top = 55, Width = 80, DialogResult = DialogResult.OK };
    Button btnCancel = new Button() { Text = "Отмена", Left = 230, Top = 55, Width = 80, DialogResult = DialogResult.Cancel };
    fRen.Controls.AddRange(new Control[] { lblName, txtName, btnOk, btnCancel });
    fRen.AcceptButton = btnOk;
    fRen.CancelButton = btnCancel;
    
    if (fRen.ShowDialog(form) == DialogResult.OK) {
        string newName = txtName.Text.Trim();
        if (string.IsNullOrWhiteSpace(newName) || newName == oldName) return;
        try {
            var op = project.CreateOperation();
            op.Start();
            propManager.SetPropertyName(Guid.Parse(propId), newName);
            op.Apply();
            
            loadProperties();
            renderPropsList();
            for(int i = 0; i < dgvProps.Rows.Count; i++) {
                if (properties[i].Item1 == propId) { dgvProps.Rows[i].Selected = true; dgvProps.FirstDisplayedScrollingRowIndex = i; break; }
            }
        } catch (Exception ex) { MessageBox.Show("Ошибка: " + ex.Message); }
    }
};

btnDeleteProp.Click += (s, e) => {
    if (dgvProps.SelectedRows.Count == 0) return;
    int selectedIndex = dgvProps.SelectedRows[0].Index;
    string propId = properties[selectedIndex].Item1;
    string propName = properties[selectedIndex].Item2;
    
    if (MessageBox.Show($"Удалить свойство '{propName}'?\nВНИМАНИЕ: Это действие полностью сотрет все его значения у всех элементов в модели Renga!", "Удаление свойства", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes) {
        try {
            var op = project.CreateOperation();
            op.Start();
            propManager.UnregisterPropertyS(propId);
            op.Apply();
            
            loadProperties();
            renderPropsList();
            if (dgvProps.Rows.Count > 0) dgvProps.Rows[0].Selected = true;
        } catch (Exception ex) { MessageBox.Show("Ошибка: " + ex.Message); }
    }
};

btnExport.Click += (s, e) => {
    SaveFileDialog sfd = new SaveFileDialog { Filter = "CSV files (*.csv)|*.csv", FileName = "Свойства_Renga.csv" };
    if (sfd.ShowDialog(form) == DialogResult.OK) {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("PropId;PropName;PropType;CategoryId;CategoryName;IsCsv;Expression");
        
        foreach (var p in properties) {
            string propId = p.Item1;
            string propName = p.Item2;
            int propType = p.Item3;
            
            bool hasAssignments = false;
            foreach(var catName in categoryList) {
                string catId = reverseCategories[catName];
                bool isAssigned = false;
                try { isAssigned = propManager.IsPropertyAssignedToTypeS(propId, catId); } catch {}
                
                if (isAssigned) {
                    hasAssignments = true;
                    bool isCsv = false;
                    string expr = "";
                    try { isCsv = propManager.GetCSVExportFlagS(propId, catId); } catch {}
                    try { expr = propManager.GetExpressionS(propId, catId); } catch {}
                    
                    string csvExpr = expr ?? "";
                    if (csvExpr.Contains(";") || csvExpr.Contains("\"") || csvExpr.Contains("\n") || csvExpr.Contains("\r")) {
                        csvExpr = "\"" + csvExpr.Replace("\"", "\"\"") + "\"";
                    }
                    string csvPropName = propName.Contains(";") || propName.Contains("\"") ? "\"" + propName.Replace("\"", "\"\"") + "\"" : propName;
                    
                    sb.AppendLine($"{propId};{csvPropName};{propType};{catId};{catName};{isCsv};{csvExpr}");
                }
            }
            if (!hasAssignments) {
                string csvPropName = propName.Contains(";") || propName.Contains("\"") ? "\"" + propName.Replace("\"", "\"\"") + "\"" : propName;
                sb.AppendLine($"{propId};{csvPropName};{propType};;;;");
            }
        }
        File.WriteAllText(sfd.FileName, sb.ToString(), new UTF8Encoding(true));
        MessageBox.Show("Свойства успешно экспортированы.", "Экспорт");
    }
};

List<List<string>> ParseCsv(string text) {
    var rows = new List<List<string>>();
    var row = new List<string>();
    bool inQuotes = false;
    StringBuilder val = new StringBuilder();
    for (int i = 0; i < text.Length; i++) {
        char c = text[i];
        if (c == '\"') {
            if (inQuotes && i + 1 < text.Length && text[i + 1] == '\"') {
                val.Append('\"');
                i++;
            } else {
                inQuotes = !inQuotes;
            }
        } else if (c == ';' && !inQuotes) {
            row.Add(val.ToString());
            val.Clear();
        } else if (c == '\r' && !inQuotes) {
            // ignore
        } else if (c == '\n' && !inQuotes) {
            row.Add(val.ToString());
            rows.Add(row);
            row = new List<string>();
            val.Clear();
        } else {
            val.Append(c);
        }
    }
    if (val.Length > 0 || row.Count > 0) {
        row.Add(val.ToString());
        rows.Add(row);
    }
    return rows;
}

btnImport.Click += (s, e) => {
    OpenFileDialog ofd = new OpenFileDialog { Filter = "CSV files (*.csv)|*.csv" };
    if (ofd.ShowDialog(form) == DialogResult.OK) {
        string text = File.ReadAllText(ofd.FileName, Encoding.UTF8);
        var rows = ParseCsv(text);
        if (rows.Count <= 1) return; // Only headers or empty
        
        var op = project.CreateOperation();
        op.Start();
        
        int importedProps = 0;
        int importedAssignments = 0;
        
        for (int i = 1; i < rows.Count; i++) { // Skip header
            var parts = rows[i];
            if (parts.Count < 3 || string.IsNullOrWhiteSpace(parts[0])) continue;
            
            string propId = parts[0];
            string propName = parts[1];
            if (!int.TryParse(parts[2], out int propType)) continue;
            
            bool exists = false;
            try { exists = propManager.IsPropertyRegisteredS(propId); } catch {}
            
            if (!exists) {
                try {
                    propManager.RegisterPropertyS(propId, propName, (Renga.PropertyType)propType);
                    importedProps++;
                } catch { continue; }
            }
            
            if (parts.Count >= 7 && !string.IsNullOrWhiteSpace(parts[3])) {
                string catId = parts[3];
                bool isCsv = false;
                bool.TryParse(parts[5], out isCsv);
                string expr = parts[6];
                
                try {
                    bool currentlyAssigned = propManager.IsPropertyAssignedToTypeS(propId, catId);
                    if (!currentlyAssigned) {
                        propManager.AssignPropertyToTypeS(propId, catId);
                    }
                    propManager.SetCSVExportFlagS(propId, catId, isCsv);
                    if (!string.IsNullOrEmpty(expr) || currentlyAssigned) {
                        propManager.SetExpressionS(propId, catId, expr);
                    }
                    importedAssignments++;
                } catch {}
            }
        }
        
        op.Apply();
        MessageBox.Show($"Импорт завершен.\nДобавлено свойств: {importedProps}\nОбновлено/назначено категорий: {importedAssignments}", "Импорт", MessageBoxButtons.OK, MessageBoxIcon.Information);
        loadProperties();
        renderPropsList();
    }
};

if (dgvProps.Rows.Count > 0) dgvProps.Rows[0].Selected = true;

form.ShowDialog();
