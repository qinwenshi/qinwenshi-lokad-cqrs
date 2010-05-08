using Bus2.Queue;
using Lokad;
using Lokad.Quality;

namespace Bus2.PubSub
{
	[UsedImplicitly]
	public sealed class PublishSubscribeProcess : IBusProcess
	{
		readonly IMessageTransport _transport;
		readonly IPublishSubscribeManager _store;
		readonly IRouteMessages _router;
		readonly ILog _log;

		public PublishSubscribeProcess(IMessageTransport transport, IPublishSubscribeManager manager, IRouteMessages router,
			ILogProvider provider)
		{
			_log = provider.CreateLog<PublishSubscribeProcess>();
			_transport = transport;
			_store = manager;
			_router = router;
		}

		public void Dispose()
		{
			_log.DebugFormat("Stopping pub/sub for {0}", _transport.ToString());
			_transport.Dispose();
			_transport.MessageRecieved -= TransportOnMessageRecieved;
		}

		public void Start()
		{
			_log.DebugFormat("Starting pub/sub for {0}", _transport.ToString());
			_transport.MessageRecieved += TransportOnMessageRecieved;
			_transport.Start();
		}

		bool Manage(object message)
		{
			var direct = message as SubscribeDirectMessage;

			if (direct != null)
			{
				_log.DebugFormat("Subscribing '{0}' to '{1}'", direct.Queue, direct.Topic);
				_store.SubscribeDirect(
					direct.SubscriptionId,
					direct.Topic,
					direct.Queue);
				return true;
			}

			var regex = message as SubscribeRegexMessage;

			if (regex != null)
			{
				_log.DebugFormat("Subscribing '{0}' to '{1}'", regex.Queue, regex.Regex);
				_store.SubscribeRegex(
					regex.SubscriptionId,
					regex.Regex,
					regex.Queue);
				return true;
			}

			//_log.ErrorFormat("Unknown management message {0}", message);
			return false;
		}

		bool TransportOnMessageRecieved(IncomingMessage incomingMessage)
		{
			if (Manage(incomingMessage.Message))
				return true;

			var topic = incomingMessage.Topic;
			if (string.IsNullOrEmpty(topic))
			{
				_log.DebugFormat("Discarding message {0} without topic", incomingMessage.TransportMessageId);
				return false;
			}

			var subscribers = _store.GetSubscribers(topic);
			if (subscribers.Length > 0)
			{
				_router.RouteMessages(new[] {incomingMessage}, subscribers);
				return true;
			}
			return false;
		}
	}
}