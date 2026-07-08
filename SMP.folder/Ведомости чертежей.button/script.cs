using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Renga;

var project = RengaApp.Project;
if (string.IsNullOrEmpty(project.FilePath))
{
    Print("Проект не сохранен. Сохраните проект для создания ведомостей.");
    return;
}

string projectDir = Path.GetDirectoryName(project.FilePath);
string projectName = Path.GetFileNameWithoutExtension(project.FilePath);
string saveDir = Path.Combine(projectDir, projectName);
if (!Directory.Exists(saveDir)) Directory.CreateDirectory(saveDir);

string logFile = Path.Combine(saveDir, $"{projectName}.txt");
var sw = new StreamWriter(logFile, false, new System.Text.UTF8Encoding(true));
try
{
    Action<string> AddStr = (s) => { sw.WriteLine(s); Print(s); };

    var projectInfo = project.ProjectInfo;
    AddStr($"Номер проекта: {projectInfo.Code}");
    AddStr($"Наименование: {projectInfo.Name}");
    AddStr($"Стадия: {projectInfo.Stage}");
    AddStr("");
    AddStr("Основные комплекты чертежей:");
    AddStr("");

    // Gather drawings
    if (project.Drawings2 == null)
    {
        Print("Модуль чертежей недоступен.");
        return;
    }
    var drawingIdsObj = project.Drawings2.GetIds();
    int[] drawingIds = drawingIdsObj as int[];
    var drawings = new List<DrawingInfo>();
    
    if (drawingIds != null)
    {
        for (int i = 0; i < drawingIds.Length; i++)
        {
            var id = drawingIds[i];
            var drawing = project.Drawings2.GetById(id);
            if (drawing == null) continue;

            var pCont = (IParameterContainer)drawing.GetInterfaceByName("IParameterContainer");

            int formatId = 0, topicId = 1;
            if (pCont != null)
            {
                try { formatId = pCont.GetS("7B547FDF-FDC8-4F1C-ADB2-94BA8EC80657").GetIntValue(); } catch {}
                try { topicId = pCont.GetS("3B7FDF99-6C5E-4FED-8A3C-42149FE5D8B4").GetIntValue(); } catch {}
            }
            
            string formatName = "";
            if (formatId != 0 && project.PageFormatStyles != null) 
            {
                try { 
                    var fmt = project.PageFormatStyles.GetById(formatId);
                    if (fmt != null) formatName = fmt.Name; 
                } catch {}
            }

            int numInTopic = 0;
            try { numInTopic = project.GetEntityNumberInTopicS(drawing.UniqueIdS); } catch {}

            drawings.Add(new DrawingInfo {
                Num = numInTopic.ToString(),
                Name = drawing.Name,
                TopicId = topicId,
                Format = formatName
            });
        }
    }

    if (drawings.Count == 0)
    {
        AddStr("В проекте нет ни одного чертежа.");
        return;
    }

    // Topics
    var topicIds = drawings.Select(d => d.TopicId).Distinct().ToList();
    var topics = new List<TopicInfo>();
    foreach (var tid in topicIds)
    {
        if (tid == 1) 
        {
            topics.Add(new TopicInfo { Id = 1, ShortName = "-", Name = "Раздел не назначен" });
        }
        else
        {
            var t = project.Topics?.GetById(tid);
            if (t == null)
            {
                topics.Add(new TopicInfo { Id = tid, ShortName = "Удаленный раздел", Name = "Удаленный раздел" });
                continue;
            }

            string tName = "";
            try {
                var p = (IPropertyContainer)t.GetInterfaceByName("IPropertyContainer");
                if (p != null) tName = p.GetS("ff2e904e-04e4-4f6e-b5d4-a6fa7a7d2cda").GetStringValue();
            } catch {}
            topics.Add(new TopicInfo { Id = tid, ShortName = t.Name, Name = tName });
        }
    }

    topics = topics.OrderBy(t => t.ShortName).ToList();

    foreach (var t in topics)
    {
        AddStr($"--- Раздел: {t.Name} ({t.ShortName}) ---");
        var topicDrawings = drawings.Where(d => d.TopicId == t.Id).OrderBy(d => d.Num).ToList();
        
        if (t.Id != 1)
        {
            string csvPath = Path.Combine(saveDir, $"{projectName}_{t.ShortName}.csv");
            var csv = new StreamWriter(csvPath, false, new System.Text.UTF8Encoding(true));
            try
            {
                csv.WriteLine("Номер_листа;Наименование;Формат");
                foreach (var d in topicDrawings)
                {
                    csv.WriteLine($"{d.Num};{d.Name};{d.Format}");
                    AddStr($"\t{d.Num}\t{d.Name}");
                }
            }
            finally
            {
                csv.Dispose();
            }
        }
        else
        {
            foreach (var d in topicDrawings)
            {
                AddStr($"\t{d.Num}\t{d.Name}");
            }
        }
        AddStr("");
    }
}
finally
{
    sw.Dispose();
}
Print($"Готово! Ведомости сохранены в папку: {saveDir}");

public class DrawingInfo 
{
    public string Num;
    public string Name;
    public int TopicId;
    public string Format;
}

public class TopicInfo
{
    public int Id;
    public string ShortName;
    public string Name;
}
