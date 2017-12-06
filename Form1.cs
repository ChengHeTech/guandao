﻿using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Xml;

namespace guandao
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //图层初始化
            Draw_Init();

            //注册鼠标滚轮事件
            this.pictureBoxForce.MouseWheel += new MouseEventHandler(pictureBoxForce_MouseWheel);

            serialPort1.Open();
        }


        Size size;
        //已绘制点坐标集合
        Point[] point;//原始坐标
        Point[] NewPoint;//新坐标
        //点坐标个数
        int length = 1;
        //光标当前位置
        Point NowPoint;

        //已经撤销的步数
        int CancelStep = 0;

        //绘图模式
        bool MapMode = false;

        //捕获值
        int Grid = 35;//单位，像素

        //缩放时的偏移率
        float Scale = 0;

        //图层初始化
        void Draw_Init()
        {
            //size = pictureBoxBackGround.Size;
            point = new Point[1000];
            NewPoint = new Point[1000];
            //point[0] = new Point(0,0);

            label_Grid.Text = "Grid:" + Grid.ToString();


            // 背景图层绘制网格
            Draw_Grid();

            #region 表层设置透明
            pictureBoxForce.BackColor = Color.Transparent;
            pictureBoxForce.Parent = pictureBoxBackGround;
            pictureBox1.BackColor = Color.Transparent;
            pictureBox1.Parent = pictureBoxForce;
            #endregion
        }

        //绘制网格
        void Draw_Grid()
        {
            size = pictureBoxBackGround.Size;

            size.Width += 50;
            size.Height -= 50;

            #region 背景图层绘制网格
            Bitmap bmp = new Bitmap(size.Width, size.Height);
            if (Grid >= 10)
            {
                Graphics g = Graphics.FromImage(bmp);
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                //画笔为灰色
                Pen pen = new Pen(Color.LightGray);
                //画虚线
                pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;

                //在图片上画线
                //横线
                for (int i = 0; i < size.Height / Grid + 1; i++)
                {
                    g.DrawLine(pen, 0, size.Height - Grid * i, size.Width, size.Height - Grid * i);
                }
                //竖线
                for (int i = 0; i < size.Width / Grid + 1; i++)
                {
                    g.DrawLine(pen, Grid * i, 0, Grid * i, size.Height);
                }
                g.Dispose();
            }
            pictureBoxBackGround.Image = bmp;
            pictureBoxBackGround.BackColor = Color.Black;
            #endregion
        }

        //改变Grid
        bool mouseWheel = false;

        public void pictureBoxForce_MouseWheel(object sender, MouseEventArgs e)
        {

            int x = e.X;
            int y = e.Y;

            //计算偏移比例
            Scale = (float)x / size.Width;
            mouseWheel = true;


            if (e.Delta > 0)
            {
                if (Grid > 5)
                {
                    Grid -= 5;
                }
                else if (Grid > 1)
                {
                    Grid -= 1;
                }
            }
            else
            {
                if (Grid < 400)
                {
                    if (Grid < 5)
                    {
                        Grid += 1;
                    }
                    else
                        Grid += 5;
                }
            }
            label_Grid.Text = "Grid:" + Grid.ToString();

            //更新原点坐标
            //point[0] = new Point(0, size.Height / Grid);

            //重绘网格
            Draw_Grid();
            //重绘线段
            ReloadUI();
        }

        private void pictureBoxForce_MouseMove(object sender, MouseEventArgs e)
        {
            int x = e.X;
            int y = e.Y;



            #region 计算捕获坐标
            //计算最近坐标点
            int xval = x % Grid;
            int yval = y % Grid;
            if (xval > (Grid / 2))
                x += Grid - xval;
            else
                x -= xval;
            if (yval > (Grid / 2))
                y += Grid - yval;
            else
                y -= yval;
            #endregion

            y = size.Height / Grid - y / Grid + 1;
            NowPoint.X = x / Grid;
            NowPoint.Y = y;

            //显示坐标到界面
            label_XY.Text = "X:" + x.ToString() + " Y:" + y.ToString();
            label_True_XY.Text = "X:" + (NowPoint.X).ToString() + "CM Y:" + (NowPoint.Y).ToString() + "CM";
            //重新加载界面
            ReloadUI();
        }

        private void pictureBoxForce_MouseClick(object sender, MouseEventArgs e)
        {
            int x = e.X;
            int y = e.Y;

            MapMode = true;

            #region 计算捕获坐标
            //计算最近坐标点
            int xval = x % Grid;
            int yval = y % Grid;
            if (xval > (Grid / 2))
                x += Grid - xval;
            else
                x -= xval;
            if (yval > (Grid / 2))
                y += Grid - yval;
            else
                y -= yval;
            #endregion
            y = size.Height / Grid - y / Grid + 1;
            NowPoint.X = x / Grid;
            NowPoint.Y = y;

            if (e.Button == MouseButtons.Left && e.Clicks == 1)
            {
                //单击了鼠标左键,添加点
                point[length++] = new Point(x / Grid, y);

                textBox1.Text = string.Empty;
                textBox1.AppendText("point.Length:" + length.ToString() + "\r\n");
                for (int i = 0; i < length; i++)
                {
                    textBox1.AppendText(i.ToString() + " x:" + point[i].X.ToString() + "CM,y:" + (point[i].Y).ToString() + "CM\r\n");
                }

            }
            else if (e.Button == MouseButtons.Right && e.Clicks == 1)
            {
                //单击了鼠标右键，退出绘图模式
                MapMode = false;
                ReloadUI();
            }

            label1.Text = length.ToString();
            label2.Text = CancelStep.ToString();

        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.Shift && e.KeyCode == Keys.Z)
            {
                //恢复撤销
                if (CancelStep > 1)
                {
                    CancelStep--;
                    length++;
                    ReloadUI();
                    label1.Text = length.ToString();
                    label2.Text = CancelStep.ToString();
                }
            }
            else
                if (e.KeyCode == Keys.Escape)
            {
                //退出绘图模式
                MapMode = false;
                ReloadUI();
            }
            else if (e.Control && e.KeyCode == Keys.Z)
            {
                //撤销
                if (length > 1)
                {
                    length--;
                    CancelStep++;
                    //重新加载界面
                    ReloadUI();
                    label1.Text = length.ToString();
                    label2.Text = CancelStep.ToString();
                }
            }
        }


        //重新加载界面
        void ReloadUI()
        {
            int x = NowPoint.X * Grid;
            int y = size.Height - NowPoint.Y * Grid;

            //pictureBox1.Location = new Point(point[length - 1].X * Grid - 17, size.Height - point[length - 1].Y * Grid - 17);
            if (MapMode)
            {
                Bitmap bmp = new Bitmap(size.Width, size.Height);
                Graphics g = Graphics.FromImage(bmp);
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                Brush bush = new SolidBrush(Color.Black);//填充的颜色

                //画定位十字线
                int snap = 50;
                g.DrawLine(new Pen(Color.DarkCyan, 1), x, y - snap, x, y + snap);//竖线
                g.DrawLine(new Pen(Color.DarkCyan, 1), x - snap, y, x + snap, y);//横线

                //画定位小交叉线
                g.DrawLine(new Pen(Color.LightGray, 1), x - snap / 3, y - snap / 3, x + snap / 3, y + snap / 3);
                g.DrawLine(new Pen(Color.LightGray, 1), x + snap / 3, y - snap / 3, x - snap / 3, y + snap / 3);

                //画已经绘制的线段,并在拐点处画小叉号
                if (length >= 2)
                {
                    for (int i = 0; i < length - 1; i++)
                    {
                        //画已经绘制的线段
                        g.DrawLine(new Pen(Color.White, 2), point[i].X * Grid, size.Height - point[i].Y * Grid, point[i + 1].X * Grid, size.Height - point[i + 1].Y * Grid);
                        //画小叉号
                        g.DrawLine(new Pen(Color.Gray, 1), point[i].X * Grid - snap / 4, size.Height - point[i].Y * Grid - snap / 4, point[i].X * Grid + snap / 4, size.Height - point[i].Y * Grid + snap / 4);
                        g.DrawLine(new Pen(Color.Gray, 1), point[i].X * Grid + snap / 4, size.Height - point[i].Y * Grid - snap / 4, point[i].X * Grid - snap / 4, size.Height - point[i].Y * Grid + snap / 4);
                    }
                }

                //画正在绘制的线段
                if (length >= 1)
                {
                    g.DrawLine(new Pen(Color.White, 2), point[length - 1].X * Grid, size.Height - point[length - 1].Y * Grid, x, y);
                }

                g.Dispose();
                pictureBoxForce.Image = bmp;
            }
            else
            {
                Bitmap bmp = new Bitmap(size.Width, size.Height);
                Graphics g = Graphics.FromImage(bmp);
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                Brush bush = new SolidBrush(Color.Red);//填充的颜色

                int r = 6;
                //画已经绘制的线段
                if (length >= 2)
                {
                    for (int i = 0; i < length - 1; i++)
                    {

                        if (mouseWheel)
                        {
                            //画已经绘制的线段
                            g.DrawLine(new Pen(Color.Blue, 3), point[i].X * Grid, size.Height - point[i].Y * Grid, point[i + 1].X * Grid, size.Height - point[i + 1].Y * Grid);
                            //在拐点处画圆
                            g.FillEllipse(bush, (point[i].X * Grid) - r, size.Height - (point[i].Y * Grid) - r, 2 * r, 2 * r);
                        }
                        else
                        {
                            //画已经绘制的线段
                            g.DrawLine(new Pen(Color.Blue, 3), point[i].X * Grid, size.Height - point[i].Y * Grid, point[i + 1].X * Grid, size.Height - point[i + 1].Y * Grid);
                            //在拐点处画圆
                            g.FillEllipse(bush, point[i].X * Grid - r, size.Height - point[i].Y * Grid - r, 2 * r, 2 * r);
                        }
                    }
                }
                if (length != 0)
                {
                    //在拐点处画圆
                    int i = length - 1;
                    if (mouseWheel)
                        g.FillEllipse(bush, (point[i].X * Grid) - r, (size.Height - point[i].Y * Grid - r), 2 * r, 2 * r);
                    else
                        g.FillEllipse(bush, point[i].X * Grid - r, size.Height - point[i].Y * Grid - r, 2 * r, 2 * r);

                }
                mouseWheel = false;
                g.Dispose();
                pictureBoxForce.Image = bmp;
            }
        }

        private void Form1_ResizeEnd(object sender, EventArgs e)
        {

        }

        private void button_saveRoute_Click(object sender, EventArgs e)
        {
            if (length > 1)
            {
                SaveFileDialog file1 = new SaveFileDialog();
                file1.Filter = "文本文件|*.xml";
                if (file1.ShowDialog() == DialogResult.OK)
                {
                    XmlTextWriter writer = new XmlTextWriter(file1.FileName, null);
                    //使用自动缩进便于阅读
                    writer.Formatting = Formatting.Indented;

                    //写入根元素
                    writer.WriteStartElement("Flash参数");

                    writer.WriteStartElement("系统参数");
                    //加入子元素
                    for (int i = 0; i < 101; i++)
                    {
                        writer.WriteStartElement("参数" + i.ToString());
                        writer.WriteElementString("参数号", i.ToString());
                        writer.WriteElementString("参数名", "");
                        if (i == 99)//路径数
                        {
                            writer.WriteElementString("当前值", "1");
                            writer.WriteElementString("设定值", "1");
                        }
                        else if (i == 96)//站点包含信息数
                        {
                            writer.WriteElementString("当前值", "10");
                            writer.WriteElementString("设定值", "10");
                        }
                        else
                        {
                            writer.WriteElementString("当前值", "0");
                            writer.WriteElementString("设定值", "0");
                        }

                        writer.WriteElementString("描述", "");
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();


                    writer.WriteStartElement("路径参数");
                    //加入子元素
                    for (int i = 0; i < 1; i++)
                    {
                        writer.WriteStartElement("路径" + (i + 1).ToString());
                        for (int j = 0; j < length; j++)
                        {
                            writer.WriteStartElement("站点" + (j + 1).ToString());
                            //站点信息  

                            writer.WriteStartElement("参数0");
                            writer.WriteElementString("参数号", "1");
                            writer.WriteElementString("参数名", " ");
                            writer.WriteElementString("当前值", "0");
                            writer.WriteElementString("设定值", "0");
                            writer.WriteElementString("描述", "X坐标，单位CM");
                            writer.WriteEndElement();

                            writer.WriteStartElement("参数1");
                            writer.WriteElementString("参数号", "2");
                            writer.WriteElementString("参数名", "X");
                            writer.WriteElementString("当前值", NewPoint[j].X.ToString());
                            writer.WriteElementString("设定值", NewPoint[j].X.ToString());
                            writer.WriteElementString("描述", "X坐标，单位CM");
                            writer.WriteEndElement();

                            writer.WriteStartElement("参数2");
                            writer.WriteElementString("参数号", "3");
                            writer.WriteElementString("参数名", "Y");
                            writer.WriteElementString("当前值", NewPoint[j].Y.ToString());
                            writer.WriteElementString("设定值", NewPoint[j].Y.ToString());
                            writer.WriteElementString("描述", "Y坐标，单位CM");
                            writer.WriteEndElement();


                            for (int k = 0; k < 2; k++)
                            {
                                writer.WriteStartElement("参数" + (k + 3).ToString());
                                writer.WriteElementString("参数号", (k + 4).ToString());
                                writer.WriteElementString("参数名", "未定义");
                                writer.WriteElementString("当前值", "0");
                                writer.WriteElementString("设定值", "0");
                                writer.WriteElementString("描述", "");
                                writer.WriteEndElement();
                            }

                            writer.WriteStartElement("参数5");
                            writer.WriteElementString("参数号", "6");
                            writer.WriteElementString("参数名", "动作");
                            if (j == (length - 1))
                            {
                                writer.WriteElementString("当前值", "1");
                                writer.WriteElementString("设定值", "1");
                            }
                            else
                            {
                                writer.WriteElementString("当前值", "0");
                                writer.WriteElementString("设定值", "0");
                            }

                            writer.WriteElementString("描述", "0:保持 1:停止 2:前进");
                            writer.WriteEndElement();

                            for (int k = 0; k < 5; k++)
                            {
                                writer.WriteStartElement("参数" + (k + 7).ToString());
                                writer.WriteElementString("参数号", (k + 8).ToString());
                                writer.WriteElementString("参数名", "未定义");
                                writer.WriteElementString("当前值", "0");
                                writer.WriteElementString("设定值", "0");
                                writer.WriteElementString("描述", "");
                                writer.WriteEndElement();
                            }

                            writer.WriteEndElement();
                        }
                        writer.WriteEndElement();
                    }

                    writer.WriteEndElement();
                    //关闭根元素，并书写结束标签
                    writer.WriteEndElement();
                    //将XML写入文件并且关闭XmlTextWriter
                    writer.Close();
                }
            }
            else
            {
                MessageBox.Show("没有路径可保存到PC！", "提示");
            }
        }

        private void pictureBoxForce_MouseEnter(object sender, EventArgs e)
        {
            //控件获取焦点后滚轮事件才有效
            this.pictureBoxForce.Focus();
        }

        private void serialPort1_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            Byte[] InputBuf = new Byte[128];
            try
            {
                //Thread.Sleep(20);
                serialPort1.Read(InputBuf, 0, serialPort1.BytesToRead);                                //读取缓冲区的数据直到“}”即0x7D为结束符  
                string str = System.Text.Encoding.Default.GetString(InputBuf);
                str = str.Replace("\0", "");

                //获取有效坐标
                int IndexofA = str.IndexOf("T");
                int IndexofB = str.IndexOf("W");
                string Ru = str.Substring(IndexofA + 1, IndexofB - IndexofA - 1);

                int x = int.Parse(Ru.Substring(Ru.IndexOf("x") + 1, Ru.IndexOf("y") - Ru.IndexOf("x") - 1));
                int y = int.Parse(Ru.Substring(Ru.IndexOf("y") + 1, Ru.Length - Ru.IndexOf("y") - 1));

                //设置坐标
                if (x < 0)
                {
                    x = 0;
                }
                if (y < 0)
                {
                    y = 0;
                }



                this.Invoke(new MethodInvoker(delegate
                {
                    textBox_port.AppendText(str + "\r\n");
                    textBox_port.AppendText("X:" + x.ToString() + " Y:" + y.ToString() + "\r\n");
                    pictureBox1.Location = new Point((x+10) * Grid - 17, size.Height - (y+10) * Grid - 17);
                }));
            }
            catch (TimeoutException ex)         //超时处理  
            {
                MessageBox.Show(ex.ToString());
            }
        }
    }
}
