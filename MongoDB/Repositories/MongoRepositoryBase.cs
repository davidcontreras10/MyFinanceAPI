using MongoDB.Driver;

namespace MongoDB.Repositories
{
	public abstract class MongoRepositoryBase<TDocument, Tid> where TDocument : class, IMongoEntity<Tid>
	{
		protected readonly IMongoCollection<TDocument> Collection;

		protected MongoRepositoryBase(IMongoDatabase database, string collectionName)
		{
			Collection = database.GetCollection<TDocument>(collectionName);
		}

		public virtual async Task InsertAsync(TDocument document)
		{
			await Collection.InsertOneAsync(document);
		}

		public virtual async Task<TDocument?> FindByIdAsync(Tid id)
		{
			var filter = Builders<TDocument>.Filter.Eq(d => d.Id, id);
			return await Collection.Find(filter).FirstOrDefaultAsync();
		}

		public virtual async Task ReplaceAsync(Tid id, TDocument replacement)
		{
			var filter = Builders<TDocument>.Filter.Eq(d => d.Id, id);
			await Collection.ReplaceOneAsync(filter, replacement, new ReplaceOptions { IsUpsert = true });
		}

		public virtual async Task DeleteByIdAsync(Tid id)
		{
			var filter = Builders<TDocument>.Filter.Eq(d => d.Id, id);
			await Collection.DeleteOneAsync(filter);
		}
	}
}
