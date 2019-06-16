﻿using System.Collections.Generic;

public interface ObjectWithDelta
{
	List<byte> ServerGetDeltaBytes(bool getFullState);
	List<byte> ClientGetRequestBytes();
	bool IsDirty();
	int GetObjectId();
	void SetObjectId(int newId);
	void ClientApplyDelta(byte[] delta);
	void ServerHandleClientRequests(byte[] delta);
}
