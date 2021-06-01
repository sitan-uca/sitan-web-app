using Hyperledger.Aries;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Configuration;
using Hyperledger.Aries.Decorators.Attachments;
using Hyperledger.Aries.Features.DidExchange;
using Hyperledger.Aries.Storage;
using Hyperledger.Aries.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebEnterpriseAgent.Protocols.Connection
{
    public class BaksakConnectionHandler : IMessageHandler
    {
        private readonly IConnectionService _connectionService;
        private readonly IMessageService _messageService;
        private readonly IWalletRecordService _walletRecordService;
        private readonly IAgentProvider _agentContextProvider;
        private readonly IProvisioningService _provisioningService;

        /// <summary>Initializes a new instance of the <see cref="DefaultConnectionHandler"/> class.</summary>
        /// <param name="connectionService">The connection service.</param>
        /// <param name="messageService">The message service.</param>
        public BaksakConnectionHandler(
            IWalletRecordService walletRecordService,
            IProvisioningService provisioningService,
            IAgentProvider agentContextProvider,
            IConnectionService connectionService,
            IMessageService messageService)
        {
            _connectionService = connectionService;
            _messageService = messageService;
            _walletRecordService = walletRecordService;
            _agentContextProvider = agentContextProvider;
            _provisioningService = provisioningService;
        }

        /// <inheritdoc />
        /// <summary>
        /// Gets the supported message types.
        /// </summary>
        /// <value>
        /// The supported message types.
        /// </value>
        public IEnumerable<MessageType> SupportedMessageTypes => new MessageType[]
        {
            MessageTypes.ConnectionInvitation,
            MessageTypes.ConnectionRequest,
            MessageTypes.ConnectionResponse,
            MessageTypesHttps.ConnectionInvitation,
            MessageTypesHttps.ConnectionRequest,
            MessageTypesHttps.ConnectionResponse
        };

        /// <summary>
        /// Processes the agent message
        /// </summary>
        /// <param name="agentContext"></param>
        /// <param name="messageContext">The agent message agentContext.</param>
        /// <returns></returns>
        /// <exception cref="AriesFrameworkException">Unsupported message type {message.Type}</exception>
        public async Task<AgentMessage> ProcessAsync(IAgentContext agentContext, UnpackedMessageContext messageContext)
        {
            switch (messageContext.GetMessageType())
            {
                case MessageTypesHttps.ConnectionInvitation:
                case MessageTypes.ConnectionInvitation:
                    {
                        var invitation = messageContext.GetMessage<ConnectionInvitationMessage>();
                        await _connectionService.CreateRequestAsync(agentContext, invitation);
                        return null;
                    }

                case MessageTypesHttps.ConnectionRequest:
                case MessageTypes.ConnectionRequest:
                    {
                        var provisioning = await _provisioningService.GetProvisioningAsync(agentContext.Wallet);
                        var request = messageContext.GetMessage<ConnectionRequestMessage>();
                        var connectionId = await _connectionService.ProcessRequestAsync(agentContext, request, messageContext.Connection);
                        messageContext.ContextRecord = messageContext.Connection;

                        // Auto accept connection if set during invitation
                        if (messageContext.Connection.GetTag(TagConstants.AutoAcceptConnection) == "true")
                        {
                            var (message, record) = await _connectionService.CreateResponseAsync(agentContext, connectionId);
                            messageContext.ContextRecord = record;

                            message.AddAttachment(
                                new Attachment
                                {
                                    Id = Guid.NewGuid().ToString(),
                                    MimeType = "text/plain",
                                    Nickname = "agent-profile-pic",
                                    Data = new AttachmentContent
                                    {
                                        Base64 = provisioning.Owner.ImageUrl
                                    }
                                });
                            return message;
                        }
                        return null;
                    }

                case MessageTypesHttps.ConnectionResponse:
                case MessageTypes.ConnectionResponse:
                    {
                        var response = messageContext.GetMessage<ConnectionResponseMessage>();
                        await _connectionService.ProcessResponseAsync(agentContext, response, messageContext.Connection);
                        messageContext.ContextRecord = messageContext.Connection;

                        var attachment = response.GetAttachment("agent-profile-pic");
                        messageContext.Connection.Alias.ImageUrl = attachment.Data.Base64;
                        var context = await _agentContextProvider.GetContextAsync();
                        await _walletRecordService.UpdateAsync(context.Wallet, messageContext.Connection);

                        return null;
                    }
                default:
                    throw new AriesFrameworkException(ErrorCode.InvalidMessage,
                        $"Unsupported message type {messageContext.GetMessageType()}");
            }
        }
    }
}
