using System.Collections.Generic;

public interface ObjectWithDelta
{
	List<byte> GetDeltaBytes(bool getFullState);
	bool IsDirty();
	int GetObjectId();
	void SetObjectId(int newId);
	void ApplyDelta(byte[] delta);
}
