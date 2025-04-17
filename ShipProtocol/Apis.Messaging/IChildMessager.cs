namespace Apis.Messaging;

public interface IChildMessager<T>
{
	void RegisterParentClass(IParentMessager<T> parent);
}
