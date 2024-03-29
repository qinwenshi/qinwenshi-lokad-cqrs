#region (c) 2010 Lokad Open Source - New BSD License 

// Copyright (c) Lokad 2010, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

using System;
using System.Diagnostics;

namespace Lokad.Cqrs.Domain
{
	[DebuggerDisplay("{MessageType.Name}")]
	public sealed class MessageInfo
	{
		public Type MessageType { get; internal set; }
		public Type[] DirectConsumers { get; internal set; }
		public Type[] DerivedConsumers { get; internal set; }
		public Type[] AllConsumers { get; internal set; }

		public bool IsDomainMessage { get; internal set; }
		public bool IsSystemMessage { get; internal set; }
	}
}