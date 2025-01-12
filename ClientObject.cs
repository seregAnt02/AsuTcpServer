using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Xml.Linq;
using System.Linq;
using ChatServer.Models;
using System.Net;
using System.IO;
using System.Threading.Tasks;
using ChatServer.videos;
using SendInfoHeader;
using System.Threading;

namespace ChatServer
{
    class ClientObject
    {
        internal string Id { get; private set; }
        //internal NetworkStream Net_stream { get; private set; }
        internal Parameter Model_parameter { get; private set; }
        internal readonly TcpClientData tcpClientData;
        //====================================================                
        internal readonly ServerObject server; // объект сервера                        
        //====================================================        
        public ClientObject(ServerObject serverObject)
        {            
            Id = Guid.NewGuid().ToString();
            server = serverObject;            
            serverObject.AddConnection(this);            
            Model_parameter = new Parameter();
            Model_parameter.LoginModel = new LoginModel();
            //create_file.AddDelete();
            tcpClientData = new TcpClientData();
        }
        //====================================================                
        internal void Process() {                        
                try
                {
                    // получаем имя пользователя
                    string message = GetMessage();
                    string userName = message;

                    message = userName + " => клиент зарегистрировался на сервере.";
                    Console.WriteLine(message);

                    // посылаем сообщение о входе в чат подключенному пользователю
                    server.BroadcastMessage(message, Id);
                    // в бесконечном цикле получаем сообщения от клиента                    
                    //while (true)
                    //{
                        try
                        {
                            //считывание байтов заголовка, объекта с последующим созданием файла
                            //create_file.ReadBytesToModel();                            

                            //парсим xml от клиента
                            Parameter model = null;
                            if (true)
                            {
                                message = GetMessage();
                                model = GetXmlModelParameter(message);

                                Console.WriteLine("получены данные от клиента:");
                                string volume = string.Format(" id: {0}\r\n codparameter: {1}\r\n meaning: {2}",
                                    model.id, model.codparameter, model.meaning);
                                message = String.Format("-----------------------------------\r\nДанные от {0} клиента получены.\r\n", userName);
                                Console.WriteLine(volume + "\r\n" + message);
                                server.BroadcastMessage(message, Id);
                                // сохранение в базе данных   
                                //server.MySqlServer.SaveVolume(model);
                            }                            
                        }
                        catch
                        {
                            message = String.Format("{0}: покинул чат", userName);
                            Console.WriteLine(message);
                            server.BroadcastMessage(message, Id);
                            //break;
                        }
                        Thread.Sleep(100);
                    //}
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                finally
                {
                    // в случае выхода из цикла закрываем ресурсы
                    server.RemoveConnection(this.Id);
                    Close();
                }            
        }
        //====================================================        
        /// <summary>
        /// Обратный метод завершения принятия клиентов
        /// </summary>
        internal void AcceptCallback(IAsyncResult ar) {

            if (server.modeNetwork == ServerObject.Mode.indeterminately) return;

            TcpListener listener = (TcpListener)ar.AsyncState;

            try {
                tcpClientData.TcpClient = listener.EndAcceptTcpClient(ar);

                tcpClientData.Net_stream = tcpClientData.TcpClient.GetStream();

                //server.ManualResetEvent.Set();

                // Немедленно запускаем асинхронный метод извлечения сетевых данных
                // для акцептированного TCP клиента
                //tcpClientData.buffer = new byte[global.LENGTHHEADER];
                //tcpClientData.Net_stream.BeginRead(tcpClientData.buffer, 0, tcpClientData.buffer.Length, server.create_file.ReadCallback, tcpClientData);
                
                // Продолжаем ждать запросы на подключение
                listener.BeginAcceptTcpClient(AcceptCallback, listener);                    
            }
            catch (Exception ex) {
                Console.Write("r/n/" + ex.Message);
            }
            ar = null;
            listener = null;
        }
        //====================================================
        async Task<string> GetRequestAsync(int id)
        {
            string htmlRespone = null;
            try
            {
                string url = "http://192.168.0.88:8080/DumaA/UserData/Parameter/" + 1;
                WebRequest request = WebRequest.Create(url);
                request.Method = "GET";
                WebResponse response = await request.GetResponseAsync();
                using (Stream stream = response.GetResponseStream())
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        htmlRespone = reader.ReadToEnd();
                        byte[] buffer = Encoding.UTF8.GetBytes(htmlRespone);
                        // получаем поток ответа и пишем в него ответ
                        //response.ContentLength = buffer.Length;
                        //Stream output = response.OutputStream;
                        //output.Write(buffer, 0, buffer.Length);   
                        //stream.SetLength(buffer.Length);
                        response.ContentLength = buffer.Length;
                        stream.Write(buffer, 0, buffer.Length);                        
                        Console.WriteLine("получаем поток ответа и пишем в него ответ");
                    }                                    
                }
                response.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return htmlRespone;        
        }
        //====================================================
        // чтение входящего сообщения и преобразование в строку
        internal string GetMessage() {
            string message = null;
            StringBuilder builder = new StringBuilder();
            string text = Encoding.Unicode.GetString(tcpClientData.buffer, 0, tcpClientData.buffer.Length);
            builder.Append(text);
            message = builder.ToString();

            text = null;
            builder = null;
            return message;
        }
        //====================================================        
        //связывание ответа с моделью
        private Parameter GetXmlModelParameter(string message)
        {
            if (message != null && message != "")
            {
                XDocument xdoc = XDocument.Parse(message);
                foreach (XElement dumElement in xdoc.Element("parameters").Elements("parameter"))
                {
                    int idElement = int.Parse(dumElement.Element("id").Value);
                    DateTime datetimeElement = DateTime.Parse(dumElement.Element("datetime").Value);
                    string parameterElement = dumElement.Element("parameter").Value;
                    string codparameterElement = dumElement.Element("codparameter").Value;
                    string lastupdateElement = dumElement.Element("lastupdate").Value;
                    int meaningElement = int.Parse(dumElement.Element("meaning").Value);
                    int dumaIdElement = int.Parse(dumElement.Element("dumaId").Value);
                    //XElement dumaIdElement = new XElement("duma.id", model.duma.id);
                    string dumaGuIdElement = dumElement.Element("duma.guId").Value;
                    DateTime dumaDatetimeElement = DateTime.Parse(dumElement.Element("duma.datetime").Value);
                    string dumaMacadressElement = dumElement.Element("duma.macadress").Value;
                    string dumaIpadressElement = dumElement.Element("duma.ipadress").Value;
                    int dumaPortElement = int.Parse(dumElement.Element("duma.port").Value);
                    string dumaStatusElement = dumElement.Element("duma.status").Value;
                    int dumaNumberElement = int.Parse(dumElement.Element("duma.number").Value);
                    string dumaMigrationElement = dumElement.Element("duma.migration").Value;
                    int dumaAgeElement = int.Parse(dumElement.Element("duma.age").Value);
                    return new Parameter
                    {
                        id = idElement,
                        datetime = datetimeElement,
                        parameter = parameterElement,
                        codparameter = codparameterElement,
                        lastupdate = lastupdateElement,
                        meaning = meaningElement,
                        DumaId = dumaIdElement,
                        Duma = new Duma
                        {
                            id = dumaIdElement,
                            guid = dumaGuIdElement,
                            datetime = dumaDatetimeElement,
                            macadress = dumaMacadressElement,
                            ipadress = dumaIpadressElement,
                            port = dumaPortElement,
                            status = dumaStatusElement,
                            number = dumaNumberElement,
                            migration = dumaMigrationElement,
                            age = dumaAgeElement
                        }
                    };
                }
            }
            return null;
        }
        //==================================================== 
        private bool ModalIs(Parameter model)
        {
            if (model.id == Model_parameter.id || model.id == 0)
                if (model.codparameter != Model_parameter.codparameter ||
                    model.meaning != Model_parameter.meaning)
                    return true;
            return false;
        }
        //====================================================
        private void Send(string message)
        {            
            string responseStr = "<html><head><meta charset='utf8'></head><body>Привет от сервер!</body></html>";
            byte[] buffer = Encoding.Unicode.GetBytes(responseStr);
            tcpClientData.Net_stream.Write(buffer, 0, buffer.Length);
            //ClientClose(null);
        }     
        //====================================================
        void ClientClose(XElement macElement)
        {
            /*if (true) //!IsClose()
                for (int x = 0; x < server.clients.Count; x++)
                    if (server.clients[x].Id != Id) server.clients[x].Close();*/
        }
        //====================================================        
        private Duma GetXmlModelDum(string message)
        {
            XDocument xdoc = XDocument.Parse(message);
            foreach (XElement dumElement in xdoc.Element("dums").Elements("dum"))
            {
                return new Duma
                {
                    ipadress = dumElement.Attribute("ip").Value,
                    id = (int)dumElement.Element("id"),
                    port = (int)dumElement.Element("port"),
                    macadress = dumElement.Element("macadress").Value,
                    guid = dumElement.Element("guid").Value
                };
            }
            return null;
        }                        
        //====================================================
        // закрытие подключения
        internal void Close()
        {
            if (tcpClientData.Net_stream != null)
                tcpClientData.Net_stream.Close();
            //if (tcp_client != null)
                //tcp_client.Close();
        }
        //====================================================
        private protected string macAdress { get; private set; }
        //protected MySqlServer my_sql_server { get; set; }        
    }
}
