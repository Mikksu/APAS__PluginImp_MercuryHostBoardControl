using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MercuryHostBoard
{
    public class DataPerChannel : INotifyPropertyChanged
    {
        #region Variables

        public event PropertyChangedEventHandler PropertyChanged;

        double refOpticalPower = 0;
        double resp = 0;
        private double rssi;

        #endregion

        public double RSSI
        {
            get
            {
                return rssi;
            }
            internal set
            {
                // calculate resp. with the RSSI
                double mw = Math.Pow(10, Reference_dBm / 10); // convert ref. optical power from dBm to mW.
                rssi = value / 4096.0 * 2.5 / 6.4 * 32;    // calculate RSSI with the sampling ADC value.
                Responsibility = rssi / mw;  // calculate resp.

                OnPropertyChanged();
            }
        }

        public double Reference_dBm
        {
            get
            {
                return refOpticalPower;
            }
            set
            {
                refOpticalPower = value;
                OnPropertyChanged();
            }
        }

        public double Responsibility
        {
            get
            {
                return resp;
            }
            private set
            {
                resp = value;
                OnPropertyChanged();
            }
        }

        private void OnPropertyChanged([CallerMemberName]string PropertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
        }
    }
}
