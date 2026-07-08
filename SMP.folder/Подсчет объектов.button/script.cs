using System;
using Renga;

var project = RengaApp.Project;
var modelObjs = project.Model.GetObjects();

var typeNames = new System.Collections.Generic.Dictionary<string, string>() { 
    {"{F1A805FF-573D-F46B-FFBA-57F4BCCAA6ED}", "Помещение"},
    {"{4329112A-6B65-48D9-9DA8-ABF1F8F36327}", "Стена"},
    {"{D9EE2442-E807-42FB-8FE5-9DCFE543035D}", "Колонна"},
    {"{1CFBA99C-01E7-4078-AE1A-3E2FF0673599}", "Дверь"},
    {"{2B02B353-2CA5-4566-88BB-917EA8460174}", "Окно"},
    {"{63478188-7C88-4A6D-B891-9725F04A5BC7}", "Балка"},
    {"{F5BD8BD8-39C1-47F8-8499-F673C580DFBE}", "Перекрытие"},
    {"{DEBDE004-AFCC-4DA8-8DD0-4223FF836ACD}", "Пандус"},
    {"{3F522F49-AEE2-4D73-9866-9B07CF336A69}", "Лестница"},
    {"{D7DD0293-DD65-4229-A64C-8B528D4E226F}", "Ленточный фундамент"},
    {"{6063816C-89FF-4C8F-A814-3BE6CB94128E}", "Столбчатый фундамент"},
    {"{96788994-B7FC-41D7-8A99-D674543E9237}", "Угловой размер"},
    {"{CB825BF3-15AE-4190-821C-8AD314951ADA}", "Сборка"},
    {"{00799249-1824-4EBD-BF93-40BB92EFA9E6}", "Экземпляр сборки"},
    {"{4B41CCF8-C969-4C55-A1F2-CCED9C164F07}", "Ось"},
    {"{165D15BC-FD8D-4BBB-B73C-56956D7CEBF1}", "Здание"},
    {"{2AABE3A4-A29E-4534-A9F5-0F070FEE240C}", "Диаметральный размер"},
    {"{A7DFE1E1-BF2C-4C4A-BA74-3F156B1BBF8F}", "Чертеж"},
    {"{107E6D8B-A68A-43FD-A3BF-A25D0A1C17F9}", "Высотная отметка"},
    {"{800081E3-065B-4AE1-86F8-3EA8EB830FEC}", "Разрез"},
    {"{688CCE66-411F-44A2-A5CC-149BDDE3169C}", "Текст"},
    {"{06CC88EE-9A67-4626-9C34-DDE03C331A74}", "Воздуховод"},
    {"{47D0D93F-3C7B-4269-BF8A-DE246E1724D0}", "Аксессуар воздуховода"},
    {"{77FFCA60-B20E-49F0-B42F-4FDC9B1C825B}", "Деталь воздуховода"},
    {"{83DE45E6-4793-49EC-8B9E-65A2438F36DE}", "Электрическая линия"},
    {"{96DA9155-43C1-42B8-BBA2-B4F61FA43ACC}", "Электрический щит"},
    {"{E1E3BD66-2E13-4FA4-A9EB-677E03067C2F}", "Элемент"},
    {"{8A49A9A8-A401-4AB1-8038-92093503C97A}", "Уровень"},
    {"{5D2F3734-5A49-4504-90B1-0676F0F25DA7}", "Оборудование"},
    {"{F9C7F77A-5644-4ED3-85CE-9EA21881D76A}", "Группа"},
    {"{84B43087-D4A4-4CCE-B34D-40E283D9E691}", "Штриховка"},
    {"{ECEF8F90-0CF9-4494-98DE-91242A2A9F5C}", "Отверстие"},
    {"{517A337A-58D5-46FF-81B8-65CF0389A191}", "Изоляция"},
    {"{C3CE17FF-6F28-411F-B18D-74FE957B2BA8}", "Уровень"},
    {"{C59FD4C5-4050-47A0-B11A-F52C4799470C}", "Осветительный прибор"},
    {"{02BBEBE8-E28B-4EE5-8916-11B514A35DCA}", "3D Линия"},
    {"{DC82CA1A-A0C3-4A1A-AEFB-A7D720DD3A09}", "Линейный размер"},
    {"{DE4420CE-02B6-4B12-9CD7-9322118BE8FE}", "Механическое оборудование"},
    {"{FC443D5A-B76C-45E5-B91C-520EF0896109}", "Проем"},
    {"{838CC9F6-E3D8-4132-AF6F-C58DF0F8D037}", "Труба"},
    {"{41E2788A-49ED-487F-9AE1-55B6E09AE6E5}", "Аксессуар трубы"},
    {"{D31DC2E3-808E-4987-8481-7F86665A07FC}", "Деталь трубы"},
    {"{62CF086E-5A39-4484-840C-FFA6A1C6E2B7}", "Пластина"},
    {"{B8C7155A-B462-4FF5-BC41-C9C17A9F48FA}", "Санитарно-техническое оборудование"},
    {"{A1ACA786-78A4-4015-B412-9150BAAD71A9}", "Ограждение"},
    {"{9FABC932-590F-4068-89A8-EE6EE3D7CBBF}", "Арматурный стержень"},
    {"{BAC4470F-D560-4F57-A49E-FAA5F6E5A279}", "Крыша"},
    {"{CE93E320-7167-4CD1-92A8-5E42D546066B}", "Трасса"},
    {"{17CABDC4-B683-484F-8858-2145688AE7F5}", "Точка трассы"},
    {"{56652D5B-536E-4EF6-A1CD-5AD69BB025AB}", "Участок"},
    {"{EAFCC366-1483-44D5-881F-B4688D306DA5}", "Тема"},
    {"{B00D5C25-92A8-4409-A3B7-7C37ED792C06}", "Электроустановочное изделие"}
};

var counts = new System.Collections.Generic.Dictionary<string, int>();
int totalCounted = 0;

for (int i = 0; i < modelObjs.Count; i++)
{
    var obj = modelObjs.GetByIndex(i);
    string objTypeS = obj.ObjectTypeS.ToUpper();
    
    if (typeNames.ContainsKey(objTypeS))
    {
        string name = typeNames[objTypeS];
        if (!counts.ContainsKey(name)) counts[name] = 0;
        counts[name]++;
        totalCounted++;
    }
}

Print("--- Подсчет объектов ---");
foreach (var kvp in counts)
{
    Print($"{kvp.Key}: {kvp.Value}");
}
Print("-----------------------");
Print($"Всего элементов: {totalCounted}");
