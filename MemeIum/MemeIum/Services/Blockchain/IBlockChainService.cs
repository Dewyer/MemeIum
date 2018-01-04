using System;
using System.Collections.Generic;
using System.Text;
using MemeIum.Requests;

namespace MemeIum.Services.Blockchain
{
    interface IBlockChainService
    {
        void ParseInvitationRequest(InvitationRequest request);
    }
}
