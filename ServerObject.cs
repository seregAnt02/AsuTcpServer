using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using ChatServer.Models;
using ChatServer.videos;

namespace ChatServer
{
    class ServerObject
    {
        internal MySqlServer MySqlServer { get; }
        internal ClientObject ClientObject { get; private set; }        
        /// <summary>
        /// Возможные режимы работы TCP модуля
        /// </summary>
        internal enum Mode { indeterminately, Server, Client };
        //====================================================
        internal AutoResetEvent auto_reset;
        internal File_stream create_file;
        public ServerObject()
        {            
            MySqlServer = new MySqlServer(this);
            auto_reset = new AutoResetEvent(false);
            ClientObject = new ClientObject(this);
            create_file = new File_stream(this);
        }
        //====================================================
        //====================================================
        private TcpListener tcpListener; // сервер для прослушивания
        internal List<ClientObject> clients = new List<ClientObject>(); // все подключения        
        internal void AddConnection(ClientObject clientObject)
        {
            clients.Add(clientObject);            
        }
        internal void RemoveConnection(string id)
        {
            // получаем по id закрытое подключение
            ClientObject client = clients.FirstOrDefault(c => c.Id == id);
            // и удаляем его из списка подключений
            if (client != null)
                clients.Remove(client);
            client = null;
        }
        //====================================================

        /// <summary>
        /// Режим работы TCP модуля
        /// </summary>
        internal Mode modeNetwork;

        // прослушивание входящих клиентских подключений
        internal void TcpListen() {
            try {
                tcpListener = new TcpListener(IPAddress.Any, 11000);
                tcpListener.Start();
                Console.WriteLine("Сервер TCP запущен. Ожидание подключений...");                              
                
                tcpListener.BeginAcceptTcpClient(ClientObject.AcceptCallback, tcpListener);
                modeNetwork = Mode.Server;

                tcpListener = null;
            }
            catch (Exception ex) {
                Console.WriteLine(ex.Message);
                Disconnect();
                tcpListener = null;
            }
        }        
        //====================================================
        // трансляция сообщения подключенным клиентам
        internal void BroadcastMessage(string message, string id)
        {
            // byte[] data = Encoding.Unicode.GetBytes(message);
            create_file.SendInfo.Message = message;

            byte[] data = create_file.Header_is_object();                       
            
            //data = null;
            message = null;
            id = null;            
        }        
        //====================================================
        // отключение всех клиентов
        internal void Disconnect()
        {
            tcpListener.Stop(); //остановка сервера

            for (int i = 0; i < clients.Count; i++)
            {
                clients[i].Close(); //отключение клиента
            }
            Environment.Exit(0); //завершение процесса
        }
        //====================================================
        //список подключенных клиентов
        internal List<ClientObject> ClientList()
        {
            return clients;
        }
        //====================================================
        //====================================================
    }
}
