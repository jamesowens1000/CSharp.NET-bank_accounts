using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using BankAccounts.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Globalization;

namespace BankAccounts.Controllers
{
    public class HomeController : Controller
    {
        private MyContext dbContext;

        public HomeController(MyContext context)
        {
            dbContext = context;
        }

        [HttpGet("")]
        public IActionResult Index()
        {
            return View("Index");
        }

        [HttpPost("")]
        public IActionResult Register(User newUser)
        {
            if(ModelState.IsValid)
            {
                if(dbContext.Users.Any(u => u.Email == newUser.Email))
                {
                    ModelState.AddModelError("Email", "Email already in use!");
                }
                else
                {
                    PasswordHasher<User> Hasher = new PasswordHasher<User>();
                    newUser.Password = Hasher.HashPassword(newUser, newUser.Password);
                    dbContext.Add(newUser);
                    dbContext.SaveChanges();
                    User currUser = dbContext.Users.FirstOrDefault(u => u.Email == newUser.Email);
                    HttpContext.Session.SetInt32("userId", newUser.UserId);
                    return Redirect("/account/"+currUser.UserId);
                }
            }
            return View("Index",newUser);
        }

        [HttpGet("login")]
        public IActionResult Login()
        {
            return View("Login");
        }

        [HttpPost("login")]
        public IActionResult TryLogin(LoginUser user)
        {
            if(ModelState.IsValid)
            {
                var userInDb = dbContext.Users.FirstOrDefault(u => u.Email == user.Email);

                if (userInDb == null)
                {
                    ModelState.AddModelError("Email", "Invalid Email/Password");
                    return View("Login",user);
                }

                var hasher = new PasswordHasher<LoginUser>();
                var result = hasher.VerifyHashedPassword(user, userInDb.Password, user.Password);

                if (result == 0)
                {
                    ModelState.AddModelError("Email", "Invalid Email/Password");
                }
                else
                {
                    HttpContext.Session.SetInt32("userId", userInDb.UserId);
                    return Redirect("/account/"+userInDb.UserId);
                }
            }
            return View("Login",user);
        }

        [HttpGet("account/{user_id}")]
        public IActionResult Account(int user_id)
        {
            if (HttpContext.Session.GetInt32("userId") != user_id)
            {
                return RedirectToAction("Login");
            }

            User currUser = dbContext.Users.Include(t => t.TransactionsMade).FirstOrDefault(u => u.UserId == user_id);
            List<Transaction> EveryTrans = currUser.TransactionsMade.OrderByDescending(t => t.CreatedAt).ToList();
            if (currUser.TransactionsMade != null)
            {
                double currentBal = currUser.TransactionsMade.Sum(a => a.Amount);
                ViewBag.CurrentBalance = currentBal.ToString("C", CultureInfo.CurrentCulture);
            }
            else
            {
                ViewBag.CurrentBalance = "$0.00";
            }
            
            Console.WriteLine("Balance: "+ViewBag.CurrentBalance);
            
            ViewBag.ThisUser = currUser;
            ViewBag.AllTransactions = EveryTrans;
            return View("Account");
        }

        [HttpPost("makeTransaction")]
        public IActionResult MakeTransaction(Transaction newTrans)
        {
            string balance = Request.Form["CurrBalance"];
            balance = balance.Substring(1);
            double dblBalance = Convert.ToDouble(balance);
            if (dblBalance < newTrans.Amount * -1)
            {
                Console.WriteLine("ERROR ON AMOUNT: Cannot withdraw more than your current balance!");
                ModelState.AddModelError("Amount", "Cannot withdraw more than your current balance!");
            }
            else
            {
                User currUser = dbContext.Users.FirstOrDefault(u => u.UserId == newTrans.UserId);
                dbContext.Add(newTrans);
                dbContext.SaveChanges();
            }
            return Redirect("/account/"+newTrans.UserId);
        }

        [HttpGet("logout")]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}