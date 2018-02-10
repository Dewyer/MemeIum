using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MemeIumServices.Models;
using MemeIumServices.Models.Transaction;

namespace MemeIumServices.Services
{
    public interface IServerWalletService
    {
        void SendServerTransaction(Transaction trans);
        List<PrizeOffer> GetOffersForCompetition(string competitionId);
    }

    public class ServerWalletService : IServerWalletService
    {

        public void SendServerTransaction(Transaction trans)
        {
            throw new NotImplementedException();
        }

        public List<PrizeOffer> GetOffersForCompetition(string competitionId)
        {
            return null;
        }
    }
}
