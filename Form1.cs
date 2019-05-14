using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SharpGL;
using BSpine;
using DevComponents.DotNetBar.Charts.Style;
using DevComponents.DotNetBar.Charts;


namespace BspineDrawing
{
    public partial class Form1 : Form
    {
        #region 成员变量
        private int mouseX = 0, mouseY = 0, delta = 0;
        //视图控制flag
        private double rotation = 0.0f;
        private double temprotation = 0.0f;
        private double scale = 1.0f;
        private bool recoverCenter = false;
        private bool viewChanged = false;
        private bool isScale = false;
        private bool isRotate = false;
        //绘图与修改
        private bool isModify = false;
        private bool isDraw = true;
        private bool isStrech = false;
        private bool pointModify = false;
        private bool isAddpoint = false;
        private bool isGenctrlGrid = false;
        private Point selectedPt;
        private int[] selectindex = new int[3];
        private int axisindex = 0;
        private int i_highlight = 0;
        private int i_u_surfacehight = 0;
        private int i_v_surfacehight = 0;
        //切换四个视图
        private bool isFrontView = false;
        private bool isLeftView = false;
        private bool isTopView = false;
        private bool isPerspective = true;
        //光源
        private float[] lightPos = new float[] { -1, 3, 1, 1 };
        private float[] lightSphereColor = new float[] { 1f, 1f, 1f };
        private IList<float[]> lightColor = new List<float[]>();
        private double[] lookatValue = { 2, 2, 2, 0, 0, 0, 0, 1, 0 };
        private IList<double[]> viewDefaultPos = new List<double[]>();
        //控制点与曲面曲线存储区
        private List<Point> ctrlPoints = new List<Point>();
        private Point[] winPoints;
        private Point[] winCoodPos = new Point[3];
        private Point[,] winSurfacePts;
        private Point[] BspineCurve;
        private List<List<Point>> surfaceCtrlPt = new List<List<Point>>();
        private List<Point[]> BspineSurface = new List<Point[]>();
        //参数化与曲线次数
        private int v_paraStyle = 2;
        private int u_paraStyle = 2;
        private int v_k = 2;
        private int u_k = 2;
        #endregion

        #region 初始化

        
        public Form1()
        {
            InitializeComponent();

        }

        private void Form1_Load(object sender, EventArgs e) //窗体加载的初始化设置
        {
            ribbonControl1.SelectedRibbonTabItem = ribbonTabItem1;
            qianshiview.Click += RadialMenu1qianshiview_Click;
            fushiview.Click += RadialMenu1fushishiview_Click;
            toushiview.Click += RadialMenu1toushiview_Click;
            zuoshiview.Click += RadialMenu1zuoshiview_Click;
            switchToLineSelectBtn.Enabled = false;
            switchToPointSelectBtn.Enabled = false;
            RadialMenu1fushishiview_Click(null, null);
            checkBoxGridShow.Checked = true;
        }
        
        private void openGLControl1_OpenGLInitialized(object sender, EventArgs e)//OpenGL界面初始化
        {
            OpenGL gl = openGLControl.OpenGL;
            //四个视图的缺省位置
            viewDefaultPos.Add(new double[] { 4, 4, 4, 0, 0, 0, 0, 1, 0 });     //透视
            viewDefaultPos.Add(new double[] { 0, 0, 2, 0, 0, 0, 0, 1, 0 });     //前视 
            viewDefaultPos.Add(new double[] { 1, 0, 0, 0, 0, 0, 0, 1, 0 });     //左视
            viewDefaultPos.Add(new double[] { 0, 15, 0, 0.1, 0, 0, 0, 1, 0 });   //顶视
            lookatValue = (double[])viewDefaultPos[0].Clone();

            lightColor.Add(new float[] { 1f, 1f, 1f, 1f });  //环境光(ambient light)
            lightColor.Add(new float[] { 1f, 1f, 1f, 1f });  //漫射光(diffuse light)
            lightColor.Add(new float[] { 0.8f, 0.8f, 0.8f, 1f });  //镜面反射光(specular light)

            SetLightColor(gl);
            gl.Light(OpenGL.GL_LIGHT0, OpenGL.GL_POSITION, lightPos);

            gl.Enable(OpenGL.GL_LIGHTING);
            gl.Enable(OpenGL.GL_LIGHT0);
            gl.Enable(OpenGL.GL_MULTISAMPLE);
            //gl.BlendFunc(OpenGL.GL_SRC_ALPHA, OpenGL.GL_ONE_MINUS_CONSTANT_ALPHA_EXT);
            //gl.Enable(OpenGL.GL_BLEND);
            gl.Enable(OpenGL.GL_DEPTH_TEST);
            gl.Enable(OpenGL.GL_NORMALIZE);
            gl.ClearColor(1f, 1f, 1f, 0f);
        }

        private void SetLightColor(OpenGL gl) //设置光照的三种反射
        {
            gl.Light(OpenGL.GL_LIGHT0, OpenGL.GL_AMBIENT, lightColor[0]);
            gl.Light(OpenGL.GL_LIGHT0, OpenGL.GL_DIFFUSE, lightColor[1]);
            gl.Light(OpenGL.GL_LIGHT0, OpenGL.GL_SPECULAR, lightColor[2]);
        }

        private void openGLControl1_Resize(object sender, EventArgs e)//窗口调整事件发生后图形显示变化
        {
            OpenGL gl = openGLControl.OpenGL;
            gl.MatrixMode(OpenGL.GL_PROJECTION);
            gl.LoadIdentity();
            if (isPerspective == true)
            {
                gl.Perspective(40.0f, (double)Width / (double)Height, 0.01, 100.0);
            }
            else
            {
                if (Width <= Height)
                    gl.Ortho(-5.0, 5.0, -5.0 * (float)Height / (float)Width,
                             5.0 * (float)Height / (float)Width, 0.1, 50.0);
                else
                    gl.Ortho(-5.0 * (float)Width / (float)Height,
                             5.0 * (float)Width / (float)Height, -5.0, 5.0, 0.1, 50.0);
            }
            gl.LookAt(lookatValue[0], lookatValue[1], lookatValue[2],
                lookatValue[3], lookatValue[4], lookatValue[5],
                lookatValue[6], lookatValue[7], lookatValue[8]);
            gl.MatrixMode(OpenGL.GL_MODELVIEW);
        }

        private void SetViewDefaultValue()//设置不同视图的摄像机位置变化
        {
            if (isPerspective)
            {
                lookatValue = (double[])viewDefaultPos[0].Clone();
            }
            else if (isFrontView)
            {
                lookatValue = (double[])viewDefaultPos[1].Clone();
            }
            else if (isLeftView)
            {
                lookatValue = (double[])viewDefaultPos[2].Clone();
            }
            else if (isTopView)
            {
                lookatValue = (double[])viewDefaultPos[3].Clone();
            }
        }

        private void UpdateViewforSelct(OpenGL gl)//当视图变化时更新窗口上点的坐标
        {
            double[] mvmatrix = new double[16];
            double[] projmatrix = new double[16];
            int[] viewport = new int[4];
            double[] winx = new double[1];
            double[] winy = new double[1];
            double[] winz = new double[1];
            double[] winForCoodiX = new double[1];
            double[] winForCoodiY = new double[1];
            double[] winForCoodiZ = new double[1];

            gl.GetInteger(OpenGL.GL_VIEWPORT, viewport);
            gl.GetDouble(OpenGL.GL_MODELVIEW_MATRIX, mvmatrix);
            gl.GetDouble(OpenGL.GL_PROJECTION_MATRIX, projmatrix);
            //曲线投影
            //if (switchToLineSelectBtn.Value) //开关打开后执行
            //{
            //    Point[] winCurvePoint = new Point[(int)BspineCurve.Length / 5];
            //    //将所有曲线投影到屏幕矩阵上
            //    for (int i = 0; i < (int)BspineCurve.Length/5; i++)
            //    {
            //        gl.Project(BspineCurve[i*5].x, BspineCurve[i * 5].y, BspineCurve[i * 5].z,
            //            mvmatrix, projmatrix, viewport, winx, winy, winz);
            //        winCurvePoint[i].x = winx[0];
            //        winCurvePoint[i].y = viewport[3] - winy[0] -1;
            //        winCurvePoint[i].z = 0;
            //    }
            //    winCurve = winCurvePoint;
            //}
            //控制点投影
            if (switchToPointSelectBtn.Value)
            {
                if (ctrlPoints != null && ctrlPoints.Count > 0)
                {
                    Point[] WindowPoint = new Point[ctrlPoints.Count];
                    for (int i = 0; i < ctrlPoints.Count; i++)
                    {
                        gl.Project(ctrlPoints[i].x, ctrlPoints[i].y, ctrlPoints[i].z, mvmatrix, projmatrix, viewport, winx, winy, winz);
                        WindowPoint[i].x = winx[0];
                        WindowPoint[i].y = viewport[3] - winy[0] - 1;
                        WindowPoint[i].z = winz[0];
                    }
                    if (winPoints != null)
                        Array.Clear(winPoints, 0, winPoints.Length);
                    winPoints = WindowPoint;
                }

                if (surfaceCtrlPt != null && surfaceCtrlPt.Count > 0)
                {
                    Point[,] WindowSurfacePoint = new Point[surfaceCtrlPt.Count, ctrlPoints.Count];
                    for (int i = 0; i < surfaceCtrlPt.Count; i++)
                    {
                        for (int j = 0; j < ctrlPoints.Count; j++)
                        {
                            gl.Project(surfaceCtrlPt[i][j].x, surfaceCtrlPt[i][j].y, surfaceCtrlPt[i][j].z, mvmatrix, projmatrix, viewport, winx, winy, winz);
                            WindowSurfacePoint[i, j].x = winx[0];
                            WindowSurfacePoint[i, j].y = viewport[3] - winy[0] - 1;
                            WindowSurfacePoint[i, j].z = winz[0];
                        }
                    }
                    winSurfacePts = WindowSurfacePoint;
                }
            }
            //座标架投影
            if (pointModify == true)
            {
                double[,] offset = new double[3, 3] { { 1, 0, 0 }, { 0, 1, 0 }, { 0, 0, 1 } };
                for (int i = 0; i < 3; i++)
                {
                    gl.Project(selectedPt.x + offset[i, 0], selectedPt.y + offset[i, 1],
                        selectedPt.z + offset[i, 2], mvmatrix, projmatrix, viewport, winForCoodiX, winForCoodiY, winForCoodiZ);
                    winCoodPos[i].x = winForCoodiX[0];
                    winCoodPos[i].y = viewport[3] - winForCoodiY[0] - 1;
                    winCoodPos[i].z = winForCoodiZ[0];
                }
            }
        }
        #endregion

        #region 绘图循环函数
        private void openGLControl1_OpenGLDraw(object sender, PaintEventArgs e)//绘图循环
        {
            SharpGL.OpenGL gl = this.openGLControl.OpenGL;
            //清除深度缓存 
            gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);
            //坐标变换
            gl.LoadIdentity();
            gl.Rotate(rotation, 0.0f, 1.0f, 0.0f);
            if (recoverCenter)
                gl.LoadIdentity();
            gl.Scale(scale, scale, scale);
            //绘图
            DrawPoint(gl);
            if (isDraw)
            {
                DrawCurve(gl);
            }
            if (checkBoxGridShow.Checked == true)
            {
                DrawCtrlGrid(gl);
            }
            DrawXoZGrids(gl);
            DrawOneCoodinate(gl, 0, 0, 0, false);
            if (pointModify == true && switchToPointSelectBtn.Value == true)
            {
                DrawCoodinate(gl, selectedPt);
            }
            DrawSurface(gl);
            //更新显示的控制点与座标架
            if (viewChanged)
            {
                UpdateViewforSelct(gl);
                viewChanged = false;
            }    
            gl.Flush();   //强制刷新
        }
        //
        #region 绘图区域函数，负责绘出特定的图形
        //
        private void DrawXoZGrids(OpenGL gl)//画xoz平面上的网格
        {
            //绘制过程
            gl.Disable(OpenGL.GL_LIGHTING);
            gl.PushAttrib(OpenGL.GL_CURRENT_BIT);  //保存当前属性
            gl.PushMatrix();                        //压入堆栈  
            gl.Color(0f, 0f, 1f);

            //绘制网格:在X,Z平面
            for (float i = -20; i <= 20; i += 1)
            {
                //绘制线            
                gl.LineWidth(1);
                gl.Begin(OpenGL.GL_LINES);
                {
                    if (i == 0)
                    {
                        gl.Color(0.9f, 0f, 0f);
                        //X轴方向
                        gl.Vertex(-20f, 0f, i);
                        gl.Vertex(20f, 0f, i);
                        // gl.Color(0f, 0f, 0.9f);
                        //Z轴方向 
                        gl.Vertex(i, 0f, -20f);
                        gl.Vertex(i, 0f, 20f);
                    }
                    else
                        gl.Color(0.7f, 0.7f, 0.7f);
                    //X轴方向
                    gl.Vertex(-20f, 0f, i);
                    gl.Vertex(20f, 0f, i);
                    //Z轴方向 
                    gl.Vertex(i, 0f, -20f);
                    gl.Vertex(i, 0f, 20f);
                }
                gl.End();
            }

            gl.PopMatrix();
            gl.PopAttrib();
            gl.Enable(OpenGL.GL_LIGHTING);
        }
        private void DrawCoodinate(OpenGL gl, Point ctrlpt)//画中心坐标架
        {
            //gl.Scale(0.66,0.66,0.66);
            if (ctrlPoints.Count != 0)
            {
                DrawOneCoodinate(gl, (float)ctrlpt.x, (float)ctrlpt.y, (float)ctrlpt.z, false);
            }

        }
        private void DrawOneCoodinate(OpenGL gl, float xPos, float yPos, float zPos, bool isLine)//画出坐标架
        {
            gl.PushMatrix();
            {
                gl.Translate(xPos, yPos, zPos);
                gl.Color(lightSphereColor);
                DrawSphere(gl, 0.2, 20, 10, isLine);
                DrawCylinder(gl, 1.0, 0.04, "z");
                gl.Rotate(90, 0, 1, 0);
                DrawCylinder(gl, 1.0, 0.02, "x");
                gl.Rotate(-90, 1, 0, 0);
                DrawCylinder(gl, 1.0, 0.04, "y");
            }
            gl.PopMatrix();
        }

        void DrawSphere(OpenGL gl, double radius, int segx, int segy, bool isLines)//画出球体
        {
            gl.PushMatrix();
            var sphere = gl.NewQuadric();
            if (isLines)
                gl.QuadricDrawStyle(sphere, OpenGL.GL_LINES);
            else
                gl.QuadricDrawStyle(sphere, OpenGL.GL_QUADS);
            gl.QuadricNormals(sphere, OpenGL.GLU_SMOOTH);
            gl.QuadricOrientation(sphere, (int)OpenGL.GLU_OUTSIDE);
            gl.QuadricTexture(sphere, (int)OpenGL.GLU_FALSE);
            gl.Sphere(sphere, radius, segx, segy);
            gl.DeleteQuadric(sphere);
            gl.PopMatrix();
        }
        void DrawCylinder(OpenGL gl, double height, double radius, string x)//画圆柱体默认
        {
            gl.PushMatrix();
            var Cylinder = gl.NewQuadric();

            gl.QuadricDrawStyle(Cylinder, OpenGL.GL_QUADS);
            gl.QuadricNormals(Cylinder, OpenGL.GLU_SMOOTH);
            gl.QuadricOrientation(Cylinder, (int)OpenGL.GLU_OUTSIDE);
            gl.QuadricTexture(Cylinder, (int)OpenGL.GLU_FALSE);
            //gl.ColorMaterial(OpenGL.GL_FRONT, OpenGL.GL_DIFFUSE);
            //gl.Enable(OpenGL.GL_COLOR_MATERIAL);
            gl.Color(1, 1, 1);
            gl.Cylinder(Cylinder, radius, radius, height, 10, 5);
            gl.Translate(0, 0, height);
            gl.Cylinder(Cylinder, 0.08, 0.0, 0.2, 10, 5);
            //gl.Disable(OpenGL.GL_COLOR_MATERIAL);
            gl.DeleteQuadric(Cylinder);
            gl.Scale(0.5, 0.5, 0.5);
            gl.Rotate(90, 0, 1, 0);
            if (x == "y")
            {
                gl.Rotate(90, 0, 0, 1);
                gl.Rotate(180, 0, 1, 0);
            }
            gl.Translate(0, 0.1, 0);
            gl.DrawText3D("Arial", 2, 1, 0.1f, x);
            gl.PopMatrix();
        }
        private void CatchPos(OpenGL gl)//获得当前鼠标的对应世界坐标系的位置
        {
            double[] mvmatrix = new double[16];
            double[] projmatrix = new double[16];
            int[] viewport = new int[4];
            int Opengl_Y = 0, Opengl_X = 0;
            double[] Pos = new double[3];
            double[] winx = new double[1];
            double[] winy = new double[1];
            double[] winz = new double[1]; ;

            gl.GetInteger(OpenGL.GL_VIEWPORT, viewport);
            gl.GetDouble(OpenGL.GL_MODELVIEW_MATRIX, mvmatrix);
            gl.GetDouble(OpenGL.GL_PROJECTION_MATRIX, projmatrix);

            Opengl_Y = viewport[3] - mouseY - 1;
            Opengl_X = mouseX;
            gl.Project(0, 0, 0, mvmatrix, projmatrix, viewport, winx, winy, winz);
            gl.UnProject(Opengl_X, Opengl_Y, winz[0],
                mvmatrix, projmatrix, viewport,
                ref Pos[0], ref Pos[1], ref Pos[2]);
            Point cPt = new Point
            {
                x = Pos[0],
                y = Pos[1],
                z = Pos[2]

            };
            if (isPerspective)
            {
                gl.UnProject(Opengl_X, Opengl_Y, 0,
                mvmatrix, projmatrix, viewport,
                ref Pos[0], ref Pos[1], ref Pos[2]);
                Pointtrans(ref cPt, Pos);
            }
            ctrlPoints.Add(cPt);
        }
        private void Pointtrans(ref Point pos, double[] wc_in_Z0)//点的转换
        {
            Point p0 = new Point
            {
                x = wc_in_Z0[0],
                y = wc_in_Z0[1],
                z = wc_in_Z0[2]
            };
            Point vdir = new Point
            {
                x = pos.x - p0.x,
                y = pos.y - p0.y,
                z = pos.z - p0.z,
            };
            pos = LinePlanelInsection(p0, vdir);
        }
        private Point LinePlanelInsection(Point p0, Point vdir, double[] planel = null)//计算直线和平面的交点，po为某点的位置，vdir是直线方向，planel表示要相交平面double[4]
        {
            //double[] highLight = { 1, 1, 1 };
            Point Insec = new Point();
            //由于工程中直线只和xoz面相交
            double rate = -p0.y / vdir.y;
            Insec.x = rate * vdir.x + p0.x;
            Insec.y = 0;
            Insec.z = rate * vdir.z + p0.z;
            return Insec;
        }
        private void DrawPoint(OpenGL gl) //画控制点
        {
            if (isAddpoint)
            {
                CatchPos(gl);
                isAddpoint = false;
            }
            if (ctrlPoints.Count == 0 || ctrlPoints == null)
            {
                return;
            }
            gl.Disable(OpenGL.GL_LIGHTING);
            gl.PushAttrib(OpenGL.GL_CURRENT_BIT);  //保存当前属性
            gl.PushMatrix();                        //压入堆栈
            gl.PointSize(5);
            gl.Enable(OpenGL.GL_POINT_SMOOTH);
            gl.Enable(OpenGL.GL_LINE_SMOOTH);
            gl.Begin(OpenGL.GL_POINTS);
            {

                for (int i = 0; i < ctrlPoints.Count; i++)
                {
                    if (i == i_highlight && isModify)
                    {
                        //gl.PushAttrib(OpenGL.GL_CURRENT_BIT);
                        //gl.PushMatrix();
                        gl.Color(0.5, 0.5, 0.5);//高亮的颜色
                        gl.Vertex(ctrlPoints[i].x, ctrlPoints[i].y, ctrlPoints[i].z);
                        //gl.PopAttrib();
                        //gl.PopMatrix();
                        continue;
                    }
                    //gaoliang
                    gl.Color(1, 0.5, 1);
                    gl.Vertex(ctrlPoints[i].x, ctrlPoints[i].y, ctrlPoints[i].z);
                }
            }
            gl.End();
            gl.Begin(OpenGL.GL_LINE_STRIP);
            {
                gl.Color(0.9f, 0.5f, 0f);
                foreach (var item in ctrlPoints)
                {
                    gl.Vertex(item.x, item.y, item.z);
                }
            }
            gl.End();
            gl.Disable(OpenGL.GL_POINT_SMOOTH);
            gl.Disable(OpenGL.GL_LINE_SMOOTH);
            gl.PopMatrix();
            gl.PopAttrib();
            gl.Enable(OpenGL.GL_LIGHTING);
        }
        private void DrawCurve(OpenGL gl)//画出样条曲线
        {
            if (BspineCurve == null)
            {
                return;
            }
            float[] color = new float[3] { 0.6f, 0f, 0.6f };
            DrawLine(gl, color, BspineCurve);

        }
        private void DrawLine(OpenGL gl, float[] pointcolor, Point[] line)//画基本线元的函数
        {
            gl.Disable(OpenGL.GL_LIGHTING);
            gl.PushAttrib(OpenGL.GL_CURRENT_BIT);  //保存当前属性
            gl.PushMatrix();                        //压入堆栈  
            gl.Enable(OpenGL.GL_LINE_SMOOTH);
            gl.LineWidth(1);
            gl.Begin(OpenGL.GL_LINE_STRIP);
            {
                gl.Color(pointcolor);
                foreach (var item in line)
                {
                    gl.Vertex(item.x, item.y, item.z);
                }
            }
            gl.End();
            gl.Disable(OpenGL.GL_LINE_SMOOTH);
            gl.PopMatrix();
            gl.PopAttrib();
            gl.Enable(OpenGL.GL_LIGHTING);

        }
        private void DrawCtrlGrid(OpenGL gl)//画出控制网格
        {
            if (surfaceCtrlPt.Count > 1 && surfaceCtrlPt != null)
            {
                Point[] tempU_CtrlPts = new Point[surfaceCtrlPt.Count];
                float[] color = new float[3] { 0.6f, 0f, 0.6f };
                for (int i = 0; i < surfaceCtrlPt.Count; i++)
                {
                    DrawLine(gl, color, surfaceCtrlPt[i].ToArray()); //画出v向
                }
                for (int i = 0; i < ctrlPoints.Count; i++)
                {
                    for (int j = 0; j < surfaceCtrlPt.Count; j++)
                    {
                        tempU_CtrlPts[j] = surfaceCtrlPt[j][i];
                    }
                    DrawLine(gl, color, tempU_CtrlPts);
                }
            }
        }
        private void DrawSurface(OpenGL gl)//画出曲面
        {
            if (BspineSurface.Count > 1)
            {
                DrawSmoothPatch(gl);
            }

        }
        private void DrawSmoothPatch(OpenGL gl)//画出光滑的小面片
        {
            gl.PushAttrib(OpenGL.GL_CURRENT_BIT);  //保存当前属性
            gl.PushMatrix();                        //压入堆栈  
            gl.ShadeModel(OpenGL.GL_SAMPLE_SHADING);
            //gl.Enable(OpenGL.GL_COLOR_MATERIAL);
            //gl.ColorMaterial(OpenGL.GL_FRONT_FACE,OpenGL.GL_AMBIENT_AND_DIFFUSE);
            gl.PolygonMode(OpenGL.GL_COLOR_MATERIAL_FACE, OpenGL.GL_POLYGON_OFFSET_FILL);
            gl.Enable(OpenGL.GL_NORMALIZE);
            for (int i = 0; i < BspineSurface.Count - 1; i++)
            {
                gl.Begin(OpenGL.GL_TRIANGLE_STRIP);
                {
                    for (int j = 0; j < BspineSurface[0].Length; j++)
                    {
                        gl.Vertex(BspineSurface[i][j].x, BspineSurface[i][j].y, BspineSurface[i][j].z);
                        gl.Vertex(BspineSurface[i + 1][j].x, BspineSurface[i + 1][j].y, BspineSurface[i + 1][j].z);
                    }
                }
                gl.End();
            }
            //gl.Disable(OpenGL.GL_COLOR_MATERIAL);
            gl.PopMatrix();
            gl.PopAttrib();
        }
        private void DrawHighLight(OpenGL gl)//高亮显示鼠标附近的控制点
        {
            if (surfaceCtrlPt == null)
            {
                MessageBox.Show("没有控制点可以修改");
                return;
            }
            if (i_u_surfacehight != -1)
            {
                return;
            }
            gl.Disable(OpenGL.GL_LIGHTING);
            gl.PushAttrib(OpenGL.GL_CURRENT_BIT);  //保存当前属性
            gl.PushMatrix();                        //压入堆栈            
            gl.PointSize(5);
            gl.Enable(OpenGL.GL_POINT_SMOOTH);
            gl.Enable(OpenGL.GL_LINE_SMOOTH);
            gl.Begin(OpenGL.GL_POINTS);
            {
                gl.Color(1, 0, 0);
                gl.Vertex(winSurfacePts[i_u_surfacehight, i_v_surfacehight].x,
                    winSurfacePts[i_u_surfacehight, i_v_surfacehight].y,
                    winSurfacePts[i_u_surfacehight, i_v_surfacehight].z);
            }
            gl.End();
            gl.Disable(OpenGL.GL_POINT_SMOOTH);
            gl.Disable(OpenGL.GL_LINE_SMOOTH);
            gl.PopMatrix();
            gl.PopAttrib();
            gl.Enable(OpenGL.GL_LIGHTING);
        }
        #endregion
        #endregion

        #region 事件触发函数

        # region 视图切换
        private void RadialMenu1qianshiview_Click(object sender, EventArgs e)//前视图
        {
            temprotation = rotation;
            isFrontView = true;
            isTopView = false;
            isPerspective = false;
            isLeftView = false;
            recoverCenter = true;
            rotation = 0;
            SetViewDefaultValue();
            openGLControl1_Resize(null, null);
        }
        private void RadialMenu1fushishiview_Click(object sender, EventArgs e)//俯视图
        {
            temprotation = rotation;
            isTopView = true;
            isPerspective = false;
            isLeftView = false;
            isFrontView = false;
            recoverCenter = true;
            rotation = 0;
            SetViewDefaultValue();
            openGLControl1_Resize(null, null);

        }
        private void RadialMenu1toushiview_Click(object sender, EventArgs e)//透视图
        {
            temprotation = rotation;
            isPerspective = true;
            isFrontView = false;
            isTopView = false;
            isLeftView = false;
            rotation = temprotation;
            SetViewDefaultValue();
            openGLControl1_Resize(null, null);

        }
        private void RadialMenu1zuoshiview_Click(object sender, EventArgs e)//左视图
        {
            temprotation = rotation;
            isLeftView = true;
            isFrontView = false;
            isPerspective = false;
            isTopView = false;
            recoverCenter = true;
            rotation = 0;
            SetViewDefaultValue();
            openGLControl1_Resize(null, null);
        }
        #endregion

        #region 键盘控制摄像机漫游
        private void openGLControl_KeyDown(object sender, KeyEventArgs e)
        {
            string name = string.Empty;
            switch (e.KeyCode)
            {
                case Keys.W:                ////移动摄像机的z坐标
                    lookatValue[2] -= 0.2;
                    break;
                case Keys.S:
                    lookatValue[2] += 0.2;
                    break;
                case Keys.A:                ////移动摄像机的x坐标
                    lookatValue[0] -= 0.2;
                    break;
                case Keys.D:
                    lookatValue[0] += 0.2;
                    break;
                case Keys.Q:                //移动摄像机的y坐标
                    lookatValue[1] += 0.2;
                    break;
                case Keys.E:
                    lookatValue[1] -= 0.2;
                    break;               
            }
            viewChanged = true;
            openGLControl1_Resize(null, null);
        }
        #endregion

        #region 图形生成与清理
        private void BtnClearScreen_Click(object sender, EventArgs e)//曲线重绘
        {
            if (BspineCurve != null)
            {
                Array.Clear(BspineCurve, 0, BspineCurve.Length);
            }
            if (ctrlPoints != null)
            {
                ctrlPoints.Clear();
            }
            isDraw = true;
        }
        private void BtnGenCurve_Click(object sender, EventArgs e)//曲线生成
        {
            if (SetParameteOK(v_paraStyle,ctrlPoints.Count,0))
            {
                int k = v_k;//曲线阶数
                int paraStyle = v_paraStyle;
                if (ctrlPoints.Count != 0)
                {
                    Point[] tempctrlPts = ctrlPoints.ToArray();
                    Bspine3D Bcurve = new Bspine3D(tempctrlPts, k, ctrlPoints.Count, paraStyle);
                    BspineCurve = Bcurve.Curve;
                    GenChartofbasefunc(tempctrlPts.Length, Bcurve.BaseFunc, "v向基函数");
                    label_vOrder.Text = v_k.ToString();
                }
                else
                {
                    MessageBox.Show("Warning:没有控制点，请点击生成控制点后绘图");
                }
            }
        }
        private void GenChartofbasefunc(int num, List<Point[]> data, string uORv)//生成基函数
        {
            chartControl1.ChartPanel.ChartContainers.Clear();           
            chartControl1.ChartPanel.Legend.Visible = false;
            ChartXy ChartBasefunc = new ChartXy();            
            ChartBasefunc.Titles.Add(SetchartTitleu_v(uORv));
            for (int j = 0; j < num; j++)
            {
                string x = "N_" + j.ToString();
                ChartSeries asd = new ChartSeries(x,SeriesType.Line);
                ChartBasefunc.ChartSeries.Add(asd);
                for (int i = 0; i < data[0].Length; i++)
                {
                    asd.SeriesPoints.Add(new SeriesPoint(data[j][i].x, data[j][i].y));
                }
            }
            ChartBasefunc.ChartSeries.Add(SetNodevetex(data));
           chartControl1.ChartPanel.ChartContainers.Add(ChartBasefunc);
        }
        private ChartSeries SetNodevetex(List<Point[]> data)
        {
            SortedSet<double> tempset = new SortedSet<double>();
            foreach (var item in data[0])
            {
                tempset.Add(item.z);
            }
            ChartSeries t = new ChartSeries("节点矢量", SeriesType.Point);
            foreach (var item in tempset)
            {
                t.SeriesPoints.Add(new SeriesPoint(item, 0.5));
            }
            return t;
        }
        private ChartTitle SetchartTitleu_v(string uORv)
        {
            DevComponents.DotNetBar.Charts.Style.Padding padding5 = new DevComponents.DotNetBar.Charts.Style.Padding
            {
                Bottom = 8,
                Left = 8,
                Right = 8,
                Top = 8
            };
            ChartTitle chartTitleu_v = new DevComponents.DotNetBar.Charts.ChartTitle();
            chartTitleu_v.ChartTitleVisualStyle.Padding = padding5;
            chartTitleu_v.ChartTitleVisualStyle.Font = new System.Drawing.Font("Georgia", 16F);
            chartTitleu_v.ChartTitleVisualStyle.Alignment = DevComponents.DotNetBar.Charts.Style.Alignment.MiddleCenter;
            chartTitleu_v.ChartTitleVisualStyle.TextColor = System.Drawing.Color.Navy;
            chartTitleu_v.Text = uORv;
            chartTitleu_v.XyAlignment = DevComponents.DotNetBar.Charts.XyAlignment.Top;
            return chartTitleu_v;
        }
        private void btnClearSurface_Click(object sender, EventArgs e)//曲面重绘
        {
            if (surfaceCtrlPt != null)
            {
                surfaceCtrlPt.Clear();
            }
            if (BspineSurface != null)
            {
                BspineSurface.Clear();
                
            }
            if (BspineCurve != null || BspineCurve.Length != 0)
            {
                Array.Clear(BspineCurve, 0, BspineCurve.Length);
            }
            if (ctrlPoints.Count > 0)
            {
                ctrlPoints.Clear();
            }
            if (checkBoxModify.Checked == true)
            {
                MessageBox.Show("取消修改模式，以便进入绘图模式");               
            }
            isDraw = true;
        }
        private void btnGenSurface_Click(object sender, EventArgs e)//曲面生成
        {
            int k_v = 2, k_u = 2;
            int m = ctrlPoints.Count, n = 5;
            if (SetParameteOK(u_paraStyle,n,1))
            {
                k_v = v_k;k_u = u_k;
                Point[,] tempctrlPts = new Point[n, m];
                if (surfaceCtrlPt.Count > 1)
                {
                    for (int i = 0; i < surfaceCtrlPt.Count; i++)
                    {
                        for (int j = 0; j < m; j++)
                        {
                            tempctrlPts[i, j] = surfaceCtrlPt[i][j];
                        }
                    }
                    Bspine3D Bsurface = new Bspine3D(tempctrlPts, m, n, k_u, k_v, v_paraStyle, u_paraStyle);
                    BspineSurface = Bsurface.Surface;
                    GenChartofbasefunc(n, Bsurface.BaseFunc, "u向基函数");
                }
            }
           
        }
        private bool SetParameteOK( int paraStyle, int num_ctrlPts,int flag)//参数化是否设置正确
        {

            int k = 1;
            if (flag == 1)
            {                
                k = int.Parse(textBox_u_order.Text);
                if (k < 0)
                {
                    MessageBox.Show("曲线次数不能为负值已调整为正");
                    k = -k;
                }
                u_k = k;
            }
            else if (flag == 0)
            {
                k = int.Parse(textBox_v_order.Text);
                if (k < 0)
                {
                    MessageBox.Show("曲线次数不能为负值已调整为正");
                    k = -k;
                }
                v_k = k;
            }
            else
            {
                MessageBox.Show("错误设置flag");
            }
            if (num_ctrlPts < k + 1)
            {
                MessageBox.Show("无法生成曲线，曲线的点的个数少于次数k+1");
                return false;
            }
            if (paraStyle == 3 && (num_ctrlPts - 1) % k != 0)
            {
                MessageBox.Show("无法生成曲线，曲线的点的个数-1并不是曲线阶数k的整数倍");
            }
            return true;
        }
        #endregion

        #region 鼠标操作
        private void openGLControl_MouseDown(object sender, MouseEventArgs e)
        {
            mouseX = e.X;
            mouseY = e.Y;
            delta = e.Delta;
            recoverCenter = false;
            switch (e.Button)
            {
                case MouseButtons.Middle:
                    isScale = true;
                    break;
                case MouseButtons.Left:
                    if (isDraw && !isModify)
                    {
                        isAddpoint = true;                       
                    }
                    if (isModify)
                    {
                        //点修改模式'
                        if (switchToPointSelectBtn.Value == true)
                        {
                            if (ctrlPoints != null && surfaceCtrlPt.Count == 0)
                            {
                                if (i_highlight != -1)//如果鼠标在控制点附近
                                {
                                    pointModify = true;//画坐标架的开关
                                    selectedPt =  ctrlPoints[i_highlight]; //在点击的控制点处增加一个标架
                                    selectindex[0] = i_highlight;
                                }
                            }
                            else if (surfaceCtrlPt.Count > 0) 
                            {
                                if (i_u_surfacehight != -1)
                                {
                                    pointModify = true;//画坐标架的开关
                                    selectedPt =  surfaceCtrlPt[i_u_surfacehight][i_v_surfacehight];
                                    selectindex[1] = i_u_surfacehight;selectindex[2] = i_v_surfacehight;
                                }
                            }
                            if (!isStrech)//准备拖拽
                            {
                                isStrech = true;
                                //callenth（坐标轴端点,mouse），返回距离最小的轴的xyz
                                ChooseCoodinatetoStrech();
                            }
                        }
                        else
                        {
                            pointModify = false;
                        }
                        //线修改模式，和ctrl同时按下后
                        if (ModifierKeys == Keys.Control && switchToLineSelectBtn.Value == true)
                        {
                            int u_num = 5;
                            if (surfaceCtrlPt .Count == 0)
                            {
                                surfaceCtrlPt.Add(ctrlPoints);
                                for (int i = 0; i < u_num - 1; i++)
                                {
                                    List<Point> templist = new List<Point>(ctrlPoints.Count);
                                    for (int j = 0; j < ctrlPoints.Count; j++)
                                    {
                                        templist.Add(ctrlPoints[j]);
                                    }
                                    surfaceCtrlPt.Add(templist);//????存疑
                                }
                            }
                            isGenctrlGrid = true;
                        }
                    }
                    break;
                case MouseButtons.Right:
                    isRotate = true;
                    break;
                default:
                    return;
            }
        }
        private void openGLControl_MouseMove(object sender, MouseEventArgs e)
        {
            double temp = 0;
            int tempHighlight = -1;
            double dist = e.X + e.Y - mouseX - mouseY;
            if (isScale)
            {
                scale -= 0.002 * (dist);

            }
            else if (isRotate)
            {
                rotation += (e.X - mouseX) * 0.1f;
            }
            if (isModify)
            {
                if (winPoints != null)
                {
                    for (int i = 0; i < winPoints.Length; i++)
                    {
                        temp = CalLength(winPoints[i], mouseX, mouseY);
                        if (350 >= temp)
                        {
                            tempHighlight = i;
                        }
                        i_highlight = tempHighlight;
                    }
                }
                if (winSurfacePts != null) //计算鼠标距离哪个控制点最近
                {
                    int index = 0;
                    int utempwinPt = -1, vtempwinPt = -1;
                    foreach (var winpoints in winSurfacePts)
                    {
                        if (CalLength(winpoints, mouseX, mouseY) <= 350)
                        {
                            utempwinPt = index / winSurfacePts.GetLength(1);
                            vtempwinPt = index % winSurfacePts.GetLength(1);
                        }

                        index++;
                    }
                    i_u_surfacehight = utempwinPt;
                    i_v_surfacehight = vtempwinPt;
                }
            }
            if (isStrech)
            {
                //改变点，通过switch传回的值，（x轴，i_pt，Δmouse）生成曲线交给按钮点击
                ModifyCtrlPoints(dist);
                BtnGenCurve_Click(null, null);
            }
            if (isGenctrlGrid)
            {
                double[] lashengplanel = new double[3] { 0, 1, 0 };
                Point tempPts = new Point();
                for (int i = 1; i < surfaceCtrlPt.Count; i++)
                {
                    float u_rate = (float)i / 4;
                    for (int j = 0; j < ctrlPoints.Count; j++)
                    {
                        tempPts = surfaceCtrlPt[i][j];
                        tempPts.x += u_rate * dist * 0.05 * lashengplanel[0];
                        tempPts.y += u_rate * dist * 0.05 * lashengplanel[1];
                        tempPts.z += u_rate * dist * 0.05 * lashengplanel[2];
                        surfaceCtrlPt[i][j] = tempPts;
                    }
                }
            }
            mouseY = e.Y;
            mouseX = e.X;
        }
        private void openGLControl_MouseUp(object sender, MouseEventArgs e)
        {
            isScale = false;
            isRotate = false;
            viewChanged = true;
            isStrech = false;//拖拽结束
            isGenctrlGrid = false;//控制网格生成结束
        }
        #endregion

        #region 修改模式下的函数
        private void switchToPointSelectBtn_MouseDown(object sender, MouseEventArgs e)//点修改模式
        {
            if (switchToLineSelectBtn.Value == true)//两者方式只能有一个
            {
                switchToLineSelectBtn.Value = false;
            }
        }
        private void checkBoxModify_Click(object sender, EventArgs e)//进入修改模式
        {
            isModify = checkBoxModify.Checked;
            if (ctrlPoints.Count != 0)
            {
                switchToPointSelectBtn.Value = checkBoxModify.Checked;               
                switchToLineSelectBtn.Enabled = checkBoxModify.Checked;
                switchToPointSelectBtn.Enabled = checkBoxModify.Checked;
                viewChanged = checkBoxModify.Checked;
                if (checkBoxModify.Checked == false)
                {
                    switchToLineSelectBtn.Value = false;
                }
            }

        }
        private void switchToLineSelectBtn_MouseDown(object sender, MouseEventArgs e)//进入线修改
        {
            if (switchToPointSelectBtn.Value == true)
            {
                switchToPointSelectBtn.Value = false;
            }
            isDraw = false;
            checkBoxGridShow.Checked = true;
        }
        private void ChooseCoodinatetoStrech()//选择座标架哪个方向进行拉伸
        {
            int Axisindex = -1;
            double ptlen = 0;
            for (int i = 0; i < 3; i++)
            {
                ptlen = CalLength(winCoodPos[i], mouseX, mouseY);
                if (ptlen < 450)
                {
                    Axisindex = i;
                }
            }
            axisindex = Axisindex;
        }
        private void ModifyCtrlPoints( double delta_mouse)//修改控制点
        {
            Point temp = selectedPt;
            double rate = 0.01;
            switch (axisindex)
            {
                case 0:
                    //move x
                    temp.x += delta_mouse * rate;
                    break;
                case 1:
                    //move y
                    temp.y-= delta_mouse * rate;
                    break;
                case 2:
                    //move z
                    temp.z -= delta_mouse * rate;
                    break;
                case -1:
                    break;
                default :
                    break;
            }
            if (surfaceCtrlPt.Count > 1)
            {
                surfaceCtrlPt[selectindex[1]][selectindex[2]] = temp;
                btnGenSurface_Click(null, null);
            }
            else
            {
                ctrlPoints[selectindex[0]] = temp;
            }
            selectedPt = temp;
        }
        private void btnCurveModify_Click(object sender, EventArgs e)//曲线修改模式
        {
            if (checkBoxModify.Checked == false)
            {
                checkBoxModify.Checked = true;
            }
            else
            {
                checkBoxModify.Checked = false;
            }
            checkBoxModify_Click(null, null);
        }
        #endregion

        #region 设置参数化
        private void btnvUniform_para_Click(object sender, EventArgs e)//v向设置一般均匀
        {
            v_paraStyle = 1;
        }

        private void btnv_QriUniform_para_Click(object sender, EventArgs e)//v向设置准均匀
        { 
            v_paraStyle = 2;
        }

        private void btnv_SegBezier_para_Click(object sender, EventArgs e)//v向设置分段贝塞尔
        {
            //分段贝塞尔条件
            v_paraStyle = 3;
        }

        private void btnv_NoneUniform_para_Click(object sender, EventArgs e)//v向设置非均匀
        {
            //显示调用非均匀的参数化方式
            v_paraStyle = 5;
            if (MessageBox.Show("是否采用RF法参数划分？, 点击否使用HJ法", "非均匀参数划分",
                MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                v_paraStyle = 4;
            } 
        }

        private void btnu_Uniform_para_Click(object sender, EventArgs e)//u向设置一般均匀
        {
            u_paraStyle = 1;
        }

        private void btnu_SegBezier_para_Click(object sender, EventArgs e)//u向设置分段贝塞尔
        {
            //分段贝塞尔条件
            u_paraStyle = 3;
        }

        private void btnu_QriUniform_para_Click(object sender, EventArgs e)//u向设置准均匀
        {
            u_paraStyle = 2;
        }

        private void btnu_NoneUniform_para_Click(object sender, EventArgs e)//u向设置非均匀
        {
            u_paraStyle = 5;
            if (MessageBox.Show("是否采用RF法参数划分？, 点击否使用HJ法", "非均匀参数划分",
                MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                u_paraStyle = 4;
            }
        }
        #endregion

        #region 其他
        private double CalLength( Point P0, int X, int Y)//计算欧式距离函数
        {
            double len = 0;
            len = Math.Pow(P0.x - X, 2) + Math.Pow(P0.y - Y, 2) + Math.Pow(P0.z - 0, 2);
            return len;

        }
        #endregion


        #endregion


    }
}
