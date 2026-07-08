using System;
using System.Windows.Forms;
using System.Drawing;
using System.Collections.Generic;

var project = RengaApp.Project;
var model = project.Model;
var modelObjects = model.GetObjects();

string NormalizeGuid(string g) => g?.Replace("{", "").Replace("}", "").ToUpper();

var dict = new Dictionary<string, string> {
    {"4329112A-6B65-48D9-9DA8-ABF1F8F36327", "Стена"},
    {"D9EE2442-E807-42FB-8FE5-9DCFE543035D", "Колонна"},
    {"63478188-7C88-4A6D-B891-9725F04A5BC7", "Балка"},
    {"F5BD8BD8-39C1-47F8-8499-F673C580DFBE", "Перекрытие"},
    {"BAC4470F-D560-4F57-A49E-FAA5F6E5A279", "Крыша"},
    {"2B02B353-2CA5-4566-88BB-917EA8460174", "Окно"},
    {"1CFBA99C-01E7-4078-AE1A-3E2FF0673599", "Дверь"},
    {"3F522F49-AEE2-4D73-9866-9B07CF336A69", "Лестница"},
    {"DEBDE004-AFCC-4DA8-8DD0-4223FF836ACD", "Пандус"},
    {"F1A805FF-573D-F46B-FFBA-57F4BCCAA6ED", "Помещение"},
    {"ECEF8F90-0CF9-4494-98DE-91242A2A9F5C", "Отверстие"},
    {"FC443D5A-B76C-45E5-B91C-520EF0896109", "Проем"},
    {"6063816C-89FF-4C8F-A814-3BE6CB94128E", "Одиночный фундамент"},
    {"D7DD0293-DD65-4229-A64C-8B528D4E226F", "Ленточный фундамент"},
    {"B8C7155A-B462-4FF5-BC41-C9C17A9F48FA", "Сантехническое оборудование"},
    {"DE4420CE-02B6-4B12-9CD7-9322118BE8FE", "Вентиляционное оборудование"},
    {"793D3F7C-905D-4D85-A351-B152241DD2E7", "Осветительный прибор"},
    {"41E2788A-49ED-487F-9AE1-55B6E09AE6E5", "Деталь трубопровода"},
    {"D31DC2E3-808E-4987-8481-7F86665A07FC", "Фитинг трубопровода"},
    {"47D0D93F-3C7B-4269-BF8A-DE246E1724D0", "Деталь воздуховода"},
    {"77FFCA60-B20E-49F0-B42F-4FDC9B1C825B", "Фитинг воздуховода"},
    {"B00D5C25-92A8-4409-A3B7-7C37ED792C06", "Электроустановочное изделие"},
    {"02BBEBE8-E28B-4EE5-8916-11B514A35DCA", "3D линия"},
    {"84B43087-D4A4-4CCE-B34D-40E283D9E691", "Штриховка"},
    {"DA557027-F243-4331-BB5B-853ABC437CD7", "Текст"},
    {"5D2F3734-5A49-4504-90B1-0676F0F25DA7", "Оборудование"},
    {"CB825BF3-15AE-4190-821C-8AD314951ADA", "Сборка"},
    {"00799249-1824-4EBD-BF93-40BB92EFA9E6", "Экземпляр сборки"},
    {"8A49A9A8-A401-4AB1-8038-92093503C97A", "Фасад"},
    {"4166FD59-64C0-45EE-AE3B-49FAE1257EF1", "Разрез"},
    {"4B41CCF8-C969-4C55-A1F2-CCED9C164F07", "Ось"},
    {"C3CE17FF-6F28-411F-B18D-74FE957B2BA8", "Уровень"},
    {"8B323BEE-3882-4744-8838-24F45DF714A9", "Трасса"},
    {"838CC9F6-E3D8-4132-AF6F-C58DF0F8D037", "Труба"},
    {"06CC88EE-9A67-4626-9C34-DDE03C331A74", "Воздуховод"},
    {"9FABC932-590F-4068-89A8-EE6EE3D7CBBF", "Арматурный стержень"}
};

var pinnedItems = new List<PinnedItem>();

for (int i = 0; i < modelObjects.Count; i++)
{
    var obj = modelObjects.GetByIndex(i);
    if (obj != null && obj.Pinned)
    {
        string typeGuid = NormalizeGuid(obj.ObjectTypeS);
        string typeName = dict.ContainsKey(typeGuid) ? dict[typeGuid] : "Неизвестный тип";
        pinnedItems.Add(new PinnedItem {
            Id = obj.Id,
            Name = obj.Name,
            ObjectType = typeName,
            UniqueId = obj.UniqueIdS
        });
    }
}

if (pinnedItems.Count == 0)
{
    MessageBox.Show("Закрепленных элементов в проекте не найдено.", "Результат", MessageBoxButtons.OK, MessageBoxIcon.Information);
    return;
}

Form form = new Form() { AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi };
form.Text = "Закрепленные элементы";
form.BackColor = Color.White;
form.Font = new Font("Segoe UI", 9);
form.FormBorderStyle = FormBorderStyle.Sizable;
form.StartPosition = FormStartPosition.CenterScreen;
form.Width = 1100;
form.Height = 600;
form.MinimumSize = new Size(800, 300);

GroupBox groupBox = new GroupBox();
groupBox.Text = "Список закрепленных элементов";
groupBox.Top = 15;
groupBox.Left = 15;
groupBox.Width = 1054;
groupBox.Height = 480;
groupBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
form.Controls.Add(groupBox);

DataGridView dgv = new DataGridView();
dgv.Top = 20;
dgv.Left = 10;
dgv.Width = 1034;
dgv.Height = 450;
dgv.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
dgv.ReadOnly = true;
dgv.AllowUserToAddRows = false;
dgv.AllowUserToDeleteRows = false;
dgv.RowHeadersVisible = false;
dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
dgv.BackgroundColor = Color.White;
dgv.BorderStyle = BorderStyle.None;

// Disable blue headers
dgv.EnableHeadersVisualStyles = false;
dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240);
dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(240, 240, 240);
dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.Black;
dgv.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.Black;

dgv.Columns.Add("Id", "ID");
dgv.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
dgv.Columns[0].Width = 60;
dgv.Columns.Add("Name", "Имя");
dgv.Columns[1].FillWeight = 350;
dgv.Columns.Add("Type", "Категория");
dgv.Columns[2].FillWeight = 150;
dgv.Columns.Add("UniqueId", "Уникальный ID");
dgv.Columns[3].FillWeight = 270;

foreach (var item in pinnedItems)
{
    dgv.Rows.Add(item.Id, item.Name, item.ObjectType, item.UniqueId);
}
dgv.ClearSelection();
groupBox.Controls.Add(dgv);

int btnTop = 510;

Button btnSelect = new Button();
btnSelect.Text = "Выделить";
btnSelect.Width = 100;
btnSelect.Height = 32;
btnSelect.Top = btnTop;
btnSelect.Left = 689;
btnSelect.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
btnSelect.FlatStyle = FlatStyle.Flat;
btnSelect.BackColor = Color.FromArgb(26, 115, 232);
btnSelect.ForeColor = Color.White;
btnSelect.FlatAppearance.BorderSize = 0;
btnSelect.Click += (s, e) => {
    if (dgv.SelectedRows.Count == 0) return;
    int[] ids = new int[dgv.SelectedRows.Count];
    for (int j = 0; j < dgv.SelectedRows.Count; j++) {
        ids[j] = (int)dgv.SelectedRows[j].Cells[0].Value;
    }
    RengaApp.Selection.SetSelectedObjects(ids);
};
form.Controls.Add(btnSelect);

Button btnUnpin = new Button();
btnUnpin.Text = "Снять закрепление";
btnUnpin.Width = 160;
btnUnpin.Height = 32;
btnUnpin.Top = btnTop;
btnUnpin.Left = 799;
btnUnpin.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
btnUnpin.FlatStyle = FlatStyle.Flat;
btnUnpin.BackColor = Color.FromArgb(26, 115, 232);
btnUnpin.ForeColor = Color.White;
btnUnpin.FlatAppearance.BorderSize = 0;
btnUnpin.Click += (s, e) => {
    if (dgv.SelectedRows.Count == 0) return;

    var selectedIds = new HashSet<int>();
    var itemsToRemove = new List<DataGridViewRow>();

    foreach (DataGridViewRow row in dgv.SelectedRows)
    {
        selectedIds.Add((int)row.Cells[0].Value);
        itemsToRemove.Add(row);
    }

    var op = project.CreateOperation();
    op.Start();
    
    for (int j = 0; j < modelObjects.Count; j++)
    {
        var mo = modelObjects.GetByIndex(j);
        if (mo != null && selectedIds.Contains(mo.Id))
        {
            mo.Pinned = false;
        }
    }
    
    op.Apply();

    foreach(var row in itemsToRemove)
    {
        dgv.Rows.Remove(row);
    }
    dgv.ClearSelection();
};
form.Controls.Add(btnUnpin);

Button btnClose = new Button();
btnClose.Text = "Закрыть";
btnClose.Width = 100;
btnClose.Height = 32;
btnClose.Top = btnTop;
btnClose.Left = 969;
btnClose.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
btnClose.FlatStyle = FlatStyle.Flat;
btnClose.BackColor = Color.White;
btnClose.ForeColor = Color.Black;
btnClose.FlatAppearance.BorderColor = Color.FromArgb(200, 208, 219);
btnClose.FlatAppearance.BorderSize = 1;
btnClose.Click += (s, e) => form.Close();
form.Controls.Add(btnClose);

form.ShowDialog();

public class PinnedItem 
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string ObjectType { get; set; }
    public string UniqueId { get; set; }
}
