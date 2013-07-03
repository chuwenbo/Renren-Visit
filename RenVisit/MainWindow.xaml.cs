using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Runtime.InteropServices;
using System.Timers;
using System.Reflection;
using System.Threading; 

namespace RenVisit
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        #region 变量定义
        private String visitUrl;  //访问的URL 
        //flag 0:冬妮娅  1：保尔
        int flag = 0;
        int counts = 0;  //刷新次数
        //定时器：
        System.Timers.Timer timer1 = new System.Timers.Timer();
        System.Timers.Timer timerClear = new System.Timers.Timer(); 
        bool isValid = false; //访问账户是否有效 
        Thread td;
        List<string[]> loginInfo = new List<string[]>(); //登陆人信息
        IntPtr pHandle; //主线程句柄
        #endregion 

        [DllImport("wininet.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool InternetSetCookie(string lpszUrlName, string lbszCookieName, string lpszCookieData);

        [DllImport("KERNEL32.DLL", EntryPoint = "SetProcessWorkingSetSize", SetLastError = true, CallingConvention = CallingConvention.StdCall)]
        internal static extern bool SetProcessWorkingSetSize(IntPtr pProcess, int dwMinimumWorkingSetSize, int dwMaximumWorkingSetSize);

        [DllImport("KERNEL32.DLL", EntryPoint = "GetCurrentProcess", SetLastError = true, CallingConvention = CallingConvention.StdCall)]
        internal static extern IntPtr GetCurrentProcess();

        #region webbrowser相关

        void webBrowser1_LoadCompleted(object sender, NavigationEventArgs e)
        {
            //Console.WriteLine("webBrowser1_LoadCompleted:" + Thread.CurrentThread.ManagedThreadId.ToString());
            //IntPtr pHandle = GetCurrentProcess();
            //SetProcessWorkingSetSize(System.Diagnostics.Process.GetProcessesByName("RenVisit.vshost")[0].Handle, -1, -1);
            //int t = counts;
            //while (t > 0)
            //{
            //    IntPtr pHandle = GetCurrentProcess();
            //    SetProcessWorkingSetSize(pHandle, -1, -1);
            //    GC.Collect();
            //    GC.WaitForPendingFinalizers();
            //    GC.Collect();
            //    t--;
            //}

            if (webBrowser1.Document != null)
            {
                Dictionary<string, string> userInfo = new Dictionary<string, string>();
                List<Dictionary<string, string>> userInfos = new List<Dictionary<string, string>>();
                //mshtml.IHTMLDocument2 doc1 = (mshtml.IHTMLDocument2)webBrowser1.Document;
                mshtml.HTMLDocument doc2 = webBrowser1.Document as mshtml.HTMLDocument;
                mshtml.IHTMLElement email = doc2.getElementById("email");
                mshtml.IHTMLElement psw = doc2.getElementById("password");
                mshtml.IHTMLElement login = doc2.getElementById("login");

                mshtml.IHTMLElement userpic = doc2.getElementById("userpic"); //判断当前页面是否为正在刷新的页面
                if (email != null && !webBrowser1.Source.AbsoluteUri.Contains("SysHome.do")) //登陆
                {
                    timer1.Stop();
                    email.setAttribute("value", loginInfo[flag][0], 0);
                    psw.setAttribute("value", loginInfo[flag][1], 0);
                    login.click();
                }
                else if (email == null && userpic == null)
                {
                    webBrowser1.Navigate(new Uri(visitUrl));
                }
                else if (email == null && userpic != null)
                {
                    #region
                    if (!isValid) //校验
                    {
                        isValid = true;
                        mshtml.IHTMLElementCollection valids = doc2.getElementsByTagName("p");
                        foreach (mshtml.IHTMLElement item in valids)
                        {
                            if (item.innerText == "该用户不存在")
                            {
                                isValid = false;
                                MessageBox.Show("该用户不存在");
                                timer1.Stop();
                                break;
                            }
                        }
                    }
                    if (isValid)
                    {
                        //退出
                        mshtml.IHTMLElementCollection logout = doc2.getElementsByTagName("a");
                        foreach (mshtml.IHTMLElement item in logout)
                        {
                            if (item.className == "logout") { item.click(); break; }
                        }
                        if (flag < loginInfo.Count - 1)
                            flag++;
                        else
                            flag = 0;

                        counts++;
                        txtCounts.Text = counts.ToString();
                        SetProcessWorkingSetSize(pHandle, -1, -1);
                    }
                    #endregion
                }
                else
                {
                    timer1.Start();
                    //webBrowser1.Dispose();
                    td.Abort();
                    td = null;
                }
            }
        }

        void webBrowser1_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            this.SuppressScriptErrors(webBrowser1, true);
        }

        //屏蔽脚本错误
        public void SuppressScriptErrors(WebBrowser webBrowser, bool Hide)
        {
            FieldInfo fiComWebBrowser = typeof(WebBrowser).GetField("_axIWebBrowser2", BindingFlags.Instance | BindingFlags.NonPublic);
            if (fiComWebBrowser == null) return;

            object objComWebBrowser = fiComWebBrowser.GetValue(webBrowser);
            if (objComWebBrowser == null) return;

            objComWebBrowser.GetType().InvokeMember("Silent", BindingFlags.SetProperty, null, objComWebBrowser, new object[] { Hide });
        }

        //定时设置线程空间，webbrowser内存占用太大
        void timerClear_Elapsed(object sender, ElapsedEventArgs e)
        {
            SetProcessWorkingSetSize(pHandle, -1, -1);
        }

        //定时器 刷新界面
        void timer1_Elapsed(object sender, ElapsedEventArgs e)
        {
            SetProcessWorkingSetSize(pHandle, -1, -1);
            td = new Thread(new ThreadStart(() =>
            {
                IntPtr pHandle1 = GetCurrentProcess();
                SetProcessWorkingSetSize(pHandle1, -1, -1);
                //Console.WriteLine("td:"+Thread.CurrentThread.ManagedThreadId.ToString());
                webBrowser1.Dispatcher.Invoke(new Action(() =>
                {
                    SetProcessWorkingSetSize(System.Diagnostics.Process.GetProcessesByName("RenVisit.vshost")[0].Handle, -1, -1);
                    SetProcessWorkingSetSize(pHandle, -1, -1);
                    //Console.WriteLine("Dispatcher:" + Thread.CurrentThread.ManagedThreadId.ToString());
                    //Console.WriteLine("Dispatcher1:" + System.Diagnostics.Process.GetProcessesByName("RenVisit.vshost")[0].Handle.ToInt32().ToString());

                    //Console.WriteLine("Dispatcher2:" + pHandle.ToInt32().ToString()); 

                    //if (counts != 0 && counts % 10 == 0)
                    //{ 
                    //    webBrowser1 = new WebBrowser();

                    //    girdBrowser.Children.Insert(0, webBrowser1);

                    //    webBrowser1.Navigate("http://www.renren.com");
                    //    webBrowser1.Navigating -= webBrowser1_Navigating;
                    //    webBrowser1.LoadCompleted -= webBrowser1_LoadCompleted;
                    //    webBrowser1.Navigating += new NavigatingCancelEventHandler(webBrowser1_Navigating);
                    //    webBrowser1.LoadCompleted += new LoadCompletedEventHandler(webBrowser1_LoadCompleted);
                    //    webBrowser1.Dispose();
                    //}
                    //else
                    //{
                    webBrowser1.Navigate("http://www.renren.com");
                    //}

                    if (timer1.Interval != 15000) timer1.Interval = 15000;
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                }));
            }));
            td.Start();

            return;
            #region
            /*
            //模拟 cookie 登陆
            if (flag == 0)//冬妮娅
            {

                Dispatcher.Invoke(new Action(() =>
                {
                    InternetSetCookie(visitUrl, "p", dongniyaCookie[0]);
                    InternetSetCookie(visitUrl, "t", dongniyaCookie[1]); 
                    //webBrowser1.Navigate("http://www.renren.com");
                    //Thread.Sleep(3000);
                    webBrowser1.Navigate(visitUrl); 
                    flag = 1;
                }));
                
            }
            else if (flag == 1)
            {
                Dispatcher.Invoke(new Action(() =>
               {
                   InternetSetCookie(visitUrl, "p", baoerCookie[0]);
                   InternetSetCookie(visitUrl, "t", baoerCookie[1]); 
                   //webBrowser1.Navigate("http://www.renren.com");
                   //Thread.Sleep(3000);
                   webBrowser1.Navigate(visitUrl); 
                   flag = 2;
               }));
            }
            else
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    InternetSetCookie(visitUrl, "p", talltreeCookie[0]);
                    InternetSetCookie(visitUrl, "t", talltreeCookie[1]); 
                    //webBrowser1.Navigate("http://www.renren.com");
                    //Thread.Sleep(3000);
                    webBrowser1.Navigate(visitUrl); 
                    flag = 0;
                }));
            }
             */
            #endregion
        }
        #endregion

        #region 界面事件

        //加载界面
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            pHandle = GetCurrentProcess();
            buttonClose.IsEnabled = false;
            textBox1.Text = "87419734";
            webBrowser1.IsEnabled = false;
            //访客账号信息 僵尸账户
            Dictionary<string, string> userInfo = new Dictionary<string, string>();
            userInfo.Add("568373468@qq.com", "chu19860222");
            userInfo.Add("1601247756@qq.com", "chu19860222");
            userInfo.Add("tall_tree@foxmail.com", "chu19860222");
            userInfo.Add("2939632189@qq.com", "chu19860222");
            userInfo.Add("idiot_0001@163.com ", "chu19860222");
            userInfo.Add("idiot_0003@163.com ", "chu19860222");
            //userInfo.Add("idiot_0004@163.com ", "chu19860222"); 被禁用
            userInfo.Add("idiot_0005@163.com", "123456");
            userInfo.Add("idiot_0006@163.com", "123456");
            userInfo.Add("idiot_0007@163.com", "123456");

            userInfo.Add("spring_00008@163.com", "chu19860222");
            userInfo.Add("spring_00009@163.com", "chu19860222");

            userInfo.Add("spring_00001@sina.cn", "123456");
            userInfo.Add("spring_00002@sina.cn", "123456");

            userInfo.Add("idiot_0001@sina.cn", "chu19860222");
            userInfo.Add("idiot_0002@sina.cn", "chu19860222");


            foreach (var item in userInfo)
            {
                string[] login = new string[] { item.Key, item.Value };
                loginInfo.Add(login);
            }
        }
        //清楚缓存
        private void buttonClear_Click(object sender, RoutedEventArgs e)
        {
            pHandle = GetCurrentProcess();
            SetProcessWorkingSetSize(pHandle, -1, -1);
        }
        //停止刷新
        private void buttonClose_Click(object sender, RoutedEventArgs e)
        {

            //webBrowser1.Navigating -= webBrowser1_Navigating;
            //webBrowser1.LoadCompleted -= webBrowser1_LoadCompleted;
            timer1.Stop();
            button1.IsEnabled = true;
            buttonClose.IsEnabled = false;
        }
        //开始刷新
        private void button1_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(textBox1.Text.Trim()))
            {
                MessageBox.Show("请输入ID");
                return;
            }
            pHandle = GetCurrentProcess();

            //判断ID是否有效
            counts = 0;
            flag = 0;
            txtCounts.Text = counts.ToString();

            visitUrl = "http://www.renren.com/" + textBox1.Text.Trim() + "/profile";

            webBrowser1.Navigating += new NavigatingCancelEventHandler(webBrowser1_Navigating);
            webBrowser1.LoadCompleted += new LoadCompletedEventHandler(webBrowser1_LoadCompleted);

            timer1.Interval = 1000;
            timer1.Elapsed += new ElapsedEventHandler(timer1_Elapsed);
            timer1.AutoReset = true;//设置是执行一次（false）还是一直执行(true)； 
            timer1.Enabled = true;//是否执行System.Timers.Timer.Elapsed事件；
            timer1.Start();


            timerClear.Interval = 3000;
            timerClear.Elapsed += new ElapsedEventHandler(timerClear_Elapsed);
            timerClear.AutoReset = true;//设置是执行一次（false）还是一直执行(true)； 
            timerClear.Enabled = true;//是否执行System.Timers.Timer.Elapsed事件；
            timerClear.Start();

            button1.IsEnabled = false;
            buttonClose.IsEnabled = true;
        } 

        #endregion
    } 
}
