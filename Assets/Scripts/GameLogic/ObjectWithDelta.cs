using System.Collections.Generic;
using GameUtils;

public interface ObjectWithDelta
{
	List<byte> GetDeltaBytes(bool getFullState);
	int GetObjectId();
	void SetObjectId(int newId);
	void ApplyDelta(byte[] delta);
	void SetDeltaToZero();
	bool HasChanged();
	CREATE_ENTITY_TYPES GetEntityType();
}
