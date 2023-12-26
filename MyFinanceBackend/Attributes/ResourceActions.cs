using System;
using System.Collections.Generic;
using System.Linq;
using MyFinanceModel;

namespace MyFinanceBackend.Attributes
{
	public class ResourceActionRequiredAttribute : Attribute
	{
		public IList<ResourceActionNames> Actions { get; }

		public ApplicationResources Resource { get;}

		public ResourceActionRequiredAttribute(ApplicationResources resource, params ResourceActionNames[] actions)
		{ 
			Actions = actions.ToList().Where(a => a != ResourceActionNames.Unknown).ToList();
			Resource = resource;
		}  
	}
}