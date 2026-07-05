// Optional: primitives that cache transform matrices; the core calls this after changing
// Position/Rotation/Size.
public interface ITransformedPrimitive
{
    void UpdateTransformMatrix();
}
