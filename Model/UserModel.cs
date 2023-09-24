using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BasicWebApi.Model
{
    public class UserModel
    {
        public int Id { get; set; }
        public string? User { get; set; }
        public string? Password { get; set; }
        //public string? Name { get; set; }
        //public string? Level { get; set; }
    }
}