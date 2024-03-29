﻿#region (c) 2010 Lokad Open Source - New BSD License 

// Copyright (c) Lokad 2010, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;

namespace Lokad.Cqrs.Domain
{
	public sealed class MessageAssemblyScanner
	{
		readonly HashSet<Assembly> _assemblies = new HashSet<Assembly>();
		readonly Filter<Type> _consumerSelector = new Filter<Type>();
		readonly Filter<Type> _messageSelector = new Filter<Type>();
		MethodInfo _consumingMethod;
		public bool IncludeSystemMessages { get; set; }

		public MethodInfo ConsumingMethod
		{
			get { return _consumingMethod; }
		}

		public MessageAssemblyScanner WithAssemblyOf<T>()
		{
			_assemblies.Add(typeof (T).Assembly);
			return this;
		}

		public MessageAssemblyScanner WithAssembly(Assembly assembly)
		{
			_assemblies.Add(assembly);
			return this;
		}

		public MessageAssemblyScanner WhereMessages(Func<Type, bool> filter)
		{
			_messageSelector.Where(filter);
			return this;
		}

		public MessageAssemblyScanner WhereConsumers(Func<Type, Boolean> filter)
		{
			_consumerSelector.Where(filter);
			return this;
		}

		public MessageAssemblyScanner ConsumerMethodSample<THandler>(Expression<Action<THandler>> expression)
		{
			_consumingMethod = MessageReflectionUtil.ExpressConsumer(expression);
			return this;
		}

		static bool IsUserAssembly(Assembly a)
		{
			if (string.IsNullOrEmpty(a.FullName))
				return false;
			if (a.FullName.StartsWith("System."))
				return false;
			if (a.FullName.StartsWith("Microsoft."))
				return false;
			return true;
		}


		public MessageAssemblyScanner ConsumerInterfaceHas<TAttribute>()
			where TAttribute : Attribute
		{
			var methods = AppDomain
				.CurrentDomain
				.GetAssemblies()
				.Where(IsUserAssembly)
				.SelectMany(e => e.GetTypes().Where(t => t.IsPublic))
				.Where(t => t.IsInterface)
				.Where(t => t.IsGenericTypeDefinition)
				.SelectMany(t => t.GetMethods())
				.Where(t => t.ContainsGenericParameters)
				.Where(t => t.IsDefined(typeof (TAttribute), false))
				.ToArray();

			if (methods.Length == 0)
				throw new InvalidOperationException("Was not able to find any generic methods marked with the attribute");
			if (methods.Length > 1)
				throw new InvalidOperationException("Only one method has to be marked with the attribute");

			_consumingMethod = methods.First();

			return this;
		}

		public IEnumerable<MessageMapping> GetSystemMessages()
		{
			return Assembly
				.GetExecutingAssembly()
				.GetTypes()
				.Where(t => t.IsPublic)
				.Where(t => t.IsDefined(typeof (DataContractAttribute), false))
				.Select(message => new MessageMapping(typeof (MessageMapping.BusSystem), message, false));
		}


		public IEnumerable<MessageMapping> Build()
		{
			if (null == _consumingMethod)
				throw new InvalidOperationException("Consuming method has not been defined");

			if (!_assemblies.Any())
				throw new InvalidOperationException("There are no assemblies to scan");

			var types = _assemblies
				.SelectMany(a => a.GetExportedTypes())
				.ToList();

			var messageTypes = _messageSelector
				.Apply(types)
				.ToArray();

			var consumerTypes = _consumerSelector
				.Apply(types)
				.Where(t => !t.IsGenericType)
				.ToArray();

			var consumingDirectly = consumerTypes
				.SelectMany(consumerType =>
					GetConsumedMessages(consumerType, _consumingMethod.DeclaringType)
						.Select(messageType => new MessageMapping(consumerType, messageType, true)))
				.ToArray();

			var consumingIndirectly = consumingDirectly
				.SelectMany(mm => messageTypes
					.Where(t => mm.Message.IsAssignableFrom(t))
					.Where(t => mm.Message != t)
					.Select(t => new MessageMapping(mm.Consumer, t, false)))
				.ToArray();


			var result = new HashSet<MessageMapping>();

			result.AddRange(consumingDirectly);
			result.AddRange(consumingIndirectly);

			if (IncludeSystemMessages)
			{
				result.AddRange(GetSystemMessages());
			}

			var allMessages = result.ToSet(m => m.Message);

			foreach (var messageType in messageTypes)
			{
				if (!allMessages.Contains(messageType))
				{
					allMessages.Add(messageType);
					result.Add(new MessageMapping(typeof (MessageMapping.BusNull), messageType, true));
				}
			}


			return result;
		}

		static IEnumerable<Type> GetConsumedMessages(Type consumerType, Type consumerTypeDefinition)
		{
			var interfaces = consumerType
				.GetInterfaces()
				.Where(i => i.IsGenericType)
				.Where(t => t.GetGenericTypeDefinition() == consumerTypeDefinition);

			foreach (var consumerInterface in interfaces)
			{
				var argument = consumerInterface.GetGenericArguments()[0];

				yield return argument;
			}
		}
	}
}