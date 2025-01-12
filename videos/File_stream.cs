using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using ChatServer.Models;
using System.Runtime.Serialization.Formatters.Binary;
using SendInfoHeader;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Net.Sockets;

namespace ChatServer.videos
{
    class File_stream
    {
        // Обработчики события извлечения данных 
        private delegate void ReceiveEventHandler(object sender, ReceiveEventArgs e);
        //private event ReceiveEventHandler Receive;
        private enum Dash { end_delete, start_delete, open_file, close_file };
        private Dash dash;
        private readonly ServerObject server_obj;
        internal SendInfo SendInfo { get; set; } = new SendInfo();

        private delegate void UpdatingData();
        private event UpdatingData Send_info_update;

        //====================================================
        //====================================================
        public File_stream(ServerObject server_obj)
        {
            this.server_obj = server_obj;            
            //string path = @"c:\Users\seregaAn\Documents\Visual Studio 2015\Projects\DumaA\DumaA\videos\";
            //string path = "//SEREGAVDS02/videos/";            
            string path = @"c:\Users\seregaAn\source\repos\duma\duma\videos\";
            dir = new DirectoryInfo(path);
            Send_info_update += MySqlServer_send_info_update;
        }
        //====================================================
        //====================================================
        private void MySqlServer_send_info_update() {

            server_obj.MySqlServer.SaveVolume();

            Console.Write("............ работает событие \"MySqlServer_send_info_update\" ................. \r\n");
        }
        //====================================================
        /// <summary>
        /// Метод упрощенного создания заголовка с информацией о размере данных отправляемых по сети.
        /// </summary>
        /// <param name="length">длина данных подготовленных для отправки по сети</param>
        /// <returns>возращает байтовый массив заголовка</returns>
        //====================================================
        internal byte[] GetHeader(int length) {
            string header = length.ToString();
            if (header.Length < 8) {
                string zeros = null;
                for (int i = 0; i < (8 - header.Length); i++) {
                    zeros += "0";
                }
                header = zeros + header;
            }
            return Encoding.Unicode.GetBytes(header);
        }
        //====================================================                    
        private object locker = new object();
        /// <summary>
        /// метод приема и расшифровки сетевых данных
        /// </summary>
        internal void ReadCallback(IAsyncResult ar) {

            //if (server_obj.modeNetwork == ServerObject.Mode.indeterminately) return;

            TcpClientData tcpClientData = (TcpClientData)ar.AsyncState;
            try {
                int r = tcpClientData.Net_stream.EndRead(ar);

                int size_header = 0;
                //client_obj.auto_reset.Set();

                if (r <= global.LENGTHHEADER) {
                    string data = server_obj.ClientObject.GetMessage();

                    tcpClientData.size_file = Number_of_drains(data);

                    size_header = tcpClientData.size_file <= 8 ? global.LENGTHHEADER : 1024;
                    string vol = tcpClientData.size_file == 0 ? " => клиент зарегистрировался на сервере." : " => принято байтов ";
                    Console.WriteLine("\n" + data + vol);

                    // посылаем сообщение о входе в чат подключенному пользователю
                    string str = tcpClientData.size_file <= 8 ? "регистрация клиента " + data + " в сети " : ":количество в файле: " + data + " байт.";
                    server_obj.BroadcastMessage(str, server_obj.ClientObject.Id);

                    data = null;
                    vol = null;
                    str = null;
                }                
                if (r > global.LENGTHHEADER) {
                    tcpClientData.numberOfBytesRead += r;
                    // Получим и десериализуем объект с подробной информацией о содержании получаемого сетевого пакета                    
                    if (tcpClientData.buffer.Length > 0) {

                        //byte[] temp = new byte[r];
                        //int count = tcpClientData.Net_stream.Read(temp, 0, temp.Length);

                        tcpClientData.Ms.Write(tcpClientData.buffer, 0, r);

                        size_header = (tcpClientData.size_file - tcpClientData.numberOfBytesRead) > 0 ? tcpClientData.size_file - tcpClientData.numberOfBytesRead : global.LENGTHHEADER;

                        //client_obj.auto_reset.WaitOne();                                                
                        if (tcpClientData.size_file == tcpClientData.Ms.Length) {
                            
                            BinaryFormatter bf = new BinaryFormatter();
                            tcpClientData.Ms.Position = 0;

                            SendInfo = (SendInfo)bf.Deserialize(tcpClientData.Ms);

                            Send_info_update.BeginInvoke(null, null);

                            while (dash == Dash.start_delete) {
                                Thread.Sleep(100);
                                if (dash == Dash.end_delete) break;
                            }

                            if (SendInfo.Filesize > 0 && dash != Dash.start_delete) {

                                dash = Dash.open_file;

                                string path = dir.FullName + SendInfo.Id_number_folder + "\\video_" + SendInfo.Index_file + "\\";
                                // Создадим файл на основе полученной информации и массива байтов следующих за объектом информации
                                using (FileStream fs = new FileStream($"{path}" + SendInfo.Filename + SendInfo.Extension, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete)) {
                                    do {
                                        // Записываем строго столько байтов сколько прочтено методом Read()
                                        fs.Write(SendInfo.Data, 0, SendInfo.Data.Length);

                                        Console.Write(SendInfo.Data.Length + " <= записано в файл байтов.\r\n");

                                        // Как только получены все байты файла, останавливаем цикл,
                                        // иначе он заблокируется в ожидании новых сетевых данных
                                        if (fs.Length == SendInfo.Filesize) {
                                            fs.Close();
                                            break;
                                        }
                                    }
                                    while (r > 0);
                                }
                                dash = Dash.close_file;
                            }                            
                            tcpClientData.Ms.Close();
                            tcpClientData.Ms.Dispose();
                            tcpClientData.Ms = null;
                            tcpClientData.buffer = null;
                            tcpClientData.size_file = 0;
                            tcpClientData.numberOfBytesRead = 0;
                            bf = null;                            
                            ar = null;
                            tcpClientData.Ms = new MemoryStream();

                            server_obj.auto_reset.Set();

                            GC.Collect();
                            GC.WaitForPendingFinalizers();                            
                        }
                    }
                }
                tcpClientData.buffer = new byte[size_header];
                tcpClientData.Net_stream.BeginRead(tcpClientData.buffer, 0, tcpClientData.buffer.Length, new AsyncCallback(ReadCallback), tcpClientData);                
            }
            catch (Exception ex) {
                Console.Write("метод ReadCallback: " + ex.Message);
            }
        }        
        //====================================================
        // число изи строки
        internal int Number_of_drains(string str) {
            int result = 0;
            if (int.TryParse(str, out result)) {
                str = null;
                return result;
            }
            return 0;
        }
        //====================================================
        /// <summary>
        /// Заголовок из объекта
        /// </summary>
        /// <returns></returns>
        internal byte[] Header_is_object() {

            //SendInfo.Header_size = header;
            //SendInfo.Message = "отправка данных клиенту";

            BinaryFormatter bf = new BinaryFormatter();
            using(MemoryStream ms = new MemoryStream()) {

                bf.Serialize(ms, SendInfo);

                ms.Position = 0;
                byte[] infobuffer = new byte[ms.Length];

                //int r = server_obj.ClientObject.tcpClientData.Net_stream.Read(infobuffer, 0, infobuffer.Length);

                int r = ms.Read(infobuffer, 0, infobuffer.Length);
                ms.Close();
                ms.Dispose();

                if (r > 0) {
                    
                    byte[] header = GetHeader(infobuffer.Length);

                    byte[] total = new byte[header.Length + infobuffer.Length];

                    Buffer.BlockCopy(header, 0, total, 0, header.Length);
                    Buffer.BlockCopy(infobuffer, 0, total, header.Length, infobuffer.Length);

                    for (int i = 0; i < server_obj.clients.Count; i++) {
                        if (server_obj.clients[i].Id == server_obj.ClientObject.Id) {

                            server_obj.clients[i].tcpClientData.length_header = int.Parse(Encoding.Unicode.GetString(header));
                            server_obj.clients[i].tcpClientData.Net_stream.Write(total, 0, total.Length); //передача данных                    
                        }
                    }

                    // Обнулим все ссылки на многобайтные объекты и попробуем очистить память                                                        
                    header = null;                    
                    infobuffer = null;
                    bf = null;                    

                    return infobuffer;
                }
            }            
            return null;
        }
        //====================================================        
        private readonly DirectoryInfo dir;        
        //====================================================
        internal void Delete_All() {

            DirectoryInfo[] directoryInfo = dir.GetDirectories()[0].GetDirectories();

            while (true) {
                server_obj.auto_reset.WaitOne();

                dash = Dash.start_delete;

                // Получить все файлы
                FileInfo[] streamsFiles_0 = directoryInfo[0].GetFiles();

                Delete_file(streamsFiles_0);

                FileInfo[] streamsFiles_1 = directoryInfo[1].GetFiles();

                Delete_file(streamsFiles_1);

                FileInfo[] streamsFiles_2 = directoryInfo[2].GetFiles();

                Delete_file(streamsFiles_2);

                FileInfo[] streamsFiles_3 = directoryInfo[3].GetFiles();

                Delete_file(streamsFiles_3);

                streamsFiles_0 = null; streamsFiles_1 = null; streamsFiles_2 = null; streamsFiles_3 = null;

                GC.Collect();
                GC.WaitForPendingFinalizers();

                dash = Dash.end_delete;

                Thread.Sleep(250);
            }
        }
        //====================================================
        private void Delete_file(FileInfo[] streamsFiles) {
            try {
                // доступ к файлу из разных потоков, за счет временного файла 
                FileInfo temp_file = Copy_temp_file(streamsFiles);

                // текущий файл воспреизведения
                int segment_time_line = SegmentTimeline(temp_file) - 3;

                for (int x = 0; x < streamsFiles.Length; x++) {
                    if (streamsFiles[x].Name != "init-stream0.m4s" && streamsFiles[x].Extension != ".mpd") {
                        if (Flag_number(ref x, streamsFiles, segment_time_line)) {
                            Console.WriteLine("удаление => " + streamsFiles[x].Directory.Name + "\\" + streamsFiles[x].Name + "  размер массива: " + streamsFiles.Length);
                            Console.WriteLine("\n******************\n");
                            if(dash != Dash.open_file) streamsFiles[x].Delete();
                            streamsFiles[x] = null;
                        }
                    }
                }
                temp_file = null;
                streamsFiles = null;
            }
            // костыль
            catch (Exception ex) {
                Console.Write(ex.Message);
            }            
        }
        //====================================================    
        //private int number_files;
        private FileInfo Copy_temp_file(FileInfo[] streamsFiles)
        {
            //number_files = 0;
            //FileInfo[] old_file = dir.GetFiles("*.mpd");            
            for (int x = 0; x < streamsFiles.Length; x++) {
                if(streamsFiles[x].Name != "init-stream0.m4s" && streamsFiles[x].Extension != ".mpd") {

                    //int new_number = int.Parse(streamsFiles[x].Name.Split('-', '.')[2]);

                    //if (new_number > number_files) number_files = new_number;
                }                
                if (streamsFiles[x].Name == "dash.mpd") {
                    //string path = Path.GetTempFileName();
                    //string path = Environment.CurrentDirectory;
                    // создаем новый объект и копируем файл                    
                    //FileInfo fi = new FileInfo($"{path}\\" + "temp_dash.tmp");
                    //fi.Refresh();
                    //FileInfo new_file = streamsFiles[x].CopyTo(fi.Name, true);                    
                    return streamsFiles[x];
                }
            }
            return null;
        }

        //====================================================
        private int SegmentTimeline(FileInfo temp_file) {
            try
            {                
                if (temp_file != null && temp_file.Length > 0 && dash != Dash.close_file) {

                    int page = 0;
                    // Создаем экземпляр Xml документа.
                    var doc = new XmlDocument();
                    // Загружаем данные из файла.
                    doc.Load(temp_file.FullName);
                    // Получаем корневой элемент документа.
                    var root = doc.DocumentElement;
                    // обходим все дочерние узлы элемента user
                    foreach (XmlNode childnode in root.ChildNodes)
                        if (childnode.Name == "Period")
                            foreach (XmlNode adaptationSet in childnode.ChildNodes)
                                if (adaptationSet.Name == "AdaptationSet")
                                    foreach (XmlNode representation in adaptationSet.ChildNodes)
                                        if (representation.Name == "Representation")
                                            foreach (XmlNode segmentTemplate in representation.ChildNodes)

                                                if (segmentTemplate.Name == "SegmentTemplate") {
                                                    foreach (XmlNode segmentTimeline in segmentTemplate.ChildNodes)

                                                        if (segmentTimeline.Name == "SegmentTimeline") {
                                                            XmlNode attribut = null;
                                                            foreach (XmlNode s in segmentTimeline.ChildNodes)
                                                                if (s.Name == "S") {
                                                                    attribut = s.Attributes.GetNamedItem("r");
                                                                    if (attribut != null)
                                                                        page += int.Parse(attribut.Value);
                                                                }
                                                            attribut = null;
                                                        }
                                                    /*foreach (XmlNode item in segmentTemplate.Attributes) {
                                                        if (item.Name == "startNumber") {
                                                            item.Value = number_files.ToString();
                                                        }
                                                    }*/
                                                }

                    root = null;                    
                    if (page > 0) {

                        //doc.Save(temp_file.FullName);
                        doc = null;
                        temp_file = null;
                        return page;
                    }
                    temp_file = null;
                    doc = null;
                }                   
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            temp_file = null;
            return 0;
        }

        //====================================================
        private bool Flag_number(ref int x, FileInfo[] streamsFiles, int segment_time_line) {
            // номер файла
            int number = Number_name_file(streamsFiles, x);
            int new_number = 0; if (x < streamsFiles.Length - 1)
                new_number = Number_name_file(streamsFiles, x + 1);
            if (new_number - number > 1)
            {
                x++;
                streamsFiles = null;
                return true;
            }
            else if (number < segment_time_line) return true;
            streamsFiles = null;
            return false;
        }

        //====================================================
        private int Number_name_file(FileInfo[] streamsFiles, int x) {
            string[] mas = streamsFiles[x].Name.Split(new char[] { '-', '.' });

            //Thread.Sleep(50);
            streamsFiles = null;
            char[] char_volume = null; string number = null;
            if (mas != null && mas.Length == 4) {
                char_volume = mas[2].ToCharArray();

                //Thread.Sleep(50);

                for (int z = 0; z < char_volume.Length; z++) number += char_volume[z];
            }
            char_volume = null; mas = null; int volume = 0;
            if (number != null) volume = int.Parse(number);
            return volume;
        }
        //====================================================
        //====================================================
    }
}
