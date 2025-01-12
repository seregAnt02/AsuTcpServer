using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.IO;
using System.Xml;
using System.IO.Ports;
using System.Net.Sockets;

namespace ChatServer
{
    class HttpObject
    {        
        //-----------------------------------------------------
        //"http://localhost/DumaA/UserData/Parameter"
        public async void PostRequestAsync(int id, string url)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";
                //данные для отправки
                string data = "id=" + id;
                //преобразуем данные в массив байтов
                byte[] byteArray = Encoding.UTF8.GetBytes(data);
                //устанавливаем тип содержимого
                request.ContentType = "application/x-www-form-urlencoded";
                //устанавливаем заголовок Contenet=Length запроса - свойство ContentLength
                request.ContentLength = byteArray.Length;

                //записываем данные в поток запроса
                using (Stream dataStream = request.GetRequestStream())
                {
                    dataStream.Write(byteArray, 0, byteArray.Length);
                }
                WebResponse respone = await request.GetResponseAsync();
                using (Stream stream = respone.GetResponseStream())
                {
                    string message = null;
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        Console.WriteLine((message = reader.ReadToEnd()));
                    }
                    //if(message != "") GetXml(message);
                }
                Console.WriteLine("Запрос выполнен ...");
                respone.Close();
                //Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        //-----------------------------------------------------
        public static IPEndPoint BindIPEndPointCallback(ServicePoint servicePoint, IPEndPoint remoteEndPoint, int retryCount)
        {
            Console.WriteLine("BindIPEndpoint called");
            return new IPEndPoint(IPAddress.Any, 8888);

        }
        //-----------------------------------------------------
        public void SocketReq(WebRequest request)
        {
            ServicePoint serv = ServicePointManager.FindServicePoint(new Uri("http://81.176.228.89"));
            //serv.BindIPEndPointDelegate = new BindIPEndPoint(BindIPEndPointCallback);
            //Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);        
            //SocketAddress socket = new SocketAddress(AddressFamily.InterNetwork);   
        }       
        //-----------------------------------------------------
        void GetXml(string message)
        {
            XDocument xdoc = XDocument.Parse(message);
            foreach (XElement dumElement in xdoc.Element("dums").Elements("dum"))
            {
                XAttribute nameAtrr = dumElement.Attribute("ip_adress");
                XElement idElement = dumElement.Element("id");
                XElement dataElement = dumElement.Element("data");
                XElement statusElement = dumElement.Element("status");

                if (nameAtrr != null) Console.WriteLine($"Ip: {nameAtrr.Value}");
                if (idElement != null) Console.WriteLine($"Id: {idElement.Value}");
                if (dataElement != null) Console.WriteLine($"Data: {dataElement.Value}");
                if(statusElement != null) Console.WriteLine($"Status: {statusElement.Value}");
            }
        }
        //-----------------------------------------------------
        //-----------------------------------------------------
    }
}
