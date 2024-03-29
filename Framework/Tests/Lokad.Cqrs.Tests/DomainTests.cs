﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lokad.Cqrs.Domain;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;

namespace CloudBus.Tests
{
	[TestFixture]
	public sealed class DomainTests
	{
		// ReSharper disable InconsistentNaming

		#region Classes

		public interface IMessage { }

		public interface IDomainCommand : IMessage{}
		public interface IDomainEvent : IMessage{}
		public interface IConsumeMessage
		{
			
		}
		public interface Handle<TMessage> : IConsumeMessage<TMessage>
			where TMessage : IDomainCommand
		{
			
		}
		public interface ConsumerOf<TMessage> : IConsumeMessage<TMessage>
			where TMessage : IDomainEvent
		{
		}

		

		public interface IConsumeMessage<TMessage> : IConsumeMessage
			where TMessage : IMessage
		{
			void Consume(TMessage message);
		}

		public sealed class ListenToAll : IConsumeMessage<IMessage>
		{
			public void Consume(IMessage message)
			{
				throw new NotImplementedException();
			}
		}

		public interface ISomethingHappenedEvent : IDomainEvent
		{
			
		}

		public sealed class SomethingHappenedEvent : ISomethingHappenedEvent
		{
			
		}


		public sealed class WhenSomethingHappened : ConsumerOf<ISomethingHappenedEvent>
		{
			public void Consume(ISomethingHappenedEvent message)
			{
				throw new NotImplementedException();
			}
		}

		public sealed class DoSomethingCommand : IDomainCommand{}

		public sealed class DoSomethingHandler : Handle<DoSomethingCommand>{
			public void Consume(DoSomethingCommand message)
			{
				throw new NotImplementedException();
			}
		}

		public sealed class OrphanCommand : IDomainCommand{}

		#endregion

		IMessageDirectoryBuilder Builder { get; set;}
		MessageMapping[] Mappings { get; set; }
		IMessageDirectory Directory { get; set; }

		[TestFixtureSetUp]
		public void FixtureSetUp()
		{
			var scanner = new MessageAssemblyScanner
				{
					IncludeSystemMessages = true
				};

			Mappings = scanner
				.ConsumerMethodSample<IConsumeMessage<IMessage>>(m => m.Consume(null))
				.WhereConsumers(t => typeof(IConsumeMessage).IsAssignableFrom(t))
				.WhereMessages(t => typeof(IMessage).IsAssignableFrom(t))
				.WithAssemblyOf<DomainTests>()
				.Build()
				.ToArray();


			Builder = new MessageDirectoryBuilder(Mappings, scanner.ConsumingMethod.Name)
				{
					
				};
			
			Directory = Builder.BuildDirectory(m => true);
		}

		[Test]
		public void Scan()
		{
			Directory.Consumers.ToArray();
		}

		[Test]
		public void Filtered()
		{
			var directory = Builder.BuildDirectory(mm => typeof (ListenToAll) == mm.Consumer);
			Assert.AreEqual(1, directory.Consumers.Length, "Length");
			var consumer = directory.Consumers[0];
			Assert.AreEqual(typeof(ListenToAll), consumer.ConsumerType, "Type");

			CollectionAssert.Contains(consumer.MessageTypes, typeof(ISomethingHappenedEvent));
			CollectionAssert.Contains(consumer.MessageTypes, typeof(SomethingHappenedEvent));
			CollectionAssert.Contains(consumer.MessageTypes, typeof(DoSomethingCommand));
		}

		[Test]
		public void Orphans_are_kept()
		{
			var directory = Builder.BuildDirectory(mm => typeof(ListenToAll) != mm.Consumer);

			CollectionAssert.Contains(directory.Messages.ToArray(m => m.MessageType), typeof(OrphanCommand));
		}
	}
}
