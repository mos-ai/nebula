#region

using NebulaAPI.Packets;
using NebulaModel.Logger;
using NebulaModel.Networking;
using NebulaModel.Packets;
using NebulaModel.Packets.Chat;
using NebulaWorld;
using NebulaWorld.Chat.Commands;

#endregion

namespace NebulaNetwork.PacketProcessors.Chat;

[RegisterPacketProcessor]
internal class ChatCommandWhisperProcessor : PacketProcessor<ChatCommandWhisperPacket>
{
    protected override void ProcessPacket(ChatCommandWhisperPacket packet, NebulaConnection conn)
    {
        if (IsClient)
        {
            WhisperCommandHandler.SendWhisperToLocalPlayer(packet.SenderUsername, packet.Message);
        }
        else
        {
            // Don't need server

            // second case, relay message to recipient
            // var recipient = Players.Get(packet.RecipientUsername);
            // if (recipient == null)
            // {
            //     Log.Warn($"Recipient not found {packet.RecipientUsername}");
            //     var sender = Players.Get(conn);
            //     sender.SendPacket(new ChatCommandWhisperPacket("SYSTEM".Translate(), packet.SenderUsername,
            //         string.Format("User not found {0}".Translate(), packet.RecipientUsername)));
            //     return;
            // }

            //// second case, relay message to recipient
            //var recipient = Multiplayer.Session.Network
            //    .PlayerManager.GetConnectedPlayerByUsername(packet.RecipientUsername);
            //if (recipient == null)
            //{
            //    Log.Warn($"Recipient not found {packet.RecipientUsername}");
            //    var sender = Multiplayer.Session.Network.PlayerManager.GetPlayer(conn);
            //    sender.SendPacket(new ChatCommandWhisperPacket("SYSTEM".Translate(), packet.SenderUsername,
            //        string.Format("User not found {0}".Translate(), packet.RecipientUsername)));
            //    return;
            //}

            //recipient.SendPacket(packet);
        }
    }
}
