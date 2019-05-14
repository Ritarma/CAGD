/*
	���ļ����ڴ洢��ά��ʽ��b��������������������ͷ��������������������
	���ߣ��޽�
	����ʱ�䣺 2019��1��28�� 
	Ϊ������Ч�ʣ����������Ĭ�ϵĸ������洢��ʽΪfloat
    ʹ�÷���������b��������������Ĺ��췽�������߹��췽�����Ի�ȡ���ߵĵ��б�
              ����Ĺ��췽������������ĵ��б�
*/
using System;
using System.Collections.Generic;

public struct Point				//��Ľṹ�嶨��
{
    public double x;
    public double y;
    public double z;
};

namespace BSpine
{
    class Bspine3D
    {
        #region ��Ա����
        private readonly Point[,] ptij;			 //���Ƶ�        
        private readonly int n, m;				 //u����Ƶ������n��v����Ƶ������m
        private readonly int ku, kv;				 //��д��K�������ߴ���
        private readonly int _vSignal;            //v��������ķ�ʽ
        private readonly int _uSignal;            //u��������ķ�ʽ
        private List<Point[]> _surface= new List<Point[]>();
        private Point[] _curvPt;
        private List<Point[]> _baseFunc = new List<Point[]>();
        #endregion

        #region ��������Ȳ�����������
        //�����ϵĵ�
        public Point[] Curve
        {
            get { return _curvPt; }            
        }
        //����ĵ�
        public List<Point[]> Surface
        {
            get { return _surface; }           
        }

        public List<Point[]> BaseFunc { get { return _baseFunc; } set { _baseFunc = value; } }
        #endregion

        #region ���캯��
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
                Console.WriteLine("�������С���㣬��ʼ��ʧ�ܣ�ʹ��Ĭ�Ͻ���ֵ��2");
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
                Console.WriteLine("���ߴ���С���㣬��ʼ��ʧ�ܣ�ʹ��Ĭ�Ͻ���ֵ��2");
            }
            kv = k;
            ku = 1;
            _vSignal = vSignal;
            _uSignal = 1;
            IbSpinecurve();
        }
        #endregion    

        #region ������
        private float[] Paraztion(int flag, int k, Point[] ctrlPoints)//numΪ���Ƶ����
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
                        Console.WriteLine(@"�ֶα��������ߣ����Ƶ����-1������Ϊk����������Ĭ�����"); //����쳣Ӧ��д�����뻷��
                    break;
                case 4:
                    N_UnifPara_RF(t, k, ctrlPoints);
                    break;
                case 5:
                    N_UnifPara_HJ(t, k, ctrlPoints);
                    break;
                default:
                    //�˴��׳��쳣
                    //�쳣��ʾ"��Ч�Ĳ������ַ�ʽ������ѡ��"
                    Console.WriteLine(@"��Ч�Ĳ������ַ�ʽ������ѡ��");
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

        #region ����������㺯��
        private Point Deboor(float ti, int k, float[] t, Point[] CtrlPt)//deboor�㷨
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

        private double CalBaseFuncN(float ti, int k, int i, float[] t)//��������������N_ik�ķ���
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

        public void DisplayBaseFunc2(int k, Point[] ctrlPoint, int sig_para)//���������ͼ��Ĭ�ϲ���0.01
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

        private void IbSpinecurve()//b�������߽ӿ�
        {
             Point[] ptv = new Point[m];//�洢v����Ƶ�
            for (int i = 0; i < ptv.Length; i++)
			{
			 ptv[i] = ptij[0,i];
			}
            _curvPt = BSpinecurve(kv, 100, ptv, _vSignal);
            DisplayBaseFunc2(kv, ptv, _vSignal);
        }

        private Point[] BSpinecurve(int order, int n_Segs, Point[] ctrlPoint, int sig_para)//B�������߼��㺯����orderΪ���߽�����n_segsΪ���ߵķֱ��ʣ�cpΪ���Ƶ�,sig_paraΪ��������ʽ
        {
            //������
            List<Point> CurvePt = new List<Point>();
            float[] t = Paraztion(sig_para, order, ctrlPoint);
            //������_t��Ϊn_Segs��
            float dt = (float)1/n_Segs;
            //�Զ������ڵ�����v�������Ӧ�������ϵĵ�
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

        private void BspineSurface() //B����������㺯��
        {
            int n_Segs = 80;                        
            List<Point[]> _vtempCurve = new List<Point[]>();
            Point[] _utempCtrlPt = new Point[n];
            Point[] v_ctrl = new Point[m];         
            //����n��v�������
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < m; j++)
                {
                    v_ctrl[j] = ptij[i, j];
                }
                _vtempCurve.Add(BSpinecurve(kv, n_Segs, v_ctrl, _vSignal));
            }
            //����ÿ��v�����ߵ� ÿһ����ɢ��
            //����n��u����¿��Ƶ�
            //���µ�u����Ƶ�����u������ 
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
