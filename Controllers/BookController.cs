using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BasicWebApi.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace BasicWebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]

    public class BookController : ControllerBase
    {
        [HttpGet]
        [Route("[action]")]
        [Authorize]
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
        [Route("[action]/{id}")]
        [Authorize]
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
        [Authorize]
        public IActionResult Edit(BookModel bookmodel)
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
                    return Ok(new { message = "success" });
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
        [Authorize]
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
                    return Ok(new { message = "success" });
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
        [Authorize]
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
                    return Ok(new { message = "success" });
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
        [Authorize]
        public IActionResult Sale(string barcode)
        {
            try
            {
                // 1 find product
                int id = 0;
                int price = 0;
                {
                    using NpgsqlConnection conn = new Connect().GetConnection();
                    using NpgsqlCommand cmd = conn.CreateCommand();

                    cmd.CommandText = "SELECT id, price FROM tb_book WHERE isbn = @barcode";
                    cmd.Parameters.AddWithValue("barcode", barcode);

                    using NpgsqlDataReader reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        id = Convert.ToInt32(reader["id"]);
                        price = Convert.ToInt32(reader["price"]);
                    }
                    else
                    {
                        return StatusCode(StatusCodes.Status501NotImplemented, new { message = "not found barcode" });
                    }
                }

                // 2 create bill sale
                int billSaleId = 0;

                {
                    using NpgsqlConnection conn = new Connect().GetConnection();
                    using NpgsqlCommand cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT id FROM tb_bill_sale WHERE pay_at IS NULL";

                    using NpgsqlDataReader reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        billSaleId = Convert.ToInt32(reader["id"]);
                    }
                }

                if (billSaleId == 0)
                {
                    {
                        using NpgsqlConnection conn = new Connect().GetConnection();
                        using NpgsqlCommand cmd = conn.CreateCommand();
                        cmd.CommandText = "INSERT INTO tb_bill_sale(created_at) VALUES(NOW()) RETURNING id";
                        int result = cmd.ExecuteNonQuery();
                        //using NpgsqlDataReader reader = cmd.ExecuteReader();

                        return Ok(new { id = result });
                    }
                }


                // 3) create bill sale detail
                {
                    bool foundProduct = false;

                    {
                        using NpgsqlConnection conn = new Connect().GetConnection();
                        using NpgsqlCommand cmd = conn.CreateCommand();
                        cmd.CommandText = @"
                            SELECT COUNT(id) AS totalRow FROM tb_bill_sale_detail
                            WHERE bill_sale_id = @bill_sale_id AND book_id = @book_id
                        ";
                        cmd.Parameters.AddWithValue("bill_sale_id", billSaleId);
                        cmd.Parameters.AddWithValue("book_id", id);

                        using NpgsqlDataReader reader = cmd.ExecuteReader();
                        if (reader.Read())
                        {
                            int totalRow = Convert.ToInt32(reader["totalRow"]);
                            foundProduct = totalRow > 0;

                        }
                    }

                    if (foundProduct)
                    {
                        //update
                        using NpgsqlConnection conn = new Connect().GetConnection();
                        using NpgsqlCommand cmd = conn.CreateCommand();
                        cmd.CommandText = @"
                            UPDATE tb_bill_sale_detail 
                            SET qty = qty + 1
                            WHERE bill_sale_id = @bill_sale_id AND book_id = @book_id
                        ";
                        cmd.Parameters.AddWithValue("bill_sale_id", billSaleId);
                        cmd.Parameters.AddWithValue("book_id", id);

                        if (cmd.ExecuteNonQuery() != -1)
                        {
                            return Ok(new { message = "success", billSaleId = billSaleId });
                        }
                    }
                    else
                    {
                        //insert
                        using NpgsqlConnection conn = new Connect().GetConnection();
                        using NpgsqlCommand cmd = conn.CreateCommand();
                        cmd.CommandText = @"
                            INSERT INTO tb_bill_sale_detail(bill_sale_id, book_id, price, qty)
                            VALUES(@bill_sale_id, @book_id, @price, @1)
                        ";
                        cmd.Parameters.AddWithValue("bill_sale_id", billSaleId);
                        cmd.Parameters.AddWithValue("book_id", id);
                        cmd.Parameters.AddWithValue("price", price);

                        if (cmd.ExecuteNonQuery() != -1)
                        {
                            return Ok(new { message = "success", billSaleId = billSaleId });
                        }
                    }

                }
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpGet]
        [Route("[action]/{billSaleId}")]
        [Authorize]
        public IActionResult BillSaleInfo(int billSaleId)
        {
            try
            {
                using NpgsqlConnection connect = new Connect().GetConnection();
                using NpgsqlCommand cmd = connect.CreateCommand();
                cmd.CommandText = @"
                    SELECT
                        tb_bill_sale_detail.id,
                        tb_bill_sale_detail.qty,
                        tb_bill_sale_detail.price,
                        tb_book.name,
                        tb_book.isbn
                    FROM tb_bill_sale_detail 
                    LEFT JOIN tb_book ON tb_book.id = tb_bill_sale_detail.book_id 
                    WHERE bill_sale_id = @bill_sale_id
                    ORDER BY tb_bill_sale_detail.id DESC
                ";
                cmd.Parameters.AddWithValue("bill_sale_id", billSaleId);

                using NpgsqlDataReader reader = cmd.ExecuteReader();
                List<object> list = new List<object>();

                while (reader.Read())
                {
                    list.Add(new
                    {
                        price = Convert.ToInt32(reader["price"]),
                        qty = Convert.ToInt32(reader["qty"]),
                        id = Convert.ToInt32(reader["id"]),
                        isbn = reader["isbn"].ToString(),
                        name = reader["name"].ToString()
                    });
                }

                return Ok(new { results = list });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpGet]
        [Route("[action]")]
        [Authorize]
        public IActionResult RecentBillSale()
        {
            try
            {
                using NpgsqlConnection connect = new Connect().GetConnection();
                using NpgsqlCommand cmd = connect.CreateCommand();
                cmd.CommandText = "SELECT id FROM tb_bill_sale WHERE pay_at IS NULL";

                using NpgsqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return Ok(new
                    {
                        billSaleId = Convert.ToInt32(reader["id"])
                    });
                }
                return StatusCode(StatusCodes.Status404NotFound);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpDelete]
        [Route("[action]/{id}")]
        [Authorize]
        public IActionResult DeleteSaleItem(int id)
        {
            try
            {
                using NpgsqlConnection connect = new Connect().GetConnection();
                using NpgsqlCommand cmd = connect.CreateCommand();
                cmd.CommandText = "DELETE FROM tb_bill_sale_detail WHERE id = @id";
                cmd.Parameters.AddWithValue("id", id);

                if (cmd.ExecuteNonQuery() != -1)
                {
                    return Ok(new { message = "success" });
                }
                else
                {
                    return StatusCode(StatusCodes.Status501NotImplemented, new { message = "delete error" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }
    }
}