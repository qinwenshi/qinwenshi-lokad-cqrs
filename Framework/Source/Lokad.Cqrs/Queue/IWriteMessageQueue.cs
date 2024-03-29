#region (c) 2010 Lokad Open Source - New BSD License 

// Copyright (c) Lokad 2010, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

using System;
using System.Collections.Specialized;

namespace Lokad.Cqrs.Queue
{
	public interface IWriteMessageQueue
	{
		Uri Uri { get; }
		void SendMessages(object[] messages, Action<MessageAttributeBuilder> headers);
		void RouteMessages(UnpackedMessage[] messages, Action<MessageAttributeBuilder> headers);
	}
}