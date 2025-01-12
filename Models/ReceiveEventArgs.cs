using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChatServer.Models;
using SendInfoHeader;

namespace ChatServer.Models {
    /// <summary>
    /// Класс для передачи десериализированного контейнера при 
    /// возникновении события получения сетевых данных.
    /// </summary>
    class ReceiveEventArgs: EventArgs {
        private SendInfo _sendinfo;

        public ReceiveEventArgs(SendInfo sendinfo) {
            _sendinfo = sendinfo;
        }

        public SendInfo sendInfo {
            get { return _sendinfo; }
        }
    }
}
