using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Mail;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Registration;
using RegistrationApi.Models;

namespace RegistrationApi.Controllers
{
    [Authorize]
    [Route("api/v1/")]
    [ApiController]
    public class RegisteredUsersController : ControllerBase
    {
        private readonly RegistrationApiContext _context;

        public RegisteredUsersController(RegistrationApiContext context)
        {
            _context = context;
        }

        // GET: api/v1/user/profile
        [HttpGet("user/profile")]
        public IEnumerable<RegisteredUser> GetRegisteredUser()
        {
            return _context.RegisteredUser.ToList();
        }

        // GET: api/v1/user/5/profile
        [HttpGet("user/{id}/profile")]
        public async Task<IActionResult> GetRegisteredUser([FromRoute] int id)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var registeredUser = await _context.RegisteredUser.FindAsync(id);

                if (registeredUser == null)
                {
                    return NotFound();
                }

                return Ok(registeredUser);
            }
            catch(Exception e)
            {
                return NotFound(e.Message);
            }
        }

        // PUT: api/v1/updateuser/5
        [HttpPut("updateuser/{id}")]
        public async Task<IActionResult> PutRegisteredUser([FromRoute] int id, [FromBody] RegisteredUser registeredUser)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != registeredUser.ID)
            {
                return BadRequest();
            }

            _context.Entry(registeredUser).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RegisteredUserExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/v1/user
        [HttpPost("user")]
        public async Task<IActionResult> PostRegisteredUser([FromBody] RegisteredUser registeredUser)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (RegisteredUserExists(registeredUser.Email))
            {
                return Conflict("Email exists");
            }

            string password = encryption(registeredUser.Password);
            registeredUser.Password = password;

            _context.RegisteredUser.Add(registeredUser);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetRegisteredUser", new { id = registeredUser.ID }, registeredUser);
        }

        public string encryption(String password)
        {
            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            byte[] encrypt;
            UTF8Encoding encode = new UTF8Encoding();
            encrypt = md5.ComputeHash(encode.GetBytes(password));
            StringBuilder encryptdata = new StringBuilder();
            for (int i = 0; i < encrypt.Length; i++)
            {
                encryptdata.Append(encrypt[i].ToString());
            }
            return encryptdata.ToString();
        }

        // Login: api/v1/user/login
        [AllowAnonymous]
        [HttpPost("user/login")]
        public async Task<IActionResult> PostLoginUser([FromBody] RegisteredUser registeredUser)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (RegisteredUserExists(registeredUser.Email))
                {

                    string password = encryption(registeredUser.Password);
                    //registeredUser.Password = password;

                    var user = (from e in _context.RegisteredUser
                                where e.Email == registeredUser.Email && e.Password == password
                                select e).FirstOrDefault();

                    //    if (result != null)
                    //    {
                    //        return Ok("login successful");
                    //    }
                    //    else
                    //        return Unauthorized();

                    //}
                    //return Unauthorized();
                    if (user == null)
                        return null;

                    // authentication successful so generate jwt token
                    var tokenHandler = new JwtSecurityTokenHandler();
                    var key = Encoding.ASCII.GetBytes("{ 060110A9 - 0948 - 48FA - 9D65 - 180A994BDD46}");
                    var tokenDescriptor = new SecurityTokenDescriptor
                    {
                        Subject = new ClaimsIdentity(new Claim[]
                        {
                    new Claim(ClaimTypes.Name, user.ID.ToString())
                        }),
                        Expires = DateTime.UtcNow.AddDays(7),
                        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                    };
                    var token = tokenHandler.CreateToken(tokenDescriptor);
                    var AuthToken = tokenHandler.WriteToken(token);
                    int otpvalue = OTP();

                    SendEmail("Testing the initial mail..."+otpvalue);
                    return Ok(AuthToken);
                }
                return Unauthorized();
            }
            catch(Exception e)
            {
                return NotFound(e.Message);
            }
            }

        // DELETE: api/v1/delete/5
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteRegisteredUser([FromRoute] int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var registeredUser = await _context.RegisteredUser.FindAsync(id);
            if (registeredUser == null)
            {
                return NotFound("Id doesn't exists.");
            }

            _context.RegisteredUser.Remove(registeredUser);
            await _context.SaveChangesAsync();

            return Ok(registeredUser);
        }

        private bool RegisteredUserExists(int id)
        {
            return _context.RegisteredUser.Any(e => e.ID == id);
        }

        private bool RegisteredUserExists(string email)
        {
            return _context.RegisteredUser.Any(e => e.Email == email);
        }

        public void SendEmail(string emailbody)
        {
            MailMessage mailMessage = new MailMessage("raghav.15bcs2078@abes.ac.in", "raghavgarg8410@gmail.com");
            mailMessage.Body = emailbody;
            mailMessage.Subject = "Exception";

            SmtpClient smtpClient = new SmtpClient("smtp.gmail.com", 587);
            smtpClient.Credentials = new System.Net.NetworkCredential()
            {
                UserName = "raghav.15bcs2078@abes.ac.in",
                Password = "craterzone@1234"
            };
            smtpClient.EnableSsl = true;
            smtpClient.Send(mailMessage);
        }

        public int OTP()
        {
            Random random = new Random();
            return random.Next(100000, 999999);
        }
    }
}
