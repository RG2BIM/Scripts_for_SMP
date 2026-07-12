using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Renga;

namespace ScriptManagerPlugin.Scripts
{
    public class CopyPropertiesScript
    {
        public IApplication App { get; private set; }
        public Action<string> Print { get; private set; }
        
        private int _sourceElementId;
        private Guid _sourceObjectType;
        private IModelObject _sourceObject;

        public Guid SourceObjectType => _sourceObjectType;
        public int SourceElementId => _sourceElementId;

        public void Execute(IApplication RengaApp, Action<string> Print)
        {
            this.App = RengaApp;
            this.Print = Print;
            
            var selection = App.Selection.GetSelectedObjects();
            if (selection == null || (selection as Array).Length == 0)
            {
                MessageBox.Show("Сначала выделите один элемент-источник в модели.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            Array selArray = (Array)selection;
            _sourceElementId = (int)selArray.GetValue(0);
            
            _sourceObject = App.Project.Model.GetObjects().GetById(_sourceElementId);
            if (_sourceObject == null) return;
            
            _sourceObjectType = _sourceObject.ObjectType;
            
            var form = new CopyPropertiesForm(this);
            form.Show(); // Не блокируем Renga!
        }
        
        public string GetSourceInfo()
        {
            return $"{_sourceObject.Name} (ID: {_sourceElementId})";
        }
        
        public void PerformCopy(List<int> targetIds, int copyMode)
        {
            var pm = App.Project.PropertyManager;
            
            var srcParams = _sourceObject.GetParameters();
            var srcProps = _sourceObject.GetProperties();
            
            var srcParamIds = srcParams.GetIds();
            var srcPropIds = srcProps.GetIds();
            
            int count = 0;
            var modelObjs = App.Project.Model.GetObjects();
            
            var operation = App.Project.CreateOperationWithUndo(App.Project.Model.Id);
            operation.Start();
            
            foreach (int targetId in targetIds)
            {
                var targetObj = modelObjs.GetById(targetId);
                if (targetObj == null) continue;
                
                
                if (copyMode == 0 || copyMode == 1)
                    CopyParams(srcParams, srcParamIds, targetObj.GetParameters());
                    
                if (copyMode == 0 || copyMode == 2)
                    CopyProps(srcProps, srcPropIds, targetObj.GetProperties(), pm);
                    
                count++;
            }
            
            operation.Apply();
            
            MessageBox.Show($"Свойства успешно скопированы на {count} элемент(ов).", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        
        private void CopyParams(IParameterContainer src, IGuidCollection ids, IParameterContainer target)
        {
            for (int i = 0; i < ids.Count; i++)
            {
                Guid pId = Guid.Parse(ids.GetS(i));
                var srcP = src.Get(pId);
                var tgtP = target.Get(pId);
                
                if (tgtP == null) continue;
                if (!srcP.HasValue) continue;
                if (tgtP.IsReadOnly) continue;
                
                try 
                { 
                    if (srcP.ValueType == ParameterValueType.ParameterValueType_Double) tgtP.SetDoubleValue(srcP.GetDoubleValue());
                    else if (srcP.ValueType == ParameterValueType.ParameterValueType_Int) tgtP.SetIntValue(srcP.GetIntValue());
                    else if (srcP.ValueType == ParameterValueType.ParameterValueType_String) tgtP.SetStringValue(srcP.GetStringValue());
                    else if (srcP.ValueType == ParameterValueType.ParameterValueType_Bool) tgtP.SetBoolValue(srcP.GetBoolValue());
                } 
                catch (Exception) 
                { 
                    // Игнорируем ошибки
                }
            }
        }
        
        private void CopyProps(IPropertyContainer src, IGuidCollection ids, IPropertyContainer target, IPropertyManager pm)
        {
            for (int i = 0; i < ids.Count; i++)
            {
                Guid pId = Guid.Parse(ids.GetS(i));
                var srcP = src.Get(pId);
                var tgtP = target.Get(pId);
                
                if (tgtP == null || !srcP.HasValue() || !pm.IsPropertyRegistered(pId)) continue;
                
                string propName = pId.ToString();
                try { propName = pm.GetPropertyDescription(pId).Name; } catch {}
                
                try 
                { 
                    var pType = pm.GetPropertyType(pId);
                    switch (pType)
                    {
                        case PropertyType.PropertyType_Double: tgtP.SetDoubleValue(srcP.GetDoubleValue()); break;
                        case PropertyType.PropertyType_Integer: tgtP.SetIntegerValue(srcP.GetIntegerValue()); break;
                        case PropertyType.PropertyType_String: tgtP.SetStringValue(srcP.GetStringValue()); break;
                        case PropertyType.PropertyType_Boolean: tgtP.SetBooleanValue(srcP.GetBooleanValue()); break;
                        case PropertyType.PropertyType_Logical: tgtP.SetLogicalValue(srcP.GetLogicalValue()); break;
                        case PropertyType.PropertyType_Enumeration: tgtP.SetEnumerationValue(srcP.GetEnumerationValue()); break;
                        case PropertyType.PropertyType_Length: 
                            tgtP.SetLengthValue(srcP.GetLengthValue(LengthUnit.LengthUnit_Millimeters), LengthUnit.LengthUnit_Millimeters); 
                            break;
                        case PropertyType.PropertyType_Area: 
                            tgtP.SetAreaValue(srcP.GetAreaValue(AreaUnit.AreaUnit_Meters2), AreaUnit.AreaUnit_Meters2); 
                            break;
                        case PropertyType.PropertyType_Volume: 
                            tgtP.SetVolumeValue(srcP.GetVolumeValue(VolumeUnit.VolumeUnit_Meters3), VolumeUnit.VolumeUnit_Meters3); 
                            break;
                        case PropertyType.PropertyType_Mass: 
                            tgtP.SetMassValue(srcP.GetMassValue(MassUnit.MassUnit_Kilograms), MassUnit.MassUnit_Kilograms); 
                            break;
                        case PropertyType.PropertyType_Angle: 
                            tgtP.SetAngleValue(srcP.GetAngleValue(AngleUnit.AngleUnit_Degrees), AngleUnit.AngleUnit_Degrees); 
                            break;
                    }
                } 
                catch (Exception)
                {
                    // Игнорируем ошибки
                }
            }
        }
    }

    public class CopyPropertiesForm : Form
    {
        private CopyPropertiesScript _script;
        private Label lblSelectedCount;
        private ComboBox cbCopyMode;
        private Timer _updateTimer;
        
        public CopyPropertiesForm(CopyPropertiesScript script)
        {
            _script = script;
            InitializeComponent();
            
            _updateTimer = new Timer();
            _updateTimer.Interval = 300;
            _updateTimer.Tick += UpdateTimer_Tick;
            _updateTimer.Start();
        }
        
        private void InitializeComponent()
        {
            this.Text = "Копирование свойств";
            this.BackColor = System.Drawing.Color.White;
            this.Font = new System.Drawing.Font("Segoe UI", 9f);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.TopMost = true; // Плавает поверх Renga
            this.StartPosition = FormStartPosition.CenterScreen;
            this.AutoScaleMode = AutoScaleMode.Dpi;
            
            var lblInfo = new Label();
            lblInfo.AutoSize = true;
            lblInfo.Text = "Источник: " + _script.GetSourceInfo();
            lblInfo.Location = new System.Drawing.Point(15, 15);
            lblInfo.Font = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold);
            this.Controls.Add(lblInfo);
            
            int requiredWidth = Math.Max(380, lblInfo.PreferredWidth + 45);
            this.ClientSize = new System.Drawing.Size(requiredWidth, 155);
            
            lblSelectedCount = new Label();
            lblSelectedCount.AutoSize = true;
            lblSelectedCount.Text = "Выделено целевых элементов: 0";
            lblSelectedCount.Location = new System.Drawing.Point(15, 45);
            lblSelectedCount.ForeColor = System.Drawing.Color.FromArgb(100, 100, 100);
            this.Controls.Add(lblSelectedCount);
            
            cbCopyMode = new ComboBox();
            cbCopyMode.DropDownStyle = ComboBoxStyle.DropDownList;
            cbCopyMode.Items.AddRange(new object[] { 
                "Все данные", 
                "Только системные параметры", 
                "Только пользовательские свойства" 
            });
            cbCopyMode.SelectedIndex = 0;
            cbCopyMode.Font = new System.Drawing.Font("Segoe UI", 11f);
            cbCopyMode.SetBounds(15, 70, requiredWidth - 30, 28);
            this.Controls.Add(cbCopyMode);
            
            var btnCancel = new Button();
            btnCancel.Text = "Закрыть";
            btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(200, 208, 219);
            btnCancel.SetBounds(requiredWidth - 115, 105, 100, 32);
            btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnCancel.Click += (s, e) => this.Close();
            this.Controls.Add(btnCancel);
            
            var btnApply = new Button();
            btnApply.Text = "Скопировать";
            btnApply.FlatStyle = FlatStyle.Flat;
            btnApply.BackColor = System.Drawing.Color.FromArgb(26, 115, 232);
            btnApply.ForeColor = System.Drawing.Color.White;
            btnApply.FlatAppearance.BorderSize = 0;
            btnApply.SetBounds(requiredWidth - 225, 105, 105, 32);
            btnApply.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnApply.Click += BtnApply_Click;
            this.Controls.Add(btnApply);
        }
        
        private void BtnApply_Click(object sender, EventArgs e)
        {
            var selection = _script.App.Selection.GetSelectedObjects();
            if (selection == null || (selection as Array).Length == 0)
            {
                MessageBox.Show("Сначала выделите целевые элементы в модели Renga!", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            var targetIds = new List<int>();
            var modelObjs = _script.App.Project.Model.GetObjects();
            
            foreach (int id in (Array)selection)
            {
                if (id == _script.SourceElementId) continue;
                
                var obj = modelObjs.GetById(id);
                if (obj != null && obj.ObjectType == _script.SourceObjectType)
                {
                    targetIds.Add(id);
                }
            }
            
            if (targetIds.Count == 0)
            {
                MessageBox.Show("Среди выделенных элементов нет подходящих (той же Категории).", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            
            int mode = cbCopyMode.SelectedIndex;
            _script.PerformCopy(targetIds, mode);
            this.Close();
        }
        
        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            try 
            {
                var selection = _script.App.Selection.GetSelectedObjects();
                int count = 0;
                
                if (selection != null)
                {
                    var modelObjs = _script.App.Project.Model.GetObjects();
                    foreach (int id in (Array)selection)
                    {
                        if (id == _script.SourceElementId) continue;
                        
                        var obj = modelObjs.GetById(id);
                        if (obj != null && obj.ObjectType == _script.SourceObjectType)
                        {
                            count++;
                        }
                    }
                }
                
                lblSelectedCount.Text = $"Выделено целевых элементов: {count}";
            }
            catch {}
        }
        
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (_updateTimer != null)
            {
                _updateTimer.Stop();
                _updateTimer.Dispose();
            }
            base.OnFormClosed(e);
        }
    }
}
