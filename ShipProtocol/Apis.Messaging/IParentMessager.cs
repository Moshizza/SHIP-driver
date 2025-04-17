namespace Apis.Messaging;

public interface IParentMessager<T>
{
	T PassMessage(T message);
}
