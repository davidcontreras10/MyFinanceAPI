using MongoDB.Bson.Serialization;

namespace MongoDB
{
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
	public class BsonCamelCaseAttribute : Attribute, IBsonMemberMapAttribute
	{
		public void Apply(BsonMemberMap memberMap)
		{
			var originalName = memberMap.MemberName;
			var camelName = Char.ToLowerInvariant(originalName[0]) + originalName.Substring(1);
			memberMap.SetElementName(camelName);
		}
	}
}
