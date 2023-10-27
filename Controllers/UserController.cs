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
using Microsoft.AspNetCore.Authorization;

namespace BasicWebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
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
                    userModel.Id = Convert.ToInt32(reader["id"]); // Set user id to userModel
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

        [HttpPost]
        [Route("[action]")]
        [Authorize]
        public IActionResult GetInfo()
        {
            try
            {
                int userId = GetUserIdFromAuth(HttpContext); // Get user id from token

                if (userId > 0)
                {
                    using NpgsqlConnection conn = new Connect().GetConnection(); // Get connection from Connect.cs
                    using NpgsqlCommand cmd = conn.CreateCommand(); // Create command to execute query
                    cmd.CommandText = @"
                        SELECT name, level, usr 
                        FROM tb_user_2 
                        WHERE id = @id";
                    cmd.Parameters.AddWithValue("id", userId);

                    using NpgsqlDataReader reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        return Ok(new
                        {
                            name = reader["name"].ToString(),
                            level = reader["level"].ToString(),
                            usr = reader["usr"].ToString()
                        });
                    }
                }

                return Unauthorized(new { message = "Not found user" });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpPost]
        [Route("[action]")]
        public IActionResult ChangeProfileSave(UserModel user)
        {
            try
            {
                using NpgsqlConnection conn = new Connect().GetConnection(); // Get connection from Connect.cs
                using NpgsqlCommand cmd = conn.CreateCommand(); // Create command to execute query
                if (user.Name != "" || user.User != "" || user.Password != "")
                {
                    string? sql = "UPDATE tb_user_2 SET ";
                    if (user.Name != null) sql += " name = @name";
                    if (user.User != null) sql += " ,usr = @usr";
                    if (user.Password != null) sql += " ,pwd = @pwd";

                    sql += " WHERE id = @id";
                    cmd.CommandText = sql;
                    int userId = GetUserIdFromAuth(HttpContext); // Get user id from token
                    cmd.Parameters.AddWithValue("id", userId);

                    if (user.Name != null) cmd.Parameters.AddWithValue("name", user.Name!);
                    if (user.User != null) cmd.Parameters.AddWithValue("usr", user.User!);
                    if (user.Password != null) cmd.Parameters.AddWithValue("pwd", user.Password!);

                    if (cmd.ExecuteNonQuery() > 0)
                    {
                        return Ok(new { message = "success" });
                    }
                }

                return Ok(new { message = "not update data" });

            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }

        }

        [HttpGet]
        [Route("[action]")]
        public IActionResult Users() // Get all users
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

        private Dictionary<string, string> GetTokenInfo(string token)
        {
            Dictionary<string, string> tokenInfo = new Dictionary<string, string>();
            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler(); // Create token handler to read token
            JwtSecurityToken jwtSecurityToken = tokenHandler.ReadJwtToken(token); // Read token
            List<Claim> claims = jwtSecurityToken.Claims.ToList(); // Get claims from token 

            foreach (Claim claim in claims)
            {
                tokenInfo.Add(claim.Type, claim.Value); // Add claims to dictionary 
            }

            return tokenInfo;
        }

        private int GetUserIdFromAuth(HttpContext context)
        {
            // การส่งข้อมูลเป็นแบบ authen barear token จะอยู่ใน header ของ request ดังนั้นเราจะต้องดึงข้อมูลมาจาก header ของ request 
            context.Request.Headers.TryGetValue("Authorization", out var token); // Get token from header
            token = token.ToString().Replace("Bearer ", ""); // Remove "Bearer " from token

            Dictionary<string, string> dic = GetTokenInfo(token!);

            return Convert.ToInt32(dic["sub"]); // Return user id 
        }
    }


}