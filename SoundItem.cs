using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MicPlay
{
    public class SoundItem : INotifyPropertyChanged
    {
        public SynchronizationContext uiSynchronizationContext;
        private string fileName;
        private DateTime? playedOn;
        private bool isCompleted;

        public string FileName
        {
            get => fileName; set
            {
                fileName = value;
                OnPropertyChanged(nameof(FileName));
            }
        }
        public DateTime? PlayedOn
        {
            get => playedOn; set
            {
                playedOn = value;
                OnPropertyChanged(nameof(PlayedOn));
            }
        }
        public bool PlayedAlready
        {
            get => isCompleted; 
            set
            {
                isCompleted = value;
                OnPropertyChanged(nameof(PlayedAlready));
            }
        }

        public SoundItem(MainViewModel bvm)
        {
            if (bvm != null)
            {
                uiSynchronizationContext = bvm.uiSynchronizationContext;
            }
        }
        public void OnPropertyChanged(string propertyName)
        {
            uiSynchronizationContext.Post(
                _ => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName))
                ,
                null
              );
        }

        #region NotifyPropertyChanged Methods

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}
