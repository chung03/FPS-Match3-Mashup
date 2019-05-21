using System.Collections.Generic;

public interface ObjectWithDelta
{
	List<byte> GetDeltaBytes(bool getFullState);
	bool IsDirty();
	int GetObjectId();
	void ApplyDelta(byte[] delta);
}
