using System;
using System.Net;
using System.Collections.Generic;
using System.Data;
using MySql.Data.MySqlClient;
using System.Net.NetworkInformation;
using ChatServer.Models;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Data.Common;
using System.Threading;

namespace ChatServer
{
    class MySqlServer
    {        
        private ServerObject _servObj;
        internal MySqlServer(ServerObject servObj)
        {
            _servObj = servObj;            
        }        
        //===============================================================================
        internal void SqlReader()
        {
            //string host = Dns.GetHostName();
            //string dataStr = string.Format("SELECT * FROM dumas");// WHERE time BETWEEN'{0}'AND'{1}'order by id asc",
                                                                  //"10.01.2019 10:00:00", "10.01.2019 24:00:00");//today_time.AddDays(1) ORDER BY id ASC
            // создаём объект для подключения к БД
            using (MySqlConnection conn = new MySqlConnection(ConnectionString))
            {
                try
                {
                    // устанавливаем соединение с БД
                    conn.Open();
                    // запрос
                    string sql_aspnet_users = "SELECT * FROM aspnetusers";
                    string sql_parameters = "Select * From parameters";
                    string sql_dumas = "Select * From dumas";

                    for(int x = 0; x < 3; x++) {

                        string sql_command = null;
                        if (x == 0) sql_command = sql_aspnet_users;
                        if (x == 1) sql_command = sql_parameters;
                        if (x == 2) sql_command = sql_dumas;
                        // объект для выполнения SQL-запроса
                        MySqlCommand command = new MySqlCommand(sql_command, conn);
                        // объект для чтения ответа сервера
                        MySqlDataReader reader = command.ExecuteReader();
                        // читаем результат
                        while (reader.Read()) {
                            // элементы массива [] - это значения столбцов из запроса SELECT                            
                            if(x == 0) {                                
                                GetLoginModel(reader);

                                //_servObj.ClientObject.Model_parameter.LoginModel.Id = int.Parse(reader["Id"].ToString());
                                _servObj.ClientObject.Model_parameter.LoginModel.UserName = reader["UserName"].ToString();
                                
                                Console.Write(string.Format("{0} {1}\r\n", reader["Id"], reader["UserName"]));                                
                            }           
                            if(x == 1) {
                                _servObj.ClientObject.Model_parameter.datetime = DateTime.Parse(reader["datetime"].ToString());
                                _servObj.ClientObject.Model_parameter.meaning = int.Parse(reader["meaning"].ToString());

                                _servObj.ClientObject.Model_parameter.Duma.status = "update";

                                Console.Write(string.Format("{0} {1} {2}\r\n", reader["id"], reader["datetime"], reader["meaning"]));                                
                            }
                            if(x == 2) {
                                _servObj.ClientObject.Model_parameter.Duma.datetime = DateTime.Parse(reader["datetime"].ToString());
                                _servObj.ClientObject.Model_parameter.Duma.id = int.Parse(reader["id"].ToString());
                                _servObj.ClientObject.Model_parameter.Duma.status = "update";
                                Console.Write(string.Format("{0} {1}\r\n", reader["id"], reader["datetime"]));                                
                            }
                            Thread.Sleep(250);
                        }
                        command = null;
                        reader.Close(); // закрываем reader
                                        // закрываем соединение с БД
                        reader.Dispose();
                        reader = null;
                        sql_command = null;
                    }

                    conn.Close();
                    conn.Dispose();
                    //Console.ReadKey();
                    Console.WriteLine(" Авторизационные данные с базы данных загружены!");
                }
                catch (Exception ex)
                {
                    Console.Write(ex.Message);
                }
            }
        }
        //===============================================================================        
        void GetLoginModel(MySqlDataReader reader)
        {
            AspNetUsers.Add(new LoginModel
            {
                Email = reader["Email"].ToString(),
                Password = reader["PasswordHash"].ToString(),
                UserName = reader["UserName"].ToString()
            });
            reader = null;
        }
        //===============================================================================
        void SaveSekcetBd(TcpClient client, Duma model)
        {
            EndPoint socket = client.Client.RemoteEndPoint;
            string[] mas = socket.ToString().Split(':');
            Console.WriteLine("\n\r RemoteEndPoint: " + socket);
            for (int x = 0; x < Dums.Count; x++)
            {
                if (model.macadress == Dums[x].macadress)
                {                    
                    Dums[x].datetime = DateTime.Now;
                    Dums[x].ipadress = mas[0];
                    Dums[x].port = int.Parse(mas[1]);
                    Dums[x].macadress = model.macadress;
                    //MySql(Dums[x]);
                }
            }            
        }
        //===============================================================================
        internal void SaveVolume()
        {            
            // запрос к parameters
            string sql = SqlCommandParameter();

            SaveParameterBd(sql);            
            
            Console.WriteLine(string.Format("{0} {1}\r\n", _servObj.ClientObject.Model_parameter.datetime, _servObj.ClientObject.Model_parameter.parameter));                        

            sql = null;            
        }
        //===============================================================================
        void SaveParameterBd(string sql)
        {
            try {
                // создаём объект для подключения к БД
                if (sql != null)
                    using (MySqlConnection conn = new MySqlConnection(ConnectionString)) {
                        // устанавливаем соединение с БД
                        conn.Open();
                        // объект для выполнения SQL-запроса
                        MySqlCommand command = new MySqlCommand(sql, conn);
                        // объект для чтения ответа сервера
                        DbDataReader reader = command.ExecuteReader();

                        // закрываем соединение с БД
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("!!!.............Сохранение данных в БД: " + sql + " время: " + DateTime.Now.TimeOfDay);
                        Console.ResetColor();

                        reader.Close();
                        conn.Close();
                        conn.Dispose();
                        sql = null;
                        command = null;
                        reader = null;

                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                    }
            }
            catch (Exception ex) {
                Console.Write(ex.Message);
            }        
        }
        //===============================================================================
        string SqlCommandDuma(Parameter model)
        {
            /* string line = null;
             IPEndPoint removeIpAdress = (IPEndPoint)_servObj.ClientObject.tcp_client.Client.RemoteEndPoint;
             IPEndPoint inputIpAdress = new IPEndPoint(IPAddress.Parse(model.Duma.ipadress), model.Duma.port);            

             if (removeIpAdress != inputIpAdress)
             {
                 line = line + string.Format("ipadress={0}", removeIpAdress);
                 _servObj.ClientObject.model_parameter.Duma.ipadress = removeIpAdress.ToString();                
             }
             if (removeIpAdress.Port != inputIpAdress.Port)
             {
                 line = line + string.Format("port={0}", removeIpAdress.Port);
                 _servObj.ClientObject.model_parameter.Duma.port = removeIpAdress.Port;
             }

             //коммандная строка
             line = string.Format(@"UPDATE dumas SET " + line + " WHERE id={0}", model.id);*/
            return null;
        }
        //===============================================================================
        private enum NumbmerCommand { datetime, next }
        private NumbmerCommand numbmerCommand;
        string SqlCommandParameter()
        {
             string line = null;
            if (_servObj.ClientObject.Model_parameter.datetime != _servObj.create_file.SendInfo.Parameter.datetime)
            {
                line = string.Format("datetime='{0}'", _servObj.create_file.SendInfo.Parameter.datetime.ToString("yyyy-MM-dd hh:mm:ss"));
                _servObj.ClientObject.Model_parameter.datetime = _servObj.create_file.SendInfo.Parameter.datetime;
                numbmerCommand = NumbmerCommand.datetime;
            }
            if (_servObj.ClientObject.Model_parameter.parameter != _servObj.create_file.SendInfo.Parameter.parameter)
            {
                line = line + string.Format(",parameter='{0}'", _servObj.create_file.SendInfo.Parameter.parameter);
                _servObj.ClientObject.Model_parameter.parameter = _servObj.create_file.SendInfo.Parameter.parameter;
                numbmerCommand = NumbmerCommand.next;
            }
            if (_servObj.ClientObject.Model_parameter.codparameter != _servObj.create_file.SendInfo.Parameter.codparameter)
            {
                line = line + string.Format(",codparameter='{0}'", _servObj.create_file.SendInfo.Parameter.codparameter);
                _servObj.ClientObject.Model_parameter.codparameter = _servObj.create_file.SendInfo.Parameter.codparameter;
                numbmerCommand = NumbmerCommand.next;
            }
            if (_servObj.ClientObject.Model_parameter.lastupdate != _servObj.create_file.SendInfo.Parameter.lastupdate)
            {
                line = line + string.Format(",lastupdate='{0}'", _servObj.create_file.SendInfo.Parameter.lastupdate);
                _servObj.ClientObject.Model_parameter.lastupdate = _servObj.create_file.SendInfo.Parameter.lastupdate;
                numbmerCommand = NumbmerCommand.next;
            }
            /*if (_servObj.ClientObject.Model_parameter.meaning != _servObj.create_file.SendInfo.Parameter.meaning)
            {
                line = line + string.Format(",meaning={0}", _servObj.create_file.SendInfo.Parameter.meaning);
                _servObj.ClientObject.Model_parameter.meaning = _servObj.create_file.SendInfo.Parameter.meaning;
            }
            if (_servObj.ClientObject.Model_parameter.DumaId != _servObj.create_file.SendInfo.Parameter.DumaId)
            {
                line = line + string.Format(",DumaId={0}", _servObj.create_file.SendInfo.Parameter.DumaId);
                _servObj.ClientObject.Model_parameter.DumaId = _servObj.create_file.SendInfo.Parameter.DumaId;
            }*/
            //коммандная строка
            line = line != null && numbmerCommand ==  NumbmerCommand.next ? string.Format(@"UPDATE parameters SET " + line + " WHERE id={0}",
                _servObj.ClientObject.Model_parameter.id) : null;            

            return line;
        }        
        //===============================================================================        
        string GetMACAddress()
        {
            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
            String sMacAddress = string.Empty;
            foreach (NetworkInterface adapter in nics)
            {
                if (sMacAddress == String.Empty)// only return MAC Address from first card  
                {
                    IPInterfaceProperties properties = adapter.GetIPProperties();
                    sMacAddress = adapter.GetPhysicalAddress().ToString();
                }
            }
            return sMacAddress;
        }
        //===============================================================================
        List<Duma> dums = new List<Duma>();
        public List<Duma> Dums
        {
            get { return dums; }
            set { dums = value; }
        }
        //=================================================
        List<LoginModel> login_model = new List<LoginModel>();
        public List<LoginModel> AspNetUsers
        {
            get { return login_model; }
            set { login_model = value; }
        }
        //=================================================
        public string ConnectionString
        {
            get { return @"Data Source=81.176.228.89;Initial Catalog=u1041417_db_dum;User ID=user;Password=1;Connection Timeout=160"; }
        }
    }
}
