using System;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.IO;
using System.Net.Http;
using System.Security.Principal;
using System.Collections.Generic;
using System.Net.Http.Headers;
namespace ChatServer
{
    class HttpListner
    {
        internal ServerObject ServerObject { get; }
        private protected  HttpObject HttpObject { get;}
        private protected SetDataLocal SetDataLocal { get; }
        public HttpListner(ServerObject serverObject)
        {
            ServerObject = serverObject;
            HttpObject = new HttpObject();
            SetDataLocal = new SetDataLocal(this);
        }        
        //-----------------------------------------------------
        internal async void HttpListenAsync()
        {
            using (HttpListener listener = new HttpListener())
            {
                listener.AuthenticationSchemes = AuthenticationSchemes.Basic;
                listener.Prefixes.Add("http://localhost:8888/");//81.176.228.89                
                listener.Start(); bool listen = true;
                Console.WriteLine("Серрвер HTTP запущен. Ожидание подключений...");

                //подгружаем авторизационные данные
                ServerObject.MySqlServer.SqlReader();

                while (listen)
                {
                    try
                    {                        
                        // метод GetContext БЛОКИРУЕТ текущий поток, ожидая получение запроса 
                        HttpListenerContext context = await listener.GetContextAsync();    
                        
                        // получаем объект ответа
                        using(HttpListenerResponse response = context.Response) {

                            HttpListenerRequest request = context.Request;

                            IPrincipal user = context.User;
                            HttpListenerBasicIdentity identity = (HttpListenerBasicIdentity)context.User.Identity;
                            // запросы к удаленному клиенту и web приложению.
                            for (int x = 0; x < ServerObject.MySqlServer.AspNetUsers.Count; x++) {
                                if (identity.Name == ServerObject.MySqlServer.AspNetUsers[x].UserName) {
                                    //метод распределяет по прифексам
                                    TargetActions(request, response);
                                    Console.WriteLine("Статус ответа http: " + response.StatusCode + " " +
                                        response.StatusDescription + "\r\n");
                                }
                            }
                            // останавливаем прослушивание подключений                                                
                            response.Close();
                            context = null;
                            request = null;
                            user = null;
                            identity = null;
                        }                        
                    }
                    catch (Exception ex)
                    {
                        listener.Stop();
                        Console.WriteLine("Обработка подключений завершена \r\n" + ex.Message);                        
                    }                    
                }
            }                
        }  
        //-----------------------------------------------------        
        void TargetActions(HttpListenerRequest request, HttpListenerResponse response)
        {
            string message = null;                       
            if (request.Url.Segments[1] == "parameter/")
            {
                //if (xmlRespons == null || xmlRespons != null && xmlRespons.IsCompleted)
                Task<string> xmlRespons = HttpRequestAutentication();//HttpRequest(request.Url.Segments[2]);                                
                message = "запрос на " + request.UserHostName + "/parameter/";

                //перенаправляем на целевой метод   
                //response.Redirect("http://192.168.0.88:8080/DumaA/UserData/Parameter/" + request.Url.Segments[2]);
                //response.Redirect("http://81.176.228.89/DumaA/UserData/Parameter/" + request.Url.Segments[2]);
                response.Redirect("http://localhost:50836/UserData/Parameter/" + request.Url.Segments[2]);
                if (xmlRespons != null)// переместил  с низу, не проверял.
                {
                    //ожидаем запрос
                    if (xmlRespons.Result != null) xmlRespons.Wait();
                    //отправляем сообщения подключенному клиенту 
                    if (xmlRespons.Result != null && xmlRespons.Result != "")
                        SetDataLocal.Send(xmlRespons.Result);
                }
                Console.WriteLine(message);
                xmlRespons = null;
            }
            if (request.Url.Segments[1] == "video/")
            {
                // запуск удаленного процесса cmd.exe
                ServerObject.BroadcastMessage(request.Url.Segments[1], ServerObject.ClientObject.Id);

                // запрос к видео-плееру.                
                //response.Redirect("http://81.176.228.89/DumaA/UserData/Dash/");

                Thread.Sleep(13000);

                response.Redirect("http://localhost:50836/UserData/VideoDash/");
            }
            request = null;
            response = null;
        }
        //----------------------------------------------------- 
        async Task HttpVideo(HttpListenerResponse response_local)
        {
            try
            {
                string email = "aaa@eee.ru";
                string password = "123456";                
                string url = "http://81.176.228.89/DumaA/Account/Login/";
                var postData = new List<KeyValuePair<string, string>>()
                {
                    new KeyValuePair<string, string>("Email", email),
                    new KeyValuePair<string, string>("Password", password),
                    new KeyValuePair<string, string>("returnUrl", "/UserData/Dash")///DumaA/UserData/XmlParameter
                };                
                var content = new FormUrlEncodedContent(postData);
                using (var client = new HttpClient())
                {
                    //Получает или задает значение заголовка Authorization для HTTP-запроса.
                    //client.DefaultRequestHeaders.Authorization = header;

                    HttpRequestMessage request = new HttpRequestMessage();
                    request.RequestUri = new Uri(url);
                    request.Method = HttpMethod.Post;
                    request.Content = content;
                    using (HttpResponseMessage response = await client.SendAsync(request))
                    {
                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            HttpContent responseContent = response.Content;

                            // запись ответа в поток
                            byte[] mas = await responseContent.ReadAsByteArrayAsync();
                            response_local.OutputStream.Write(mas, 0, mas.Length);
                        }
                        else
                        {
                            Console.WriteLine("статус кода: " + response.StatusCode);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }            
        }
        //----------------------------------------------------- 
        async Task<string> HttpRequestAutentication()
        {
            string xml = null;
            try
            {
                string email = "aaa@eee.ru";
                string password = "123456";
                //string url = "http://192.168.0.88:8080/DumaA/Account/Login/";
                string url = "http://81.176.228.89/DumaA/Account/Login/";
                var postData = new List<KeyValuePair<string, string>>()
                {
                    new KeyValuePair<string, string>("Email", email),
                    new KeyValuePair<string, string>("Password", password),
                    new KeyValuePair<string, string>("returnUrl", "/DumaA/UserData/XmlParameter")///DumaA/UserData/XmlParameter
                };
                //string creds = string.Format("{0}:{1}", email, password);
                //byte[] bytes = UTF8Encoding.ASCII.GetBytes(creds);
                //var header = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(bytes));
                var content = new FormUrlEncodedContent(postData);                
                using (var client = new HttpClient())
                {                    
                    //Получает или задает значение заголовка Authorization для HTTP-запроса.
                    //client.DefaultRequestHeaders.Authorization = header;

                    HttpRequestMessage request = new HttpRequestMessage();
                    request.RequestUri = new Uri(url);
                    request.Method = HttpMethod.Post;
                    request.Content = content;
                    using (HttpResponseMessage response = await client.SendAsync(request))
                    {
                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            HttpContent responseContent = response.Content;
                            xml = await responseContent.ReadAsStringAsync();
                            if (xml != "") Console.WriteLine(xml);
                            responseContent = null;
                        }
                        else
                        {
                            Console.WriteLine("статус кода: " + response.StatusCode);
                        }
                        response.Dispose();
                    }
                    request.RequestUri = null;
                    request.Method = null;
                    request.Content = null;
                    request = null;
                }
                postData = null;
                content = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return xml;         
        }
        //----------------------------------------------------- 
        //запрос на выборку xml данных
        async Task<string> HttpRequest(string id = "1")
        {
            string xml = null;
            try
            {
                //ICredentials credentials = CredentialCache.DefaultCredentials;
                //NetworkCredential credential = credentials.GetCredential(new Uri(url), "Basic");                
                string url = "http://81.176.228.89/DumaA/UserData/XmlParameter/" + id;
                using (var client = new HttpClient(new HttpClientHandler {
                    PreAuthenticate = true,
                    UseDefaultCredentials = false,
                    UseProxy = false,
                    Credentials = new NetworkCredential("aaa@eee.ru", "123456")
            }))
                {
                    HttpRequestMessage request = new HttpRequestMessage();
                    request.RequestUri = new Uri(url);
                    request.Method = HttpMethod.Get;
                    request.Headers.Add("Accept", "application/xml");
                    using (HttpResponseMessage response = await client.SendAsync(request))
                    {
                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            HttpContent responseContent = response.Content;
                            xml = await responseContent.ReadAsStringAsync();
                            if(xml != "") Console.WriteLine(xml);
                        }
                        else
                        {
                            Console.WriteLine("статус кода: " + response.StatusCode);
                        }
                    }
                }                                     
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return xml;
        }
        //-----------------------------------------------------          
    }
}
