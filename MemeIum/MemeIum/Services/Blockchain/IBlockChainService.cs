using System;
using System.Collections.Generic;
using System.Text;
using MemeIum.Misc;
using MemeIum.Requests;

namespace MemeIum.Services.Blockchain
{
    interface IBlockChainService
    {
        LocalChainInfo Info { get; set; }
        void ParseInvitationRequest(InvitationRequest request,Peer from);
        void ParseInvitationResponseRequest(InvitationResponseRequest request, Peer from);
        void SaveBlock(Block block);
        Block LookUpBlock(string Id);
        BlockInfo LookUpBlockInfo(string Id);
        BlockInfo LookUpBlockInfoByHeight(int height);
        bool IsBlockInLongestChain(string blockid);
        void ParseDataRequest(object data);
        void TryLoadSavedInfo();

    }
}
