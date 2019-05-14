/*
	该文件用于存储三维形式的b样条曲线曲面的类声明和方法，服务于主界面程序
	作者：邹捷
	创建时间： 2019年1月28日 
	为了增加效率，如果不声明默认的浮点数存储格式为float
    使用方法：给出b样条曲线与曲面的构造方法，曲线构造方法可以获取曲线的点列表
              曲面的构造方法，给出曲面的点列表
*/
using System;
using System.Collections.Generic;

public struct Point				//点的结构体定义
{
    public double x;
    public double y;
    public double z;
};

namespace BSpine
{
    class Bspine3D
    {
        #region 成员变量
        private readonly Point[,] ptij;			 //控制点        
        private readonly int n, m;				 //u向控制点的数量n，v向控制点的数量m
        private readonly int ku, kv;				 //大写的K代表曲线次数
        private readonly int _vSignal;            //v向参数化的方式
        private readonly int _uSignal;            //u向参数化的方式
        private List<Point[]> _surface= new List<Point[]>();
        private Point[] _curvPt;
        private List<Point[]> _baseFunc = new List<Point[]>();
        #endregion

        #region 曲线曲面等参数的索引器
        //曲线上的点
        public Point[] Curve
        {
            get { return _curvPt; }            
        }
        //曲面的点
        public List<Point[]> Surface
        {
            get { return _surface; }           
        }

        public List<Point[]> BaseFunc { get { return _baseFunc; } set { _baseFunc = value; } }
        #endregion

        #region 构造函数
        public Bspine3D(Point[,] pij, int m, int n = 1, int ku = 2, int kv = 2, int _vSignal = 1,int _uSignal = 1)
        {
            ptij = new Point[n, m];
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < m; j++)
                {
                    ptij[i, j] = pij[i, j];
                }
            }
            if (kv <= 0 && ku <= 0)
            {
                Console.WriteLine("曲面次数小于零，初始化失败，使用默认阶数值：2");
            }
            this.n = n;
            this.m = m;
            this.kv = kv;
            this.ku = ku;
            this._vSignal = _vSignal;
            this._uSignal = _uSignal;
            BspineSurface();
        }
        public Bspine3D(Point[] pi, int k, int m, int vSignal = 1)
        {
            ptij = new Point[1, m];
            for (int i = 0; i < m; i++)
            {
                ptij[0, i] = pi[i];
            }
            this.n = 1;
            this.m = m;
            if (k <= 0)
            {
                Console.WriteLine("曲线次数小于零，初始化失败，使用默认阶数值：2");
            }
            kv = k;
            ku = 1;
            _vSignal = vSignal;
            _uSignal = 1;
            IbSpinecurve();
        }
        #endregion    

        #region 参数化
        private float[] Paraztion(int flag, int k, Point[] ctrlPoints)//num为控制点个数
        {
            int num = ctrlPoints.Length;
            int segT = num + k;           
            float[] t = new float[segT + 1];
            switch (flag)
            {
                case 1:
                    UnifPara(t, segT);
                    break;
                case 2:
                    Qs_UnifPara(t, segT, k);
                    break;
                case 3:
                    if ((num - 1) % k == 0)
                    {
                        SegBeizier(t, segT, k);
                    }
                    else
                        Console.WriteLine(@"分段贝塞尔曲线（控制点个数-1）必须为k的整数倍，默认输出"); //这个异常应该写在输入环节
                    break;
                case 4:
                    N_UnifPara_RF(t, k, ctrlPoints);
                    break;
                case 5:
                    N_UnifPara_HJ(t, k, ctrlPoints);
                    break;
                default:
                    //此处抛出异常
                    //异常显示"无效的参数划分方式，重新选择"
                    Console.WriteLine(@"无效的参数划分方式，重新选择");
                    break;
            }
            return t;
        }

        private void UnifPara(float[] t, int num)
        {
            float dx = 0;
            dx = (float)1/num;
            t[0] = 0;
            for (int i = 1; i <= num; i++)
            {
                t[i] = t[i - 1] + dx;
            }
        }
        private void Qs_UnifPara(float[] t, int num, int k)
        {
            float dx =(float) 1 / (num - 2 * k);
            for (int i = 0; i <= k; i++)
            {
                t[i] = 0;
                t[num - i] = 1;
            }
            for (int i = k + 1; i <= num - k - 1; i++)
            {
                t[i] = t[i - 1] + dx;
            }         
        }
        private void SegBeizier(float[] t, int num, int k)
        {
            int inter = num - 2 * k - 1;
            int se = inter / k + 1;
            float dx = (float)1 / se;
            float x = 0;
            for (int i = 0; i <= k; i++)
            {
                t[i] = 0;
                t[num - i] = 1;
            }
            for (int j = 0; j < se; j++)
            {
                x += dx;
                for (int i = 0; i < k; i++)
                {
                    t[k + 1 + j * k + i] = x;
                }
            }
        }
        private void N_UnifPara_RF(float[] t, int k,Point[] ctrlPoints)
        {
            
            int num = ctrlPoints.Length;
            float[] suml = new float[num - 1];
            float[] Len;
            Len = CalLength( ctrlPoints,suml);
            for (int i = 0; i <= k; i++)
            {
                t[i] = 0;
                t[num + k - i] = 1;
            }
            int s = k % 2;
            if (s == 0)
            {
                for (int i = k + 1; i <= num - 1; i++)
                {
                    t[i] = (suml[k / 2 + i - k - 2] + Len[k / 2 + i - k - 1] / 2) / suml[num - 2];
                }
            }
            else
            {
                for (int i = k + 1; i <= num - 1; i++)
                {
                    t[i] = suml[(k + 1) / 2 + i - k - 2] / suml[num - 2];
                }
            }
        }
        private void N_UnifPara_HJ(float[] t, int k, Point[] ctrlPoints)
        {
            int num = ctrlPoints.Length;
            float[] suml = new float[num - 1];
            float[] Len;
            Len = CalLength(ctrlPoints, suml);
            for (int i = 0; i <= k; i++)
            {
                t[i] = 0;
                t[num + k - i] = 1;
            }
            float[] temp1 = new float[num - k];
            float tempSum = 0;
            for (int i = k+1; i <= num; i++)
            {
                for (int j = i-k; j < i; j++)
                {
                    temp1[i - k -1] += Len[j-1];
                }
                tempSum += temp1[i - k -1];
            } 
            for (int i = k + 1; i <= num - 1; i++)
            {
                t[i] = t[i-1] + temp1[i - k - 1] / tempSum;
            }
        }

        private float[] CalLength( Point[] ctrlPoints, float[] suml)
        {
            float[] len = new float[ctrlPoints.Length];
            float sumL = 0;
            double _dX2, _dY2, _dZ2;
            for (int i = 0; i < ctrlPoints.Length-1; i++)
            {
                _dX2 = Math.Pow(ctrlPoints[i + 1].x - ctrlPoints[i].x, 2);
                _dY2 = Math.Pow(ctrlPoints[i + 1].y - ctrlPoints[i].y, 2);
                _dZ2 = Math.Pow(ctrlPoints[i + 1].z - ctrlPoints[i].z, 2);
                len[i] =(float) Math.Sqrt(_dX2 + _dY2 + _dZ2);              
                sumL += len[i];
                suml[i] = sumL;
            }   
            return len;
        }
        #endregion

        #region 曲线曲面计算函数
        private Point Deboor(float ti, int k, float[] t, Point[] CtrlPt)//deboor算法
        {
            Point[] d = new Point[CtrlPt.Length];
            for (int i = 0; i < CtrlPt.Length; i++)
            {
                d[i] = CtrlPt[i];
            }
            int anchor = -1;
            float alj = 0;
            for (int i = 0; i < t.Length; i++)
            {
                if (t[i] <= ti && t[i + 1] > ti)
                {
                    anchor = i;
                    break;
                }
            }
            for (int i = 1; i <= k; i++)
            {
                for (int j = anchor - k; j <= anchor - i; j++)
                {
                    float temp = t[j + k + 1] - t[j + i];
                    if ( temp == 0)
                    {
                        alj = 0;
                    }
                    else
                    {
                        alj = (ti - t[j + i]) / temp;
                    } 
                    d[j].x += alj * (d[j + 1].x - d[j].x); 
                    d[j].y += alj * (d[j + 1].y - d[j].y);
                    d[j].z += alj * (d[j + 1].z - d[j].z);
                }
            }
            return d[anchor - k];
        }

        private double CalBaseFuncN(float ti, int k, int i, float[] t)//常规计算基函数的N_ik的方法
        {
            double N = 0.0, alpha = 0, belta = 0;
            if (k == 0)
            {
                if (ti < t[i + 1] && ti > t[i])
                {
                    return 1;
                }
                else
                    return 0.0;
            }
            else
            {
                if (ti < t[i + k + 1] && ti > t[i])
                {
                    float val = t[i + k] - t[i];
                    if (val != 0)
                        alpha = (ti - t[i]) / val;
                    float va2 = t[i + k + 1] - t[i + 1];
                    if (va2 != 0)
                        belta = (t[i + k + 1] - ti) / va2;
                    N = alpha * CalBaseFuncN(ti, k - 1, i, t) + belta * CalBaseFuncN(ti, k - 1, i + 1, t);
                    return N;
                }
                else
                {
                    return 0.0;
                }
            }
        }

        public void DisplayBaseFunc2(int k, Point[] ctrlPoint, int sig_para)//计算基函数图像，默认步长0.01
        {
            float[] t = Paraztion(sig_para, k, ctrlPoint);
            float dt = (float)0.005;
            List<Point[]> baseFunc = new List<Point[]>();
            for (int i = 0; i < ctrlPoint.Length; i++)
            {
                int j = 0;
                Point[] _nik = new Point[201];
                for (float ti = t[i] + dt; ti < t[i+k+1]; ti += dt)
                {
                     _nik[j].y =  CalBaseFuncN(ti, k ,i,t);
                    _nik[j].x = ti;
                    j++;
                }
                baseFunc.Add(_nik);
            }
            _baseFunc = baseFunc;
        }

        private void IbSpinecurve()//b样条曲线接口
        {
             Point[] ptv = new Point[m];//存储v向控制点
            for (int i = 0; i < ptv.Length; i++)
			{
			 ptv[i] = ptij[0,i];
			}
            _curvPt = BSpinecurve(kv, 100, ptv, _vSignal);
            DisplayBaseFunc2(kv, ptv, _vSignal);
        }

        private Point[] BSpinecurve(int order, int n_Segs, Point[] ctrlPoint, int sig_para)//B样条曲线计算函数，order为曲线阶数，n_segs为曲线的分辨率，cp为控制点,sig_para为参数化方式
        {
            //参数化
            List<Point> CurvePt = new List<Point>();
            float[] t = Paraztion(sig_para, order, ctrlPoint);
            //将参数_t分为n_Segs段
            float dt = (float)1/n_Segs;
            //对定义域内的所有v，求出对应的曲线上的点
            int i = 0;
            for (float v = t[order]; v < t[ctrlPoint.Length]; v += dt)
            {
                CurvePt.Add( Deboor(v, order, t, ctrlPoint));
                i++;
            }
            Point[] curve = new Point[CurvePt.Count];
            for (int j = 0; j < CurvePt.Count; j++)
            {
                curve[j] = CurvePt[j];
            }
            return curve;
        }

        private void BspineSurface() //B样条曲面计算函数
        {
            int n_Segs = 80;                        
            List<Point[]> _vtempCurve = new List<Point[]>();
            Point[] _utempCtrlPt = new Point[n];
            Point[] v_ctrl = new Point[m];         
            //生成n条v向的曲线
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < m; j++)
                {
                    v_ctrl[j] = ptij[i, j];
                }
                _vtempCurve.Add(BSpinecurve(kv, n_Segs, v_ctrl, _vSignal));
            }
            //依靠每条v向曲线的 每一个离散点
            //建立n个u向的新控制点
            //对新的u向控制点生成u向曲线 
            for (int i = 0; i < _vtempCurve[0].Length; i++)
			{              
                for (int j = 0; j < n; j++)
                {
                    _utempCtrlPt[j] = _vtempCurve[j][i];
                }                
                _surface.Add( BSpinecurve(ku, n_Segs, _utempCtrlPt, _uSignal));
			}

        }
        #endregion
    }
}
