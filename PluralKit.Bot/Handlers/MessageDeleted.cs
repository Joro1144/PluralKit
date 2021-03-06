using System;
using System.Linq;
using System.Threading.Tasks;

using DSharpPlus;
using DSharpPlus.EventArgs;

using PluralKit.Core;

using Serilog;

namespace PluralKit.Bot
{
    // Double duty :)
    public class MessageDeleted: IEventHandler<MessageDeleteEventArgs>, IEventHandler<MessageBulkDeleteEventArgs>
    {
        private static readonly TimeSpan MessageDeleteDelay = TimeSpan.FromSeconds(15);
        
        private readonly IDatabase _db;
        private readonly ModelRepository _repo;
        private readonly ILogger _logger;

        public MessageDeleted(ILogger logger, IDatabase db, ModelRepository repo)
        {
            _db = db;
            _repo = repo;
            _logger = logger.ForContext<MessageDeleted>();
        }
        
        public Task Handle(DiscordClient shard, MessageDeleteEventArgs evt)
        {
            // Delete deleted webhook messages from the data store
            // Most of the data in the given message is wrong/missing, so always delete just to be sure.

            async Task Inner()
            {
                await Task.Delay(MessageDeleteDelay);
                await _db.Execute(c => _repo.DeleteMessage(c, evt.Message.Id));
            }

            // Fork a task to delete the message after a short delay
            // to allow for lookups to happen for a little while after deletion
            _ = Inner();
            return Task.CompletedTask;
        }

        public Task Handle(DiscordClient shard, MessageBulkDeleteEventArgs evt)
        {
            // Same as above, but bulk
            async Task Inner()
            {
                await Task.Delay(MessageDeleteDelay);
                _logger.Information("Bulk deleting {Count} messages in channel {Channel}", evt.Messages.Count, evt.Channel.Id);
                await _db.Execute(c => _repo.DeleteMessagesBulk(c, evt.Messages.Select(m => m.Id).ToList()));
            }
            
            _ = Inner();
            return Task.CompletedTask;
        }
    }
}