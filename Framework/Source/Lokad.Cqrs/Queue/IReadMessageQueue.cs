#region (c) 2010 Lokad Open Source - New BSD License 

// Copyright (c) Lokad 2010, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

using System;

namespace Lokad.Cqrs.Queue
{
	public interface IReadMessageQueue
	{
		Uri Uri { get; }
		void Init();
		GetMessageResult GetMessage();
		void AckMessage(UnpackedMessage message);
		void DiscardMessage(UnpackedMessage message);
	}
}