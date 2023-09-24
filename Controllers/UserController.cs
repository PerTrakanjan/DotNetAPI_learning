using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using BasicWebApi.Model;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Npgsql;

namespace BasicWebApi.Controllers
{
    [ApiController]
    [Route("/api/[controller]")]
    public class UserController : ControllerBase
    {
        [HttpGet]
        [Route("[action]")]
        public IActionResult Users()
        {
            try
            {
                using NpgsqlConnection conn = new Connect().GetConnection(); // Get connection from Connect.cs
                using NpgsqlCommand cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT * FROM tb_user";
                using NpgsqlDataReader reader = cmd.ExecuteReader();

                List<object> users = new List<object>();
                if (reader.Read())
                {
                    users.Add(new
                    {
                        id = Convert.ToInt32(reader["id"]),
                        user = reader["user"].ToString(),
                        password = reader["password"].ToString(),
                        name = reader["name"].ToString(),
                        level = reader["level"].ToString()
                    });

                    return Ok(new { users = users, message = "success" });
                }

                return NotFound(new { message = "Users not found" });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpPost]
        [Route("[action]")]
        public IActionResult Login(UserModel userModel)
        {
            // Check if user exists in database and return token
            try
            {
                using NpgsqlConnection conn = new Connect().GetConnection(); // Get connection from Connect.cs
                using NpgsqlCommand cmd = conn.CreateCommand(); // Create command to execute query
                cmd.CommandText = @"
                    SELECT id 
                    FROM tb_user_2 
                    WHERE usr = @usr 
                    AND pwd = @pwd";

                cmd.Parameters.AddWithValue("usr", userModel.User!);
                cmd.Parameters.AddWithValue("pwd", userModel.Password!);

                using NpgsqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read()) // If user exists in database 
                {
                    int id = Convert.ToInt32(reader["id"]);
                    string token = GenerateToken(userModel);

                    return Ok(new { token = token, message = "success" });
                }

                return Unauthorized(new { message = "User or password is incorrect" });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        private string GenerateToken(UserModel userModel)
        {
            try
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
                            new Claim(JwtRegisteredClaimNames.Sub, userModel.Id.ToString()),
                            new Claim(JwtRegisteredClaimNames.Email, "user@gmail.com"),
                            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                        }),
                    Expires = DateTime.UtcNow.AddDays(30),
                    Issuer = issuer,
                    Audience = audience,
                    SigningCredentials = new SigningCredentials(
                        new SymmetricSecurityKey(key),
                        SecurityAlgorithms.HmacSha512Signature)
                };

                var tokenHandler = new JwtSecurityTokenHandler();
                var token = tokenHandler.CreateToken(tokenDescriptor);
                return tokenHandler.WriteToken(token);

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

    }


}