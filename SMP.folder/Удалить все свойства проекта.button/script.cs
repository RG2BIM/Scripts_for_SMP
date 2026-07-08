using System;
using Renga;

var project = RengaApp.Project;
var propertyManager = project.PropertyManager;
int count = propertyManager.PropertyCount;

if (count == 0) 
{
    System.Windows.Forms.MessageBox.Show("Свойств нет, удалять нечего.", "Информация", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
    return;
}

var result = System.Windows.Forms.MessageBox.Show($"Внимание! Вы собираетесь безвозвратно удалить все свойства ({count} шт.) из проекта. Продолжить?", "Удаление свойств", System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Warning);
if (result != System.Windows.Forms.DialogResult.Yes) return;

var properties = new System.Collections.Generic.List<Guid>();
for (int i = 0; i < count; i++)
{
    properties.Add(propertyManager.GetPropertyId(i));
}

var operation = project.CreateOperation();
operation.Start();
foreach (var prop in properties)
{
    propertyManager.UnregisterProperty(prop);
}
operation.Apply();

System.Windows.Forms.MessageBox.Show($"Все свойства ({count} шт.) успешно удалены из проекта.", "Успех", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
