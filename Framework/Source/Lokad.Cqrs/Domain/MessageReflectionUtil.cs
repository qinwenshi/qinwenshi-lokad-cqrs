#region (c) 2010 Lokad Open Source - New BSD License 

// Copyright (c) Lokad 2010, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Lokad.Reflection;

namespace Lokad.Cqrs.Domain
{
	static class MessageReflectionUtil
	{
		[DebuggerNonUserCode]
		public static void InvokeConsume(object messageHandler, object messageInstance, string methodName)
		{
			Enforce.Arguments(() => messageHandler, () => messageInstance, () => methodName);
			try
			{
				var handlerType = messageHandler.GetType();
				var messageType = messageInstance.GetType();
				var consume = handlerType.GetMethod(methodName, new[] {messageType});

				if (null == consume)
					throw Errors.InvalidOperation("Unable to find consuming method {0}.{1}({2}).", 
						handlerType.Name, 
						methodName, 
						messageType.Name);

				consume.Invoke(messageHandler, new[] {messageInstance});
			}
			catch (TargetInvocationException e)
			{
				throw Throw.InnerExceptionWhilePreservingStackTrace(e);
			}
		}


		public static MethodInfo ExpressConsumer<THandler>(Expression<Action<THandler>> expression)
		{
			if (false == typeof (THandler).IsGenericType)
				throw new InvalidOperationException("Type should be a generic like 'IConsumeMessage<IMessage>'");

			var generic = typeof (THandler).GetGenericTypeDefinition();

			var methodInfo = Express<THandler>.Method(expression);

			var parameters = methodInfo.GetParameters();
			if ((parameters.Length != 1)) //|| (parameters[0].ParameterType != typeof (string))
				throw new InvalidOperationException("Expression should consume object like: 'i => i.Consume(null)'");

			var method = generic
				.GetMethods()
				.Where(mi => mi.Name == methodInfo.Name)
				.Where(mi => mi.GetParameters().Length == 1)
				.Where(mi => mi.ContainsGenericParameters)
				.First();
			return method;
		}
	}
}