using System;
using System.Collections.Generic;
using Color = System.Drawing.Color;
using Font = System.Drawing.Font;
using FontStyle = System.Drawing.FontStyle;
using System.Linq;
using System.Windows.Forms;
using Renga;

// @UseLib
// Инициализация формы в неблокирующем режиме.
// Форма будет отображаться поверх Renga (TopMost = true) 
// для удобной работы с моделью и таблицей одновременно.
try
{
    var levelManagerForm = new LevelManagerForm(RengaApp);
    levelManagerForm.Show();
}
catch (Exception ex)
{
    MessageBox.Show(ex.ToString(), "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
}


// --- Определение классов в самом конце файла ---

public class LevelManagerForm : Form
{
    private Renga.IApplication _app;
    private DataGridView _grid;
    private Button _btnApply;
    private Button _btnCancel;
    private Button _btnPinAll;
    private Button _btnUnpinAll;
    private Button _btnSelectLevel;
    private Button _btnHideLevel;
    private Button _btnShowLevel;
    private Button _btnIsolateLevel;
    private BindingSource _bindingSource;
    private List<LevelItem> _levelsData;

    // GUID параметров Renga
    private readonly Guid LevelElevationParam = new Guid("440a20f8-42b8-4a5f-9000-39ef58e0302b");
    private readonly Guid LevelNameParam = new Guid("1bb1addf-a3c0-4356-9525-107ea7df1513");
    private readonly Guid LevelEntityType = new Guid("c3ce17ff-6f28-411f-b18d-74fe957b2ba8");

    public LevelManagerForm(Renga.IApplication app)
    {
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
        _app = app;
        InitializeComponent();
        LoadData();
    }

    private void InitializeComponent()
    {
        this.Text = "Создание и управление уровнями";
        this.Width = 760;
        this.Height = 500;
        this.BackColor = Color.White;
        this.Font = new Font("Segoe UI", 9);
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.StartPosition = FormStartPosition.CenterScreen;
        this.TopMost = true; // Окно поверх всех окон, чтобы не терялось за Renga

        var groupBox = new GroupBox();
        groupBox.Text = "Таблица уровней";
        groupBox.ForeColor = Color.FromArgb(26, 115, 232);
        groupBox.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        groupBox.Top = 15;
        groupBox.Left = 15;
        groupBox.Width = 715;
        groupBox.Height = 380;
        this.Controls.Add(groupBox);

        _grid = new DataGridView();
        _grid.Top = 25;
        _grid.Left = 15;
        _grid.Width = 530;
        _grid.Height = 338;
        _grid.Font = new Font("Segoe UI", 9, FontStyle.Regular);
        _grid.ForeColor = Color.Black;
        _grid.EnableHeadersVisualStyles = false;
        _grid.ColumnHeadersDefaultCellStyle.BackColor = Color.White;
        _grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.White;
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.Black;
        _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Regular);
        _grid.DefaultCellStyle.ForeColor = Color.Black;
        _grid.AutoGenerateColumns = false;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.AllowUserToResizeRows = false;
        _grid.BackgroundColor = Color.White;
        _grid.RowHeadersVisible = false;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        
        _grid.Columns.Add(new DataGridViewTextBoxColumn() { Name = "Name", HeaderText = "Имя уровня", DataPropertyName = "Name" });
        var elevationCol = new DataGridViewTextBoxColumn() { Name = "Elevation", HeaderText = "Отметка, мм", DataPropertyName = "Elevation" };
        elevationCol.ReadOnly = true;
        elevationCol.DefaultCellStyle.Format = "0.0000";
        _grid.Columns.Add(elevationCol);
        _grid.Columns.Add(new DataGridViewCheckBoxColumn() { Name = "Pin", HeaderText = "Закрепить", DataPropertyName = "Pin", Width = 90 });
        groupBox.Controls.Add(_grid);

        _btnPinAll = new Button();
        _btnPinAll.Text = "Закрепить все";
        _btnPinAll.Top = 25;
        _btnPinAll.Left = 560;
        _btnPinAll.Width = 140;
        _btnPinAll.Height = 32;
        _btnPinAll.FlatStyle = FlatStyle.Flat;
        _btnPinAll.FlatAppearance.BorderColor = Color.FromArgb(200, 208, 219);
        _btnPinAll.BackColor = Color.White;
        _btnPinAll.ForeColor = Color.FromArgb(85, 85, 85);
        _btnPinAll.Font = new Font("Segoe UI", 9, FontStyle.Regular);
        _btnPinAll.Click += (s, e) => { SetAllPins(true); };
        groupBox.Controls.Add(_btnPinAll);

        _btnUnpinAll = new Button();
        _btnUnpinAll.Text = "Снять все";
        _btnUnpinAll.Top = 65;
        _btnUnpinAll.Left = 560;
        _btnUnpinAll.Width = 140;
        _btnUnpinAll.Height = 32;
        _btnUnpinAll.FlatStyle = FlatStyle.Flat;
        _btnUnpinAll.FlatAppearance.BorderColor = Color.FromArgb(200, 208, 219);
        _btnUnpinAll.BackColor = Color.White;
        _btnUnpinAll.ForeColor = Color.FromArgb(85, 85, 85);
        _btnUnpinAll.Font = new Font("Segoe UI", 9, FontStyle.Regular);
        _btnUnpinAll.Click += (s, e) => { SetAllPins(false); };
        groupBox.Controls.Add(_btnUnpinAll);

        _btnSelectLevel = new Button();
        _btnSelectLevel.Text = "Выбрать уровень";
        _btnSelectLevel.Top = 211;
        _btnSelectLevel.Left = 560;
        _btnSelectLevel.Width = 140;
        _btnSelectLevel.Height = 32;
        _btnSelectLevel.FlatStyle = FlatStyle.Flat;
        _btnSelectLevel.FlatAppearance.BorderColor = Color.FromArgb(200, 208, 219);
        _btnSelectLevel.BackColor = Color.White;
        _btnSelectLevel.ForeColor = Color.FromArgb(85, 85, 85);
        _btnSelectLevel.Font = new Font("Segoe UI", 9, FontStyle.Regular);
        _btnSelectLevel.Click += BtnSelectLevel_Click;
        groupBox.Controls.Add(_btnSelectLevel);

        _btnHideLevel = new Button();
        _btnHideLevel.Text = "Скрыть";
        _btnHideLevel.Top = 251;
        _btnHideLevel.Left = 560;
        _btnHideLevel.Width = 140;
        _btnHideLevel.Height = 32;
        _btnHideLevel.FlatStyle = FlatStyle.Flat;
        _btnHideLevel.FlatAppearance.BorderColor = Color.FromArgb(200, 208, 219);
        _btnHideLevel.BackColor = Color.White;
        _btnHideLevel.ForeColor = Color.FromArgb(85, 85, 85);
        _btnHideLevel.Font = new Font("Segoe UI", 9, FontStyle.Regular);
        _btnHideLevel.Click += BtnHideLevel_Click;
        groupBox.Controls.Add(_btnHideLevel);

        _btnShowLevel = new Button();
        _btnShowLevel.Text = "Показать";
        _btnShowLevel.Top = 291;
        _btnShowLevel.Left = 560;
        _btnShowLevel.Width = 140;
        _btnShowLevel.Height = 32;
        _btnShowLevel.FlatStyle = FlatStyle.Flat;
        _btnShowLevel.FlatAppearance.BorderColor = Color.FromArgb(200, 208, 219);
        _btnShowLevel.BackColor = Color.White;
        _btnShowLevel.ForeColor = Color.FromArgb(85, 85, 85);
        _btnShowLevel.Font = new Font("Segoe UI", 9, FontStyle.Regular);
        _btnShowLevel.Click += BtnShowLevel_Click;
        groupBox.Controls.Add(_btnShowLevel);

        _btnIsolateLevel = new Button();
        _btnIsolateLevel.Text = "Изолировать";
        _btnIsolateLevel.Top = 331;
        _btnIsolateLevel.Left = 560;
        _btnIsolateLevel.Width = 140;
        _btnIsolateLevel.Height = 32;
        _btnIsolateLevel.FlatStyle = FlatStyle.Flat;
        _btnIsolateLevel.FlatAppearance.BorderColor = Color.FromArgb(200, 208, 219);
        _btnIsolateLevel.BackColor = Color.White;
        _btnIsolateLevel.ForeColor = Color.FromArgb(85, 85, 85);
        _btnIsolateLevel.Font = new Font("Segoe UI", 9, FontStyle.Regular);
        _btnIsolateLevel.Click += BtnIsolateLevel_Click;
        groupBox.Controls.Add(_btnIsolateLevel);

        _btnApply = new Button();
        _btnApply.Text = "Применить";
        _btnApply.Top = 105;
        _btnApply.Left = 560;
        _btnApply.Width = 140;
        _btnApply.Height = 32;
        _btnApply.FlatStyle = FlatStyle.Flat;
        _btnApply.FlatAppearance.BorderSize = 0;
        _btnApply.BackColor = Color.FromArgb(26, 115, 232);
        _btnApply.ForeColor = Color.White;
        _btnApply.Font = new Font("Segoe UI", 9, FontStyle.Regular);
        _btnApply.Click += BtnApply_Click;
        groupBox.Controls.Add(_btnApply);

        _btnCancel = new Button();
        _btnCancel.Text = "Закрыть";
        _btnCancel.Top = 410;
        _btnCancel.Left = 620;
        _btnCancel.Width = 110;
        _btnCancel.Height = 32;
        _btnCancel.FlatStyle = FlatStyle.Flat;
        _btnCancel.FlatAppearance.BorderColor = Color.FromArgb(200, 208, 219);
        _btnCancel.BackColor = Color.White;
        _btnCancel.ForeColor = Color.FromArgb(85, 85, 85);
        _btnCancel.Click += (s, e) => { this.Close(); };
        this.Controls.Add(_btnCancel);
    }

    private void SetAllPins(bool pinState)
    {
        foreach (LevelItem item in _bindingSource.List)
        {
            item.Pin = pinState;
        }
        _grid.Refresh();
    }

    private void BtnSelectLevel_Click(object sender, EventArgs e)
    {
        try
        {
            if (_grid.SelectedRows.Count > 0 && _app.Project != null)
            {
                var ids = new List<int>();
                LevelItem lowestLevel = null;

                foreach (DataGridViewRow row in _grid.SelectedRows)
                {
                    var item = row.DataBoundItem as LevelItem;
                    if (item != null && item.Id != -1)
                    {
                        ids.Add(item.Id);
                        
                        if (lowestLevel == null || item.Elevation < lowestLevel.Elevation)
                        {
                            lowestLevel = item;
                        }
                    }
                }
                if (ids.Count > 0)
                {
                    var view = _app.ActiveView as Renga.IModelView;
                    if (view != null && lowestLevel != null)
                    {
#if RENGA_9_2_OR_GREATER
                        view.ActiveLevelId = lowestLevel.Id;
#endif
                    }
                    
                    _app.Selection.SetSelectedObjects(ids.ToArray());
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Ошибка при выборе уровня:\n" + ex.Message);
        }
    }

    private void BtnHideLevel_Click(object sender, EventArgs e)
    {
        try
        {
            if (_grid.SelectedRows.Count > 0 && _app.Project != null)
            {
                var view = _app.ActiveView as Renga.IModelView;
                if (view != null)
                {
                    var objectsToHide = new List<int>();
                    foreach (DataGridViewRow row in _grid.SelectedRows)
                    {
                        var item = row.DataBoundItem as LevelItem;
                        if (item != null && item.Id != -1)
                        {
                            objectsToHide.AddRange(GetLevelAndDependentObjects(item.Id));
                        }
                    }
                    if (objectsToHide.Count > 0)
                        view.SetObjectsVisibility(objectsToHide.Distinct().ToArray(), false);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Ошибка при скрытии уровня:\n" + ex.Message);
        }
    }

    private void BtnShowLevel_Click(object sender, EventArgs e)
    {
        try
        {
            if (_grid.SelectedRows.Count > 0 && _app.Project != null)
            {
                var view = _app.ActiveView as Renga.IModelView;
                if (view != null)
                {
                    var objectsToShow = new List<int>();
                    foreach (DataGridViewRow row in _grid.SelectedRows)
                    {
                        var item = row.DataBoundItem as LevelItem;
                        if (item != null && item.Id != -1)
                        {
                            objectsToShow.AddRange(GetLevelAndDependentObjects(item.Id));
                        }
                    }
                    if (objectsToShow.Count > 0)
                        view.SetObjectsVisibility(objectsToShow.Distinct().ToArray(), true);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Ошибка при показе уровня:\n" + ex.Message);
        }
    }

    private void BtnIsolateLevel_Click(object sender, EventArgs e)
    {
        try
        {
            if (_grid.SelectedRows.Count > 0 && _app.Project != null)
            {
                var view = _app.ActiveView as Renga.IModelView;
                if (view != null)
                {
                    var model = _app.Project.Model;
                    var allObjects = model.GetObjects();
                    var objectsToHide = new List<int>();
                    var objectsToShow = new List<int>();
                    var selectedIds = new List<int>();

                    foreach (DataGridViewRow row in _grid.SelectedRows)
                    {
                        var item = row.DataBoundItem as LevelItem;
                        if (item != null && item.Id != -1)
                        {
                            objectsToShow.AddRange(GetLevelAndDependentObjects(item.Id));
                            selectedIds.Add(item.Id);
                        }
                    }

                    var distinctToShow = objectsToShow.Distinct().ToList();

                    for (int i = 0; i < allObjects.Count; i++)
                    {
                        int objId = allObjects.GetByIndex(i).Id;
                        if (!distinctToShow.Contains(objId))
                        {
                            objectsToHide.Add(objId);
                        }
                    }

                    if (objectsToHide.Count > 0)
                        view.SetObjectsVisibility(objectsToHide.ToArray(), false);

                    if (distinctToShow.Count > 0)
                        view.SetObjectsVisibility(distinctToShow.ToArray(), true);
                        
                    if (selectedIds.Count > 0)
                        _app.Selection.SetSelectedObjects(selectedIds.ToArray());
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Ошибка при изоляции уровня:\n" + ex.Message);
        }
    }

    private List<int> GetLevelAndDependentObjects(int levelId)
    {
        var list = new List<int> { levelId };
        var model = _app.Project.Model;
        var allObjects = model.GetObjects();
        for (int i = 0; i < allObjects.Count; i++)
        {
            var obj = allObjects.GetByIndex(i);
            try {
                var levelObj = obj.GetInterfaceByName("ILevelObject") as Renga.ILevelObject;
                if (levelObj != null && levelObj.LevelId == levelId)
                {
                    list.Add(obj.Id);
                }
            } catch { }
        }
        return list;
    }

    private void LoadData()
    {
        _levelsData = new List<LevelItem>();
        if (_app.Project == null) return;
        
        var model = _app.Project.Model;
        var objCol = model.GetObjects();
        
        for (int i = 0; i < objCol.Count; i++)
        {
            var obj = objCol.GetByIndex(i);
            if (obj.ObjectType == LevelEntityType)
            {
                var item = new LevelItem { Id = obj.Id, OriginalId = obj.Id };
                
                var parameters = obj.GetParameters();
                if (parameters.Contains(LevelNameParam))
                    item.Name = parameters.Get(LevelNameParam).GetStringValue();
                else
                    item.Name = obj.Name;

                if (parameters.Contains(LevelElevationParam))
                    item.Elevation = Math.Round(parameters.Get(LevelElevationParam).GetDoubleValue(), 4);
                    
                item.Pin = obj.Pinned;

                item.OriginalName = item.Name;
                item.OriginalElevation = item.Elevation;
                item.OriginalPin = item.Pin;
                
                _levelsData.Add(item);
            }
        }
        
        // Сортировка по отметке
        _levelsData = _levelsData.OrderBy(l => l.Elevation).ToList();

        _bindingSource = new BindingSource();
        _bindingSource.DataSource = _levelsData.Where(x => !x.IsDeleted).ToList();
        _grid.DataSource = _bindingSource;
    }

    private void BtnApply_Click(object sender, EventArgs e)
    {
        if (_app.Project == null) return;

        try
        {
            var model = _app.Project.Model;
            var operation = model.CreateOperation();
            operation.Start();

            int created = 0, updated = 0, deleted = 0;

            // Сначала удаляем
            var deletedItems = _levelsData.Where(x => x.IsDeleted).ToList();
            foreach (var item in deletedItems)
            {
                model.DeleteObjectById(item.Id);
                deleted++;
            }

            // Обработка добавленных и измененных
            var currentItems = _bindingSource.List.Cast<LevelItem>().ToList();
            foreach (var item in currentItems)
            {
                if (item.Id == -1) // Новый уровень
                {
                    var args = model.CreateNewEntityArgs();
                    args.TypeId = LevelEntityType;
                    var newObj = model.CreateObject(args);
                    
                    var parameters = newObj.GetParameters();
                    if (parameters.Contains(LevelNameParam))
                        parameters.Get(LevelNameParam).SetStringValue(string.IsNullOrEmpty(item.Name) ? "Новый уровень" : item.Name);
                    
                    if (parameters.Contains(LevelElevationParam))
                        parameters.Get(LevelElevationParam).SetDoubleValue(item.Elevation);
                    
                    newObj.Pinned = item.Pin;

                    item.Id = newObj.Id;
                    item.OriginalName = item.Name;
                    item.OriginalElevation = item.Elevation;
                    item.OriginalPin = item.Pin;
                    created++;
                }
                else // Существующий
                {
                    if (item.Name != item.OriginalName || Math.Abs(item.Elevation - item.OriginalElevation) > 0.001 || item.Pin != item.OriginalPin)
                    {
                        var obj = model.GetObjects().GetById(item.Id);
                        if (obj != null)
                        {
                            var parameters = obj.GetParameters();
                            if (parameters.Contains(LevelNameParam))
                                parameters.Get(LevelNameParam).SetStringValue(string.IsNullOrEmpty(item.Name) ? "Уровень" : item.Name);
                            if (parameters.Contains(LevelElevationParam))
                                parameters.Get(LevelElevationParam).SetDoubleValue(item.Elevation);
                                
                            obj.Pinned = item.Pin;

                            item.OriginalName = item.Name;
                            item.OriginalElevation = item.Elevation;
                            item.OriginalPin = item.Pin;
                            updated++;
                        }
                    }
                }
            }

            operation.Apply();

            LoadData(); // Обновляем грид после изменений
            
            // MessageBox.Show($"Создано: {created}\nИзменено: {updated}\nУдалено: {deleted}", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
            // this.Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Ошибка при применении изменений: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

public class LevelItem
{
    public int Id { get; set; }
    public int OriginalId { get; set; }
    public string Name { get; set; }
    public double Elevation { get; set; }
    public string OriginalName { get; set; }
    public double OriginalElevation { get; set; }
    public bool Pin { get; set; }
    public bool OriginalPin { get; set; }
    public bool IsDeleted { get; set; } = false;
}
