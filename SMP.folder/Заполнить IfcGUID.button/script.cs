using System;
using System.Collections.Generic;
using System.Text;
using Renga;

var project = RengaApp.Project;
var pm = project.PropertyManager;
var model = project.Model;
var objects = model.GetObjects();

// Функция преобразования UniqueId (Guid) в формат IfcGUID
string EncodeIfcGuid(Guid guid)
{
    byte[] bytes = guid.ToByteArray();
    // Конвертируем в Big-Endian (сетевой порядок байт), который ожидается в IFC
    byte[] b = new byte[16];
    b[0] = bytes[3];
    b[1] = bytes[2];
    b[2] = bytes[1];
    b[3] = bytes[0];
    b[4] = bytes[5];
    b[5] = bytes[4];
    b[6] = bytes[7];
    b[7] = bytes[6];
    Array.Copy(bytes, 8, b, 8, 8);

    // Добавляем два нулевых байта спереди, чтобы получить 18 байт.
    // 18 байт в стандартном Base64 конвертируются ровно в 24 символа (где первые два - "AA")
    byte[] bytes18 = new byte[18];
    Array.Copy(b, 0, bytes18, 2, 16);
    string stdBase64 = Convert.ToBase64String(bytes18);
    string b64std_str = stdBase64.Substring(2); // Отбрасываем "AA", остается 22 символа
    
    string B64_STD = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
    string B64_IFC = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz_$";
    
    // Заменяем символы стандартного алфавита Base64 на алфавит IFC
    StringBuilder sb = new StringBuilder(22);
    foreach (char ch in b64std_str)
    {
        int i = B64_STD.IndexOf(ch);
        sb.Append(B64_IFC[i]);
    }
    return sb.ToString();
}

string propName = "IfcGUID";
Guid propId = Guid.Empty;
for(int i=0; i<pm.PropertyCount; i++) {
    var id = pm.GetPropertyId(i);
    if(pm.GetPropertyName(id) == propName) { propId = id; break; }
}

var op = project.CreateOperation();
op.Start();

if (propId == Guid.Empty)
{
    propId = Guid.NewGuid();
    var propDesc = new Renga.PropertyDescription();
    propDesc.Name = propName;
    propDesc.Type = Renga.PropertyType.PropertyType_String;
    pm.RegisterProperty(propId, propDesc);
}

var allCategories = new List<Guid> {
    Guid.Parse("{67A0B42C-8C1E-47E8-B46E-78D8BB260DE0}"), Guid.Parse("{6485AC11-5B26-4D77-9788-7936AF87C85F}"),
    Guid.Parse("{47D0D93F-3C7B-4269-BF8A-DE246E1724D0}"), Guid.Parse("{41E2788A-49ED-487F-9AE1-55B6E09AE6E5}"),
    Guid.Parse("{9FABC932-590F-4068-89A8-EE6EE3D7CBBF}"), Guid.Parse("{63478188-7C88-4A6D-B891-9725F04A5BC7}"),
    Guid.Parse("{DE4420CE-02B6-4B12-9CD7-9322118BE8FE}"), Guid.Parse("{06CC88EE-9A67-4626-9C34-DDE03C331A74}"),
    Guid.Parse("{F9C7F77A-5644-4ED3-85CE-9EA21881D76A}"), Guid.Parse("{1CFBA99C-01E7-4078-AE1A-3E2FF0673599}"),
    Guid.Parse("{77FFCA60-B20E-49F0-B42F-4FDC9B1C825B}"), Guid.Parse("{D31DC2E3-808E-4987-8481-7F86665A07FC}"),
    Guid.Parse("{2AABE3A4-A29E-4534-A9F5-0F070FEE240C}"), Guid.Parse("{165D15BC-FD8D-4BBB-B73C-56956D7CEBF1}"),
    Guid.Parse("{857A042D-7D3C-4715-9EBF-95E2E9648ADF}"), Guid.Parse("{517A337A-58D5-46FF-81B8-65CF0389A191}"),
    Guid.Parse("{D9EE2442-E807-42FB-8FE5-9DCFE543035D}"), Guid.Parse("{BAC4470F-D560-4F57-A49E-FAA5F6E5A279}"),
    Guid.Parse("{D7DD0293-DD65-4229-A64C-8B528D4E226F}"), Guid.Parse("{3F522F49-AEE2-4D73-9866-9B07CF336A69}"),
    Guid.Parse("{DC82CA1A-A0C3-4A1A-AEFB-A7D720DD3A09}"), Guid.Parse("{02BBEBE8-E28B-4EE5-8916-11B514A35DCA}"),
    Guid.Parse("{0ABCB18F-0AAF-4509-BF89-5C5FAD9D5D8B}"), Guid.Parse("{0F0ADBA0-5C06-46C0-9C8A-B9D69EF1251F}"),
    Guid.Parse("{5D2F3734-5A49-4504-90B1-0676F0F25DA7}"), Guid.Parse("{A1ACA786-78A4-4015-B412-9150BAAD71A9}"),
    Guid.Parse("{2B02B353-2CA5-4566-88BB-917EA8460174}"), Guid.Parse("{F6647DC9-CFAE-4C6B-9312-CD6D8010C340}"),
    Guid.Parse("{793D3F7C-905D-4D85-A351-B152241DD2E7}"), Guid.Parse("{4B41CCF8-C969-4C55-A1F2-CCED9C164F07}"),
    Guid.Parse("{ECEF8F90-0CF9-4494-98DE-91242A2A9F5C}"), Guid.Parse("{DEBDE004-AFCC-4DA8-8DD0-4223FF836ACD}"),
    Guid.Parse("{F5BD8BD8-39C1-47F8-8499-F673C580DFBE}"), Guid.Parse("{62CF086E-5A39-4484-840C-FFA6A1C6E2B7}"),
    Guid.Parse("{F1A805FF-573D-F46B-FFBA-57F4BCCAA6ED}"), Guid.Parse("{9BD80F5A-9448-48DE-A9AB-935A946DAB65}"),
    Guid.Parse("{FC443D5A-B76C-45E5-B91C-520EF0896109}"), Guid.Parse("{377C2FDA-9411-43AC-A6C6-0E3B520BE721}"),
    Guid.Parse("{4166FD59-64C0-45EE-AE3B-49FAE1257EF1}"), Guid.Parse("{B8C7155A-B462-4FF5-BC41-C9C17A9F48FA}"),
    Guid.Parse("{CB825BF3-15AE-4190-821C-8AD314951ADA}"), Guid.Parse("{CA526024-04A1-40C7-87FD-2E95C722CC50}"),
    Guid.Parse("{5CBF0016-32BC-4630-99EA-C7CC94DDA8E3}"), Guid.Parse("{4329112A-6B65-48D9-9DA8-ABF1F8F36327}"),
    Guid.Parse("{6C671391-BFEA-4E92-9753-8855C05640A0}"), Guid.Parse("{A31CF7CA-F17B-422A-886A-7A8C362CD49A}"),
    Guid.Parse("{608EDB78-96F3-40A6-A0EC-71000105581B}"), Guid.Parse("{7EE13BD6-7C0A-47D3-ADCE-35B8E0DAE28A}"),
    Guid.Parse("{CF2B8B04-F595-4432-98F4-8234C95ADBDD}"), Guid.Parse("{D43C7509-A92C-4E32-BD2D-BA6DD8F5B7A1}"),
    Guid.Parse("{A999F05A-D730-42E7-BFC8-E4433EBACE78}"), Guid.Parse("{19D0649F-582A-488E-A52B-585C1151A5E4}"),
    Guid.Parse("{6C6821A0-EBB9-445B-84A2-ED9EB0938E4F}"), Guid.Parse("{B1359BDC-F7FF-43A4-BCA0-8D09BC974537}"),
    Guid.Parse("{BE49A354-19B7-435A-8957-9EF8782630C2}"), Guid.Parse("{A369AD70-C1FE-41DD-AF3D-BD659EA5B360}"),
    Guid.Parse("{FAC43446-031C-413E-9993-6E9CF9F2306A}"), Guid.Parse("{1F85F676-BB99-4A6F-9F72-1789F2F7B362}"),
    Guid.Parse("{83085C7B-16C4-473E-85BC-9AAFA504FF7D}"), Guid.Parse("{9B60D6AD-3468-478E-94DF-A535C5AEAA3E}"),
    Guid.Parse("{FA7F1AE9-F4F4-4F95-B108-FEEA4D7EFEB7}"), Guid.Parse("{344299F5-7D7F-43E2-B0A2-1DB8E06E8AC8}"),
    Guid.Parse("{9D6DFFB9-4828-40D8-8529-BF5CD2B58C4E}"), Guid.Parse("{861C0037-7797-43A9-96E7-833A7A2C6EA4}"),
    Guid.Parse("{33FB4B37-83F9-422A-81D4-640A152C619E}"), Guid.Parse("{A6E0BA72-ACBD-4423-9AFC-04D84A09211A}"),
    Guid.Parse("{514A3AE7-F551-4D0F-B5BA-5D4F0ECF4E7A}"), Guid.Parse("{6063816C-89FF-4C8F-A814-3BE6CB94128E}"),
    Guid.Parse("{ED1F87A1-5C9C-4994-969D-6D3854571193}"), Guid.Parse("{DA557027-F243-4331-BB5B-853ABC437CD7}"),
    Guid.Parse("{CE93E320-7167-4CD1-92A8-5E42D546066B}"), Guid.Parse("{8B323BEE-3882-4744-8838-24F45DF714A9}"),
    Guid.Parse("{838CC9F6-E3D8-4132-AF6F-C58DF0F8D037}"), Guid.Parse("{96788994-B7FC-41D7-8A99-D674543E9237}"),
    Guid.Parse("{C3CE17FF-6F28-411F-B18D-74FE957B2BA8}"), Guid.Parse("{56652D5B-536E-4EF6-A1CD-5AD69BB025AB}"),
    Guid.Parse("{8A49A9A8-A401-4AB1-8038-92093503C97A}"), Guid.Parse("{A7DFE1E1-BF2C-4C4A-BA74-3F156B1BBF8F}"),
    Guid.Parse("{84B43087-D4A4-4CCE-B34D-40E283D9E691}"), Guid.Parse("{83DE45E6-4793-49EC-8B9E-65A2438F36DE}"),
    Guid.Parse("{96DA9155-43C1-42B8-BBA2-B4F61FA43ACC}"), Guid.Parse("{B00D5C25-92A8-4409-A3B7-7C37ED792C06}"),
    Guid.Parse("{E1E3BD66-2E13-4FA4-A9EB-677E03067C2F}")
};

foreach(var t in allCategories) {
    if(!pm.IsPropertyAssignedToType(propId, t)) {
        pm.AssignPropertyToType(propId, t);
    }
}

var progressForm = new System.Windows.Forms.Form() { AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi, 
    Text = "Заполнение IfcGUID",
    Size = new System.Drawing.Size(300, 120),
    FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog,
    StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen,
    ControlBox = false,
    TopMost = true
};
var label = new System.Windows.Forms.Label() {
    Text = "Подготовка...",
    AutoSize = true,
    Location = new System.Drawing.Point(20, 20)
};
var progressBar = new System.Windows.Forms.ProgressBar() {
    Location = new System.Drawing.Point(20, 45),
    Size = new System.Drawing.Size(240, 20),
    Style = System.Windows.Forms.ProgressBarStyle.Continuous,
    Minimum = 0,
    Maximum = objects.Count,
    Value = 0
};
progressForm.Controls.Add(label);
progressForm.Controls.Add(progressBar);
progressForm.Show();

int ok = 0;
for(int i=0; i<objects.Count; i++) {
    var obj = objects.GetByIndex(i);
    var props = obj.GetProperties();
    if(props != null) {
        var p = props.Get(propId);
        if(p != null) {
            string expectedGuid = EncodeIfcGuid(obj.UniqueId);
            // Проверка для ускорения: если значение уже верно, пропускаем перезапись
            if (p.GetStringValue() != expectedGuid) {
                p.SetStringValue(expectedGuid);
                ok++;
            }
        }
    }
    
    if (i % 50 == 0) {
        progressBar.Value = i;
        label.Text = $"Обработано {i} из {objects.Count} элементов...";
        System.Windows.Forms.Application.DoEvents();
    }
}
progressForm.Close();

op.Apply();
System.Windows.Forms.MessageBox.Show(
    $"Свойство {propName} успешно присвоено/обновлено {ok} элементам проекта.", 
    "Выполнение завершено", 
    System.Windows.Forms.MessageBoxButtons.OK, 
    System.Windows.Forms.MessageBoxIcon.Information
);
