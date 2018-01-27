using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MemeIumServices.DatabaseContexts;
using MemeIumServices.Models;
using MemeIumServices.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Rewrite.Internal.UrlActions;

namespace MemeIumServices.Services
{
    public interface IAuthService
    {
        IActionResult AuthenticatedResponse(HttpRequest request,HttpResponse response,IActionResult correct);
        IActionResult Login(HttpRequest request,HttpResponse response);
        IActionResult Register(IFormCollection form);
        User GetAuthUser(HttpRequest request, HttpResponse response);
        IActionResult UnAuthenticatedResult { get; }
        bool IsRsaStringValid(string rsaString);
        bool AddWallet(string key,string name,User toUser);
    }

    public class AuthService : IAuthService
    {
        private UASContext context;
        private ITransactionUtil transactionUtil;
        private IWalletUtil walletUtil;

        public AuthService(UASContext _context,ITransactionUtil _transactionUtil, IWalletUtil _walletUtil)
        {
            context = _context;
            transactionUtil = _transactionUtil;
            walletUtil = _walletUtil;
        }
        public IActionResult UnAuthenticatedResult {
            get
            {
                var errors = new LoginFormErrorViewModel()
                {
                    Message = "<span class='text-danger'>Please first log in!</span>",
                    Email = "",
                    Errors = new List<string>()
                };
                return new RedirectToActionResult("Login", "Home", routeValues: errors);
            }
        }

        public bool IsRsaStringValid(string rsaString)
        {
            try
            {
                var rsa = walletUtil.RsaFromString(rsaString);
                return !rsa.PublicOnly;
            }
            catch (Exception e)
            {
                return false;
            }
        }


        public bool AddWallet(string key,string name,User toUser)
        {
            var walletCount = context.Wallets.Where(r => r.OwnerId == toUser.UId).ToList().Count;
            if (walletCount <= 41)
            {
                context.Database.EnsureCreated();
                if (key == "")
                {
                    var wallet = walletUtil.GenerateNewWallet(toUser, name);
                    context.Wallets.Add(wallet);
                    context.SaveChanges();
                    return true;
                }
                else if (IsRsaStringValid(key))
                {
                    var wallet = walletUtil.WalletFromKey(key, toUser, name);
                    context.Wallets.Add(wallet);
                    context.SaveChanges();
                    return true;
                }
            }
            return false;
        }

        bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private bool IsAlreadyRegisteredEmail(string email)
        {
            context.Database.EnsureCreated();
            return !context.Users.All(r => r.Email != email);
        }

        public bool TryRegister(IFormCollection form,out RegisterFormErrorViewModel error)
        {
            var success = true;
            var email = form.ContainsKey("email") ? form["email"].ToString():"";
            var password = form.ContainsKey("password") ? form["password"].ToString() : "";
            var confirmPassword = form.ContainsKey("confirmpassword") ? form["confirmpassword"].ToString() : "";
            var privkey = form.ContainsKey("privkey") ? form["privkey"].ToString() : "";

            error = new RegisterFormErrorViewModel()
            {
                Errors = new List<string>() ,
                Email=email,
                PrivKey = privkey
            };

            if (!(email.Length > 5 && email.Length <= 80) || !IsValidEmail(email))
            {
                success = false;
                error.Errors.Add("Please enter a real e-mail address!");
            }
            if (IsAlreadyRegisteredEmail(email))
            {
                success = false;
                error.Errors.Add("This email address is already registered!");
            }
            if (!(password.Length > 6 && password.Length <= 100))
            {
                success = false;
                error.Errors.Add("Passwords must be at least 6 characters and less than 100 characters.");
            }
            if (confirmPassword != password)
            {
                success = false;
                error.Errors.Add("Passwords do not match!");
            }
            if (!IsRsaStringValid(privkey) && privkey != "")
            {
                success = false;
                error.Errors.Add("Private key not valid!");

            }
            return success;
        }

        public string PasswordHash(string from, string salt)
        {
            return Convert.ToBase64String(MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(from + ":" + salt)));
        }

        public User GetAuthUser(HttpRequest request, HttpResponse response)
        {
            if (request.Cookies["token"] != null)
            {
                var tokenStr = request.Cookies["token"];
                var tokenOb = context.UserTokens.First(r => r.Token == tokenStr);
                if (tokenOb != null)
                {
                    if (tokenOb.Expiration < DateTime.UtcNow)
                    {
                        response.Cookies.Delete("token");
                        context.UserTokens.Remove(tokenOb);
                    }
                    else
                    {
                        return context.Users.First(r=>r.UId == tokenOb.OwnerId);
                    }
                }
            }
            return null;

        }


        public IActionResult AuthenticatedResponse(HttpRequest request,HttpResponse response,IActionResult correct)
        {
            var usr = GetAuthUser(request, response);
            if (usr == null)
            {
                return UnAuthenticatedResult;
            }

            return correct;
        }

        public bool PasswordCorrect(string email,string rawPwd)
        {
            var usr = context.Users.First(r => r.Email == email);
            if (usr != null)
            {
                return usr.HashedPassword == PasswordHash(rawPwd, usr.Salt);
            }
            return false;
        }

        public bool TryLogin(IFormCollection form,out LoginFormErrorViewModel error)
        {
            var email = form.ContainsKey("email") ? form["email"].ToString() : "";
            var password = form.ContainsKey("password") ? form["password"].ToString() : "";

            var success = true;
            error = new LoginFormErrorViewModel()
            {
                Errors = new List<string>(),
                Email = email,
                Message=""
            };

            if (!IsAlreadyRegisteredEmail(email))
            {
                success = false;
                error.Errors.Add("Email does not exist!");
            }

            if (!PasswordCorrect(email, password))
            {
                success = false;
                error.Errors.Add("Password is not correct!");
            }
            return success;
        }

        public string GenerateRandomToken()
        {
            using (RandomNumberGenerator rng = new RNGCryptoServiceProvider())
            {
                byte[] tokenData = new byte[128];
                rng.GetBytes(tokenData);

                string token = Convert.ToBase64String(tokenData);
                return token;
            }
        }

        public IActionResult Login(HttpRequest request,HttpResponse response)
        {
            var form = request.Form; 
            var email = form.ContainsKey("email") ? form["email"].ToString() : "";
            var remember = form.ContainsKey("remember") ? form["remember"].ToString() == "remember" : false;
            var success = TryLogin(form,out LoginFormErrorViewModel errors);

            if (success)
            {
                context.Database.EnsureCreated();

                var user = context.Users.First(r => r.Email == email);
                var exp = DateTime.UtcNow.AddHours(1);
                if (remember)
                {
                    exp = DateTime.UtcNow.AddDays(7);
                }
                var token = new UserToken()
                {
                    Expiration = exp,
                    Token = GenerateRandomToken(),
                    User = user,
                    OwnerId = user.UId
                };
                context.UserTokens.Add(token);

                response.Cookies.Append("token",token.Token,new CookieOptions(){Expires = token.Expiration});
                context.SaveChanges();
                return new RedirectResult("/Home/Index");
            }
            return new RedirectToActionResult("Login", "Home", routeValues: errors);

        }

        public IActionResult Register(IFormCollection form)
        {
            var regSuccess = TryRegister(form, out RegisterFormErrorViewModel errors);

            if (regSuccess)
            {
                var email = form.ContainsKey("email") ? form["email"].ToString() : "";
                var password = form.ContainsKey("password") ? form["password"].ToString() : "";
                var privkey = form.ContainsKey("privkey") ? form["privkey"].ToString() : "";
                var salt = Guid.NewGuid().ToString().Replace("-", string.Empty).Substring(0, 8);
                var hashed = PasswordHash(password, salt);
                
                var user = new User()
                {
                    Email = email,
                    Salt = salt,
                    HashedPassword = hashed,
                    HistoricalTransactions = new List<HistoricalTransaction>(),
                    RegisteredTime = DateTime.UtcNow,
                    UId = Guid.NewGuid().ToString(),
                    UserTokens = new List<UserToken>(),
                    Wallets = new List<Wallet>()
                };


                context.Database.EnsureCreated();
                context.Users.Add(user);
                if (privkey != "")
                {
                    var wallet = walletUtil.WalletFromKey(privkey, user,"Main");
                    context.Wallets.Add(wallet);
                }
                else
                {
                    var wallet = walletUtil.GenerateNewWallet(user,"Main");
                    context.Wallets.Add(wallet);
                }

                context.SaveChanges();

                var sucError = new LoginFormErrorViewModel()
                {
                    Email =  email,
                    Errors = new List<string>(),
                    Message = "<span class='text-success'>Registered succesfully ! Now log in.</span>"
                };
                return new RedirectToActionResult("Login", "Home", routeValues:sucError);
            }
            else
            {
                return new RedirectToActionResult("Register","Home",routeValues:errors);
            }
        }

    }
}
