using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using ChatServer.Models;
using System.Net;

namespace ChatServer
{
    class SetDataLocal
    {
        private  protected HttpListner HttpListner { get; }
        //-----------------------------------------------------      
        public SetDataLocal(HttpListner httpListner)
        {
            HttpListner = httpListner;
        }
        //-----------------------------------------------------
        internal void Send(string html)
        {
            //парсим html в xml
            XDocument xDoc = GetXmlToModel(html);
        }
        //-----------------------------------------------------
        XDocument GetXmlToModel(string message)
        {
            try
            {
                if (message != null)
                {

                    XDocument xdoc = XDocument.Parse(message);
                    foreach (XElement dumElement in xdoc.Element("parameters").Elements("parameter"))
                    {
                        XElement idElement = dumElement.Element("id");
                        XElement ipElement = dumElement.Element("codparameter");
                        XElement meaningElement = dumElement.Element("meaning");
                        //XElement idDumaParameterElement = dumElement.Element("DumaId");


                        //IPAddress soket = IPAddress.Parse(ipElement.Value);  
                        //трансляция сообщения клиенту 
                        Send(message, HttpListner.ServerObject.ClientObject.Id);
                        Console.WriteLine("\r\nтрансляция сообщения клиенту...");

                        if (idElement != null) Console.WriteLine($"id: {idElement.Value}");
                        if (ipElement != null) Console.WriteLine($"ip: {ipElement.Value}");
                        if (meaningElement != null) Console.WriteLine($"meaning: {meaningElement.Value}");
                        //if (idDumaParameterElement != null) Console.WriteLine($"DumaId: {idDumaParameterElement.Value}");
                    }
                    return xdoc;
                }
            }    
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }        
            return null;
        }
        //-----------------------------------------------------
        void Send(string message, string id)
        {
            for (int x = 0; x < HttpListner.ServerObject.ClientList().Count; x++)
            {
                string idObject = HttpListner.ServerObject.ClientList()[x].Id;
                if (id == idObject)
                    HttpListner.ServerObject.BroadcastMessage(message, HttpListner.ServerObject.ClientList()[x].Id);
            }            
        }
        //-----------------------------------------------------
    }
}
