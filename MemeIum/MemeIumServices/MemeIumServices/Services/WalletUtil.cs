using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using MemeIumServices.Models;
using ZXing;
using ZXing.QrCode;
using ZXing.QrCode.Internal;
using System.Drawing;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using SixLabors.ImageSharp;

namespace MemeIumServices.Services
{
    public interface IWalletUtil
    {
        Wallet GenerateNewWallet(User target,string name);
        Wallet WalletFromKey(string key, User user,string name);
        RSACryptoServiceProvider RsaFromString(string from);
        string SaveQrCode(string text);

    }

    public class WalletUtil : IWalletUtil
    {
        private IHostingEnvironment env;

        public WalletUtil(IHostingEnvironment env)
        {
            this.env = env;
        }

        public string SaveQrCode(string text)
        {
            var qrCode = new QRCodeWriter();
            var bits = qrCode.encode(text, BarcodeFormat.QR_CODE, 300, 300);
            var image = new Image<Rgba32>(bits.Width,bits.Height);
            for (int ii = 0; ii < bits.Width; ii++)
            {
                for (int kk = 0; kk < bits.Height; kk++)
                {
                    if (bits[ii, kk])
                    {
                        image[ii, kk] = new Rgba32(0, 0, 0);
                    }
                    else
                    {
                        image[ii, kk] = new Rgba32(255, 255, 255);
                    }
                }
            }
            var name = DateTime.UtcNow.Ticks.ToString();
            var nameFP = env.WebRootPath + "/images/" + name + ".png";
            image.Save(nameFP);
            return name;
        }

        public RSAParameters RsaParametersFromString(string str)
        {
            var sr = new System.IO.StringReader(str);
            var xs = new System.Xml.Serialization.XmlSerializer(typeof(RSAParameters));
            return (RSAParameters)xs.Deserialize(sr);
        }

        public RSACryptoServiceProvider RsaFromString(string from)
        {
            var rsa = new RSACryptoServiceProvider();
            rsa.ImportParameters(RsaParametersFromString(from));

            return rsa;
        }

        public string StringFromRsaParam(RSAParameters parameters)
        {
            var sw = new System.IO.StringWriter();
            var xs = new System.Xml.Serialization.XmlSerializer(typeof(RSAParameters));
            xs.Serialize(sw, parameters);
            return sw.ToString();
        }

        public Wallet GenerateNewWallet(User target,string name)
        {
            var rsa = new RSACryptoServiceProvider(2000);
            var parameters = rsa.ExportParameters(true);
            var exp = Convert.ToBase64String(parameters.Exponent);
            var mod = Convert.ToBase64String(parameters.Modulus);
            var key = StringFromRsaParam(parameters);
            var addr = Convert.ToBase64String(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(exp + " " + mod)));
            var wallet = new Wallet()
            {
                Address = addr,
                KeyString = key,
                User = target,
                Name=name,
                OwnerId = target.UId
            };
            return wallet;
        }

        public Wallet WalletFromKey(string key, User user,string name)
        {
            var rsa = RsaFromString(key);
            var parameters = rsa.ExportParameters(true);
            var exp = Convert.ToBase64String(parameters.Exponent);
            var mod = Convert.ToBase64String(parameters.Modulus);

            var addr = Convert.ToBase64String(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(exp + " " + mod)));
            var wallet = new Wallet()
            {
                Address = addr,
                KeyString = key,
                User = user,
                Name = name,
                OwnerId = user.UId
            };
            return wallet;
        }

    }
}
