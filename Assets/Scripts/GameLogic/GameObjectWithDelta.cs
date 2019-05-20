public interface GameObjectWithDelta
{
	byte[] GetDeltaBytes();
	void CalculateDelta();
	bool IsDirty();
	int GetObjectId();
}
