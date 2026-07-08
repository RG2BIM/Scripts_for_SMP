using System;
using System.Linq;
using System.Collections.Generic;
using Renga;

var project = RengaApp.Project;
var modelObjs = project.Model.GetObjects();

Print("--- Уникальные окна и двери ---");

var windowGuid = "{2B02B353-2CA5-4566-88BB-917EA8460174}";
var doorGuid = "{1CFBA99C-01E7-4078-AE1A-3E2FF0673599}";

var uniqueWindows = new Dictionary<string, List<int>>();
var uniqueDoors = new Dictionary<string, List<int>>();

for (int i = 0; i < modelObjs.Count; i++)
{
    var obj = modelObjs.GetByIndex(i);
    bool isWindow = obj.ObjectTypeS.Equals(windowGuid, StringComparison.OrdinalIgnoreCase);
    bool isDoor = obj.ObjectTypeS.Equals(doorGuid, StringComparison.OrdinalIgnoreCase);

    if (isWindow || isDoor)
    {
        string typeName = isWindow ? "Окно" : "Дверь";
        string propsString = "";
        var propLines = new List<string>();

        // 1. Извлекаем Стиль объекта (он определяет общую форму и конструкцию)
        IParameterContainer stylePCont = null;
        var pCont = obj.GetParameters();
        string shapeStr = "";
        
        if (pCont != null)
        {
            try {
                if (isWindow) {
                    var styleParam = pCont.GetS("{cb4882f5-ba8b-4690-8bf0-0e48b52561ae}");
                    if (styleParam != null && styleParam.HasValue) {
                        int styleId = styleParam.GetIntValue();
                        var style = project.WindowStyles.GetById(styleId);
                        if (style != null) {
                            propLines.Add($"  Стиль (форма): {style.Name}");
                            try { stylePCont = (IParameterContainer)style.GetInterfaceByName("IParameterContainer"); } catch {}
                        }
                    }
                    var shapeParam = pCont.GetS("{faf78a04-fda6-40ae-add6-dec437bdd1d8}");
                    if (shapeParam != null && shapeParam.HasValue) {
                        int sVal = shapeParam.GetIntValue();
                        switch(sVal) {
                            case 1: shapeStr = "Прямоугольный проём"; break;
                            case 2: shapeStr = "Овальный проём"; break;
                            case 3: shapeStr = "Арочный проём"; break;
                            case 4: shapeStr = "Левый полуарочный проём"; break;
                            case 5: shapeStr = "Правый полуарочный проём"; break;
                            case 6: shapeStr = "Трапециевидный проём"; break;
                            case 7: shapeStr = "Левый полутрапециевидный проём"; break;
                            case 8: shapeStr = "Правый полутрапециевидный проём"; break;
                            default: shapeStr = "Проём тип " + sVal; break;
                        }
                    }
                } else if (isDoor) {
                    var styleParam = pCont.GetS("{dd5ce6b5-5cec-4782-bfd6-04492230bce0}");
                    if (styleParam != null && styleParam.HasValue) {
                        int styleId = styleParam.GetIntValue();
                        var style = project.DoorStyles.GetById(styleId);
                        if (style != null) {
                            propLines.Add($"  Стиль (форма): {style.Name}");
                            try { stylePCont = (IParameterContainer)style.GetInterfaceByName("IParameterContainer"); } catch {}
                        }
                    }
                    var shapeParam = pCont.GetS("{155e84bd-1683-4f73-ba3d-df5f9fb67be9}");
                    if (shapeParam != null && shapeParam.HasValue) {
                        int sVal = shapeParam.GetIntValue();
                        switch(sVal) {
                            case 1: shapeStr = "Прямоугольный проём"; break;
                            case 2: shapeStr = "Овальный проём"; break;
                            case 3: shapeStr = "Арочный проём"; break;
                            case 4: shapeStr = "Левый полуарочный проём"; break;
                            case 5: shapeStr = "Правый полуарочный проём"; break;
                            case 6: shapeStr = "Трапециевидный проём"; break;
                            case 7: shapeStr = "Левый полутрапециевидный проём"; break;
                            case 8: shapeStr = "Правый полутрапециевидный проём"; break;
                            default: shapeStr = "Проём тип " + sVal; break;
                        }
                    }
                }
            } catch {}
        }

        // Вспомогательная функция для чтения системных параметров (ширина, высота и т.д.)
        Action<IParameterContainer, string> ReadParams = (pContRef, prefix) => {
            if (pContRef == null) return;
            try {
                var pIds = pContRef.GetIds();
                for (int j = 0; j < pIds.Count; j++) {
                    var param = pContRef.GetS(pIds.GetS(j));
                    if (param != null && param.HasValue) {
                        try {
                            string name = param.Definition.Name;
                            int pType = (int)param.Definition.ParameterType;
                            string val = "";
                            try {
                                if (pType == 2) val = param.GetStringValue();
                                else if (pType == 7) val = param.GetIntValue().ToString();
                                else if (pType == 9) val = param.GetBoolValue().ToString();
                                else val = param.GetDoubleValue().ToString();
                            } catch {
                                try { val = param.GetStringValue(); } catch {}
                            }
                            if (!string.IsNullOrEmpty(val)) propLines.Add($"  {prefix}{name}: {val}");
                        } catch {}
                    }
                }
            } catch {}
        };

        // 2. Читаем системные параметры стиля и самого объекта
        ReadParams(stylePCont, "[Стиль] ");
        ReadParams(obj.GetParameters(), "[Параметр] ");

        // 3. Читаем пользовательские свойства
        var objProperties = obj.GetProperties();
        if (objProperties != null)
        {
            var ids = objProperties.GetIds();
            for (int j = 0; j < ids.Count; j++)
            {
                var property = objProperties.GetS(ids.GetS(j));
                string propertyValue = "";
                switch ((int)property.Type)
                {
                    case 0: propertyValue = "тип не определен"; break;
                    case 1: propertyValue = property.GetDoubleValue().ToString(); break;
                    case 2: propertyValue = property.GetStringValue(); break;
                    case 3: propertyValue = property.GetAngleValue((Renga.AngleUnit)1).ToString(); break;
                    case 4: propertyValue = property.GetAreaValue((Renga.AreaUnit)1).ToString(); break;
                    case 5: propertyValue = property.GetDoubleValue().ToString(); break;
                    case 6: propertyValue = property.GetEnumerationValue(); break;
                    case 7: propertyValue = property.GetIntegerValue().ToString(); break;
                    case 8: propertyValue = property.GetLengthValue((Renga.LengthUnit)1).ToString(); break;
                    case 9: propertyValue = property.GetLogicalValue().ToString(); break;
                    case 10: propertyValue = property.GetMassValue((Renga.MassUnit)1).ToString(); break;
                    case 11: propertyValue = property.GetVolumeValue((Renga.VolumeUnit)3).ToString(); break;
                }
                propLines.Add($"  [Свойство] {property.Name}: {propertyValue}");
            }
        }

        propLines.Sort(); // Сортируем свойства, чтобы гарантировать точное совпадение
        propsString = string.Join("\n", propLines);

        // Ключ для группировки состоит из имени и всех свойств
        string hashKey = $"{typeName} '{obj.Name}'";
        if (!string.IsNullOrEmpty(shapeStr)) {
            hashKey += $": {shapeStr}";
        }
        hashKey += $"\n{propsString}";
        
        if (isWindow) {
            if (!uniqueWindows.ContainsKey(hashKey)) uniqueWindows[hashKey] = new List<int>();
            uniqueWindows[hashKey].Add(obj.Id);
        } else {
            if (!uniqueDoors.ContainsKey(hashKey)) uniqueDoors[hashKey] = new List<int>();
            uniqueDoors[hashKey].Add(obj.Id);
        }
    }
}

Print("=== ОКНА ===");
int count = 1;
foreach (var kvp in uniqueWindows)
{
    var objList = kvp.Value;
    Print($"{count}. {kvp.Key.Split('\n')[0]} (Количество: {objList.Count}, ID: {string.Join(", ", objList)})");
    count++;
}

Print("");
Print("=== ДВЕРИ ===");
count = 1;
foreach (var kvp in uniqueDoors)
{
    var objList = kvp.Value;
    Print($"{count}. {kvp.Key.Split('\n')[0]} (Количество: {objList.Count}, ID: {string.Join(", ", objList)})");
    count++;
}

Print("-----------------------");

try {
    int maxLen = 0;
    foreach(var kvp in uniqueWindows) {
        var str = $"{count}. {kvp.Key.Split('\n')[0]} (Количество: {kvp.Value.Count}, ID: {string.Join(", ", kvp.Value)})";
        if (str.Length > maxLen) maxLen = str.Length;
    }
    foreach(var kvp in uniqueDoors) {
        var str = $"{count}. {kvp.Key.Split('\n')[0]} (Количество: {kvp.Value.Count}, ID: {string.Join(", ", kvp.Value)})";
        if (str.Length > maxLen) maxLen = str.Length;
    }
    
    int desiredWidth = Math.Min(1600, Math.Max(600, maxLen * 8 + 50)); // Consolas 10pt is ~8px per char

    bool formFound = false;
    foreach (System.Windows.Forms.Form f in System.Windows.Forms.Application.OpenForms) {
        if (f.Text != null && f.Text.Contains("Консоль")) {
            f.Width = desiredWidth;
            formFound = true;
            break;
        }
    }
    if (!formFound) Print("P.S. Окно консоли не найдено для изменения ширины.");
} catch {}
