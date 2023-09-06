using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using BasicWebApi.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;

namespace BasicWebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HomeController : ControllerBase
    {
        [HttpGet]
        [Route("[action]")]
        public IActionResult Index()
        {
            return Ok(new { message = "Hello" });
        }

        [HttpPost]
        [Route("[action]/{name}")]
        public IActionResult Index(string name)
        {
            return Ok(new { message = $"Hello {name}" });
        }

        [HttpPut]
        [Route("[action]")]
        public IActionResult MyPut()
        {
            return Ok(new { message = $"my put" });
        }

        [HttpDelete]
        [Route("[action]/{id}")]
        public IActionResult Delete(int id)
        {
            return Ok(new { message = $"delete {id}" });
        }

        [HttpGet]
        [Route("[action]")]
        public IActionResult TestConnect()
        {
            try
            {
                using NpgsqlConnection conn = new Connect().GetConnection();
                return Ok(new { message = "Connected" });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpGet]
        [Route("[action]")]
        public IActionResult List()
        {
            try
            {
                using NpgsqlConnection conn = new Connect().GetConnection();
                using NpgsqlCommand cmd = conn.CreateCommand(); // gen commad ไว้ติดต่อ database
                cmd.CommandText = "SELECT * FROM tb_book";

                using NpgsqlDataReader reader = cmd.ExecuteReader();
                List<object> list = new List<object>();

                while (reader.Read())
                {
                    list.Add(new
                    {
                        id = Convert.ToInt32(reader["id"]),
                        isbn = reader["isbn"].ToString(),
                        name = reader["name"].ToString(),
                        price = Convert.ToInt32(reader["price"])
                    });
                }

                return Ok(list);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpGet]
        [Route("[action]")]
        public IActionResult Info(int id)
        {
            try
            {
                using NpgsqlConnection conn = new Connect().GetConnection();
                using NpgsqlCommand cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT * FROM tb_book WHERE id = @id"; // สร้างคำสั่ง sql
                cmd.Parameters.AddWithValue("id", id);

                NpgsqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read()) // found id
                {
                    return Ok(new
                    {
                        id = Convert.ToInt32(reader["id"]),
                        isbn = reader["isbn"].ToString(),
                        name = reader["name"].ToString(),
                        price = Convert.ToInt32(reader["price"])
                    });
                }
                else
                {
                    return StatusCode(StatusCodes.Status501NotImplemented, new
                    {
                        messeage = "not found id"
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }



        }

        [HttpPost]
        [Route("[action]")]
        public IActionResult edit(BookModel bookmodel)
        {
            try
            {
                using NpgsqlConnection conn = new Connect().GetConnection();
                using NpgsqlCommand cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    UPDATE tb_book SET
                        isbn = @isbn,
                        name = @name,
                        price = @price
                    WHERE id = @id"; // สร้างคำสั่ง sql

                cmd.Parameters.AddWithValue("isbn", bookmodel.isbn!);
                cmd.Parameters.AddWithValue("name", bookmodel.name!);
                cmd.Parameters.AddWithValue("price", bookmodel.price);
                cmd.Parameters.AddWithValue("id", bookmodel.id);

                // execute โดยไม่ดึงข้อมูลจาก database = ExecuteNonQuery : create delete
                if (cmd.ExecuteNonQuery() != -1)
                {
                    return Ok(new { mmessage = "success" });
                }
                else
                {
                    return StatusCode(StatusCodes.Status501NotImplemented, new { message = "update error" });
                }


            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpPut]
        [Route("[action]")]
        public IActionResult Create(BookModel bookModel)
        {
            try
            {
                using NpgsqlConnection connect = new Connect().GetConnection();
                using NpgsqlCommand cmd = connect.CreateCommand();
                cmd.CommandText = "INSERT INTO tb_book(isbn, name, price) VALUES(@isbn, @name, @price)";
                cmd.Parameters.AddWithValue("isbn", bookModel.isbn!);
                cmd.Parameters.AddWithValue("name", bookModel.name!);
                cmd.Parameters.AddWithValue("price", bookModel.price);

                if (cmd.ExecuteNonQuery() != -1)
                {
                    return Ok(new { mmessage = "success" });
                }
                else
                {
                    return StatusCode(StatusCodes.Status501NotImplemented, new { message = "insert error" });
                }

            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpDelete]
        [Route("[action]/{id}")]
        public IActionResult Remove(int id)
        {
            try
            {
                using NpgsqlConnection connect = new Connect().GetConnection();
                using NpgsqlCommand cmd = connect.CreateCommand();
                cmd.CommandText = "DELETE FROM tb_book WHERE id = @id";
                cmd.Parameters.AddWithValue("id", id);

                if (cmd.ExecuteNonQuery() != -1)
                {
                    return Ok(new { mmessage = "success" });
                }
                else
                {
                    return StatusCode(StatusCodes.Status501NotImplemented, new { message = "delete erro" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpPost]
        [Route("[action]")]
        public IActionResult UploadFile(IFormFile file)
        {
            try
            {
                if (file == null)
                {
                    return StatusCode(StatusCodes.Status501NotImplemented, new { message = "please chose file" });
                }

                string extension = Path.GetExtension(file.FileName).ToLower();

                if (!(extension == ".jpg" || extension == ".jpeg" || extension == ".png"))
                {
                    return StatusCode(StatusCodes.Status501NotImplemented, new { message = "Type file must be jpg, jpeg, png" });
                }

                DateTime dateTime = DateTime.Now;
                Random random = new Random();
                int randomNumber = random.Next(100000);

                string currentDate = $"{dateTime.Year}{dateTime.Month}{dateTime.Day}";
                string currentTime = $"{dateTime.Hour}{dateTime.Minute}{dateTime.Second}";
                string newNameFile = $"{currentDate}{currentTime}{randomNumber}{extension}";

                string target = $"UploadedFiles/{newNameFile}";

                FileStream fileStream = new FileStream(target, FileMode.Create); //สร้างไฟล์
                file.CopyTo(fileStream); // copy

                return Ok(new { mmessage = "success" });


            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpGet]
        [Route("[action]")]
        public async Task<IActionResult> MyGet() // ไว้เรียกใช้ API ภายนอกได้
        {
            try
            {
                HttpClient client = new HttpClient();
                HttpResponseMessage res = await client.GetAsync("https://localhost:7198/Home/List");

                if (res.IsSuccessStatusCode)
                {
                    return Ok(await res.Content.ReadAsStringAsync());
                }

                return StatusCode(StatusCodes.Status501NotImplemented, new { message = "call to api error" });

            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> MyPost() // ไว้เรียกใช้ API ภายนอกได้
        {
            try
            {
                HttpClient client = new HttpClient();
                HttpResponseMessage res = await client.PostAsJsonAsync("https://localhost:7198/Home/Edit", new
                {
                    id = 3,
                    isbn = "edit by client",
                    name = "name by client",
                    price = 999
                });

                if (res.IsSuccessStatusCode)
                {
                    return Ok(await res.Content.ReadAsStringAsync());
                }

                return StatusCode(StatusCodes.Status501NotImplemented, new { message = "call to api error" });

            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpPut]
        [Route("[action]")]
        public async Task<IActionResult> MyHttpPut() // ไว้เรียกใช้ API ภายนอกได้
        {
            try
            {
                HttpClient client = new HttpClient();
                HttpResponseMessage res = await client.PutAsJsonAsync("https://localhost:7198/Home/Create", new
                {
                    id = 3,
                    isbn = "edit by client",
                    name = "name by client",
                    price = 999
                });

                if (res.IsSuccessStatusCode)
                {
                    return Ok(await res.Content.ReadAsStringAsync());
                }

                return StatusCode(StatusCodes.Status501NotImplemented, new { message = "call to api error" });

            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpDelete]
        [Route("[action]")]
        public async Task<IActionResult> MyHttpDelete(int id) // ไว้เรียกใช้ API ภายนอกได้
        {
            try
            {
                HttpClient client = new HttpClient();
                HttpResponseMessage res = await client.DeleteAsync("https://localhost:7198/Home/Remove/" + id);

                if (res.IsSuccessStatusCode)
                {
                    return Ok(await res.Content.ReadAsStringAsync());
                }

                return StatusCode(StatusCodes.Status501NotImplemented, new { message = "call to api error" });

            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpGet]
        [Route("action")]
        public IActionResult GenerateToken(string username, string password)
        {
            try
            {
                if (username == "admin" && password == "admin")
                {
                    var MyConfig = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
                    var issuer = MyConfig.GetValue<string>("Jwt:Issuer");
                    var audience = MyConfig.GetValue<string>("Jwt:Audience");
                    var key = Encoding.ASCII.GetBytes(MyConfig.GetValue<string>("Jwt:Key")!);

                    var tokenDescriptor = new SecurityTokenDescriptor
                    {
                        Subject = new ClaimsIdentity(new[]
                        {
                            new Claim("Id", Guid.NewGuid().ToString()),
                            new Claim(JwtRegisteredClaimNames.Sub, username),
                            new Claim(JwtRegisteredClaimNames.Email, "user@gmail.com"),
                            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                        }),
                        Expires = DateTime.UtcNow.AddDays(1),
                        Issuer = issuer,
                        Audience = audience,
                        SigningCredentials = new SigningCredentials(
                            new SymmetricSecurityKey(key),
                            SecurityAlgorithms.HmacSha512Signature)
                    };

                    var tokenHandler = new JwtSecurityTokenHandler();
                    var token = tokenHandler.CreateToken(tokenDescriptor);
                    var jwtTooken = tokenHandler.WriteToken(token);
                }

                return Unauthorized();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status501NotImplemented, new { message = ex.Message });
            }
        }

    }
}