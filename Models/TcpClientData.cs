using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.IO;

namespace ChatServer.Models {

    ///////////////////////////////////////////////////////////////////////////
    // ВСПОМОГАТЕЛЬНЫЕ КЛАССЫ ДЛЯ ОРГАНИЗАЦИИ СЕТЕВОЙ РАБОТЫ TCP МОДУЛЯ
    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Класс для организации непрерывного извлечения сетевых данных,
    /// для чего необходимо, как минимум, одновременно TcpClient
    /// и буфер приема.
    /// </summary>
    class TcpClientData {
        internal TcpClient TcpClient { get; set; } = new TcpClient();
        internal byte[] buffer = null;
        internal int size_file = 0;
        internal int numberOfBytesRead;
        internal int length_header = 0;
        public TcpClientData() {
            TcpClient.ReceiveBufferSize = global.MAXBUFFER;
        }
        //---------------------------------------------
        //---------------------------------------------
        internal NetworkStream Net_stream { get; set; }
        //---------------------------------------------
        internal MemoryStream Ms { get; set; } = new MemoryStream();
        //---------------------------------------------

    }
}
