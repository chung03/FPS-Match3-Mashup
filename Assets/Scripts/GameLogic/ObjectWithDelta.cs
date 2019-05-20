public interface ObjectWithDelta
{
	byte[] GetDeltaBytes();
	bool IsDirty();
	int GetObjectId();
	void ApplyDelta(byte[] delta);
}
