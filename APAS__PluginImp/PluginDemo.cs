using APAS__PluginContract.ImplementationBase;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using SystemServiceContract.Core;

namespace MercuryHostBoard
{
    public class PluginDemo : PluginMultiChannelMeasurableEquipment
    {

        #region Variables

        readonly string param1 = "";

        AAB_HostBoard hostBoard;
        int rssiMaxChannel = 4;

        readonly object hostBoardLock = new object();

        CancellationTokenSource cts;
        CancellationToken ct;

        double resp_diff = 0;


        #endregion

        #region Constructors

        public PluginDemo(ISystemService APASService) : base(Assembly.GetExecutingAssembly(), APASService)
        {
            var config = GetAppConfig();

            param1 = config.AppSettings.Settings["Param1"].Value;

            TestResult = new DataPerChannel[rssiMaxChannel];

            // Set Ref. optical power to default value
            for (int i = 0; i < rssiMaxChannel; i++)
            {
                TestResult[i] = new DataPerChannel();

                if (double.TryParse(config.AppSettings.Settings[$"Ref_dBm_{i}"].Value, out double refPower))
                    TestResult[i].Reference_dBm = refPower;
                else
                    TestResult[i].Reference_dBm = 0;
            }
            
            this.UserView = new PluginDemoView();
            this.UserView.DataContext = this;

            this.HasView = true;

        }

        #endregion

        #region Properties

        public override string Name => "MercuryHostBoard";

        public override string Usage => 
            "Irixi Mercury Host板控制程序。\n" +
            "通道0-3表示1-4通道的响应度。\n" +
            "通道4表示通道平衡性插值。";

        /// <summary>
        /// 最大测量通道。
        /// <para>通道5表示通道平衡差值。</para>
        /// </summary>
        public override int MaxChannel => rssiMaxChannel + 1;

        public DataPerChannel[] TestResult { get; }

        public double RespDiff
        {
            get
            {
                return resp_diff;
            }
            private set
            {
                resp_diff = value;
                OnPropertyChanged();
            }
        }
    
        #endregion

        #region Methods

        public sealed override async Task<object> Execute(object args)
        {
            await Task.CompletedTask;
            return null;
        }

        /// <summary>
        /// Switch to the specific channel.
        /// </summary>
        /// <param name="param">[int] The specific channel.</param>
        /// <returns></returns>
        public sealed override async Task Control(string param)
        {
            if (param == "RECONN")
            {
                StopBackgroundTask();

                hostBoard.DeInit();
                Thread.Sleep(200);
                hostBoard.Init();
                StartBackgroundTask();
            }

            await Task.CompletedTask;
        }

        protected override void Dispose(bool disposing)
        {
            StopBackgroundTask();

            hostBoard.DeInit();
        }

        /// <summary>
        /// 获取指定通道的测量值。
        /// </summary>
        /// <param name="Channel">0至<see cref="MaxChannel"/>MaxChannel - 1</param>
        /// <returns>double</returns>
        public override object Fetch(int Channel)
        {
            if (Channel >= 0 && Channel < MaxChannel)
            {
                double[] rssi = null;

                lock (hostBoardLock)
                {
                    rssi = hostBoard.ReadRSSI();
                }

                FlushTestResult(rssi);

                if (Channel < rssiMaxChannel)
                    return TestResult[Channel].Responsibility;
                else
                    return this.RespDiff;
            }
            else
                throw new ArgumentOutOfRangeException("Channel");
        }

        public override object[] FetchAll()
        {
            double[] rssi = null;

            lock (hostBoardLock)
            {
                rssi = hostBoard.ReadRSSI();
            }

            FlushTestResult(rssi);

            return TestResult.Select(a => a.Responsibility).Cast<object>().ToArray();
        }

        public override object Fetch()
        {
            try
            {
                double[] rssi = null;

                lock(hostBoardLock)
                {
                    rssi = hostBoard.ReadRSSI();
                }

                FlushTestResult(rssi);
                return this.RespDiff;

            }
            catch(Exception ex)
            {
                throw ex;
            }
        }

        public override bool Init()
        {
            hostBoard = new AAB_HostBoard();

            // Deinit the host board since it is not connectable if it's not disconnected correctly.
            hostBoard.DeInit();

            hostBoard.Init();

            IsInitialized = true;
            IsEnabled = true;
            StartBackgroundTask();
            return true;
        }

        public override void StartBackgroundTask()
        {
            cts = new CancellationTokenSource();
            ct = cts.Token;

            Task.Run(() =>
            {
                while (true)
                {
                    double[] rssi = null;

                    lock (hostBoardLock)
                    {
                        try
                        {
                            rssi = hostBoard.ReadRSSI();
                        }
                        catch(Exception)
                        {
                            return;
                        }
                    }


                    FlushTestResult(rssi);

                    if (ct.IsCancellationRequested)
                        return;

                    Thread.Sleep(10);

                    if (ct.IsCancellationRequested)
                        return;
                }
            });
        }

        public override void StopBackgroundTask()
        {
            // 结束背景线程
            cts.Cancel();

            //! 延时，确保背景线程正确退出
            Thread.Sleep(200);
        }

        private void FlushTestResult(double[] RSSI)
        {
            for (int i = 0; i < rssiMaxChannel; i++)
            {
                TestResult[i].RSSI = RSSI[i];
            }

            var resp = TestResult.Select(a => a.Responsibility).ToList();
            if (resp != null)
                RespDiff = resp.Max() - resp.Min();
            else
                RespDiff = double.NaN;
        }

        #endregion
    }
}
