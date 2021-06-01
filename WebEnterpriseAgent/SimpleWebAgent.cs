using System;
using Hyperledger.Aries.Agents;
using WebAgent.Messages;
using WebAgent.Protocols.BasicMessage;
using WebEnterpriseAgent.Protocols.Connection;

namespace WebAgent
{
    public class SimpleWebAgent : AgentBase
    {
        public SimpleWebAgent(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }

        protected override void ConfigureHandlers()
        {
            AddHandler<BaksakConnectionHandler>();
            AddForwardHandler();
            AddHandler<BasicMessageHandler>();
            AddHandler<TrustPingMessageHandler>();
            AddDiscoveryHandler();
            AddTrustPingHandler();
            AddCredentialHandler();
            AddProofHandler();
        }
    }
}