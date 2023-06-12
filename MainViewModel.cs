using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using NAudio.Wave;
using Newtonsoft.Json;
using Services;
using NAudio;
using System.Windows.Data;
using NAudio.Wave.SampleProviders;

namespace MicPlay
{
    public partial class MainViewModel : ViewModelBase
    {
        string sourceFolder;
        MainWindow main;
        string _status, _status2;
        List<SoundItem> _items;
        IWavePlayer outputDevice;

        public bool CanContinue { get; private set; }
        public ObservableCollection<SoundItem> SentList { get; set; }
        public MainViewModel(MainWindow _main)
        {
            main = _main;
            sourceFolder = System.AppDomain.CurrentDomain.BaseDirectory;
            uiSynchronizationContext = SynchronizationContext.Current;
            ControlsEnabled = true;
            // Commands initialization
            this.PlayCommand = new RelayCommand(this.Play);
            this.AddFilesCommand = new RelayCommand(this.AddFiles);
            this.ClearFilesCommand = new RelayCommand(this.ClearFiles);
            this.ClearStatusCommand = new RelayCommand(this.ClearStatus);
            this.CloseCommand = new RelayCommand(this.CloseMain);
            this.MinimizeToTrayCommand = new RelayCommand(this.MinimizeToTray);

            IList<DeviceEntry> list = new List<DeviceEntry>();

            for (int n = -1; n < WaveOut.DeviceCount; n++)
                list.Add(new DeviceEntry(n, WaveOut.GetCapabilities(n).ProductName));
            
            _deviceEntries = new CollectionView(list);

            //WaveOutDevice = new WaveOutEvent() { DeviceNumber = 0 };
            CanContinue = true;
            if (!CanContinue)
                return;

            rnd = new Random();

            var tsk = Task.Factory.StartNew(() => ReadItems());
        }

        private SoundItem _currentItem;
        public SoundItem CurrentItem
        {
            get => _currentItem;
            set
            {
                _currentItem = value;
                OnPropertyChanged(nameof(CurrentItem));
            }
        }

        public ICommand PlayCommand { get; set; }
        private void Play(object obj)
        {
            var playDevice = new WaveOutEvent() ;
            AudioFileReader audioFileReader = new AudioFileReader(CurrentItem.FileName);
            playDevice.Init(audioFileReader);
            playDevice.Play();
        }
        private void ReadItems()
        {
            var filename = $"{sourceFolder}\\files.json";
            if (File.Exists(filename))
            {
                using (StreamReader file = File.OpenText(filename))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    _items = (List<SoundItem>)serializer.Deserialize(file, typeof(List<SoundItem>));
                    foreach (var i in _items)
                        i.uiSynchronizationContext = uiSynchronizationContext;
                }
            }
            else
                _items = new List<SoundItem>();

            SentList = new ObservableCollection<SoundItem>(_items);
            OnPropertyChanged(nameof(SentList));
        }

        private void WriteTasks()
        {
            using (StreamWriter file = File.CreateText($"{sourceFolder}\\files.json"))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, _items);
            }
        }

        internal void PlayRandom()
        {
            Status = "Key was pressed";
            if (Device == null)
            {
                Status = "Select audio device";
                return;
            }
            PlayAudioTask();
        }

        public ICommand AddFilesCommand { get; set; }
        private async void AddFiles(object parameter)
        {
            FileDialogViewModel fdvm = new FileDialogViewModel
            {
                Filter = "Audio files|*.mp3;*.wav",
                Multiselect = true
            };

            fdvm.OpenCommand.Execute(null);
            if (fdvm.FileNames == null)
                return;

            if (_items==null)
                _items = new List<SoundItem>();

            foreach (var item in fdvm.FileNames)
            {
                var i = new SoundItem(this);
                i.FileName = item;
                _items.Add(i);
            }

            SentList = new ObservableCollection<SoundItem>(_items);
            OnPropertyChanged(nameof(SentList));
            WriteTasks();
        }

        public ICommand ClearStatusCommand { get; set; }
        private async void ClearStatus(object parameter)
        {
            if (_items != null)
            {
                foreach (var item in _items)
                    item.PlayedAlready = false;
            }
            OnPropertyChanged(nameof(SentList));
            WriteTasks();
        }

        public ICommand ClearFilesCommand { get; set; }
        private async void ClearFiles(object parameter)
        {
            _items = new List<SoundItem>();
            SentList = new ObservableCollection<SoundItem>(_items);
            OnPropertyChanged(nameof(SentList));
            WriteTasks();
        }

        public ICommand MinimizeToTrayCommand { get; set; }
        private void MinimizeToTray(object parameter)
        {
            main.HideToTray();
        }

        public ICommand CloseCommand { get; set; }
        private void CloseMain(object parameter)
        {
            main.Close();
        }

        private void PlayAudioTask()
        {
            if (outputDevice != null && outputDevice.PlaybackState == PlaybackState.Playing)
                return;

            if (_items == null || !_items.Any())
                return;

            int next = -1;
            while (true)
            {
                next = rnd.Next(_items.Count);
                if (!_items[next].PlayedAlready)
                    break;
            }
            if (next == -1)
                return;
            try
            {
                /*var inPath = _items[next].FileName;
                var semitone = Math.Pow(2, 1.0 / 12);
                var upOneTone = semitone * semitone;
                var downOneTone = 1.0 / upOneTone;
                using (var reader = new MediaFoundationReader(inPath))
                {
                    var pitch = new SmbPitchShiftingSampleProvider(reader.ToSampleProvider());
                    using (var device = new WaveOutEvent())
                    {
                        pitch.PitchFactor = (float)downOneTone; // or downOneTone
                                                                // just playing the first 10 seconds of the file
                        device.Init(pitch.Take(TimeSpan.FromSeconds(10)));
                        device.Play();
                        while (device.PlaybackState == PlaybackState.Playing)
                        {
                            Thread.Sleep(500);
                        }
                    }
                }*/
                
                outputDevice = new WaveOutEvent() { DeviceNumber = Device.Id };
                AudioFileReader audioFileReader = new AudioFileReader(_items[next].FileName);
                //var sampleChannel = new SampleChannel(audioFileReader, true);
                //sampleChannel.Volume = 0.1f;
                
                var volumeSampleProvider = new VolumeSampleProvider(audioFileReader.ToSampleProvider());
                //Min dB=-48
                var dbVolume = -12f;
                var volume = (float)Math.Pow(10, dbVolume / 20);
                volumeSampleProvider.Volume = volume; 

                outputDevice.Init(volumeSampleProvider);
                outputDevice.Play();
                _items[next].PlayedAlready = true;
                _items[next].PlayedOn=DateTime.Now;
            }
            catch (Exception e)
            {
                Status = $"Problem playing file:{_items[next].FileName}, Message: {e.Message}";
            }
        }

        public string Status
        {
            get => _status;
            set
            {
                if (_status == value) return;
                if (string.IsNullOrEmpty(value))
                    _status = string.Empty;
                else
                    _status += value + Environment.NewLine;

                OnPropertyChanged(nameof(Status));
            }
        }

        public string Status2
        {
            get => _status2;
            set
            {
                if (_status2 == value) 
                    return;

                _status2 = value;

                OnPropertyChanged(nameof(Status2));
            }
        }
        public Random rnd { get; private set; }
        private readonly CollectionView _deviceEntries;
        private DeviceEntry _deviceEntry;
        public CollectionView DeviceEntries => _deviceEntries;
        public DeviceEntry Device
        {
            get => _deviceEntry;
            set
            {
                if (_deviceEntry == value) return;
                _deviceEntry = value;
                OnPropertyChanged(nameof(Device));
            }
        }
    }

    public class DeviceEntry
    {
        public string Name { get; set; }
        public int Id { get; set; }

        public DeviceEntry(int id, string name)
        {
            Name = name;
            Id = id;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
