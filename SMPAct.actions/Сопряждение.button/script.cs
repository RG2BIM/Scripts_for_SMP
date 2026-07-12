using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using Renga;

namespace ConnectPlugin
{
    public class ConnectCommand
    {
        public void Execute(IApplication RengaApp, Action<object> Print)
        {
            var selectionArray = (Array)RengaApp.Selection.GetSelectedObjects();
            if (selectionArray == null || selectionArray.Length != 2)
            {
                MessageBox.Show("Пожалуйста, выделите ровно два объекта (Стена, Балка, Ограждение, 3D-линия или Линия) перед запуском.", "Сопряжение", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var objectIds = new List<int>();
            for (int i = 0; i < selectionArray.Length; i++) 
                objectIds.Add(Convert.ToInt32(selectionArray.GetValue(i)));

            int objId1 = objectIds[0];
            int objId2 = objectIds[1];

            bool proceed = false;
            double chordLength = 100.0;
            bool doArc = false;

            Form form = new Form();
            form.Text = "Сопряжение";
            form.BackColor = System.Drawing.Color.White;
            form.Font = new Font("Segoe UI", 9);
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.MaximizeBox = false;
            form.MinimizeBox = false;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.AutoScaleMode = AutoScaleMode.Dpi;
            form.Width = 350;
            form.Height = 195;

            GroupBox groupBox = new GroupBox();
            groupBox.Text = "";
            groupBox.Location = new Point(15, 5);
            groupBox.Size = new Size(300, 95);
            form.Controls.Add(groupBox);

            Label lblLength = new Label();
            lblLength.Text = "Длина хорды";
            lblLength.Location = new Point(10, 20);
            lblLength.AutoSize = true;
            groupBox.Controls.Add(lblLength);

            TextBox txtLength = new TextBox();
            txtLength.Text = "1000";
            txtLength.Location = new Point(10, 45);
            txtLength.Width = 100;
            txtLength.Font = new Font("Segoe UI", 10);
            groupBox.Controls.Add(txtLength);

            RadioButton rbConnect = new RadioButton();
            rbConnect.Text = "Соединить";
            rbConnect.Location = new Point(130, 35);
            rbConnect.AutoSize = true;
            rbConnect.Checked = true;
            groupBox.Controls.Add(rbConnect);

            RadioButton rbArc = new RadioButton();
            rbArc.Text = "Скруглить";
            rbArc.Location = new Point(130, 60);
            rbArc.AutoSize = true;
            groupBox.Controls.Add(rbArc);

            Button btnOk = new Button();
            btnOk.Text = "ОК";
            btnOk.Location = new Point(155, 110);
            btnOk.Width = 75;
            btnOk.Height = 32;
            btnOk.BackColor = System.Drawing.Color.FromArgb(26, 115, 232);
            btnOk.ForeColor = System.Drawing.Color.White;
            btnOk.FlatStyle = FlatStyle.Flat;
            btnOk.FlatAppearance.BorderSize = 0;
            form.Controls.Add(btnOk);

            Button btnCancel = new Button();
            btnCancel.Text = "Отмена";
            btnCancel.Location = new Point(240, 110);
            btnCancel.Width = 75;
            btnCancel.Height = 32;
            btnCancel.BackColor = System.Drawing.Color.White;
            btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(200, 208, 219);
            form.Controls.Add(btnCancel);

            btnOk.Click += (s, e) => {
                if (!double.TryParse(txtLength.Text, out chordLength) || chordLength <= 0)
                {
                    MessageBox.Show("Введите корректное положительное число для длины хорды.", "Ошибка ввода", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                doArc = rbArc.Checked;
                proceed = true;
                form.Close();
            };

            btnCancel.Click += (s, e) => {
                form.Close();
            };

            form.ShowDialog();

            if (!proceed) return;

            var model = RengaApp.Project.Model;
            var obj1 = model.GetObjects().GetById(objId1);
            var obj2 = model.GetObjects().GetById(objId2);

            if (obj1 == null || obj2 == null) return;

            LineInfo line1 = GetLineInfo(obj1);
            LineInfo line2 = GetLineInfo(obj2);

            if (line1 == null || line2 == null)
            {
                MessageBox.Show("Один из выбранных объектов не поддерживает редактирование базовой линии или не является прямым отрезком.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Renga.Point2D p1a = new Renga.Point2D { X = line1.GlobalStart.X, Y = line1.GlobalStart.Y };
            Renga.Point2D p1b = new Renga.Point2D { X = line1.GlobalEnd.X, Y = line1.GlobalEnd.Y };
            Renga.Point2D p2a = new Renga.Point2D { X = line2.GlobalStart.X, Y = line2.GlobalStart.Y };
            Renga.Point2D p2b = new Renga.Point2D { X = line2.GlobalEnd.X, Y = line2.GlobalEnd.Y };





            Renga.Point2D intersection;
            if (!IntersectLines2D(p1a, p1b, p2a, p2b, out intersection))
            {
                MessageBox.Show("Линии параллельны или не пересекаются в плоскости XY.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }


            double d_aa = Dist2D(p1a, p2a);
            double d_ab = Dist2D(p1a, p2b);
            double d_ba = Dist2D(p1b, p2a);
            double d_bb = Dist2D(p1b, p2b);

            double minD = Math.Min(Math.Min(d_aa, d_ab), Math.Min(d_ba, d_bb));

            Renga.Point3D anchor1, anchor2;
            if (minD == d_aa) { anchor1 = line1.GlobalEnd; anchor2 = line2.GlobalEnd; }
            else if (minD == d_ab) { anchor1 = line1.GlobalEnd; anchor2 = line2.GlobalStart; }
            else if (minD == d_ba) { anchor1 = line1.GlobalStart; anchor2 = line2.GlobalEnd; }
            else { anchor1 = line1.GlobalStart; anchor2 = line2.GlobalStart; }




            Renga.Vector2D dir1 = new Renga.Vector2D { X = anchor1.X - intersection.X, Y = anchor1.Y - intersection.Y };
            Renga.Vector2D dir2 = new Renga.Vector2D { X = anchor2.X - intersection.X, Y = anchor2.Y - intersection.Y };
            
            dir1 = Normalize2D(dir1);
            dir2 = Normalize2D(dir2);

            double dot = dir1.X * dir2.X + dir1.Y * dir2.Y;
            if (dot > 1.0) dot = 1.0;
            if (dot < -1.0) dot = -1.0;
            double alpha = Math.Acos(dot);

            if (alpha < 0.01 || Math.Abs(alpha - Math.PI) < 0.01)
            {
                MessageBox.Show("Угол между объектами слишком мал или они лежат на одной прямой.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            double d = (chordLength / 2.0) / Math.Sin(alpha / 2.0);

            double zInter1 = GetZAtXY(line1.GlobalStart, line1.GlobalEnd, intersection);
            double zInter2 = GetZAtXY(line2.GlobalStart, line2.GlobalEnd, intersection);
            double zInter = (zInter1 + zInter2) / 2.0;

            Renga.Point3D i3d = new Renga.Point3D { X = intersection.X, Y = intersection.Y, Z = zInter };

            Renga.Vector3D v1 = new Renga.Vector3D { X = anchor1.X - i3d.X, Y = anchor1.Y - i3d.Y, Z = anchor1.Z - i3d.Z };
            v1 = Normalize3D(v1);
            Renga.Vector3D v2 = new Renga.Vector3D { X = anchor2.X - i3d.X, Y = anchor2.Y - i3d.Y, Z = anchor2.Z - i3d.Z };
            v2 = Normalize3D(v2);

            Renga.Point3D t1_3d = new Renga.Point3D { X = i3d.X + v1.X * d, Y = i3d.Y + v1.Y * d, Z = i3d.Z + v1.Z * d };
            Renga.Point3D t2_3d = new Renga.Point3D { X = i3d.X + v2.X * d, Y = i3d.Y + v2.Y * d, Z = i3d.Z + v2.Z * d };
            




            var op = RengaApp.Project.CreateOperationWithUndo(model.Id);
            op.Start();

            SetLine(RengaApp, obj1, line1.Placement, anchor1, t1_3d, line1.Is2D);
            SetLine(RengaApp, obj2, line2.Placement, anchor2, t2_3d, line2.Is2D);

            int sourceId = Math.Min(objId1, objId2);
            var sourceObj = sourceId == objId1 ? obj1 : obj2;
            var sourceLine = sourceId == objId1 ? line1 : line2;

            var args = model.CreateNewEntityArgs();
            args.TypeId = sourceObj.ObjectType;
            
            if (sourceObj is Renga.ILevelObject lvlObj) 
            {
                args.HostObjectId = lvlObj.LevelId;
            }
            if (sourceLine.Placement != null)
            {
                var p = sourceLine.Placement;
                args.Placement3D = new Renga.Placement3D {
                    Origin = new Renga.Point3D { X = p.Origin.X, Y = p.Origin.Y, Z = p.Origin.Z },
                    xAxis = new Renga.Vector3D { X = p.AxisX.X, Y = p.AxisX.Y, Z = p.AxisX.Z },
                    zAxis = new Renga.Vector3D { X = p.AxisZ.X, Y = p.AxisZ.Y, Z = p.AxisZ.Z }
                };
            }

            var newObj = model.CreateObject(args);

            var sParams = sourceObj.GetParameters();
            var tParams = newObj.GetParameters();
            if (sParams != null && tParams != null)
            {
                var ids = sParams.GetIds();
                for (int i = 0; i < ids.Count; i++)
                {
                    var id = ids.Get(i);
                    var sVal = sParams.Get(id);
                    if (sVal != null && sVal.HasValue && tParams.Contains(id))
                    {
                        var tVal = tParams.Get(id);
                        try 
                        {
                            if (sVal.ValueType == Renga.ParameterValueType.ParameterValueType_Double) tVal.SetDoubleValue(sVal.GetDoubleValue());
                            else if (sVal.ValueType == Renga.ParameterValueType.ParameterValueType_String) tVal.SetStringValue(sVal.GetStringValue());
                            else if (sVal.ValueType == Renga.ParameterValueType.ParameterValueType_Int) tVal.SetIntValue(sVal.GetIntValue());
                            else if (sVal.ValueType == Renga.ParameterValueType.ParameterValueType_Bool) tVal.SetBoolValue(sVal.GetBoolValue());
                        }
                        catch { } // Игнорируем параметры только для чтения (Объем, Площадь и т.д.)
                    }
                }
            }

            if (doArc)
            {
                Renga.Vector3D bisector = new Renga.Vector3D { X = v1.X + v2.X, Y = v1.Y + v2.Y, Z = v1.Z + v2.Z };
                bisector = Normalize3D(bisector);
                double distToCenter = d / Math.Cos(alpha / 2.0);
                Renga.Point3D c3d = new Renga.Point3D { X = i3d.X + bisector.X * distToCenter, Y = i3d.Y + bisector.Y * distToCenter, Z = i3d.Z + bisector.Z * distToCenter };
                
                double radius = d * Math.Tan(alpha / 2.0);
                Renga.Point3D m3d = new Renga.Point3D { X = c3d.X - bisector.X * radius, Y = c3d.Y - bisector.Y * radius, Z = c3d.Z - bisector.Z * radius };

                Renga.Point3D localT1 = GlobalToLocal(sourceLine.Placement, t1_3d);
                Renga.Point3D localM = GlobalToLocal(sourceLine.Placement, m3d);
                Renga.Point3D localT2 = GlobalToLocal(sourceLine.Placement, t2_3d);

                if (line1.Is2D)
                {
                    var arc2d = RengaApp.Math.CreateArc2DByThreePoints(
                        new Renga.Point2D { X = localT1.X, Y = localT1.Y },
                        new Renga.Point2D { X = localM.X, Y = localM.Y },
                        new Renga.Point2D { X = localT2.X, Y = localT2.Y }
                    );
                    (newObj as Renga.IBaseline2DObject)?.SetBaseline(arc2d);
                }
                else
                {
                    var arc3d = RengaApp.Math.CreateArc3DByThreePoints(localT1, localM, localT2);
                    (newObj as Renga.IBaseline3DObject)?.SetBaseline(arc3d);
                }
            }
            else
            {
                SetLine(RengaApp, newObj, sourceLine.Placement, t1_3d, t2_3d, line1.Is2D);
            }

            op.Apply();




            MessageBox.Show("Готово", "Сопряжение", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // --- HELPER CLASSES & METHODS ---

        Renga.Point3D LocalToGlobal(Renga.IPlacement3D placement, Renga.Point3D pt)
        {
            if (placement != null)
                return placement.GetTransformFrom().TransformPoint(pt);
            return pt;
        }

        Renga.Point3D GlobalToLocal(Renga.IPlacement3D placement, Renga.Point3D pt)
        {
            if (placement != null)
                return placement.GetTransformInto().TransformPoint(pt);
            return pt;
        }

        class LineInfo 
        {
            public Renga.Point3D LocalStart;
            public Renga.Point3D LocalEnd;
            public Renga.Point3D GlobalStart;
            public Renga.Point3D GlobalEnd;
            public bool Is2D;
            public Renga.IPlacement3D Placement;
        }

        LineInfo GetLineInfo(Renga.IModelObject obj)
        {
            Renga.Point3D p1 = new Renga.Point3D();
            Renga.Point3D p2 = new Renga.Point3D();
            bool is2D = false;
            
            if (obj is Renga.IBaseline2DObject b2d)
            {
                var curve = b2d.GetBaseline();
                if (curve != null && curve.Curve2DType == Renga.Curve2DType.Curve2DType_LineSegment)
                {
                    var cp1 = curve.GetBeginPoint();
                    var cp2 = curve.GetEndPoint();
                    p1 = new Renga.Point3D { X=cp1.X, Y=cp1.Y, Z=0 };
                    p2 = new Renga.Point3D { X=cp2.X, Y=cp2.Y, Z=0 };
                    is2D = true;
                }
            }
            else if (obj is Renga.IBaseline3DObject b3d)
            {
                var curve = b3d.GetBaseline();
                if (curve != null && curve.Curve3DType == Renga.Curve3DType.Curve3DType_LineSegment)
                {
                    p1 = curve.GetBeginPoint();
                    p2 = curve.GetEndPoint();
                    is2D = false;
                }
            }
            else return null;

            Renga.IPlacement3D placement = null;
            if (obj is Renga.ILevelObject lo) placement = lo.GetPlacement();

            return new LineInfo 
            { 
                LocalStart = p1, LocalEnd = p2, Is2D = is2D,
                GlobalStart = LocalToGlobal(placement, p1),
                GlobalEnd = LocalToGlobal(placement, p2),
                Placement = placement
            };
        }

        void SetLine(Renga.IApplication RengaApp, Renga.IModelObject obj, Renga.IPlacement3D placement, Renga.Point3D globalStart, Renga.Point3D globalEnd, bool is2D)
        {
            Renga.Point3D localStart = GlobalToLocal(placement, globalStart);
            Renga.Point3D localEnd = GlobalToLocal(placement, globalEnd);
            
            if (is2D && obj is Renga.IBaseline2DObject b2d)
            {
                var curve = RengaApp.Math.CreateLineSegment2D(
                    new Renga.Point2D { X = localStart.X, Y = localStart.Y },
                    new Renga.Point2D { X = localEnd.X, Y = localEnd.Y }
                );
                b2d.SetBaseline(curve);
            }
            else if (!is2D && obj is Renga.IBaseline3DObject b3d)
            {
                var curve = RengaApp.Math.CreateLineSegment3D(localStart, localEnd);
                b3d.SetBaseline(curve);
            }
        }

        bool IntersectLines2D(Renga.Point2D p1, Renga.Point2D p2, Renga.Point2D p3, Renga.Point2D p4, out Renga.Point2D intersection)
        {
            intersection = new Renga.Point2D();
            double denom = (p1.X - p2.X) * (p3.Y - p4.Y) - (p1.Y - p2.Y) * (p3.X - p4.X);
            if (Math.Abs(denom) < 1e-6) return false; 
            double t = ((p1.X - p3.X) * (p3.Y - p4.Y) - (p1.Y - p3.Y) * (p3.X - p4.X)) / denom;
            intersection.X = p1.X + t * (p2.X - p1.X);
            intersection.Y = p1.Y + t * (p2.Y - p1.Y);
            return true;
        }

        double Dist2D(Renga.Point2D p1, Renga.Point2D p2)
        {
            return Math.Sqrt((p1.X - p2.X)*(p1.X - p2.X) + (p1.Y - p2.Y)*(p1.Y - p2.Y));
        }

        double GetZAtXY(Renga.Point3D start, Renga.Point3D end, Renga.Point2D xy)
        {
            double dTotal = Math.Sqrt((start.X - end.X)*(start.X - end.X) + (start.Y - end.Y)*(start.Y - end.Y));
            if (dTotal < 1e-6) return start.Z;
            double dXY = Math.Sqrt((xy.X - start.X)*(xy.X - start.X) + (xy.Y - start.Y)*(xy.Y - start.Y));
            double dot = (xy.X - start.X)*(end.X - start.X) + (xy.Y - start.Y)*(end.Y - start.Y);
            if (dot < 0) dXY = -dXY;
            return start.Z + (end.Z - start.Z) * (dXY / dTotal);
        }

        Renga.Vector2D Normalize2D(Renga.Vector2D v)
        {
            double len = Math.Sqrt(v.X*v.X + v.Y*v.Y);
            if (len < 1e-6) return v;
            return new Renga.Vector2D { X = v.X/len, Y = v.Y/len };
        }

        Renga.Vector3D Normalize3D(Renga.Vector3D v)
        {
            double len = Math.Sqrt(v.X*v.X + v.Y*v.Y + v.Z*v.Z);
            if (len < 1e-6) return v;
            return new Renga.Vector3D { X = v.X/len, Y = v.Y/len, Z = v.Z/len };
        }
    }
}
