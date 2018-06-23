using AForge.Video.DirectShow;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WpfVideoCapture
{
    public class VideoViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public ICommand CommandStart { get; set; }
        public ICommand CommandStop { get; set; }

        private ImageSource _Source;
        public ImageSource Source
        {
            get
            {
                return _Source;
            }
            set
            {
                _Source = value;
                RaisePropertyChanged("Source");
            }
        }

        private VideoCaptureDevice _VideoDevice = null;
        private System.Threading.SynchronizationContext _SyncContext;
        public VideoViewModel()
        {
            _SyncContext = System.Threading.SynchronizationContext.Current;
            CommandStart = new DelegateCommand(Start, null);
            CommandStop = new DelegateCommand(Stop, null);

        }
        private void Start(object o)
        {
            if (_VideoDevice == null)
            {
                var videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                if (videoDevices.Count > 0)
                {
                    _VideoDevice = new VideoCaptureDevice(videoDevices[0].MonikerString);//连接摄像头。
                    _VideoDevice.VideoResolution = _VideoDevice.VideoCapabilities[0];
                }
            }
            _VideoDevice.Start();
            _VideoDevice.NewFrame += _VideoDevice_NewFrame;

        }



        public void Stop(object o = null)
        {
            if (_VideoDevice != null)
            {
                _VideoDevice.NewFrame -= _VideoDevice_NewFrame;

                _VideoDevice.SignalToStop();//.Stop();
            }
        }
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);
        private void _VideoDevice_NewFrame(object sender, AForge.Video.NewFrameEventArgs eventArgs)
        {
            _SyncContext.Send(o =>
            {
                IntPtr hBitmap = eventArgs.Frame.GetHbitmap();
                Source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());

                DeleteObject(hBitmap);

            }, null);
        }

        private void RaisePropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }


    }

    public class DelegateCommand : ICommand
    {

        public Action<object> ExecuteCommand
        { get; set; }


        public Predicate<object> CanExecuteCommand
        {
            get;
            set;
        }

        public DelegateCommand(Action<object> executeCommand, Predicate<object> canExecuteCommand)
        {
            this.ExecuteCommand = executeCommand;
            this.CanExecuteCommand = canExecuteCommand;
        }


        #region 接口
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            if (CanExecuteCommand != null)
            {
                return this.CanExecuteCommand(parameter);
            }
            else
            {
                return true;
            }
        }

        public void Execute(object parameter)
        {
            if (this.ExecuteCommand != null)
            {
                this.ExecuteCommand(parameter);
            }
        }

        #endregion



        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
