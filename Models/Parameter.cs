using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ChatServer.Models
{
    ///перед миграцией, создать архив старой миграций, Имя миграций всегда НОВОЕ !!!

    //провести миграцию от старой схемы базы данных к новой => enable-migrations
    //создать саму миграцию => PM> Add-Migration "MigrateDB"
    //и в завершении чтобы выполнить миграцию => PM> Update-Database
    public class Parameter
    {
        public int id { get; set; }        
        public DateTime datetime { get; set; }
        public string parameter { get; set; }
        public string codparameter { get; set; }
        public string lastupdate { get; set; }
        public int meaning { get; set; }
        public int? DumaId { get; set; }// внешний ключь
        public Duma Duma { get; set; } = new Duma();//навигационное свойство
        public int? LoginModelId { get; set; }
        public LoginModel LoginModel { get; set; }
    }
}