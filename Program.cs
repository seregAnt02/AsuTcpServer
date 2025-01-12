using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using ChatServer.videos;
using System.Threading.Tasks;

namespace ChatServer
{
    class Program
    {                
        static void Main(string[] args)
        {
            ServerObject server = null; // сервер            
            try
            {                
                server = new ServerObject();

                //поток для удаление видео файлов.
                Task result = new Task(new Action(server.create_file.Delete_All));
                result.Start();

                // Инициализирует новый экземпляр класса System.Net.Sockets.TcpListener, который
                //  выполняет ожидание входящих попыток подключения для заданных локального IP-адреса
                //  и номера локального порта.
                Thread listenThread = new Thread(new ThreadStart(server.TcpListen));
                listenThread.Start();

                // Предоставляет простой программно управляемый прослушиватель протокола HTTP.Этот
                //  класс не наследуется.
                HttpListner objListnerHttp = new HttpListner(server);
                Thread listenHttp = new Thread(new ThreadStart(objListnerHttp.HttpListenAsync));
                listenHttp.Start();                                

                Console.Read();
            }
            catch (Exception ex)
            {
                if(server != null) server.Disconnect();
                Console.WriteLine(ex.Message);
            }
        }
    }
}
