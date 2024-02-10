#region

using System;
using System.Linq;
using NebulaModel.DataStructures.Chat;
using NebulaModel.Packets.Chat;
using NebulaWorld.MonoBehaviours.Local.Chat;

using NebulaModel.Utils;
using AsyncAwaitBestPractices;
using NebulaEasyRShim;

#endregion

namespace NebulaWorld.Chat.Commands;

public class WhisperCommandHandler : IChatCommandHandler
{
    public void Execute(ChatWindow window, string[] parameters)
    {
        if (parameters.Length < 2)
        {
            throw new ChatCommandUsageException("Not enough arguments!".Translate());
        }

        var senderUsername = Multiplayer.Session?.LocalPlayer?.Data?.Username ?? "UNKNOWN";
        if (senderUsername == "UNKNOWN" || Multiplayer.Session == null || Multiplayer.Session.LocalPlayer == null)
        {
            window.SendLocalChatMessage("Not connected, can't send message".Translate(), ChatMessageType.CommandErrorMessage);
            return;
        }

        var recipientUserName = parameters[0];
        var fullMessageBody = string.Join(" ", parameters.Skip(1));
        // first echo what the player typed so they know something actually happened
        ChatManager.Instance.SendChatMessage($"[{DateTime.Now:HH:mm}] [To: {recipientUserName}] : {fullMessageBody}",
            ChatMessageType.PlayerMessage);

        //var packet = new ChatCommandWhisperPacket(senderUsername, recipientUserName, fullMessageBody);
        if (Multiplayer.Session.LocalPlayer.IsHost)
        {
            var recipient = Multiplayer.Session.Network.PlayerManager.GetConnectedPlayerByUsername(recipientUserName);
            if (recipient == null)
            {
                window.SendLocalChatMessage("Player not found: ".Translate() + recipientUserName,
                    ChatMessageType.CommandErrorMessage);
                // TODO: Remove, only for testing.
                NebulaModel.Logger.Log.Info("[Whisper] Player not found.");
                Cloud.Server.Chat.Whisper(senderUsername, recipientUserName, fullMessageBody).SafeFireAndForget();
                NebulaModel.Logger.Log.Info("[Whisper] (Sent) Player not found.");
                return;
            }

            Cloud.Server.Chat.Whisper(senderUsername, recipientUserName, fullMessageBody).SafeFireAndForget();
        }
        else
        {
            Cloud.Server.Chat.Whisper(senderUsername, recipientUserName, fullMessageBody).SafeFireAndForget();
        }
    }

    public string GetDescription()
    {
        return string.Format("Send direct message to player. Use /who for valid user names".Translate());
    }

    public string[] GetUsage()
    {
        return new[] { "<player> <message>" };
    }

    public static void SendWhisperToLocalPlayer(string sender, string mesageBody)
    {
        ChatManager.Instance.SendChatMessage($"[{DateTime.Now:HH:mm}] [{sender} whispered] : {mesageBody}",
            ChatMessageType.PlayerMessagePrivate);
    }
}
