
// -----------------------------------------------------------------------
// <copyright file="FtpEndpoint.cs" company="Agora SA">
// <legal>Copyright (c) Development IT, kwiecien 2020</legal>
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent
{
    using System;

    /// <summary>
    /// Endpoint FTP
    /// </summary>
    public class FtpEndpoint : EditableItem<FtpEndpointModel>
    {
        #region constructors
        /// <summary>
        /// Inicjalizacja dodawanego obiektu
        /// </summary>
        /// <param name="endpoint">Klucz rodzica</param>
        public FtpEndpoint()
        {
            Protocol = eFtpProtocol.FTP;
            Direction = eFtpDirection.Get;
            Mode = eFtpTransferMode.Binary;
        }
        #endregion

        #region properties
        public string Host {
            get { return _currentData.host; }
            set {
                if (_currentData.host != value) {
                    _currentData.host = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public string Userid {
            get { return _currentData.uid; }
            set {
                if (_currentData.uid != value) {
                    _currentData.uid = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public string Password {
            get { return _currentData.pwd; }
            set {
                if (_currentData.pwd != value) {
                    _currentData.pwd = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public string RemoteDirectory {
            get { return _currentData.remDir; }
            set {
                if (_currentData.remDir != value) {
                    _currentData.remDir = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public string LocalDirectory {
            get { return _currentData.locDir; }
            set {
                if (_currentData.locDir != value) {
                    _currentData.locDir = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public int XX {
            get { return _currentData.xx; }
            set {
                if (_currentData.xx != value) {
                    _currentData.xx = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public int Instance {
            get { return _currentData.insXX; }
            set {
                if (_currentData.insXX != value) {
                    _currentData.insXX = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public eFtpDirection Direction {
            get { return _currentData.direction; }
            set {
                if (_currentData.direction != value) {
                    _currentData.direction = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public eFtpTransferMode Mode {
            get { return _currentData.mode; }
            set {
                if (_currentData.mode != value) {
                    _currentData.mode = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public eFtpProtocol Protocol {
            get { return _currentData.protocol; }
            set {
                if (_currentData.protocol != value) {
                    _currentData.protocol = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public DateTime LastSyncTime {
            get { return _currentData.lastSync; }
            set {
                if (_currentData.lastSync != value) {
                    _currentData.lastSync = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public DateTime NextSyncTime {
            get { return _currentData.nextSync; }
            set {
                if (_currentData.nextSync != value) {
                    _currentData.nextSync = value;
                    NotifyPropertyChanged();
                }
            }
        }
        #endregion
    }
}
