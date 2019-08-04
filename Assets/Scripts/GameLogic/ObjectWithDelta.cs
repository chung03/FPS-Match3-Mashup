using System.Collections.Generic;

public interface ObjectWithDelta
{
	List<byte> GetDeltaBytes(bool getFullState);
	int GetObjectId();
	void SetObjectId(int newId);
	void ApplyDelta(byte[] delta, bool isServer);
	void SetDeltaToZero();
}
