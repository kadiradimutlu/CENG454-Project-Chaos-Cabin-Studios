public interface IMovementSpeedModifierReceiver
{
    void AddSpeedModifier(string sourceId, float multiplier);
    void RemoveSpeedModifier(string sourceId);
}