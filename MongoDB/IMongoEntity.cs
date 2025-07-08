namespace MongoDB
{
	public interface IMongoEntity<T>
	{
		T Id { get; set; }
	}
}
