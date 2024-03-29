﻿namespace Lokad.Cqrs
{
	public enum MessageAttributeType : uint 
	{
		Undefined = 0,
		ContractName = 1,
		ContractDefinition = 2,
		BinaryBody = 3,
		CustomString = 4,
		CustomBinary = 5,
		CustomNumber = 6,
		Topic = 7,
		Sender = 8,
		Identity = 9,
		CreatedUtc =10,
		StorageReference = 11,
		StorageContainer = 12,
		ErrorText = 13
	}
}