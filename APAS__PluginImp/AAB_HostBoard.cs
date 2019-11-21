using System;
using TestSystem.TestLibrary.Instruments;

namespace MercuryHostBoard
{
    public   class AAB_HostBoard
    {
        public const int MAX_CHANNEL = 4;

        Inst_AAB_HostBoard inst_ABB;

        byte SlaveAdd = 0x50;

        #region Construcotrs

        public AAB_HostBoard()
        {
           //  ReferenceOpticalPower_dBm = new double[MAX_CHANNEL] { 1.44, -0.45, 2.67, 0.59 };
        }

        #endregion


        #region Properties

        // public double[] ReferenceOpticalPower_dBm { get; }

        #endregion

        public void Init()
        {
            try
            {
                inst_ABB = new Inst_AAB_HostBoard();
                inst_ABB.EnterEngMod();

                for (int i = 0; i < MAX_CHANNEL; i++)
                {
                    inst_ABB.SetDCCurrent(i, 0);
                    System.Threading.Thread.Sleep(100);
                    inst_ABB.SetModulationCurrent(i, 0);
                }
                inst_ABB.SetAPCLoop(false);
                inst_ABB.DDM_Enable();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public double[] ReadRSSI()
        {

            double[] rssi = new double[MAX_CHANNEL];
            for (int i = 0; i < MAX_CHANNEL; i++)
            {
                byte Page = byte.Parse((0x81 + i).ToString());
                inst_ABB.SelectPage(SlaveAdd, Page);
                byte Data1 = inst_ABB.I2CMasterSingleReadModule(SlaveAdd, 0x92);
                byte Data2 = inst_ABB.I2CMasterSingleReadModule(SlaveAdd, 0x93);
                rssi[i] = Convert.ToDouble(Data1 * 256 + Data2);
            }

            return rssi;

        }
    }
}
