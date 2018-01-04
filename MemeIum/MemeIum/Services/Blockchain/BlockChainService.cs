using System;
using System.Collections.Generic;
using System.Text;
using MemeIum.Requests;

namespace MemeIum.Services.Blockchain
{
    class BlockChainService : IBlockChainService
    {
        private string _blockChainPath;

        public void ParseInvitationRequest(InvitationRequest request)
        {
            _blockChainPath = Configurations.CurrentPath + "\\BlockChain\\";
        }
    }
}
