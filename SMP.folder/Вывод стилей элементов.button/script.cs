using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Renga;

var project = RengaApp.Project;
if (string.IsNullOrEmpty(project.FilePath))
{
    Print("Ошибка: Проект не сохранен на диск. Пожалуйста, сохраните проект перед выводом стилей.");
    return;
}

string fullFilePath = project.FilePath;
string nameFile = Path.GetFileNameWithoutExtension(fullFilePath);
string savePath = Path.GetDirectoryName(fullFilePath);
string txtFilePath = Path.Combine(savePath, $"{nameFile}_Стили.txt");

var categoryNames = new Dictionary<string, string>
{
    { "ElementStyles", "Стили элементов" },
    { "WindowStyles", "Стили окон" },
    { "DoorStyles", "Стили дверей" },
    { "AssemblyStyles", "Стили сборок" },
    { "SystemStyles", "Стили инженерных систем" },
    { "EquipmentStyles", "Стили оборудования" },
    { "PlumbingFixtureStyles", "Стили санитарно-технического оборудования" },
    { "PipeStyles", "Стили труб" },
    { "PipeFittingStyles", "Стили деталей трубопроводов" },
    { "PipeAccessoryStyles", "Стили арматуры трубопроводов" },
    { "DuctStyles", "Стили воздуховодов" },
    { "DuctFittingStyles", "Стили деталей воздуховодов" },
    { "DuctAccessoryStyles", "Стили арматуры воздуховодов" },
    { "WiringAccessoryStyles", "Стили электроустановочных изделий" },
    { "LightFixtureStyles", "Стили осветительных приборов" },
    { "ElectricalCircuitLineStyles", "Стили электрических линий" },
    { "ElectricDistributionBoardStyles", "Стили электрических щитов" },
    { "RebarStyles", "Стили арматуры" },
    { "Materials", "Материалы" },
    { "LayeredMaterials", "Многослойные материалы" },
    { "Profiles", "Профили" }
};

var allStyles = new Dictionary<string, List<string>>();

foreach (var prop in typeof(Renga.IProject).GetProperties())
{
    string pName = prop.Name;
    if (pName.EndsWith("Styles") || pName == "Materials" || pName == "LayeredMaterials" || pName == "Profiles")
    {
        try 
        {
            object propValue = prop.GetValue(project);
            if (propValue == null) continue;
            
            dynamic collection = propValue;
            Array idsArray = null;
            try { idsArray = collection.GetIds() as Array; } catch { continue; }
            
            if (idsArray != null && idsArray.Length > 0)
            {
                var names = new HashSet<string>();
                foreach (var idObj in idsArray)
                {
                    try 
                    {
                        dynamic item = collection.GetById(idObj);
                        if (item != null)
                        {
                            string name = item.Name;
                            if (!string.IsNullOrEmpty(name)) names.Add(name);
                        }
                    } catch { }
                }
                
                if (names.Count > 0)
                {
                    string header = categoryNames.ContainsKey(pName) ? categoryNames[pName] : pName;
                    allStyles[header] = names.OrderBy(x => x).ToList();
                }
            }
        } 
        catch { }
    }
}

string sep = new string('_', 64);
var sw = new StreamWriter(txtFilePath);
try 
{
    Action<string> AddStr = s => { sw.WriteLine(s); Print(s); };

    AddStr(sep);
    AddStr($"Имя файла: {nameFile}");
    AddStr(DateTime.Now.ToString("MM/dd/yyyy HH:mm"));
    AddStr(sep);
    
    if (allStyles.Count == 0)
    {
        AddStr("Стили не найдены.");
    }
    else
    {
        foreach (var kvp in allStyles.OrderBy(x => x.Key))
        {
            AddStr($"=== {kvp.Key} ===");
            foreach (var style in kvp.Value)
            {
                AddStr(style);
            }
            AddStr("");
        }
    }
    AddStr(sep);
} 
finally 
{
    sw.Dispose();
}

Print($"Стили успешно выгружены в файл:");
Print(txtFilePath);
