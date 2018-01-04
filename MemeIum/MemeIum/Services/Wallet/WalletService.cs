using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using MemeIum.Misc;
using MemeIum.Requests;
using Newtonsoft.Json;

namespace MemeIum.Services.Wallet
{
    class WalletService : IWalletService
    {
        public readonly ILogger Logger;
        public string KeysFolderPath;
        public string PubKeysPath;
        public string PrivKeysPath;

        private readonly RSACryptoServiceProvider _provider;
        public string PubKey {
            get
            {
                var pub = _provider.ExportParameters(false);
                var exp = Convert.ToBase64String(pub.Exponent);
                var mod = Convert.ToBase64String(pub.Modulus);
                return exp+" "+mod;
            }
        }

        public string Address
        {
            get
            {
                var hash = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(PubKey));
                return Convert.ToBase64String(hash);
            }

        }

        public WalletService()
        {
            Logger = Services.GetService<ILogger>();
            //lets take a new CSP with a new 2048 bit rsa key pair
            _provider = new RSACryptoServiceProvider(2048);
            KeysFolderPath = $"{Configurations.CurrentPath}\\Keys";
            PubKeysPath = $"{KeysFolderPath}\\pub.key";
            PrivKeysPath = $"{KeysFolderPath}\\priv.key";

            if (!TryLoadSavedKeys())
            {
                Logger.Log("Failed to find wallet keys, generating new ones");
                GenerateNewKeys();
            }
        }

        public void GenerateNewKeys()
        {
            SaveKeys();
        }

        public string StringFromRsaParam(RSAParameters parameters)
        {
            var sw = new System.IO.StringWriter();
            var xs = new System.Xml.Serialization.XmlSerializer(typeof(RSAParameters));
            xs.Serialize(sw, parameters);
            return  sw.ToString();
        }

        public RSAParameters RsaParametersFromString(string str)
        {
            var sr = new System.IO.StringReader(str);
            var xs = new System.Xml.Serialization.XmlSerializer(typeof(RSAParameters));
            return (RSAParameters)xs.Deserialize(sr);
        }

        public void SaveKeys()
        {
            var pubKeyString = StringFromRsaParam(_provider.ExportParameters(false));
            var privKeyString = StringFromRsaParam(_provider.ExportParameters(true));

            File.WriteAllText($"{Configurations.CurrentPath}\\Keys\\pub.key",pubKeyString);
            File.WriteAllText($"{Configurations.CurrentPath}\\Keys\\priv.key", privKeyString);
        }

        public void LoadKeys()
        {
            _provider.ImportParameters(RsaParametersFromString(File.ReadAllText(PubKeysPath)));
            _provider.ImportParameters(RsaParametersFromString(File.ReadAllText(PrivKeysPath)));
        }

        public bool TryLoadSavedKeys()
        {
            if (Directory.Exists(KeysFolderPath))
            {
                if (File.Exists(PubKeysPath) &&
                    File.Exists(PrivKeysPath))
                {
                    try
                    {
                       LoadKeys();
                       return true;
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(ex.Message,2);
                        return false;
                    }
                }
                else
                {
                    Logger.Log("No Key files found.", 1);
                }
            }
            else
            {
                Directory.CreateDirectory(KeysFolderPath);
            }
            return false;
        }

        public TransactionRequest MakeTransaction(TransactionBody body)
        {
            var bod = JsonConvert.SerializeObject(body);
            var bbytes = Encoding.UTF8.GetBytes(bod);
            var sign = _provider.SignData(bbytes, new SHA256CryptoServiceProvider());
            var signString = Convert.ToBase64String(sign);
            
            var req = new TransactionRequest()
            {
                Body = body,
                Signature = signString
            };

            return req;
        }
    }

}
