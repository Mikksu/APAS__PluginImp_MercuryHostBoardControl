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

            TestResult = new DataPerChannel[MaxChannel];

            // Set Ref. optical power to default value
            for (int i = 0; i < MaxChannel; i++)
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

        public override string Usage => "Irixi Mercury Host Board Control App";

        /// <summary>
        /// 最大测量通道。
        /// </summary>
        public override int MaxChannel => 4;

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
            await Task.CompletedTask;
        }

        protected override void Dispose(bool disposing)
        {
            StopBackgroundTask();
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
                return TestResult[Channel].Responsibility;
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
                return TestResult[0].Responsibility;

            }
            catch(Exception ex)
            {
                throw ex;
            }
        }

        public override bool Init()
        {
            hostBoard = new AAB_HostBoard();
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
            for (int i = 0; i < MaxChannel; i++)
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
